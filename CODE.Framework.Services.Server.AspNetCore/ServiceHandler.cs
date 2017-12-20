using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Claims;
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
        HttpRequest HttpRequest { get; }

        /// <summary>
        /// Http Response output object to send response data into
        /// </summary>
        HttpResponse HttpResponse { get; }

        /// <summary>
        /// HttpContext instance passed from ASP.NET request context
        /// </summary>
        HttpContext HttpContext { get;  }

        /// <summary>
        /// Request route data for the current request
        /// </summary>
        RouteData RouteData { get; }


        /// <summary>
        /// Internally info about the method that is to be executed. This info
        /// is created when the handler is first initialized.
        /// </summary>
        MethodInvocationContext MethodContext { get; }

        /// <summary>
        /// The service specific configuration information
        /// </summary>
        ServiceHandlerConfigurationInstance ServiceInstanceConfiguration { get; }
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="routeData"></param>
        /// <param name="methodContext"></param>
        
        public ServiceHandler(HttpContext httpContext,
            RouteData routeData,
            MethodInvocationContext methodContext)
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
            var context = new ServiceHandlerRequestContext()
            {
                HttpRequest = HttpRequest,
                HttpResponse = HttpResponse,
                HttpContext = HttpContext,
                ServiceInstanceConfiguration = ServiceInstanceConfiguration,
                MethodContext = MethodContext,
                Url = new ServiceHandlerRequestContextUrl()
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
                {
                    if (!await ServiceInstanceConfiguration.OnAuthorize(context))
                        throw new UnauthorizedAccessException("Not authorized to access this request");
                }

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
        async Task ExecuteMethod(ServiceHandlerRequestContext handlerContext)
        {
            var serviceConfig = ServiceHandlerConfiguration.Current;
            var methodToInvoke = handlerContext.MethodContext.MethodInfo;
            var serviceType = handlerContext.ServiceInstanceConfiguration.ServiceType;


            var httpVerb = handlerContext.HttpRequest.Method;
            if (httpVerb == "OPTIONS" && serviceConfig.Cors.UseCorsPolicy)
            {
                // emty response - ASP.NET will provide CORS headers via applied policy
                handlerContext.HttpResponse.StatusCode = StatusCodes.Status204NoContent;
                return;
            }


            // Compose type using DI
            var inst = HttpContext.RequestServices.GetService(serviceType);            
            if (inst == null)
                throw new InvalidOperationException(string.Format(Resources.UnableToCreateTypeInstance, serviceType));



            try
            {
                UserPrincipalHelper.AddPrincipal(inst, HttpContext.User);

                var parameterList = GetMethodParameters(handlerContext);

                if (!handlerContext.MethodContext.IsAsync)
                    handlerContext.ResultValue = methodToInvoke.Invoke(inst, parameterList);
                else
                    handlerContext.ResultValue = await (dynamic) methodToInvoke.Invoke(inst, parameterList);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(Resources.UnableToExecuteMethod, methodToInvoke.Name,
                    ex.Message));
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
            object result = null;

            // simplistic - no parameters or single body post parameter
            var paramInfos = handlerContext.MethodContext.MethodInfo.GetParameters();
            if (paramInfos.Length > 1)
                throw new ArgumentNullException(string.Format(
                    Resources.OnlySingleParametersAreAllowedOnServiceMethods,
                    MethodContext.MethodInfo.Name));

            // if there is a parameter create and de-serialize, then add url parameters
            if (paramInfos.Length == 1)
            {
                var parm = paramInfos[0];

                // First Deserialize from body if any
                JsonSerializer serializer = new JsonSerializer();

                // there's always 1 parameter
                object parameterData = null;
                if (HttpRequest.ContentLength == null || HttpRequest.ContentLength < 1)
                    // if no content create an empty one
                    parameterData = ReflectionUtils.CreateInstanceFromType(parm.ParameterType);
                else
                {
                    using (var sw = new StreamReader(HttpRequest.Body))
                    using (JsonReader reader = new JsonTextReader(sw))
                    {
                        parameterData = serializer.Deserialize(reader, parm.ParameterType);
                    }
                }

                // Map named URL parameters to properties
                if (RouteData != null)
                {
                    foreach (var kv in RouteData.Values)
                    {
                        var prop = parm.ParameterType.GetProperty(kv.Key,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty |
                            BindingFlags.IgnoreCase);
                        if (prop != null)
                        {
                            try
                            {
                                var val = ReflectionUtils.StringToTypedValue(kv.Value as string, prop.PropertyType);
                                ReflectionUtils.SetProperty(parameterData, kv.Key, val);
                            }
                            catch
                            {
                                throw new InvalidOperationException(
                                    string.Format("Unable set parameter from URL segment for property: {0}", kv.Key));
                            }
                        }
                    }
                }

                parameterList = new[] {parameterData};
            }
            return parameterList;
        }


        static DefaultContractResolver CamelCaseNamingStrategy =
            new DefaultContractResolver {NamingStrategy = new CamelCaseNamingStrategy()};

        static DefaultContractResolver SnakeCaseNamingStrategy =
            new DefaultContractResolver {NamingStrategy = new SnakeCaseNamingStrategy()};

        static void SendJsonResponse(ServiceHandlerRequestContext context, object value)
        {
            var response = context.HttpResponse;

            response.ContentType = "application/json; charset=utf-8";

            JsonSerializer serializer = new JsonSerializer();

            if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.CamelCase)
                serializer.ContractResolver = CamelCaseNamingStrategy;
            else if (context.ServiceInstanceConfiguration.JsonFormatMode == JsonFormatModes.SnakeCase)
                serializer.ContractResolver = SnakeCaseNamingStrategy;            

#if DEBUG
            serializer.Formatting = Formatting.Indented;
#endif

            using (var sw = new StreamWriter(response.Body))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, value);
                }
            }
        }
                
    }

}

