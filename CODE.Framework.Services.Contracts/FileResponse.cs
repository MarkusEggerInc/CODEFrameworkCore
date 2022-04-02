using System.Runtime.Serialization;

namespace CODE.Framework.Services.Contracts
{
    /// <summary>
    /// This response class can be used to return files to the caller.
    /// Depending on the protocol, this will be handled different from other responses. 
    /// For instance, REST calls actually turn this response into a raw file response.
    /// </summary>
    [Summary("Special response type used to return file content as the service payload.")]
    [Description("Using this content type (or a subclassed version) indicates the intent to return file content. Different service protocols will create different behavior for this. For instance, in REST APIs, the return value of this response is *not* serialized as JSON, but simply returned as a binary response instead. This response type is sealed, as some protocols handle this response type in a special way. This means that no additional members can be serialized, and thus subclassing makes no sense.")]
    public interface IFileResponse
    {
        [Description("File name (such as 'list.csv').")]
        string FileName { get; set; }

        [Description("Mime content type.")]
        string ContentType { get; set; }

        [Description("The file bytes (binary content of the file).")]
        byte[] FileBytes { get; set; }
    }

    /// <summary>
    /// This response class can be used to return files to the caller.
    /// Depending on the protocol, this will be handled different from other responses. 
    /// For instance, REST calls actually turn this response into a raw file response.
    /// </summary>
    /// <remarks>
    /// This response type is sealed, as some protocols handle this response type in a special way. 
    /// For instance, REST calls return a raw file response. 
    /// But this means that no additional members can be serialized, and thus subclassing makes no sense.
    /// </remarks>
    [DataContract]
    public sealed class FileResponse : IFileResponse
    {
        [DataMember(IsRequired = true)]
        public string FileName { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string ContentType { get; set; } = "application/x-binary";

        [DataMember(IsRequired = true), FileContent]
        public byte[] FileBytes { get; set; } = new byte[0];
    }

    [DataContract]
    public class FileRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        public string FileName { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string ContentType { get; set; } = "application/x-binary";

        [DataMember(IsRequired = true), FileContent]
        public byte[] FileBytes { get; set; } = new byte[0];
    }
}
