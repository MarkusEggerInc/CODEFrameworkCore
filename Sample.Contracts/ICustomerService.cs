using System.Collections.Generic;
using System.Runtime.Serialization;
using CODE.Framework.Services.Contracts;

namespace Sample.Contracts
{
    public interface ICustomerService
    {
        [Rest(Method = RestMethods.Get, Name ="Customer", AuthorizationRoles = "Administrators")]
        GetCustomerResponse GetCustomer(GetCustomerRequest request);

        [Rest(Method = RestMethods.Get, Name = "")]
        GetCustomersResponse GetCustomers(GetCustomersRequest request);

        [Rest(Method = RestMethods.Get, Name = "Search")]
        SearchTestResponse SearchTest(SearchTestRequest request);

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
        [DataMember, RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public string Id { get; set; }
    }

    [DataContract]
    public class GetCustomersResponse : BaseServiceResponse
    {
        [DataMember]
        public List<Customer> CustomerList { get; set; }
    }

    [DataContract]
    public class GetCustomerResponse : BaseServiceResponse
    {
        [DataMember]
        public Customer Customer { get; set; }
    }

    public class Customer
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public string Id { get; set; }
    }
}
