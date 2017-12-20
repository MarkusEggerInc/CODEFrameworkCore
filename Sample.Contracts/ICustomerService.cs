using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CODE.Framework.Services.Contracts;
using CODE.Framework.Services.Server.AspNetCore;

namespace Sample.Contracts
{
    public interface ICustomerService
    {
        [Rest(Method = RestMethods.Post, Name ="Customer", Route = "{id:guid}")]
        Task<GetCustomerResponse> GetCustomer(GetCustomerRequest request);

        [Rest(Method = RestMethods.Get, Name = "", Route = "")]
        GetCustomersResponse GetCustomers();
    }


    [DataContract]
    public class GetCustomerRequest : BaseServiceRequest
    {
        [DataMember]
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
