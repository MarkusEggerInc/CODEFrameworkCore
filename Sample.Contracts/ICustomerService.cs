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
        [Rest(Method = RestMethods.Get, Name ="Customer", AuthorizationRoles = "Administrators")]
        GetCustomerResponse GetCustomer(GetCustomerRequest request);

        /// <summary>
        /// Retrieves a list of customers
        /// </summary>
        [Rest(Method = RestMethods.Get, Name = "")]
        GetCustomersResponse GetCustomers(GetCustomersRequest request);

        /// <summary>
        /// Example of searching for customers
        /// </summary>
        [Rest(Method = RestMethods.Get, Name = "Search")]
        SearchTestResponse SearchTest(SearchTestRequest request);

        /// <summary>
        /// Test method to demonstrate date handling
        /// </summary>
        [Rest(Method = RestMethods.Get)]
        DateTestResponse DateTest(DateTestRequest request);
    }

    [DataContract]
    public class DateTestRequest
    {
        [DataMember(IsRequired = true), RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public DateTime FirstDate { get; set; } = DateTime.MinValue;

        [DataMember(IsRequired = true), RestUrlParameter(Mode = UrlParameterMode.Named)]
        public DateTime SecondDate { get; set; } = DateTime.MinValue;
    }

    [DataContract]
    public class DateTestResponse : BaseServiceResponse
    {
        [DataMember(IsRequired = true)]
        public DateTime FirstDateReturned { get; set; } = DateTime.MinValue;

        [DataMember(IsRequired = true)]
        public DateTime SecondDateReturned { get; set; } = DateTime.MinValue;
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
