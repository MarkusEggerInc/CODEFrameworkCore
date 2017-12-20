using CODE.Framework.Services.Server.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;

namespace CODE.Framework.Services.Server.AspNetCore
{
    /// <summary>
    /// Holds Request related data through the lifetime ofr
    /// a service handler request.
    /// </summary>
    public class ServiceHandlerRequestContext
    {
        public HttpContext HttpContext { get; set; }

        public HttpRequest HttpRequest { get; set;  }

        public HttpResponse HttpResponse { get; set; }

        public ServiceHandlerConfigurationInstance ServiceInstanceConfiguration { get; set; }

        public object ResultValue { get; set; }

        public string ResultJson { get; set; }
    
        public ServiceHandlerRequestContextUrl Url { get; set; } = new ServiceHandlerRequestContextUrl();

        public MethodInvocationContext MethodContext { get; internal set; }
    }

    public class ServiceHandlerRequestContextUrl
    {
        public string Url { get; internal set; }
        public string UrlPath { get; internal set; }
        public QueryString QueryString { get; internal set; }
        public string HttpMethod { get; internal set; }
    }
}

