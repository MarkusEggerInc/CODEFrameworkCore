﻿using System;
using CODE.Framework.Fundamentals.Configuration;
using CODE.Framework.Services.Client;
using Sample.Contracts;

namespace Sample.Client.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var originalColor = System.Console.ForegroundColor;

            ConfigurationSettings.Sources["Memory"].Settings["RestServiceUrl:ICustomerService"] = "http://localhost:5008/api/customers";
            ConfigurationSettings.Sources["Memory"].Settings["RestServiceUrl:IUserService"] = "http://localhost:5008/api/users";

            System.Console.WriteLine("CODE Framework Service Example Test Client.\r");
            System.Console.WriteLine("Press key to call ICustomerService.GetCustomers().\r");
            System.Console.ReadLine();

            ServiceClient.Call<ICustomerService>(c =>
            {
                try
                {
                    System.Console.WriteLine("Calling service....");
                    var response = c.GetCustomers(new GetCustomersRequest());
                    if (response.Success)
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                        System.Console.WriteLine("Customers Retrieved:\r");
                        foreach (var customer in response.CustomerList)
                            System.Console.WriteLine($"Customer: {customer.Name} - Company: {customer.Company}");
                    }
                    else
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkBlue;
                        System.Console.WriteLine($"Service call returned Success = false. Failure Information: {response.FailureInformation}\r");
                    }
                }
                catch (Exception e)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(e);
                    throw;
                }
            });

            System.Console.ForegroundColor = originalColor;
            System.Console.WriteLine();
            System.Console.WriteLine("Press key to call ICustomerService.SearchTest().\r");
            System.Console.ReadLine();

            ServiceClient.Call<ICustomerService>(c =>
            {
                try
                {
                    System.Console.WriteLine("Calling service....");
                    var response = c.SearchTest(new SearchTestRequest {SearchString = "Example Search String", IncludeInactive = true});
                    if (response.Success)
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                        System.Console.WriteLine("Search Test Result:");
                        System.Console.WriteLine($"Search string used: {response.SearchStringUsed}");
                        System.Console.WriteLine($"Inactive Included: {response.InactivesAreIncluded}");
                    }
                    else
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkBlue;
                        System.Console.WriteLine($"Service call returned Success = false. Failure Information: {response.FailureInformation}\r");
                    }
                }
                catch (Exception e)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(e);
                    throw;
                }
            });

            System.Console.ForegroundColor = originalColor;
            System.Console.WriteLine("Done.");
        }
    }
}
