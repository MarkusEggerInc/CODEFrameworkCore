using System;
using System.Collections.Generic;
using System.Text;

namespace CODE.Framework.Services.Server.AspNetCore
{
    public class SwaggerInformation
    {
        public string Swagger => "2.0";
        public SwaggerInfo Info { get; set; } = new SwaggerInfo(); 
        public Dictionary<string, SwaggerPathInfo> Paths { get; set; } = new Dictionary<string, SwaggerPathInfo>();
    }

    public class SwaggerInfo
    {
        public string Description { get; set; } = string.Empty;
    }

    public class SwaggerPathInfo
    {
        private string _path; 
        public SwaggerPathInfo(string path)
        {
            _path = path;
        }

        public string OperationId { get; set; }

        public override string ToString() => _path;
    }
}
