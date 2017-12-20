using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CODE.Framework.Services.Server.AspNetCore;

namespace CODE.Framework.Services.Server.AspNetCore.GenericServiceHandler
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceHandler(config =>
            {
                //config.Services.AddRange(new List<ServiceHandlerConfigurationInstance>
                //{
                //    new ServiceHandlerConfigurationInstance
                //    {
                //        ServiceType = typeof(UserService),
                //        //ServiceTypeName = "Sample.Services.Implementation.UserService",
                //        //AssemblyName = "Sample.Services.Implementation",
                //        RouteBasePath = "/api/users"
                //    },
                //    new ServiceHandlerConfigurationInstance
                //    {
                //        ServiceTypeName = "Sample.Services.Implementation.CustomerService",
                //        AssemblyName = "Sample.Services.Implementation",
                //        RouteBasePath = "/api/customers"
                //    }
                //});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {            
            app.UseServiceHandler();
        }
    }
}
