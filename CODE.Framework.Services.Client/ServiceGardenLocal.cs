using System;
using System.Collections.Generic;
using System.Linq;
using CODE.Framework.Fundamentals.Configuration;
using CODE.Framework.Services.Contracts;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Provides an in-process service garden
    /// </summary>
    public static class ServiceGardenLocal
    {
        /// <summary>
        /// Collection of known hosts
        /// </summary>
        /// <value>Hosts</value>
        private static Dictionary<Type, object> Hosts { get; } = new Dictionary<Type, object>();

        /// <summary>
        /// Adds a local service based on the services type
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>True if successful</returns>
        /// <remarks>The interface used by the service is automatically determined.</remarks>
        /// <example>
        /// ServiceGardenLocal.AddServiceHost(typeof(MyNamespace.CustomerService));
        /// </example>
        public static bool AddServiceHost(Type serviceType)
        {
            Type contractType;
            var interfaces = serviceType.GetInterfaces();
            if (interfaces.Length == 1)
                contractType = interfaces[0];
            else if (interfaces.Length == 2)
            {
                if (interfaces[0].FullName == "CODE.Framework.Services.Contracts.IServiceEvents")
                    contractType = interfaces[1];
                else if (interfaces[1].FullName == "CODE.Framework.Services.Contracts.IServiceEvents")
                    contractType = interfaces[0];
                else
                    throw new IndexOutOfBoundsException("Service contract cannot be automatically determined for the specified service type.");
            }
            else
                throw new IndexOutOfBoundsException("Service contract cannot be automatically determined for the specified service type.");
            return AddServiceHost(serviceType, contractType);
        }

        /// <summary>
        /// Adds a local service
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the operation contract (interface).</param>
        /// <returns>True if successful</returns>
        /// <example>
        /// ServiceGardenLocal.AddServiceHost(typeof(MyNamespace.CustomerService), typeof(MyContracts.ICustomerServicce));
        /// </example>
        public static bool AddServiceHost(Type serviceType, Type contractType)
        {
            var serviceInstance = Activator.CreateInstance(serviceType);
            Hosts.Add(contractType, serviceInstance);

            var settingName = "ServiceProtocol:" + contractType.Name;
            if (!ConfigurationSettings.Settings.IsSettingSupported(settingName))
                if (ConfigurationSettings.Sources.Any(s => s.FriendlyName == "Memory"))
                    ConfigurationSettings.Sources["Memory"].Settings[settingName] = "InProcess";

            if (serviceInstance is IServiceEvents serviceEvents) 
                serviceEvents.OnInProcessHostLaunched();

            return true;
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <typeparam name="TContractType">The type of the operations contract (interface).</typeparam>
        /// <returns></returns>
        public static TContractType GetService<TContractType>() => Hosts.ContainsKey(typeof(TContractType)) ? (TContractType) Hosts[typeof(TContractType)] : default;

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <param name="contractType">Contract Type</param>
        /// <returns></returns>
        public static object GetService(Type contractType) => Hosts.ContainsKey(contractType) ? Hosts[contractType] : null;
    }
}
