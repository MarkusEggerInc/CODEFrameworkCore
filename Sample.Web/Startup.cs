using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CODE.Framework.Services.Server.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Services.Implementation;

namespace CODE.Framework.Services.Server.AspNetCore.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) =>
            services.AddServiceHandler(config =>
            {
                config.Services.Clear();

                config.Services.AddRange(new List<ServiceHandlerConfigurationInstance>
                {
                    new ServiceHandlerConfigurationInstance
                    {
                        ServiceType = typeof(UserService), // Using an explicit Type (assembly reference comes in)
                        //ServiceTypeName = "Sample.Services.Implementation.UserService",
                        //AssemblyName = "Sample.Services.Implementation",
                        RouteBasePath = "/api/users",
                        JsonFormatMode = JsonFormatModes.CamelCase,
                        OnAuthorize = context =>
                        {
                            // fake a user context 
                            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                            {
                                new Claim("Permission", "CanViewPage"),
                                new Claim(ClaimTypes.Role, "Administrator"),
                                new Claim(ClaimTypes.NameIdentifier, "Markus E. User")
                            }, "Basic"));

                            return Task.FromResult(true);
                        }
                    },
                    new ServiceHandlerConfigurationInstance
                    {
                        ServiceTypeName = "Sample.Services.Implementation.CustomerService", // dynamically loaded type
                        AssemblyName = "Sample.Services.Implementation", // framework needs to load assembly - might need .dll extension
                        RouteBasePath = "/api/customers",
                        JsonFormatMode = JsonFormatModes.ProperCase,
                        OnAuthorize = context =>
                        {
                            // fake a user context 
                            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                            {
                                new Claim("Permission", "CanViewPage"),
                                new Claim(ClaimTypes.Role, "Administrators"),
                                new Claim(ClaimTypes.NameIdentifier, "Rick S. Cust")
                            }, "Basic"));

                            return Task.FromResult(true);
                        }
                    }
                });

                // These are the defaults anyway:
                //config.Cors.UseCorsPolicy = true;
                //config.Cors.AllowedOrigins = "*";
            });

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ServiceHandlerConfiguration config)
        {
            app.UseServiceHandler();
            app.UseOpenApiHandler(info: new OpenApiInfo
            {
                Title = "CODE Framework Service/API Example",
                Description = "This service/api example is used to test, demonstrate, and document some of the CODE Framework service/api features.",
                TermsOfService = "http://codeframework.io",
                License = "MIT",
                Contact = "info@codemag.com"
            });

            // TODO: For now, we are using Swashbuckle, but it would be nice to just have this without an outside dependency
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi.json", "Service Description");
            });
        }
    }
}