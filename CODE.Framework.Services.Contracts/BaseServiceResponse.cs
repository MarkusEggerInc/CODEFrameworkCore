using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Services.Contracts
{
    /// <summary>
    /// Standard CODE Framework service response object that returns a
    /// standard response that wraps the result along with status information.
    /// </summary>
    [DataContract]
    public class BaseServiceResponse
    {
        /// <summary>
        /// Gets or sets the success.
        /// </summary>
        /// <value>The success.</value>
        [DataMember(IsRequired = true)]
        public bool Success { get; set; } = false;

        [DataMember]
        public string FailureInformation { get; set; } = string.Empty;
    }

    [DataContract]
    public class BaseServiceRequest { }

    [DataContract]
    public class ErrorResponse : BaseServiceResponse
    {
        [DataMember]
        public string StackTrace { get; set; }

        [DataMember]
        public string Source { get; set; }

        [DataMember]
        public string ExceptionType { get; set; }

        public ErrorResponse() => Success = false;

        public ErrorResponse(Exception ex)
        {
            ex = ex.GetBaseException();
            Success = false;
            FailureInformation = ex.Message;
#if DEBUG
            StackTrace = ex.StackTrace;
            Source = ex.Source;
            ExceptionType = ex.GetType().ToString();
#endif
        }

        public ErrorResponse(string errorMessage)
        {
            Success = false;
            FailureInformation = errorMessage;
        }
    }
}