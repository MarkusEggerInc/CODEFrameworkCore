using CODE.Framework.Services.Contracts;
using Sample.Contracts;
using Sample.Services.Implementation.Properties;
using System;

namespace Sample.Services.Implementation
{
    public class CustomerService : ICustomerService, IServiceEvents
    {
        public PingResponse Ping(PingRequest request) => this.GetPopulatedPingResponse();

        public DateTestResponse DateTest(DateTestRequest request)
        {
            try
            {
                return new DateTestResponse
                {
                    Success = true,
                    FirstDateReturned = request.FirstDate,
                    SecondDateReturned = request.SecondDate
                };
            }
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<DateTestResponse>(ex);
            }
        }

        public GetCustomersResponse GetCustomers(GetCustomersRequest request)
        {
            try
            {
                var response = new GetCustomersResponse();

                // Real code goes here...
                //response.CustomerList.Add(new Customer { Name = "Markus Egger", Company = "CODE" });
                //response.CustomerList.Add(new Customer { Name = "Ellen Whitney", Company = "CODE" });
                //response.CustomerList.Add(new Customer { Name = "Mike Yeager", Company = "CODE" });
                //response.CustomerList.Add(new Customer { Name = "Otto Dobretsberger", Company = "CODE" });

                response.CustomerList = new Customer[] {
                    new Customer { Name = "Markus Egger", Company = "CODE" },
                    new Customer { Name = "Ellen Whitney", Company = "CODE" },
                    new Customer { Name = "Mike Yeager", Company = "CODE" },
                    new Customer { Name = "Otto Dobretsberger", Company = "CODE" }
                };

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
                    Customer = new Customer {Id = request.Id, Name = "Markus Egger", Company = "CODE"},
                    Success = true
                };
                return result;
            }
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<GetCustomerResponse>(ex);
            }

        }

        public FileResponse GetPhoto(GetPhotoRequest request)
        {
            try
            {
                return new FileResponse
                {
                    ContentType = "image/png",
                    FileName = "ExampleImage.png",
                    FileBytes = Resources.RocketMan
                };
            }
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<FileResponse>(ex);
            }
        }

        public void OnInProcessHostLaunched()
        {
        }

        public GetStatusResponse GetStatus(GetStatusRequest request) => new GetStatusResponse { Status = request.Status, Success = true };

        public FileResponse UploadCustomerFile(UploadCustomerFileRequest request) => new FileResponse { ContentType = request.ContentType, FileBytes = request.FileBytes, FileName = request.FileName };
    }
}
