using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Services.Contracts;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Standard implementation for a REST Proxy handler
    /// </summary>
    public class RestProxyHandler : IProxyHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestProxyHandler"/> class.
        /// </summary>
        /// <param name="serviceUri">The service root URI.</param>
        public RestProxyHandler(Uri serviceUri) => _serviceUri = serviceUri;

        private readonly Uri _serviceUri;
        private Type _contractType;

        /// <summary>
        /// This method is called when any method on a proxied object is invoked.
        /// </summary>
        /// <param name="method">Information about the method being called.</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns>Result value from the proxy call</returns>
        public object OnMethod(MethodInfo method, object[] args)
        {
            if (args.Length != 1) throw new Exception("Only methods with one parameter can be used through REST proxies.");
            var data = args[0];

            if (_contractType == null)
            {
                var declaringType = method.DeclaringType;
                if (declaringType == null) throw new Exception("Can't determine declaring type of method '" + method.Name + "'.");
                if (declaringType.IsInterface)
                    _contractType = declaringType;
                else
                {
                    var interfaces = declaringType.GetInterfaces();
                    if (interfaces.Length != 1) throw new Exception("Can't determine declaring contract interface for method '" + method.Name + "'.");
                    _contractType = interfaces[0];
                }
            }

            var httpMethod = RestHelper.GetHttpMethodFromContract(method.Name, _contractType);
            var exposedMethodName = RestHelper.GetExposedMethodNameFromContract(method.Name, httpMethod, _contractType);
            var serviceUri = _serviceUri.AbsoluteUri.Trim();
            if (serviceUri.EndsWith("/")) serviceUri = serviceUri.Substring(0, serviceUri.Length - 1);
            var serviceUriAbsoluteUri = serviceUri + "/" + exposedMethodName;

            try
            {
                if (method.ReturnType.FullName == typeof(FileResponse).FullName)
                {
                    // File response from a REST/HTTP call is a special case, as it returns the raw file. 
                    // To make everything work transparently, we thus re-assembly a file response object here.
                    var fileResponseContent = new byte[0];
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json; charset=utf-8");
                        client.Encoding = Encoding.UTF8;
                        switch (httpMethod)
                        {
                            case "POST":
                                fileResponseContent = client.UploadData(serviceUriAbsoluteUri, Encoding.UTF8.GetBytes(JsonHelper.SerializeToRestJson(data)));
                                break;
                            case "GET":
                                var serializedData = RestHelper.SerializeToUrlParameters(data);
                                var serviceFullUrl = serviceUriAbsoluteUri + serializedData;
                                fileResponseContent = client.DownloadData(serviceFullUrl);
                                break;
                            default:
                                fileResponseContent = client.UploadData(serviceUriAbsoluteUri, httpMethod, Encoding.UTF8.GetBytes(JsonHelper.SerializeToRestJson(data)));
                                break;
                        }
                        var fileName = string.Empty;
                        if (client.ResponseHeaders["Content-Disposition"] != null)
                        {
                            fileName = client.ResponseHeaders["Content-Disposition"];
                            if (fileName.IndexOf("filename=\"") > -1)
                            {
                                fileName = fileName.Substring(fileName.IndexOf("filename=\"") + 10);
                                if (fileName.EndsWith("\""))
                                    fileName = fileName.Substring(0, fileName.Length - 1);
                            } 
                        }

                        var contentType = "application/x-binary";
                        if (client.ResponseHeaders["Content-Type"] != null)
                            contentType = client.ResponseHeaders["Content-Type"];

                        return new FileResponse
                        {
                            FileBytes = fileResponseContent,
                            ContentType = contentType,
                            FileName = fileName
                        };
                    }
                }
                else
                {
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json; charset=utf-8");
                        client.Encoding = Encoding.UTF8;
                        string restResponse;
                        switch (httpMethod)
                        {
                            case "POST":
                                restResponse = client.UploadString(serviceUriAbsoluteUri, JsonHelper.SerializeToRestJson(data));
                                break;
                            case "GET":
                                var serializedData = RestHelper.SerializeToUrlParameters(data);
                                var serviceFullUrl = serviceUriAbsoluteUri + serializedData;
                                restResponse = client.DownloadString(serviceFullUrl);
                                break;
                            default:
                                restResponse = client.UploadString(serviceUriAbsoluteUri, httpMethod, JsonHelper.SerializeToRestJson(data));
                                break;
                        }

                        return JsonHelper.DeserializeFromRestJson(restResponse, method.ReturnType);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ServiceClient.LogCommunicationErrors) LoggingMediator.Log($"Unable to communicate with service at endpoint '" + serviceUriAbsoluteUri + "' [" + httpMethod + "].\r\n\r\n", ex);
                throw new CommunicationException("Unable to communicate with REST service.", ex);
            }
        }
    }
    /// <summary>Represents a communication error in either the service or client application.</summary>
    [Serializable]
    public class CommunicationException : SystemException
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.ServiceModel.CommunicationException" /> class. </summary>
        public CommunicationException() { }

        /// <summary>Initializes a new instance of the <see cref="T:System.ServiceModel.CommunicationException" /> class, using the specified message.</summary>
        /// <param name="message">The description of the error condition.</param>
        public CommunicationException(string message) : base(message) { }

        /// <summary>Initializes a new instance of the <see cref="T:System.ServiceModel.CommunicationException" /> class, using the specified message and the inner exception.</summary>
        /// <param name="message">The description of the error condition.</param>
        /// <param name="innerException">The inner exception to be used.</param>
        public CommunicationException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Initializes a new instance of the <see cref="T:System.ServiceModel.CommunicationException" /> class, using the specified serialization information and context objects. </summary>
        /// <param name="info">Information relevant to the deserialization process.</param>
        /// <param name="context">The context of the deserialization process.</param>
        protected CommunicationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}