using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using CODE.Framework.Services.Server.AspNetCore.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;

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

                if (context.ResultValue is IFileResponse fileResponse)
                {
                    // This is a special case in which we stream the file back low level (side-stepping any kind of JSON serialization)
                    context.HttpResponse.ContentType = fileResponse.ContentType;
                    context.HttpResponse.Headers.Add("Content-Disposition" , $"inline; filename=\"{fileResponse.FileName.Trim()}\"");
                    context.HttpResponse.Headers.Add("x-powered-by", "CODE Framework - codeframework.io");
                    await context.HttpResponse.Body.WriteAsync(fileResponse.FileBytes, 0, fileResponse.FileBytes.Length);
                }
                else
                {
                    if (string.IsNullOrEmpty(context.ResultJson))
                    {
                        var options = new JsonSerializerOptions();

                        if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.CamelCase)
                            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

#if DEBUG
                        options.WriteIndented = true;
#endif

                        var inputType = context.ResultValue.GetType();
                        context.ResultJson = JsonSerializer.Serialize(context.ResultValue, inputType, options);

                        context.HttpResponse.Headers.Add("x-powered-by", "CODE Framework - codeframework.io");
                    }

                    await SendJsonResponseAsync(context, context.ResultValue);
                }
            }
            catch (Exception ex)
            {
                var error = new ErrorResponse(ex);
                await SendJsonResponseAsync(context, error);
            }
        }

        /// <summary>
        /// Runs the actual method that implements the service
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
                // TODO: Adding this header for now after all, since it doesn't seem to work, but we should remove this later.
                //handlerContext.HttpResponse.Headers.Add("Access-Control-Allow-Origin", new StringValues(serviceConfig.Cors.AllowedOrigins));
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
                var parameterList = await GetMethodParametersAsync(handlerContext);

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
        private async Task<object[]> GetMethodParametersAsync(ServiceHandlerRequestContext handlerContext)
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

            // First Deserialize from body if any

            // there's always 1 parameter
            object parameterData;
            if (HttpRequest.ContentLength == null || HttpRequest.ContentLength < 1)
                parameterData = ObjectHelper.CreateInstanceFromType(parameter.ParameterType); // if no content create an empty one
            else
                parameterData = await JsonSerializer.DeserializeAsync(HttpRequest.Body, parameter.ParameterType);

            // We map all parameters passed as named parameters in the URL to their respective properties
            foreach (var key in handlerContext.HttpRequest.Query.Keys)
            {
                var property = parameter.ParameterType.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.IgnoreCase);
                if (property == null) continue;
                try
                {
                    var urlParameterValue = handlerContext.HttpRequest.Query[key].ToString();
                    var parameterValue = UrlParameterToValue(urlParameterValue, property.PropertyType);
                    ObjectHelper.SetPropertyValue(parameterData, key, parameterValue);
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
                        var parameterValue = UrlParameterToValue(value as string, property.PropertyType);
                        ObjectHelper.SetPropertyValue(parameterData, key, parameterValue);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Unable set parameter from URL segment for property: {key}");
                    }
                }

            parameterList = new[] {parameterData};

            return parameterList;
        }

        /// <summary>
        /// Converts a URL parameter value to a typed object
        /// </summary>
        /// <param name="sourceString">The parameter value as a string</param>
        /// <param name="targetType">Intended return type</param>
        /// <param name="culture">String culture (optional)</param>
        /// <returns>Typed value</returns>
        private static object UrlParameterToValue(string sourceString, Type targetType, CultureInfo culture = null)
        {
            var isEmpty = string.IsNullOrEmpty(sourceString);
            if (culture == null) culture = CultureInfo.InvariantCulture;

            if (targetType == typeof(string))
                return sourceString;
            else if (targetType == typeof(int) || targetType == typeof(int))
                return isEmpty ? 0 : int.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            else if (targetType == typeof(long))
                return isEmpty ? (long)0 : long.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            else if (targetType == typeof(short))
                return isEmpty ? (short)0 : short.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            else if (targetType == typeof(decimal))
                return isEmpty ? 0m : decimal.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            else if (targetType == typeof(DateTime))
                return isEmpty ? DateTime.MinValue : Convert.ToDateTime(sourceString, culture.DateTimeFormat);
            else if (targetType == typeof(byte))
                return isEmpty ? 0 : Convert.ToByte(sourceString);
            else if (targetType == typeof(double))
                return isEmpty ? 0d : double.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            else if (targetType == typeof(float))
                return isEmpty ? 0d : float.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            else if (targetType == typeof(bool))
            {
                sourceString = sourceString.ToLower();
                return !isEmpty && sourceString == "true" || sourceString == "on" || sourceString == "1" || sourceString == "yes";
            }
            else if (targetType == typeof(Guid))
                return isEmpty ? Guid.Empty : new Guid(sourceString);
            else if (targetType.IsEnum)
                return Enum.Parse(targetType, sourceString);
            else if (targetType == typeof(byte[]))
                return new byte[0]; // We are not supporting type arrays for this purpose
            else if (targetType.Name.StartsWith("Nullable`")) // Nullables are special. If they are null, we just return that. Otherwise, we unpack them and then run the current method again with that value.
            {
                if (sourceString.ToLower() == "null" || sourceString == string.Empty)
                    return null;
                else
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                    return UrlParameterToValue(sourceString, targetType);
                }
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                    return converter.ConvertFromString(null, culture, sourceString);
                else
                {
                    Debug.WriteLine($"Type Conversion not handled in StringToTypedValue for {targetType.Name} {sourceString}");
                    return null;
                }
            }
        }

        private void ValidateRoles(IReadOnlyCollection<string> authorizationRoles, IPrincipal user)
        {
            if (user?.Identity != null && user.Identity.IsAuthenticated)
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
        
        public static async Task SendJsonResponseAsync(ServiceHandlerRequestContext context, object value)
        {
            var response = context.HttpResponse;

            response.ContentType = "application/json; charset=utf-8";

            var options = new JsonSerializerOptions();

            if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.CamelCase)
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            //else if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.SnakeCase)
            //    serializer.ContractResolver = SnakeCaseNamingStrategy;

#if DEBUG
            options.WriteIndented = true;
#endif

            var inputType = value.GetType();
            await JsonSerializer.SerializeAsync(response.Body, value, inputType, options);
        }
    }
}