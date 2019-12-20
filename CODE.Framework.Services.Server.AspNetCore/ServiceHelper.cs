using System;
using System.Reflection;
using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Services.Contracts;

namespace CODE.Framework.Services.Server.AspNetCore
{
    static class ServiceHelper
    {
        public static PingResponse GetPopulatedPingResponse(this object referenceObject)
        {
            try
            {
                return new PingResponse
                {
                    ServerDateTime = DateTime.Now, 
                    Version = referenceObject?.GetType().Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                    OperatingSystemDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
                };
            }
            catch (Exception ex)
            {
                LoggingMediator.Log(ex);
                return new PingResponse
                {
                    Success = false,
                    FailureInformation = "PingService::GetPopulatedPingResponse() - generic error."
                };
            }
        }
    }
}
