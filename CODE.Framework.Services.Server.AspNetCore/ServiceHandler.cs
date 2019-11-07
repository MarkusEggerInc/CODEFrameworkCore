using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using CODE.Framework.Services.Server.AspNetCore.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
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
                if (context.ServiceInstanceConfiguration.HttpsMode == ControllerHttpsMode.RequireHttps && HttpRequest.Scheme != "https")
                    throw new UnauthorizedAccessException(Resources.ServiceMustBeAccessedOverHttps);

                if (ServiceInstanceConfiguration.OnAuthorize != null)
                    if (!await ServiceInstanceConfiguration.OnAuthorize(context))
                        throw new UnauthorizedAccessException("Not authorized to access this request");

                if (ServiceInstanceConfiguration.OnBeforeMethodInvoke != null)
                    await ServiceInstanceConfiguration.OnBeforeMethodInvoke(context);

                await ExecuteMethod(context);

                ServiceInstanceConfiguration.OnAfterMethodInvoke?.Invoke(context);

                if (string.IsNullOrEmpty(context.ResultJson))
                    context.ResultJson = JsonSerializer.Serialize(context.ResultValue);

                await SendJsonResponse(context, context.ResultValue);
            }
            catch (Exception ex)
            {
                var error = new ErrorResponse(ex);
                await SendJsonResponse(context, error);
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
                var parameterList = await GetMethodParameters(handlerContext);

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
        private async Task<object[]> GetMethodParameters(ServiceHandlerRequestContext handlerContext)
        {
            // parameter parsing
            var parameterList = new object[] { };

            // simplistic - no parameters or single body post parameter
            var paramInfos = handlerContext.MethodContext.MethodInfo.GetParameters();
            if (paramInfos.Length > 1)
                throw new ArgumentNullException(string.Format(Resources.OnlySingleParametersAreAllowedOnServiceMethods, MethodContext.MethodInfo.Name));

            // if there is a parameter create and de-serialize, then add url parameters
            if (paramInfos.Length != 1) return parameterList;
            var parameter = paramInfos[0];

            // there's always 1 parameter
            object parameterData;
            if (HttpRequest.ContentLength == null || HttpRequest.ContentLength < 1)
                // if no content create an empty one
                parameterData = ReflectionUtils.CreateInstanceFromType(parameter.ParameterType);
            else
            {
                JsonNamingPolicy policy = null;
                if (handlerContext.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.CamelCase)
                    policy = JsonNamingPolicy.CamelCase;
                parameterData = await JsonSerializer.DeserializeAsync(HttpRequest.Body, parameter.ParameterType, new JsonSerializerOptions {PropertyNamingPolicy = policy, PropertyNameCaseInsensitive = true});
            }

            // We map all parameters passed as named parameters in the URL to their respective properties
            foreach (var key in handlerContext.HttpRequest.Query.Keys)
            {
                var prop = parameter.ParameterType.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.IgnoreCase);
                if (prop == null) continue;
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
                foreach (var (key, value) in RouteData.Values)
                {
                    var property = parameter.ParameterType.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.IgnoreCase);
                    if (property == null) continue;
                    try
                    {
                        var val = ReflectionUtils.StringToTypedValue(value as string, property.PropertyType);
                        ReflectionUtils.SetProperty(parameterData, key, val);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Unable set parameter from URL segment for property: {key}");
                    }
                }

            parameterList = new[] {parameterData};

            return parameterList;
        }

        private void ValidateRoles(IReadOnlyCollection<string> authorizationRoles, IPrincipal user)
        {
            if (user?.Identity == null || !user.Identity.IsAuthenticated) throw new UnauthorizedAccessException("Access denied: User is not part of required Role.");
            if (!(user.Identity is ClaimsIdentity identity)) throw new UnauthorizedAccessException("Access denied: User is not part of required Role.");

            var rolesClaim = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (rolesClaim == null) return; // no role requirement
            if (string.IsNullOrEmpty(rolesClaim.Value)) return; // no role requirement or empty and we're authenticated

            var roles = rolesClaim.Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            if (roles.Any(role => authorizationRoles.Any(r => r == role))) return; // matched a role

            throw new UnauthorizedAccessException("Access denied: User is not part of required Role.");
        }

        public static async Task SendJsonResponse(ServiceHandlerRequestContext context, object value)
        {
            var response = context.HttpResponse;

            response.ContentType = "application/json; charset=utf-8";
            response.Headers.Add("X-Powered-By", "CODE Framework");

            JsonNamingPolicy policy = null;
            if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.CamelCase)
                policy = JsonNamingPolicy.CamelCase;

            var settings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = policy,
#if DEBUG
                WriteIndented = true
#endif
            };

            await JsonSerializer.SerializeAsync(response.Body, value, value.GetType(), settings);
        }
    }
}