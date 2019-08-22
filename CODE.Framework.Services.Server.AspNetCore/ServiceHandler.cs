using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using CODE.Framework.Services.Server.AspNetCore.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Westwind.Utilities;

namespace CODE.Framework.Services.Server.AspNetCore
{
    /// <summary>
    /// Handles executing methods on a service instance using 
    /// ASP.NET Request data
    /// </summary>
    public class ServiceHandler
    {
        /// <summary>
        /// Http Request instance to get request inputs
        /// </summary>
        private HttpRequest HttpRequest { get; }

        /// <summary>
        /// Http Response output object to send response data into
        /// </summary>
        private HttpResponse HttpResponse { get; }

        /// <summary>
        /// HttpContext instance passed from ASP.NET request context
        /// </summary>
        private HttpContext HttpContext { get; }

        /// <summary>
        /// Request route data for the current request
        /// </summary>
        private RouteData RouteData { get; }

        /// <summary>
        /// Internally info about the method that is to be executed. This info
        /// is created when the handler is first initialized.
        /// </summary>
        private MethodInvocationContext MethodContext { get; }

        /// <summary>
        /// The service specific configuration information
        /// </summary>
        private ServiceHandlerConfigurationInstance ServiceInstanceConfiguration { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="routeData"></param>
        /// <param name="methodContext"></param>
        public ServiceHandler(HttpContext httpContext, RouteData routeData, MethodInvocationContext methodContext)
        {
            HttpContext = httpContext;
            HttpRequest = httpContext.Request;
            HttpResponse = httpContext.Response;

            RouteData = routeData;
            MethodContext = methodContext;

            ServiceInstanceConfiguration = MethodContext.InstanceConfiguration;
        }

        /// <summary>
        /// Main entry point method that handles processing the active request
        /// </summary>
        /// <returns></returns>
        public async Task ProcessRequest()
        {
            var context = new ServiceHandlerRequestContext
            {
                HttpRequest = HttpRequest,
                HttpResponse = HttpResponse,
                HttpContext = HttpContext,
                ServiceInstanceConfiguration = ServiceInstanceConfiguration,
                MethodContext = MethodContext,
                Url = new ServiceHandlerRequestContextUrl
                {
                    Url = HttpRequest.GetDisplayUrl(),
                    UrlPath = HttpRequest.Path.Value,
                    QueryString = HttpRequest.QueryString,
                    HttpMethod = HttpRequest.Method.ToUpper()
                }
            };

            try
            {
                if (context.ServiceInstanceConfiguration.HttpsMode == ControllerHttpsMode.RequireHttps &&
                    HttpRequest.Scheme != "https")
                    throw new UnauthorizedAccessException(Resources.ServiceMustBeAccessedOverHttps);

                if (ServiceInstanceConfiguration.OnAuthorize != null)
                    if (!await ServiceInstanceConfiguration.OnAuthorize(context))
                        throw new UnauthorizedAccessException("Not authorized to access this request");

                if (ServiceInstanceConfiguration.OnBeforeMethodInvoke != null)
                    await ServiceInstanceConfiguration.OnBeforeMethodInvoke(context);

                await ExecuteMethod(context);

                ServiceInstanceConfiguration.OnAfterMethodInvoke?.Invoke(context);

                if (string.IsNullOrEmpty(context.ResultJson))
                    context.ResultJson = JsonSerializationUtils.Serialize(context.ResultValue);

                SendJsonResponse(context, context.ResultValue);
            }
            catch (Exception ex)
            {
                var error = new ErrorResponse(ex);
                SendJsonResponse(context, error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handlerContext"></param>
        /// <returns></returns>
        private async Task ExecuteMethod(ServiceHandlerRequestContext handlerContext)
        {
            var serviceConfig = ServiceHandlerConfiguration.Current;
            var methodToInvoke = handlerContext.MethodContext.MethodInfo;
            var serviceType = handlerContext.ServiceInstanceConfiguration.ServiceType;

            var httpVerb = handlerContext.HttpRequest.Method;
            if (httpVerb == "OPTIONS" && serviceConfig.Cors.UseCorsPolicy)
            {
                // Empty response - ASP.NET will provide CORS headers via applied policy
                handlerContext.HttpResponse.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            // Let DI create the Service instance
            var inst = HttpContext.RequestServices.GetService(serviceType);
            if (inst == null)
                throw new InvalidOperationException(string.Format(Resources.UnableToCreateTypeInstance, serviceType));

            var principal = HttpContext.User;
            UserPrincipalHelper.AddPrincipal(inst, principal);

            if (MethodContext.AuthorizationRoles != null && MethodContext.AuthorizationRoles.Count > 0)
                ValidateRoles(MethodContext.AuthorizationRoles, principal);

            try
            {
                var parameterList = GetMethodParameters(handlerContext);

                if (!handlerContext.MethodContext.IsAsync)
                    handlerContext.ResultValue = methodToInvoke.Invoke(inst, parameterList);
                else
                    handlerContext.ResultValue = await (dynamic) methodToInvoke.Invoke(inst, parameterList);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(Resources.UnableToExecuteMethod, methodToInvoke.Name, ex.Message));
            }
            finally
            {
                UserPrincipalHelper.RemovePrincipal(inst);
            }
        }

        /// <summary>
        /// Retrieve parameters from body and URL parameters
        /// </summary>
        /// <param name="handlerContext"></param>
        /// <returns></returns>
        private object[] GetMethodParameters(ServiceHandlerRequestContext handlerContext)
        {
            // parameter parsing
            var parameterList = new object[] { };

            // simplistic - no parameters or single body post parameter
            var paramInfos = handlerContext.MethodContext.MethodInfo.GetParameters();
            if (paramInfos.Length > 1)
                throw new ArgumentNullException(string.Format(Resources.OnlySingleParametersAreAllowedOnServiceMethods, MethodContext.MethodInfo.Name));

            // if there is a parameter create and de-serialize, then add url parameters
            if (paramInfos.Length == 1)
            {
                var parameter = paramInfos[0];

                // First Deserialize from body if any
                var serializer = new JsonSerializer();

                // there's always 1 parameter
                object parameterData = null;
                if (HttpRequest.ContentLength == null || HttpRequest.ContentLength < 1)
                    // if no content create an empty one
                    parameterData = ReflectionUtils.CreateInstanceFromType(parameter.ParameterType);
                else
                    using (var sw = new StreamReader(HttpRequest.Body))
                    using (JsonReader reader = new JsonTextReader(sw))
                        parameterData = serializer.Deserialize(reader, parameter.ParameterType);

                // We map all parameters passed as named parameters in the URL to their respective properties
                foreach (var key in handlerContext.HttpRequest.Query.Keys)
                {
                    var prop = parameter.ParameterType.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.IgnoreCase);
                    if (prop != null)
                        try
                        {
                            var urlParameterValue = handlerContext.HttpRequest.Query[key].ToString();
                            var val = ReflectionUtils.StringToTypedValue(urlParameterValue, prop.PropertyType);
                            ReflectionUtils.SetProperty(parameterData, key, val);
                        }
                        catch
                        {
                            throw new InvalidOperationException($"Unable set parameter from URL segment for property: {key}");
                        }
                }

                // Map inline URL parameters defined in the route to properties.
                // Note: Since this is done after the named parameters above, parameters that are part of the route definition win out over simple named parameters
                if (RouteData != null)
                    foreach (var kv in RouteData.Values)
                    {
                        var prop = parameter.ParameterType.GetProperty(kv.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.IgnoreCase);
                        if (prop != null)
                            try
                            {
                                var val = ReflectionUtils.StringToTypedValue(kv.Value as string, prop.PropertyType);
                                ReflectionUtils.SetProperty(parameterData, kv.Key, val);
                            }
                            catch
                            {
                                throw new InvalidOperationException($"Unable set parameter from URL segment for property: {kv.Key}");
                            }
                    }

                parameterList = new[] {parameterData};
            }

            return parameterList;
        }

        private void ValidateRoles(List<string> authorizationRoles, IPrincipal user)
        {
            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                if (user.Identity is ClaimsIdentity identity) {
                    var rolesClaim = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                    if (rolesClaim == null)
                        return; // no role requirement

                    if (string.IsNullOrEmpty(rolesClaim.Value))
                        return; // no role requirement or empty and we're authenticated

                    var roles = rolesClaim.Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var role in roles)
                        if (authorizationRoles.Any(r => r == role))
                            return; // matched a role
                }
            }

            throw new UnauthorizedAccessException("Access denied: User is not part of required Role.");
        }

        private static readonly DefaultContractResolver CamelCaseNamingStrategy = new DefaultContractResolver {NamingStrategy = new CamelCaseNamingStrategy()};

        private static readonly DefaultContractResolver SnakeCaseNamingStrategy = new DefaultContractResolver {NamingStrategy = new SnakeCaseNamingStrategy()};

        private static void SendJsonResponse(ServiceHandlerRequestContext context, object value)
        {
            var response = context.HttpResponse;

            response.ContentType = "application/json; charset=utf-8";

            var serializer = new JsonSerializer();

            if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.CamelCase)
                serializer.ContractResolver = CamelCaseNamingStrategy;
            else if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.SnakeCase)
                serializer.ContractResolver = SnakeCaseNamingStrategy;

#if DEBUG
            serializer.Formatting = Formatting.Indented;
#endif

            using (var sw = new StreamWriter(response.Body))
            using (JsonWriter writer = new JsonTextWriter(sw))
                serializer.Serialize(writer, value);
        }
    }
}