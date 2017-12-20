namespace CODE.Framework.Services.Server.AspNetCore.Configuration
{

    /// <summary>
    /// Configuration for the global CORS policy that is applied to the service handler.
    /// </summary>
    /// <remarks>
    /// This policy is applied globally to the entire server and all requests
    /// by using appBuilder.UseCors()    
    /// </remarks>
    public class ServiceHandlerCorsConfiguration
    {
        /// <summary>
        /// Determines whether Cors policy is applied globally using 
        /// the settings in of this configuration class
        /// </summary>
        public bool UseCorsPolicy { get;  set; } = true;

        /// <summary>
        /// Name of the ASP.NET Core Policy created that is applied to all requests
        /// </summary>
        public string CorsPolicyName { get; set; } = "ServiceHandlerCorsPolicy";

        /// <summary>
        /// domains that are allowed to connection. * for all domains (default)
        /// </summary>
        public string AllowedOrigins { get; set; } = "*";

        /// <summary>
        /// Http Verbs that are allowed. Defaults to all verbs.
        /// </summary>
        public string AllowedMethods { get; set; } = "GET,POST,PUT,OPTIONS,DELETE,MOVE,COPY,TRACE,CONNECT,MKCOL";

        /// <summary>
        /// Custom headers that are allowed as part of a cross domain request
        /// </summary>
        public string AllowedHeaders { get; set; } 

        /// <summary>
        /// Allow credentials and Cookies as part of cross domain requests
        /// </summary>
        public bool AllowCredentials { get; set; } = true;
        
    }
}