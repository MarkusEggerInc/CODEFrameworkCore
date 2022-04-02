using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using CODE.Framework.Services.Server.AspNetCore.Properties;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CODE.Framework.Services.Server.AspNetCore
{
    public static class ServiceHandlerExtensions
    {
        /// <summary>
        /// Configure the service and make it so you can inject IOptions
        /// </summary>
        /// <param name="services"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddServiceHandler(this IServiceCollection services, Action<ServiceHandlerConfiguration> optionsAction)
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
                    var type = ObjectHelper.GetTypeFromName(svc.ServiceTypeName);
                    if (type == null)
                    {
                        var assemblyNameWithPath = svc.AssemblyName;
                        if (assemblyNameWithPath.IndexOf("\\", StringComparison.Ordinal) < 0 && assemblyNameWithPath.IndexOf("/", StringComparison.Ordinal) < 0)
                        {
                            var entryAssembly = Assembly.GetEntryAssembly();
                            if (entryAssembly != null)
                            {
                                var directoryName = Path.GetDirectoryName(entryAssembly.Location);
                                if (directoryName != null) assemblyNameWithPath = Path.Combine(directoryName, assemblyNameWithPath);
                            }
                        }

                        var assemblyNameWithFullPath = Path.GetFullPath(assemblyNameWithPath);
                        if (ObjectHelper.LoadAssembly(assemblyNameWithFullPath) == null)
                            throw new ArgumentException(string.Format(Resources.InvalidServiceType, svc.ServiceTypeName));
                        type = ObjectHelper.GetTypeFromName(svc.ServiceTypeName);
                        if (type == null)
                            throw new ArgumentException(string.Format(Resources.InvalidServiceType, svc.ServiceTypeName));
                    }

                    svc.ServiceType = type;
                }

                // Add to DI so we can compose the constructor
                services.AddTransient(svc.ServiceType);
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
                                          builder = builder.SetIsOriginAllowed(s => true);
                                      else if (!string.IsNullOrEmpty(config.Cors.AllowedOrigins))
                                          builder.WithOrigins(config.Cors.AllowedOrigins.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

                                      if (!string.IsNullOrEmpty(config.Cors.AllowedMethods))
                                          builder.WithMethods(config.Cors.AllowedMethods.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

                                      if (!string.IsNullOrEmpty(config.Cors.AllowedHeaders))
                                          builder.WithHeaders(config.Cors.AllowedHeaders.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

                                      if (config.Cors.AllowCredentials)
                                          builder.AllowCredentials();
                                  });
            });

            // Allow injection of the IPrincipal
            services.AddScoped<IPrincipal>(serviceProvider =>
            {
                try
                {
                    // get User from Http Context
                    return serviceProvider.GetRequiredService(typeof(IHttpContextAccessor)) is IHttpContextAccessor context ? context.HttpContext.User : null;
                }
                catch
                {
                    // return an empty identity
                    return new ClaimsPrincipal(new ClaimsIdentity());
                }
            });

            return services;
        }

        /// <summary>
        /// Enabled CODE Framework service hosting
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseServiceHandler(this IApplicationBuilder appBuilder)
        {
            var serviceConfig = ServiceHandlerConfiguration.Current;

            if (serviceConfig.Cors.UseCorsPolicy)
                appBuilder.UseCors(serviceConfig.Cors.CorsPolicyName);

            // Endpoints require routing, so we make sure it is there
            appBuilder.UseRouting();

            appBuilder.UseEndpoints(endpoints =>
            {
                foreach (var serviceInstanceConfig in serviceConfig.Services)
                    // conditionally route to service handler based on RouteBasePath
                    appBuilder.MapWhen(
                                       context =>
                                       {
                                           var requestPath = context.Request.Path.ToString().ToLower();
                                           if (SwaggerRoutes != null && SwaggerRoutes.Contains(requestPath)) return false; // We make sure we are not accidently eating up a configured swagger/openapi route
                                           var servicePath = serviceInstanceConfig.RouteBasePath.ToLower();
                                           var matched = requestPath == servicePath || requestPath.StartsWith(servicePath.Replace("//", "/") + "/");
                                           return matched;
                                       },
                                       builder =>
                                       {
                                           //if (serviceConfig.Cors.UseCorsPolicy)
                                           //    builder.UseCors(serviceConfig.Cors.CorsPolicyName);

                                           // Build up route mapping
                                           builder.UseRouter(routeBuilder =>
                                           {
                                               // Get Service interface = assuming first interface def is service interface
                                               var interfaces = serviceInstanceConfig.ServiceType.GetInterfaces();
                                               if (interfaces.Length < 1)
                                                   throw new NotSupportedException(Resources.HostedServiceRequiresAnInterface);

                                               // Loop through service methods and cache the propertyInfo info, parameter info, and RestAttribute
                                               // in a MethodInvocationContext so we don't have to do this for each propertyInfo call
                                               foreach (var method in serviceInstanceConfig.ServiceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
                                               {
                                                   // find service contract                                
                                                   var interfaceMethod = interfaces[0].GetMethod(method.Name);
                                                   if (interfaceMethod == null) continue; // Should never happen, but doesn't hurt to check

                                                   var restAttribute = GetRestAttribute(interfaceMethod);
                                                   if (restAttribute == null) continue; // This should never happen since GetRestAttribute() above returns a default attribute if none is attached

                                                   var relativeRoute = restAttribute.Route;
                                                   if (relativeRoute == null)
                                                   {
                                                       // If no route is defined, we either build a route out of name and other attributes, or we use the propertyInfo name as the last resort.
                                                       // Note: string.Empty is a valid route (and also a valid name). Only null values indicate that the setting has not been set!

                                                       relativeRoute = restAttribute.Name ?? method.Name;

                                                       // We also have to take a look at the parameter(s) - there should be only one - to build the route
                                                       var parameters = method.GetParameters();
                                                       if (parameters.Length > 0)
                                                       {
                                                           var parameterType = parameters[0].ParameterType;
                                                           var parameterProperties = parameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                                                           var inlineParameters = GetSortedInlineParameterNames(parameterProperties);
                                                           foreach (var inlineParameter in inlineParameters)
                                                               relativeRoute += $"/{{{inlineParameter}}}";
                                                       }
                                                   }

                                                   if (relativeRoute.StartsWith("/")) relativeRoute = relativeRoute.Substring(1);

                                                   // Figure out the full route we pass the ASP.NET Core Route Manager
                                                   var fullRoute = (serviceInstanceConfig.RouteBasePath + "/" + relativeRoute).Replace("//", "/");
                                                   if (fullRoute.StartsWith("/")) fullRoute = fullRoute.Substring(1);

                                                   // Cache reflection and context data
                                                   var methodContext = new MethodInvocationContext(method, serviceConfig, serviceInstanceConfig);

                                                   var roles = restAttribute.AuthorizationRoles;
                                                   if (roles != null)
                                                       methodContext.AuthorizationRoles = roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                                                   // This code is what triggers the SERVICE METHOD EXECUTION via a delegate that is called when the route is matched
                                                   Func<HttpRequest, HttpResponse, RouteData, Task> exec =
                                                       async (req, resp, routeData) =>
                                                       {
                                                           // ReSharper disable once AccessToModifiedClosure
                                                           var handler = new ServiceHandler(req.HttpContext, routeData, methodContext);
                                                           await handler.ProcessRequest();
                                                       };

                                                   routeBuilder.MapVerb("OPTIONS", fullRoute, async (req, resp, route) =>
                                                   {
                                                       resp.StatusCode = StatusCodes.Status204NoContent;
                                                       await Task.CompletedTask;
                                                   });

                                                   routeBuilder.MapVerb(restAttribute.Method.ToString(), fullRoute, exec);
                                               }
                                           });
                                       });
            });

            return appBuilder;
        }

        /// <summary>
        /// Enabled CODE Framework Open API support.
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOpenApiHandler(this IApplicationBuilder appBuilder, bool supportOpenApiJson = true, string openApiJsonRoute = "openapi.json", OpenApiInfo info = null)
        {
            if (SwaggerRoutes == null)
            {
                SwaggerRoutes = new List<string>();
                SwaggerRoutes.Add("/swagger"); // We assume swagger API is used when OpenAPI features are on. This makes sure that the general handler does not eat up other service routes
                SwaggerRoutes.Add("/swagger/index.html");
            }

            var serviceConfig = ServiceHandlerConfiguration.Current;

            // Endpoints require routing, so we make sure it is there
            appBuilder.UseRouting();

            appBuilder.UseEndpoints(endpoints =>
            {
                appBuilder.MapWhen(
                                    context =>
                                    {
                                        var requestPath = context.Request.Path.ToString().Trim().ToLowerInvariant();
                                        var openApiFullRoute = !string.IsNullOrEmpty(openApiJsonRoute) ? openApiJsonRoute : "/openapi.json";
                                        if (!openApiFullRoute.StartsWith("/"))
                                            openApiFullRoute = "/" + openApiFullRoute;
                                        openApiFullRoute = openApiFullRoute.Trim().ToLowerInvariant();
                                        return requestPath == openApiFullRoute;
                                    },
                                    builder =>
                                    {
                                        // Build up route mapping
                                        builder.UseRouter(routeBuilder =>
                                        {
                                            var openApiFullRoute = !string.IsNullOrEmpty(openApiJsonRoute) ? openApiJsonRoute : "/openapi.json";
                                            if (!openApiFullRoute.StartsWith("/"))
                                                openApiFullRoute = "/" + openApiFullRoute;
                                            routeBuilder.MapVerb("GET", openApiFullRoute, GetOpenApiJson(serviceConfig.Services, info));
                                            SwaggerRoutes.Add(openApiFullRoute);

                                            // TODO: Add openapi.yaml support?
                                        });
                                    });
            });

            return appBuilder;
        }

        private static List<string> SwaggerRoutes { get; set; }

        private static Func<HttpRequest, HttpResponse, RouteData, Task> GetOpenApiJson(List<ServiceHandlerConfigurationInstance> serviceInstanceConfigurations, OpenApiInfo info = null) => async (req, resp, route) =>
        {
            resp.ContentType = "application/json; charset=utf-8";

            var xmlDocumentationFiles = new Dictionary<Assembly, OpenApiXmlDocumentationFile>();
            //ComponentsJsonConverter.XmlDocumentationFiles = xmlDocumentationFiles; // This is a little dirty, but it is a good way to get this into the converter

            if (info == null) info = new OpenApiInfo();
            var openApiInfo = new OpenApiInformation { Info = info };

            foreach (var serviceInstanceConfig in serviceInstanceConfigurations)
            {
                var interfaces = serviceInstanceConfig.ServiceType.GetInterfaces();
                if (interfaces.Length < 1)
                    throw new NotSupportedException(Resources.HostedServiceRequiresAnInterface);

                if (!xmlDocumentationFiles.ContainsKey(interfaces[0].Assembly))
                    xmlDocumentationFiles.Add(interfaces[0].Assembly, new OpenApiXmlDocumentationFile(interfaces[0].Assembly));

                // If we do not have a version number yet, we use the first interface to see if that can give us a version number
                if (string.IsNullOrEmpty(info.Version)) 
                    info.Version = interfaces[0].Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

                var tag = new OpenApiTag { Name = serviceInstanceConfig.ServiceType.Name };
                tag.Description = OpenApiHelper.GetDescription(serviceInstanceConfig.ServiceType, interfaces[0], xmlDocumentationFiles);
                tag.ExternalDocs = OpenApiHelper.GetExternalDocs(serviceInstanceConfig.ServiceType, interfaces[0]);
                openApiInfo.Tags.Add(tag);

                var methods = serviceInstanceConfig.ServiceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var interfaceMethod = interfaces[0].GetMethod(method.Name);
                    if (interfaceMethod == null) continue; // Should never happen, but doesn't hurt to check
                    var restAttribute = GetRestAttribute(interfaceMethod);
                    if (restAttribute == null) continue; // This should never happen since GetRestAttribute() above returns a default attribute if none is attached

                    var httpVerb = restAttribute.Method.ToString().ToLowerInvariant();
                    var pathInfo = new OpenApiPathInfo(restAttribute.Method.ToString(), httpVerb, method.Name, method);

                    // Get method summary and description
                    pathInfo.Verbs[pathInfo.Verbs.Keys.First()].Summary = OpenApiHelper.GetSummary(interfaceMethod, interfaces[0], xmlDocumentationFiles);
                    pathInfo.Verbs[pathInfo.Verbs.Keys.First()].Description = OpenApiHelper.GetDescription(interfaceMethod, interfaces[0], xmlDocumentationFiles);

                    pathInfo.Tags.Add(new OpenApiTag { Name = serviceInstanceConfig.ServiceType.Name });

                    pathInfo.ReturnType = interfaceMethod.ReturnType;
                    var returnTypeOpenApiString = OpenApiHelper.GetOpenApiType(interfaceMethod.ReturnType);
                    if (string.IsNullOrEmpty(returnTypeOpenApiString)) // If it isn't a simple type (which it really shouldn't be, based on our usage patterns), we add the component definition (schema)
                    {
                        var obsolete = false;
                        var obsoleteReason = string.Empty;
                        var obsoleteAttribute = interfaceMethod.GetCustomAttribute<ObsoleteAttribute>();
                        if (obsoleteAttribute != null)
                        {
                            obsolete = true;
                            if (!string.IsNullOrEmpty(obsoleteAttribute.Message))
                                obsoleteReason = obsoleteAttribute.Message.Trim();
                        }
                        OpenApiHelper.AddTypeToComponents(openApiInfo, interfaceMethod.ReturnType, obsolete, obsoleteReason, xmlDocumentationFiles);
                        pathInfo.Obsolete = obsolete;
                        pathInfo.ObsoleteReason = obsoleteReason;
                    }

                    if (httpVerb == "get")
                        // Get operations do not have a payload/body, so everything must be coming in from the URL
                        OpenApiHelper.ExtractOpenApiParameters(interfaceMethod, pathInfo, xmlDocumentationFiles);
                    else
                    {
                        var methodParameters = interfaceMethod.GetParameters();
                        foreach (var parameter in methodParameters) // Should always be a single parameter
                        {
                            var obsolete2 = false;
                            var obsoleteReason2 = string.Empty;
                            var obsoleteAttribute2 = parameter.GetCustomAttribute<ObsoleteAttribute>();
                            if (obsoleteAttribute2 != null)
                            {
                                obsolete2 = true;
                                if (!string.IsNullOrEmpty(obsoleteAttribute2.Message))
                                    obsoleteReason2 = obsoleteAttribute2.Message.Trim();
                            }
                            OpenApiHelper.AddTypeToComponents(openApiInfo, parameter.ParameterType, obsolete2, obsoleteReason2, xmlDocumentationFiles);
                        }
                        OpenApiHelper.ExtractOpenApiParameters(interfaceMethod, pathInfo, xmlDocumentationFiles);
                        if (methodParameters.Length > 0)
                        {
                            pathInfo.Payload = new OpenApiPayload { Type = methodParameters[0].ParameterType };
                            pathInfo.Payload.Description = OpenApiHelper.GetDescription(methodParameters[0].ParameterType, xmlDocumentationFiles);
                            if (string.IsNullOrEmpty(pathInfo.Payload.Description))
                                pathInfo.Payload.Description = OpenApiHelper.GetSummary(methodParameters[0].ParameterType, xmlDocumentationFiles);
                        }
                    }

                    var definedRoute = restAttribute.Route != null ? restAttribute.Route : restAttribute.Name == null ? $"{method.Name}" : $"{restAttribute.Name}";
                    var fullRoute = string.IsNullOrEmpty(definedRoute) ? $"{serviceInstanceConfig.RouteBasePath}" : $"{serviceInstanceConfig.RouteBasePath}/{definedRoute}";
                    var fullRouteKey = $"{httpVerb}::{fullRoute}";

                    if (!openApiInfo.Paths.ContainsKey(fullRouteKey))
                        openApiInfo.Paths.Add(fullRouteKey, pathInfo);
                    else
                        throw new Exception($"Duplicate path/route in service definition.{Environment.NewLine}Route: {fullRoute}");
                }
            }

            resp.ContentType = "application/json; charset=utf-8";
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            await System.Text.Json.JsonSerializer.SerializeAsync(resp.Body, openApiInfo, typeof(OpenApiInformation), options);
        };

        /// <summary>
        /// Extracts the RestAttribute from a propertyInfo's attributes
        /// </summary>
        /// <param name="method">The method-info to be inspected</param>
        /// <returns>The applied RestAttribute or a default RestAttribute.</returns>
        public static RestAttribute GetRestAttribute(MethodInfo method)
        {
            var customAttributes = method.GetCustomAttributes(typeof(RestAttribute), true);
            if (customAttributes.Length <= 0) return new RestAttribute();
            var restAttribute = customAttributes[0] as RestAttribute;
            return restAttribute ?? new RestAttribute();
        }

        /// <summary>
        /// Extracts the RestUrlParameterAttribute from a propertyInfo's attributes
        /// </summary>
        /// <param name="propertyInfo">The property-info to be inspected</param>
        /// <returns>The applied RestUrlParameterAttribute or a default RestUrlParameterAttribute.</returns>
        public static RestUrlParameterAttribute GetRestUrlParameterAttribute(PropertyInfo propertyInfo)
        {
            var customAttributes = propertyInfo.GetCustomAttributes(typeof(RestUrlParameterAttribute), true);
            if (customAttributes.Length <= 0) return null;
            return customAttributes[0] as RestUrlParameterAttribute;
        }

        private static IEnumerable<string> GetSortedInlineParameterNames(IEnumerable<PropertyInfo> propertyInfos)
        {
            var list = new List<PropertyInfoHelper>();
            foreach (var propertyInfo in propertyInfos)
            {
                var attribute = GetRestUrlParameterAttribute(propertyInfo);
                if (attribute == null) continue;
                if (attribute.Mode == UrlParameterMode.Inline)
                    list.Add(new PropertyInfoHelper { Name = propertyInfo.Name, Order = attribute.Sequence });
            }

            return list.OrderBy(a => a.Order).Select(a => a.Name);
        }

        private class PropertyInfoHelper
        {
            public string Name { get; set; }
            public int Order { get; set; }
        }
    }
}