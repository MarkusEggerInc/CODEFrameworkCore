using System;
using CODE.Framework.Services.Client;
using Sample.Contracts;
using Sample.Services.Implementation;

namespace Sample.Client.ConsoleInProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceGardenLocal.AddServiceHost(typeof(CustomerService));

            ServiceClient.Call<ICustomerService>(s =>
            {
                var response = s.GetCustomers(new GetCustomersRequest());
                Console.WriteLine("Success: " + response.Success);
            });
        }
    }
}
