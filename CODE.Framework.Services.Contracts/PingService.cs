using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Services.Contracts
{
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
