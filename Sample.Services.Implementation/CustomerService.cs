using CODE.Framework.Services.Contracts;
using Sample.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample.Services.Implementation
{
    public class CustomerService : ICustomerService
    {        

        public GetCustomersResponse GetCustomers()
        {
            return new GetCustomersResponse()
            {
                CustomerList = new List<Customer>() {
                    new Customer {
                         Name = "Rick Strahl",
                        Company = "West wind"
                    },
                    new Customer {
                         Name = "Markus Egger",
                        Company = "Eps Software"
                    },
                }
            };
        }

        public async Task<GetCustomerResponse> GetCustomer(GetCustomerRequest request)
        {
            var user = this.GetCurrentPrincipal();
            var isValid = user.IsInRole("Administrators");


            var result = new GetCustomerResponse()
            {
                Customer = new Customer() {
                    Id = request.Id,
                    Name = "Rick Strahl",
                    Company = "West wind"
                }                
            };

            return result;
        }

    }

}
