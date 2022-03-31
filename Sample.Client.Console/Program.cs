using System;
using System.Reflection;
using CODE.Framework.Fundamentals.Configuration;
using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Fundamentals.Utilities.CODE.Framework.Core.Utilities;
using CODE.Framework.Services.Client;
using Sample.Contracts;

namespace Sample.Client.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var originalColor = System.Console.ForegroundColor;

            //var markdown = "# Hello World\r\n\r\nThis _is_ a test :-)\r\n\r\ncodemag.com";
            //var html = MarkdownHelper.ToHtml(markdown);
            //var text = MarkupHelper.GetStrippedBodyOnly(html);

            //var implementation = TransparentProxyGenerator.GetProxy<ITest>(new MyProxyHandler());
            //var test = implementation.ToString();
            //var result = implementation.HelloWorld("Test");

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
            System.Console.WriteLine();
            System.Console.WriteLine("Press key to call ICustomerService.DateTest().\r");
            System.Console.ReadLine();

            ServiceClient.Call<ICustomerService>(c =>
            {
                try
                {
                    System.Console.WriteLine("Calling service....");
                    var response = c.DateTest(new DateTestRequest { FirstDate = DateTime.Now, SecondDate = DateTime.Now.AddYears(1) });
                    if (response.Success)
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                        System.Console.WriteLine("Search Test Result:");
                        System.Console.WriteLine($"First date returned: {response.FirstDateReturned}");
                        System.Console.WriteLine($"Second date returned: {response.SecondDateReturned}");
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
            System.Console.WriteLine("Press key to call ICustomerService.GetPhoto().\r");
            System.Console.ReadLine();

            ServiceClient.Call<ICustomerService>(s =>
            {
                System.Console.WriteLine("Calling service....");
                var response = s.GetPhoto(new GetPhotoRequest { CustomerId = "1" });
                System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                System.Console.WriteLine($"File Name: {response.FileName}");
                System.Console.WriteLine($"Content Type: {response.ContentType}");
                System.Console.WriteLine($"Content Length: {response.FileBytes.Length}");
            });

            System.Console.ForegroundColor = originalColor;
            System.Console.WriteLine();
            System.Console.WriteLine("Done.");
        }
    }

    public interface ITest
    {
        string HelloWorld(string test);
    }

    public class MyProxyHandler : IProxyHandler
    {
        public object OnMethod(MethodInfo method, object[] args)
        {
            System.Console.WriteLine("Method called: " + method.Name);
            return Activator.CreateInstance(method.ReturnType);
        }
    }
}
