using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using CODE.Framework.Services.Server.AspNetCore.Properties;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Westwind.Utilities;

namespace CODE.Framework.Services.Server.AspNetCore
{
    public static class ServiceHandlerExtensions
    {

        /// <summary>
        /// Configure the service and make it so you can inject 
        /// IOptions<ServiceHandlerConfiguration>
        /// You can also 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddServiceHandler(this IServiceCollection services,
            Action<ServiceHandlerConfiguration> optionsAction)
        {
            // add strongly typed configuration
            services.AddOptions();
            services.AddRouting();

            var provider = services.BuildServiceProvider();
            var serviceConfiguration = provider.GetService<IConfiguration>();

            var config = new ServiceHandlerConfiguration();
            serviceConfiguration.Bind("ServiceHandler", config);

            ServiceHandlerConfiguration.Current = config;

            optionsAction?.Invoke(config);

            foreach (var svc in config.Services)
            {
                if (svc.ServiceType == null)
                {
                    Type type = ReflectionUtils.GetTypeFromName(svc.ServiceTypeName);
                    if (type == null)
                    {
                        if (ReflectionUtils.LoadAssembly(svc.AssemblyName) == null)
                            throw new ArgumentException(
                                string.Format(Resources.InvalidServiceType, svc.ServiceTypeName));
                        type = ReflectionUtils.GetTypeFromName(svc.ServiceTypeName);
                        if (type == null)
                            throw new ArgumentException(
                                string.Format(Resources.InvalidServiceType, svc.ServiceTypeName));
                    }
                    svc.ServiceType = type;

                    // Add to DI so we can compose the constructor
                    services.AddTransient(type);
                }
            }

            // Add configured instance to DI
            services.AddSingleton(config);


            // Add service and create Policy with options
            services.AddCors(options =>
            {
                options.AddPolicy(config.Cors.CorsPolicyName,
                    builder =>
                    {
                        if (config.Cors.AllowedOrigins == "*")
                            builder = builder.AllowAnyOrigin();
                        else if (!string.IsNullOrEmpty(config.Cors.AllowedOrigins))
                        {
                            var origins = config.Cors.AllowedOrigins.Split(new[] {',', ';'},
                                StringSplitOptions.RemoveEmptyEntries);

                            builder.WithOrigins(origins);
                        }

                        if (!string.IsNullOrEmpty(config.Cors.AllowedMethods))
                        {
                            var methods = config.Cors.AllowedMethods.Split(new[] {',', ';'},
                                StringSplitOptions.RemoveEmptyEntries);
                            builder.WithMethods(methods);
                        }

                        if (!string.IsNullOrEmpty(config.Cors.AllowedHeaders))
                        {
                            var headers = config.Cors.AllowedHeaders.Split(new[] {',', ';'},
                                StringSplitOptions.RemoveEmptyEntries);
                            builder.WithHeaders(headers);
                        }

                        if (config.Cors.AllowCredentials)
                            builder.AllowCredentials();
                    });
            });

            return services;
        }


        /// <summary>
        /// Hook up routed maps to service handlers.
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseServiceHandler(
            this IApplicationBuilder appBuilder)
        {
            var serviceConfig = ServiceHandlerConfiguration.Current;


            foreach (var serviceInstanceConfig in serviceConfig.Services)
            {
                // conditionally route to service handler based on RouteBasePath
                appBuilder.MapWhen(
                    context =>
                    {
                        var requestPath = context.Request.Path.ToString().ToLower();
                        var servicePath = serviceInstanceConfig.RouteBasePath.ToLower();
                        bool matched =
                            requestPath == servicePath ||
                            requestPath.StartsWith(servicePath.Replace("//", "/") + "/");

                        return matched;
                    },                    
                    builder =>
                    {
                        if (serviceConfig.Cors.UseCorsPolicy)
                            builder.UseCors(serviceConfig.Cors.CorsPolicyName);

                        // Build up route mapping
                        builder.UseRouter(routeBuilder =>
                        {
                            // Get Service interface = assuming first interface def is service interface
                             var interfaces = serviceInstanceConfig.ServiceType.GetInterfaces();
                             if (interfaces.Length < 1)
                                    throw new NotSupportedException(Resources.HostedServiceRequiresAnInterface);

                             // Loop through service methods and cache the method info, parameter info, and RestAttribute
                             // in a MethodInvocationContext so we don't have to do this for each method call
                            foreach (var method in serviceInstanceConfig.ServiceType.GetMethods(
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
                            {
                                // find service contract                                
                                var interfaceMethod = interfaces[0].GetMethod(method.Name);
                                if (interfaceMethod == null)
                                    continue;

                                var restAttribute = GetRestAttribute(interfaceMethod);
                                if (restAttribute == null)
                                    continue;
                                
                                var relativeRoute = restAttribute.Route;
                                if (relativeRoute == null)
                                    // if no route assume we use the method name
                                    // Note: string.Empty is a valid route!
                                    relativeRoute = method.Name;


                               


                                // figure out the full route we pass the ASP.NET Core Route Manager
                                string fullRoute =
                                    (serviceInstanceConfig.RouteBasePath + "/" + relativeRoute).Replace("//", "/");

                                if (fullRoute.StartsWith("/"))
                                    fullRoute = fullRoute.Substring(1);

                                // Cache reflection and context data
                                var methodContext = new MethodInvocationContext(method, serviceConfig, serviceInstanceConfig);

                                var roles = restAttribute.AuthorizationRoles;
                                if (roles != null)
                                {
                                    methodContext.AuthorizationRoles = roles.Split( new char[] { ','},StringSplitOptions.RemoveEmptyEntries).ToList();
                                }


                                // This code is what triggers the SERVICE METHOD EXECUTION
                                // via a delegate that is called when the route is matched
                                Func<HttpRequest, HttpResponse, RouteData, Task> exec =
                                    async (req, resp, routeData) =>
                                    {                                                                                
                                        // ReSharper disable once AccessToModifiedClosure
                                        var handler = new ServiceHandler(req.HttpContext, routeData, methodContext);
                                        await handler.ProcessRequest();
                                    };

                                routeBuilder.MapVerb(restAttribute.Method.ToString(), fullRoute, exec);
                                routeBuilder.MapVerb("OPTIONS", fullRoute, async (req, resp, route) =>
                                {
                                    resp.StatusCode = StatusCodes.Status204NoContent;
                                });
                                
                            }
                        });
                      
                        // TODO: Should move into a separate 
                        // builder.UseMiddleware<ServiceHandlerMiddleware>();                        

                    });
            }

            return appBuilder;
        }


        

        /// <summary>
        /// Extracts the RestAttribute from a method's attributes
        /// </summary>
        /// <param name="method">The method to be inspected</param>
        /// <returns>The applied RestAttribute or a default RestAttribute.</returns>
        public static RestAttribute GetRestAttribute(MethodInfo method)
        {
            var customAttributes = method.GetCustomAttributes(typeof(RestAttribute), true);
            if (customAttributes.Length <= 0) return new RestAttribute();
            var restAttribute = customAttributes[0] as RestAttribute;
            return restAttribute ?? new RestAttribute();
        }


    }
}