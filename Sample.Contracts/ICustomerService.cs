using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using CODE.Framework.Services.Contracts;

namespace Sample.Contracts
{
    /// <summary>
    /// Service operations related to customers
    /// </summary>
    [ServiceContract]
    public interface ICustomerService
    {
        /// <summary>
        /// Implements a standard Ping operation used to see if the services is running, responsive, and some basic information about the service.
        /// </summary>
        /// <remarks>
        /// This method returns information about the server, the contract version, the runtime, and operating system. **It should only be exposed where this does not create security concerns.**
        /// </remarks>
        [OperationContract, Rest(Method = RestMethods.Get)]
        PingResponse Ping(PingRequest request);

        /// <summary>
        /// Retrieves a single customer
        /// </summary>
        [OperationContract, Rest(Method = RestMethods.Get, Name ="Customer", AuthorizationRoles = "Administrators")]
        GetCustomerResponse GetCustomer(GetCustomerRequest request);

        /// <summary>
        /// Retrieves a list of customers
        /// </summary>
        [OperationContract, Rest(Method = RestMethods.Get, Name = "")]
        GetCustomersResponse GetCustomers(GetCustomersRequest request);

        /// <summary>
        /// Example of searching for customers
        /// </summary>
        [OperationContract, Rest(Method = RestMethods.Get, Name = "Search")]
        SearchTestResponse SearchTest(SearchTestRequest request);

        /// <summary>
        /// Test method to demonstrate date handling
        /// </summary>
        [OperationContract, Rest(Method = RestMethods.Get), Obsolete("Flagged as obsolete for test purposes.")]
        DateTestResponse DateTest(DateTestRequest request);

        /// <summary>
        /// Returns the photo of a customer
        /// </summary>
        [OperationContract, Rest(Method = RestMethods.Get, Name = "Photo"), RestContentType("image/png")]
        FileResponse GetPhoto(GetPhotoRequest request);

        /// <summary>
        /// Example method using enums
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [OperationContract, Rest(Method = RestMethods.Post, Name = "Status")]
        GetStatusResponse GetStatus(GetStatusRequest request);

        [OperationContract, Rest(Method = RestMethods.Put, Name = "Upload")]
        FileResponse UploadCustomerFile(UploadCustomerFileRequest request);
    }

    [DataContract]
    public class UploadCustomerFileRequest : FileRequest
    {
        [DataMember]
        public string CustomerId { get; set; } = string.Empty;

        [DataMember]
        public string FileDescription { get; set; } = string.Empty;
    }

    [DataContract]
    public class GetStatusResponse : BaseServiceResponse
    {
        [DataMember()]
        public CustomerStatus Status { get; set; } = CustomerStatus.Normal;
    }


    [DataContract]
    public class GetStatusRequest
    {
        [DataMember()]
        public CustomerStatus Status { get; set; } = CustomerStatus.Normal;
    }

    /// <summary>
    /// Flag inticating the significance status of a customer
    /// </summary>
    public enum CustomerStatus
    {
        /// <summary>
        /// Normal customers
        /// </summary>
        Normal = 10,

        /// <summary>
        /// Pretty important customers
        /// </summary>
        Important = 25,

        /// <summary>
        /// These customers are the bee's knees!
        /// </summary>
        Premium = 100
    }

    [DataContract]
    public class GetPhotoRequest
    {
        [DataMember(IsRequired = true), RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public string CustomerId { get; set; } = string.Empty;
    }

    [DataContract]
    public class DateTestRequest
    {
        [DataMember(IsRequired = true), RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public DateTime FirstDate { get; set; } = DateTime.MinValue;

        [DataMember(IsRequired = true), RestUrlParameter(Mode = UrlParameterMode.Named)]
        public DateTime SecondDate { get; set; } = DateTime.MinValue;
    }

    [DataContract, Obsolete("Flagged obsolete for test purposes only.")]
    public class DateTestResponse : BaseServiceResponse
    {
        [DataMember(IsRequired = true)]
        public DateTime FirstDateReturned { get; set; } = DateTime.MinValue;

        [DataMember(IsRequired = true), Description("This once was an awesome date."), Obsolete("Flagged obsolete for test purposes only.")]
        public DateTime? SecondDateReturned { get; set; } = DateTime.MinValue;
    }

    [DataContract]
    public class SearchTestRequest
    {
        [DataMember, RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public string SearchString { get; set; } = string.Empty;

        [DataMember, RestUrlParameter(Mode = UrlParameterMode.Named)]
        public bool IncludeInactive { get; set; } = false;
    }

    [DataContract]
    public class SearchTestResponse : BaseServiceResponse
    {
        [DataMember]
        public string SearchStringUsed { get; set; } = string.Empty;

        [DataMember]
        public bool InactivesAreIncluded { get; set; } = false;
    }

    [DataContract]
    public class GetCustomersRequest { }

    [DataContract]
    public class GetCustomerRequest : BaseServiceRequest
    {
        /// <summary>
        /// Customer ID (free-form string value)
        /// </summary>
        [DataMember, RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public string Id { get; set; }
    }

    [DataContract]
    public class GetCustomersResponse : BaseServiceResponse
    {
        [DataMember]
        //public List<Customer> CustomerList { get; set; } = new List<Customer>();
        public Customer[] CustomerList { get; set; }
    }

    [DataContract]
    public class GetCustomerResponse : BaseServiceResponse
    {
        /// <summary>
        /// Individual customer
        /// </summary>
        [DataMember]
        public Customer Customer { get; set; } = new Customer();
    }

    /// <summary>
    /// Customer structure used by this service
    /// </summary>
    [DataContract]
    public class Customer
    {
        /// <summary>
        /// Customer full name (first + last)
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Customer company name
        /// </summary>
        [DataMember]
        public string Company { get; set; }

        /// <summary>
        /// Customer ID (free-form text)
        /// </summary>
        [DataMember]
        public string Id { get; set; }
    }
}
