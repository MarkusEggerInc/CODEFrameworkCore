using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore.Configuration;

namespace CODE.Framework.Services.Server.AspNetCore
{
    /// <summary>
    /// Method that holds cache-able method invocation logic
    /// </summary>
    [DebuggerDisplay("{MethodInfo.Name} - {RestAttribute.Method}")]
    public class MethodInvocationContext
    {
        public static ConcurrentDictionary<MethodInfo, MethodInvocationContext> ActiveMethodContexts { get; set; }
            = new ConcurrentDictionary<MethodInfo, MethodInvocationContext>();

        public RestAttribute RestAttribute { get; set; }

        public List<string> AuthorizationRoles { get; set; } = new List<string>();

        public MethodInfo MethodInfo { get; set; }

        public bool IsAsync { get; set; }



        public ServiceHandlerConfigurationInstance InstanceConfiguration { get; set; }

        public ServiceHandlerConfiguration ServiceConfiguration { get; set; }

        public MethodInvocationContext(MethodInfo method, ServiceHandlerConfiguration serviceConfiguration, ServiceHandlerConfigurationInstance instanceConfiguration)
        {
            InstanceConfiguration = instanceConfiguration;
            ServiceConfiguration = serviceConfiguration;
            MethodInfo = method;

            var attribute = (AsyncStateMachineAttribute) method.GetCustomAttribute(typeof(AsyncStateMachineAttribute));
            if (attribute != null)
                IsAsync = true;

            RestAttribute = method.GetCustomAttribute(typeof(RestAttribute), true) as RestAttribute;

            // set allowable authorization roles
            if (RestAttribute?.AuthorizationRoles == null) return;
            AuthorizationRoles = RestAttribute.AuthorizationRoles.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (AuthorizationRoles.Count == 0)
                AuthorizationRoles.Add(string.Empty); // Any authorized user
        }
    }
}