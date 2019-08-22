using CODE.Framework.Services.Contracts;
using Sample.Contracts;
using System.Collections.Generic;

namespace Sample.Services.Implementation
{
    public class CustomerService : ICustomerService, IServiceEvents
    {
        public GetCustomersResponse GetCustomers(GetCustomersRequest request)
        {
            return new GetCustomersResponse
            {
                CustomerList = new List<Customer>
                {
                    new Customer {Name = "Rick Strahl", Company = "West wind"},
                    new Customer {Name = "Markus Egger", Company = "EPS Software Corp."}
                }
            };
        }

        public SearchTestResponse SearchTest(SearchTestRequest request)
        {
            return new SearchTestResponse
            {
                SearchStringUsed = request.SearchString,
                InactivesAreIncluded = request.IncludeInactive,
                Success = true
            };
        }

        public GetCustomerResponse GetCustomer(GetCustomerRequest request)
        {
            // This is possible, but we would have to reference the AspNetCore package
            //var user = this.GetCurrentPrincipal();
            //var isValid = user.IsInRole("Administrators");

            var result = new GetCustomerResponse
            {
                Customer = new Customer {Id = request.Id, Name = "Rick Strahl", Company = "West wind"}
            };

            return result;
        }

        public void OnInProcessHostLaunched()
        {
        }
    }
}
