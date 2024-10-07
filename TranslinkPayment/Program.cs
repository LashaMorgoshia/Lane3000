using System;
using System.Threading.Tasks;

namespace TranslinkPayment
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the payment service
            var paymentService = new TranslinkPaymentService();

            // Initialize the test class with the service
            var paymentTests = new TranslinkPaymentTests(paymentService);

            // Run the tests
            await paymentTests.RunTests();

            Console.WriteLine("All tests completed.");
        }
    }
}
