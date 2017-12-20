# CODE Framework ASP.NET Core Services

### Create a new Project
The following provides basic steps for a service handler based project setup.

> #### Pre-release NuGet Packages
> Framework is current in alpha stage. All NuGet packages have to be loaded using the **pre-release** flag in the Package Manager.


##### Create Contracts .NET Standard Class Library

* Create a new .NET Standard Class library project
* Add Nuget Packages
    *  `CODE.Framework.Services.Contracts`
*  Create a Service Contract
*  Create [DataContract] objects for inputs and outputs
    * `BaseServiceRequest` 
    * `BaseServiceResponse`

##### Example:

```cs
namespace Sample.Contracts
{
    public interface ICustomerService
    {
        [Rest(Method = RestMethods.Post, Name ="Customer", Route = "{id:guid}")]
        Task<GetCustomerResponse> GetCustomer(GetCustomerRequest request);

        [Rest(Method = RestMethods.Get, Name = "", Route = "")]
        GetCustomersResponse GetCustomers();
    }


    [DataContract]
    public class GetCustomerRequest : BaseServiceRequest
    {
        [DataMember]
        public string Id { get; set; }
    }

    [DataContract]
    public class GetCustomersResponse : BaseServiceResponse
    {
        [DataMember]
        public List<Customer> CustomerList { get; set; }
    }

    [DataContract]
    public class GetCustomerResponse : BaseServiceResponse
    {
        [DataMember]
        public Customer Customer { get; set; }
    }


    // whatever business data might be exposed either as
    // individual values or business object entities
    public class Customer
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public string Id { get; set; }
    }

}
```

##### Create a Service Implementation .NET Standard Class Library

* Create a new .NET Standard Class library project
* Add reference to the Contract project created above
* Implement Services that implement the contracts

Example:

```cs
namespace Sample.Services.Implementation
{
    public class CustomerService : ICustomerService
    {        

        public GetCustomersResponse GetCustomers()
        {
            return new GetCustomersResponse()
            {
                Success = true,
                CustomerList = new List<Customer>() {
                    new Customer {
                         Name = "Rick Strahl",
                        Company = "West wind"
                    },
                    new Customer {
                         Name = "Markus Egger",
                        Company = "Eps Software"
                    },
                }
            };
        }

        public async Task<GetCustomerResponse> GetCustomer(GetCustomerRequest request)
        {   
            var result = new GetCustomerResponse()
            {
                Customer = new Customer() {
                    Id = request.Id,
                    Name = "Rick Strahl",
                    Company = "West wind"
                }                
            };

            return result;
        }
    }
}
```

### Implement Web Project to host the Service

* Create an empty .NET Core Web site
* Add Nuget package for `CODE.Framework.Services.Server.AspNetCore`
* Add a reference to the Implementation project from above
* Configure the Startup configuration for the Service hosting
* Add `applicationhost.json` for configuration

```csharp
public class Startup
{
    public IConfiguration Configuration { get; }
    
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Add the Service Handler middleware
        // `config` is optional - you can configure with `applicationhost.json`
        // if you specify it overrides the config file
        services.AddServiceHandler(config =>
        {
            config.Services.Clear();                
            config.Services.AddRange(new List<ServiceHandlerConfigurationInstance>
            {
                new ServiceHandlerConfigurationInstance
                {
                    //ServiceType = typeof(UserService), // Using an explicit Type (assembly reference comes in)
                    ServiceTypeName = "Sample.Services.Implementation.UserService",
                    AssemblyName = "Sample.Services.Implementation",
                    RouteBasePath = "/api/users",
                    JsonFormatMode = JsonFormatModes.CamelCase
                },
                new ServiceHandlerConfigurationInstance
                {
                    ServiceTypeName = "Sample.Services.Implementation.CustomerService", // dynamically loaded type
                    AssemblyName = "Sample.Services.Implementation",  // framework needs to load assembly - might need .dll extension
                    RouteBasePath = "/api/customers",
                    JsonFormatMode = JsonFormatModes.ProperCase,
                    OnAuthorize = context =>
                    {
                        context.HttpContext.User = new ClaimsPrincipal(
                            new ClaimsIdentity(
                                new Claim[] {
                                    new Claim("Permission", "CanViewPage"),
                                    new Claim(ClaimTypes.Role, "Administrator"),
                                    new Claim(ClaimTypes.NameIdentifier, "Rick")},
                                "Basic"));

                        return Task.FromResult(true);
                    }
                }
            });

            config.Cors.UseCorsPolicy = true;
            config.Cors.AllowedOrigins = "*";
        });                        
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ServiceHandlerConfiguration config)
    {            
        app.UseServiceHandler();            
    }
}
```

The code above explicitly uses code to configure services and the service configuration. As an alternative you can use configuration settings in `applicationhost.json` instead:

```json
{
  "Logging": { ... },
  "ServiceHandler": {
    "Services": [
      {
        "ServiceTypeName": "Sample.Services.Implementation.UserService",
        "AssemblyName": "Sample.Services.Implementation.dll",
        "RouteBasePath": "/api/users",
        "JsonFormatMode": "CamelCase"
      },
      {
        "ServiceTypeName": "Sample.Services.Implementation.CustomerService",
        "AssemblyName": "Sample.Services.Implementation.dll",
        "RouteBasePath": "/api/customers",
        "JsonFormatMode": "ProperCase",
        "HttpsMode": "Http"
      }
    ],
    "Cors": {
      "UseCorsPolicy": true,
      "CorsPolicyName": "ServiceHandlerCorsPolicy",
      "AllowedOrigins": "*",
      "AllowedMethods": "\"GET,POST,PUT,OPTIONS,DELETE,MOVE,COPY,TRACE,CONNECT,MKCOL\"",
      "AllowedHeaders": null,
      "AllowCredentials": true
    }
  }
}
```

## Authentication
Authentication can be managed using the standard `IPrinciple` based user store. In .NET Core there is no thread specific user context, so access to the user context is a little more tricky. In our service structure you have to use a special helper to retrieve an instance of the user principal:

```cs
var principal = this.GetCurrentPrincipal();  // on Service instance

// or
var principal = UserPrincipalHelper.GetCurrentPrincipal(service);   // service instance

if (principal.Identity.IsAuthenticated)
{
    // comma delimited string
    var roles = pricipal.Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role); 
}
```

Once you have the principal you can check for authentication using standard claims/user validation.

You can manage authentication in two ways:

* Via service contract on service methods via `[Rest(AuthorizationRoles="Administrators"]`
* Explicitly in code using `UserPrincipleHelper` to retrieve a `ClaimsIdentity`

### Using REST Attribute AuthorizationRoles
You can configure a service method in the contract to explicitly require a set of access roles:

```cs
[Rest(Method = RestMethods.Post, Name ="Customer", 
      Route = "{id:guid}", AuthorizationRoles = "Administratortt")]
GetCustomerResponse GetCustomer(GetCustomerRequest request);
```

Using explicit code you can do the following:

```cs
 public GetCustomerResponse GetCustomer(GetCustomerRequest request)
{
    var user = this.GetCurrentPrincipal();
    var isValid = user.IsInRole("Administrators");
}
```

> ### Warning: IPrinciple not available in CTOR
> The IPrinciple user is not available in constructor code as an instance of the service is required to set the principal without a dependency on ASP.NET Core libs or dependency injection.



