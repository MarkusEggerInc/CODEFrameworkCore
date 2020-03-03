using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using CODE.Framework.Services.Server.AspNetCore.Properties;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Westwind.Utilities;

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
                    var type = ReflectionUtils.GetTypeFromName(svc.ServiceTypeName);
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

                        if (ReflectionUtils.LoadAssembly(Path.GetFullPath(assemblyNameWithPath)) == null)
                            throw new ArgumentException(string.Format(Resources.InvalidServiceType, svc.ServiceTypeName));
                        type = ReflectionUtils.GetTypeFromName(svc.ServiceTypeName);
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
        /// Hook up routed maps to service handlers.
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseServiceHandler(this IApplicationBuilder appBuilder)
        {
            var serviceConfig = ServiceHandlerConfiguration.Current;

            if (serviceConfig.Cors.UseCorsPolicy)
                appBuilder.UseCors(serviceConfig.Cors.CorsPolicyName);

            // Endpoints require routing 
            appBuilder.UseRouting();

            appBuilder.UseEndpoints(endpoints =>
            {
                foreach (var serviceInstanceConfig in serviceConfig.Services)
                    // conditionally route to service handler based on RouteBasePath
                    appBuilder.MapWhen(
                                       context =>
                                       {
                                           var requestPath = context.Request.Path.ToString().ToLower();
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

                                               // TODO: Optionally enable swagger support.
                                               var swaggerFullRoute = (serviceInstanceConfig.RouteBasePath + "/swagger.json").Replace("//", "/");
                                               if (swaggerFullRoute.StartsWith("/")) swaggerFullRoute = swaggerFullRoute.Substring(1);

                                               routeBuilder.MapVerb("GET", swaggerFullRoute, GetSwaggerJson(serviceInstanceConfig, interfaces));

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
                                                               relativeRoute += "/{" + inlineParameter + "}";
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

        private static Func<HttpRequest, HttpResponse, RouteData, Task> GetSwaggerJson(ServiceHandlerConfigurationInstance serviceInstanceConfig, Type[] interfaces) => async (req, resp, route) =>
        {
            OpenApiDocument openApiDocument = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = "EPS ServiceHandlerExtensions",
                    Description = "EPS ServiceHandlerExtensions class.",
                    Version = "4.2.3",
                    Contact = new OpenApiContact
                    {
                        Email = "megger@eps-software.com",
                        Name = "Markus Egger",
                        Url = new Uri("https://www.codemag.com/people/bio/markus.egger")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "GNU AGPLv3",
                        Url = new Uri("https://choosealicense.com/licenses/agpl-3.0/")
                    },
                    TermsOfService = new Uri("https://docs.codeframework.io/")
                },
                Paths = new OpenApiPaths(),
                Tags = new List<OpenApiTag>()
            };

            var routeURL = GetRouteURL(req.Host.Value, serviceInstanceConfig.RouteBasePath);

            openApiDocument.Servers = new List<OpenApiServer>
            {
                new OpenApiServer
                {
                    Description = serviceInstanceConfig.AssemblyName,
                    Url = routeURL,
                }
            };

            // Loop through service methods and cache the propertyInfo info, parameter info, and RestAttribute
            // in a MethodInvocationContext so we don't have to do this for each propertyInfo call
            foreach (var method in serviceInstanceConfig.ServiceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
            {
                // find service contract                                
                var interfaceMethod = interfaces[0].GetMethod(method.Name);
                if (interfaceMethod == null) continue; // Should never happen, but doesn't hurt to check

                OpenApiOperation operation = new OpenApiOperation
                {
                    Summary = "",
                    Description = "",
                    OperationId = interfaceMethod.Name,
                    Responses = new OpenApiResponses(),
                    Tags = new List<OpenApiTag>()
                };

                operation.Responses.Add("200", new OpenApiResponse { Description = "Success." });
                operation.Responses.Add("400", new OpenApiResponse { Description = "Bad Request." });
                operation.Responses.Add("404", new OpenApiResponse { Description = "Not Found." });

                operation.Parameters = new List<OpenApiParameter>();

                var restAttribute = GetRestAttribute(interfaceMethod);
                if (restAttribute == null) continue; // This should never happen since GetRestAttribute() above returns a default attribute if none is attached

                var relativeRoute = restAttribute.Route;

                if (relativeRoute == null)
                {
                    // If no route is defined, we either build a route out of name and other attributes, or we use the propertyInfo name as the last resort.
                    // Note: string.Empty is a valid route (and also a valid name). Only null values indicate that the setting has not been set!

                    if (restAttribute.Name == null)
                        relativeRoute = method.Name;
                    else
                        relativeRoute = restAttribute.Name;
                }

                // We also have to take a look at the parameter(s) - there should be only one - to build the route
                var parameters = method.GetParameters();
                if (parameters.Length > 0)
                {
                    var parameterType = parameters[0].ParameterType;
                    var parameterProperties = parameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    var inlineParameters = GetSortedInlineParameterNames(parameterProperties);
                    foreach (var inlineParameter in inlineParameters)
                    {
                        relativeRoute += "/{" + inlineParameter + "}";
                        OpenApiParameter parameter = new OpenApiParameter
                        {
                            Description = "Description.",
                            Name = inlineParameter,

                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema
                            {
                                Type = "string"
                            }
                        };
                        operation.Parameters.Add(parameter);
                    }
                }

                if (relativeRoute.StartsWith("/")) relativeRoute = relativeRoute.Substring(1);

                // Figure out the full route we pass the ASP.NET Core Route Manager
                var fullRoute = (serviceInstanceConfig.RouteBasePath + "/" + relativeRoute).Replace("//", "/");
                if (fullRoute.StartsWith("/")) fullRoute = fullRoute.Substring(1);

                OpenApiPathItem pathItem = new OpenApiPathItem();

                //Enum.TryParse("Get", out OperationType operationType);
                operation.Tags = new List<OpenApiTag> { new OpenApiTag { Name = interfaceMethod.Name } };

                pathItem.AddOperation(OperationType.Get, operation);

                openApiDocument.Paths.Add((BuildSwaggerInterfaceMethodName(interfaceMethod.Name)), pathItem);

                openApiDocument.Tags.Add(new OpenApiTag() { Name = interfaceMethod.Name, Description = interfaceMethod.Name + " Description." });
            }

            var response = resp;
            response.ContentType = "application/json; charset=utf-8";
            var jsonDoc = WriteSwaggerToJson(openApiDocument);

            using (var sw = new StreamWriter(response.Body))
                sw.Write(jsonDoc);
        };

        internal static string BuildSwaggerInterfaceMethodName(string interfaceMethodName)
        {
            return (interfaceMethodName[0] != '/' ? "/" : "") + interfaceMethodName;
        }

        internal static string GetRouteURL(string hostValue, string routeBase)
        {
            if (!hostValue.ToLower().Contains("https"))
            {
                hostValue = "https://" + hostValue;
            }

            if (routeBase[0] != '/')
            {
                routeBase = "/" + routeBase;
            }

            return hostValue + routeBase;
        }

        internal static string WriteSwaggerToJson(OpenApiDocument docuemt)
        {
            try
            {
                var outputStringWriter = new StringWriter(CultureInfo.InvariantCulture);
                var writer = new OpenApiJsonWriter(outputStringWriter);
                docuemt.SerializeAsV2(writer);
                writer.Flush();
                return outputStringWriter.GetStringBuilder().ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

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