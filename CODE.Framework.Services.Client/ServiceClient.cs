using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using CODE.Framework.Fundamentals.Configuration;
using CODE.Framework.Fundamentals.Utilities;

namespace CODE.Framework.Services.Client
{
    public static class ServiceClient
    {
        static ServiceClient()
        {
            if (GetSetting("ServiceClient:" + nameof(LogCommunicationErrors)).ToLowerInvariant() == "true")
                LogCommunicationErrors = true;
        }

        /// <summary>Internal message size cache</summary>
        private static readonly Dictionary<string, MessageSize> CachedMessageSizes = new Dictionary<string, MessageSize>();

        /// <summary>Cached protocols</summary>
        private static readonly Dictionary<string, Protocol> CachedProtocols = new Dictionary<string, Protocol>();

        /// <summary>The cached service ids</summary>
        private static readonly Dictionary<Type, string> CachedServiceIds = new Dictionary<Type, string>();

        /// <summary>Internal settings cache</summary>
        private static readonly Dictionary<string, string> SettingsCache = new Dictionary<string, string>();

        /// <summary>
        /// Indicates whether configuration settings should be cached (default = yes)
        /// </summary>
        /// <value><c>true</c> if [cache settings]; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// When settings get cached, they are only retrieved from the configuration system
        /// the first time they are needed. Subsequent uses will be based on the cache, which improves performance.
        /// However, if settings are meant to change dynamically, caching needs to be turned off, otherwise
        /// new configuration settings will be ignored by the service client.
        /// </remarks>
        public static bool CacheSettings { get; set; } = true;

        /// <summary>
        /// Opens a channel to the service by means of a transparently generated proxy and then performs the provided action on that call.
        /// </summary>
        /// <typeparam name="TServiceType">Service contract (interface) to call.</typeparam>
        /// <param name="action">Code to run once the connection is established</param>
        /// <param name="restServiceUrl">Optional URL for configuration-less calling of REST services. (Note: The standard approach is to not use this parameter and configure the system for the call)</param>
        /// <example>
        /// ServiceClient.Call<ISearchService>(s => {
        ///     var response = s.Search(new SearchRequest { SearchText = "Hello World" });
        ///     if (response.Success)
        ///         Console.WriteLine(response.Results);
        /// });
        /// </ISearchService>
        /// </example>
        public static void Call<TServiceType>(Action<TServiceType> action, string restServiceUrl = null) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(restServiceUrl);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(restServiceUrl);
                    try
                    {
                        action(channel);
                    }
                    catch
                    {
                        // TODO: Do we need this for REST? -- AbortChannel(channel, ex2);
                    }
                }
            }
        }

        public static bool AutoRetryFailedCalls { get; set; } = false;

        /// <summary>Defines the delay (milliseconds) between auto-retry calls</summary>
        /// <remarks>The delay is defined in milliseconds (-1 = no delay). Note that the delay puts the thread to sleep, so this should not be done on foreground threads.</remarks>
        public static int AutoRetryDelay { get; set; } = -1;

        /// <summary>Defines the list of exception types for which to auto-retry calls</summary>
        public static List<Type> AutoRetryFailedCallsForExceptions { get; set; } // TODO: = new List<Type> { typeof (EndpointNotFoundException), typeof (ServerTooBusyException) };

        /// <summary>When set to true (non-default), service communication errors are forwarded to the logging mediator.</summary>
        public static bool LogCommunicationErrors { get; set; } = false;

        /// <summary>Determines whether a call needs to be auto-retried</summary>
        /// <param name="exception">The exception that caused the original failure.</param>
        /// <returns>True if the call should be retried</returns>
        private static bool MustRetryCall(Exception exception)
        {
            if (!AutoRetryFailedCalls) return false;
            if (AutoRetryFailedCallsForExceptions.Count == 0) return true;

            if (AutoRetryFailedCallsForExceptions.Any(exceptionType => exception.GetType() == exceptionType))
            {
                if (AutoRetryDelay > 0) Thread.Sleep(AutoRetryDelay);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a dedicated channel to a data contract.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="restUrl">Optional URL for a rest service call. (If not passed, will be retrieved from settings, which is the usual scenario)</param>
        /// <returns>Operations service</returns>
        /// <example>
        /// var service = ServiceClient.GetChannelDedicated&lt;IUserService&gt;();
        /// var result = service.GetUsers();
        /// ServiceClient.CloseChannel(service);
        /// </example>
        /// <remarks>
        /// Relies on service configurations to figure out which protocol (and so forth) to use for the desired service.
        /// Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!
        /// </remarks>
        public static TServiceType GetChannelDedicated<TServiceType>(string restUrl = null) where TServiceType : class
        {
            switch (GetProtocol<TServiceType>())
            {
#if FULLFRAMEWORK
                case Protocol.NetTcp:
                    var port = GetServicePort<TServiceType>();
                    var serviceId = GetServiceId<TServiceType>();
                    return GetChannelDedicated<TServiceType>(port, serviceId, protocol, GetMessageSize<TServiceType>());

                case Protocol.BasicHttp:
                case Protocol.WsHttp:
                    var serviceId2 = GetServiceId<TServiceType>();
                    return GetChannelDedicated<TServiceType>(serviceId2, protocol, GetMessageSize(serviceId2));
#endif

                case Protocol.InProcess:
                    return ServiceGardenLocal.GetService<TServiceType>();
                case Protocol.RestHttpJson:
                    var serviceId3 = GetServiceId<TServiceType>();
                    var restUri = string.IsNullOrEmpty(restUrl) ? new Uri(GetSetting($"RestServiceUrl:{serviceId3}")) : new Uri(restUrl);
                    var restHandler = new RestProxyHandler(restUri);
                    var proxy = TransparentProxyGenerator.GetProxy<TServiceType>(restHandler);
                    return proxy;
                default:
                    return default;
            }
        }

        /// <summary>
        /// Gets the protocol for the specified service.
        /// </summary>
        /// <typeparam name="TServiceType">Type of the service (contract).</typeparam>
        /// <returns>Protocol.</returns>
        private static Protocol GetProtocol<TServiceType>()
        {
            var serviceType = typeof(TServiceType);
            var interfaceName = serviceType.Name;
            var key = "ServiceProtocol:" + interfaceName;

            lock (CachedProtocols)
                if (CacheSettings && CachedProtocols.ContainsKey(key))
                    return CachedProtocols[key];

            var protocolName = GetSetting(key).ToLower(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(protocolName)) protocolName = GetSetting("ServiceProtocol").ToLower(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(protocolName)) return Protocol.RestHttpJson; // This default has changed since the full framework implementation, where it was TCP/IP

            Protocol protocol;
            switch (protocolName)
            {
                case "inprocess":
                    protocol = Protocol.InProcess;
                    break;
                case "wshttp":
                    protocol = Protocol.WsHttp;
                    break;
                case "basichttp":
                    protocol = Protocol.BasicHttp;
                    break;
                case "rest":
                case "restjson":
                case "resthttpjson":
                    protocol = Protocol.RestHttpJson;
                    break;
                case "restxml":
                case "resthttpxml":
                    protocol = Protocol.RestHttpXml;
                    break;
                default:
                    protocol = Protocol.NetTcp;
                    break;
            }

            if (CacheSettings)
                lock (CachedProtocols)
                    if (CachedProtocols.ContainsKey(key))
                        CachedProtocols[key] = protocol;
                    else
                        CachedProtocols.Add(key, protocol);

            return protocol;
        }

        /// <summary>
        /// Gets the allowable message size for a specific interface
        /// </summary>
        /// <typeparam name="TServiceType">The type of the T service type.</typeparam>
        /// <returns>MessageSize.</returns>
        private static MessageSize GetMessageSize<TServiceType>() => GetMessageSize(typeof(TServiceType).Name);

        /// <summary>
        /// Gets the allowable message size for a specific interface
        /// </summary>
        /// <param name="interfaceName">Name of the interface.</param>
        /// <returns>MessageSize.</returns>
        private static MessageSize GetMessageSize(string interfaceName)
        {
            var key = "ServiceMessageSize:" + interfaceName;
            lock (CachedMessageSizes)
                if (CacheSettings && CachedMessageSizes.ContainsKey(key))
                    return CachedMessageSizes[key];

            var messageSize = GetSetting(key).ToLower(CultureInfo.InvariantCulture);
            var size = MessageSize.Medium;

            switch (messageSize)
            {
                case "large":
                    size = MessageSize.Large;
                    break;
                case "normal":
                    size = MessageSize.Normal;
                    break;
                case "verylarge":
                    size = MessageSize.VeryLarge;
                    break;
                case "max":
                    size = MessageSize.Max;
                    break;
            }

            if (CacheSettings)
                lock (CachedMessageSizes)
                    if (CachedMessageSizes.ContainsKey(key))
                        CachedMessageSizes[key] = size;
                    else
                        CachedMessageSizes.Add(key, size);

            return size;
        }

        /// <summary>
        /// Gets the service id.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <returns></returns>
        private static string GetServiceId<TServiceType>()
        {
            lock (CachedServiceIds)
                if (CacheSettings && CachedServiceIds.ContainsKey(typeof(TServiceType)))
                    return CachedServiceIds[typeof(TServiceType)];

            string serviceId;
            var contractType = typeof(TServiceType);
            if (contractType.IsInterface)
                serviceId = contractType.Name;
            else
            {
                var interfaces = contractType.GetInterfaces();
                if (interfaces.Length == 1)
                    serviceId = interfaces[0].Name;
                else if (interfaces.Length == 2)
                {
                    if (interfaces[0].FullName == "CODE.Framework.Services.Contracts.IServiceEvents")
                        serviceId = interfaces[1].Name;
                    else if (interfaces[1].FullName == "CODE.Framework.Services.Contracts.IServiceEvents")
                        serviceId= interfaces[0].Name;
                    else
                        throw new IndexOutOfBoundsException("Service information must be the service contract, not the service implementation type.");
                }
                else
                    throw new IndexOutOfBoundsException("Service information must be the service contract, not the service implementation type.");
            }

            if (CacheSettings)
                lock (CachedServiceIds)
                    if (CachedServiceIds.ContainsKey(typeof(TServiceType)))
                        CachedServiceIds[typeof(TServiceType)] = serviceId;
                    else
                        CachedServiceIds.Add(typeof(TServiceType), serviceId);

            return serviceId;
        }

        /// <summary>
        /// Retrieves a setting from the configuration system
        /// </summary>
        /// <param name="setting">Name of the setting.</param>
        /// <param name="ignoreCache">If set to <c>true</c> setting caching is ignored.</param>
        /// <param name="defaultValue">Default value in case the setting is not found</param>
        /// <returns>Setting value</returns>
        private static string GetSetting(string setting, bool ignoreCache = false, string defaultValue = "")
        {
            var settingValue = defaultValue;

            if (CacheSettings && !ignoreCache)
                lock (SettingsCache)
                    if (SettingsCache.ContainsKey(setting))
                        return SettingsCache[setting];

            if (ConfigurationSettings.Settings.IsSettingSupported(setting))
                settingValue = ConfigurationSettings.Settings[setting];

            if (CacheSettings && !ignoreCache)
                lock (SettingsCache)
                    if (SettingsCache.ContainsKey(setting))
                        SettingsCache[setting] = settingValue;
                    else
                        SettingsCache.Add(setting, settingValue);

            return settingValue;
        }
    }

    /// <summary>
    /// Communication Protocol
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// Net TCP
        /// </summary>
        NetTcp,
        /// <summary>
        /// Local in process service
        /// </summary>
        InProcess,
        /// <summary>
        /// Basic HTTP
        /// </summary>
        BasicHttp,
        /// <summary>
        /// WS HTTP
        /// </summary>
        WsHttp,
        /// <summary>
        /// XML Formatted REST over HTTP
        /// </summary>
        RestHttpXml,
        /// <summary>
        /// JSON Formatted REST over HTTP
        /// </summary>
        RestHttpJson
    }

    /// <summary>
    /// Message size
    /// </summary>
    public enum MessageSize
    {
        /// <summary>
        /// Normal (default message size as defined by WCF)
        /// </summary>
        Normal,
        /// <summary>
        /// Large (up to 100MB)
        /// </summary>
        Large,
        /// <summary>
        /// Medium (up to 10MB) - this is the default
        /// </summary>
        Medium,
        /// <summary>
        /// For internal use only
        /// </summary>
        Undefined,
        /// <summary>
        /// Very large (up to 1GB)
        /// </summary>
        VeryLarge,
        /// <summary>
        /// Maximum size (equal to int.MaxValue, about 2GB)
        /// </summary>
        Max
    }

    /// <summary>
    /// Exception class used for enumeration errors.
    /// The error is raised when an enumeration finds its enumeration source in disarray
    /// and thus overshoots the sources bounds
    /// </summary>
    [Serializable]
    public class IndexOutOfBoundsException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public IndexOutOfBoundsException() : base("Index out of bounds") { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public IndexOutOfBoundsException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public IndexOutOfBoundsException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected IndexOutOfBoundsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
