using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Services.Contracts
{
    public interface IPingService
    {
        PingResponse Ping(PingRequest request);
    }

    [DataContract]
    public class PingResponse : BaseServiceResponse
    {
        [DataMember(IsRequired = true)]
        public DateTime ServerDateTime { get; set; } = DateTime.MinValue;

        [DataMember(IsRequired = true)]
        public string Version { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string OperatingSystemDescription { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string FrameworkDescription { get; set; } = string.Empty;
    }

    [DataContract]
    public class PingRequest : BaseServiceRequest
    {
    }
}
