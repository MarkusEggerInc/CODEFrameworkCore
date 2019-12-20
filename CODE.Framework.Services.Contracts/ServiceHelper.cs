using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CODE.Framework.Services.Contracts
{
    public static class ServiceHelper
    {
        public static PingResponse GetPopulatedPingResponse(this object referenceObject)
        {
            try
            {
                return new PingResponse
                {
                    ServerDateTime = DateTime.Now, 
                    Version = referenceObject?.GetType().Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                    OperatingSystemDescription = RuntimeInformation.OSDescription,
                    FrameworkDescription = RuntimeInformation.FrameworkDescription
                };
            }
            catch
            {
                return new PingResponse
                {
                    Success = false,
                    FailureInformation = "PingService::GetPopulatedPingResponse() - generic error."
                };
            }
        }
    }
}
