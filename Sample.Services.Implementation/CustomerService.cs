using CODE.Framework.Services.Contracts;
using Sample.Contracts;
using System;

namespace Sample.Services.Implementation
{
    public class CustomerService : ICustomerService, IServiceEvents
    {
        public PingResponse Ping(PingRequest request) => this.GetPopulatedPingResponse();

        public GetCustomersResponse GetCustomers(GetCustomersRequest request)
        {
            try
            {
                var response = new GetCustomersResponse();

                // Real code goes here...
                response.CustomerList.Add(new Customer {Name = "Rick Strahl", Company = "West wind"});
                response.CustomerList.Add(new Customer {Name = "Markus Egger", Company = "EPS Software Corp."});

                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<GetCustomersResponse>(ex);
            }
        }

        public SearchTestResponse SearchTest(SearchTestRequest request)
        {
            try
            {
                return new SearchTestResponse
                {
                    SearchStringUsed = request.SearchString,
                    InactivesAreIncluded = request.IncludeInactive,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<SearchTestResponse>(ex);
            }
        }

        public GetCustomerResponse GetCustomer(GetCustomerRequest request)
        {
            try
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
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<GetCustomerResponse>(ex);
            }

        }

        public void OnInProcessHostLaunched()
        {
        }
    }
}
