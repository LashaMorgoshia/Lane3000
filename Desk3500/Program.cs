using System;
using System.Threading.Tasks;

namespace Desk3500
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // პარამეტრები
            var baseUrl = "http://localhost:6678";
            var licenseToken = "your-license-token";
            var alias = "POS1";
            var userName = "your-username";
            var password = "your-password";

            var docNo = $"S-ORD{DateTime.Now.Ticks.ToString().Substring(0, 7)}";

            // API-ის ინიცირება
            var desk3500Api = new Desk3500Api(baseUrl, licenseToken, alias, userName, password);
            
            try
            {
                // 1. POS_თან კავშირის დამყარება
                await desk3500Api.OpenPos();

                Console.WriteLine();

                await desk3500Api.UnlockAndWaitForCard(100, "Cashier-5");

                Console.WriteLine();

                // 2. გადახდის ავტორიზაცია (თანხა არის თეთრებში. 1000 ნიშნავს 10.00 ლარს)
                await desk3500Api.AuthorizePayment(100, docNo);

                Console.WriteLine();

                // 3. სტატუსის გადამოწმება (optional)
                await desk3500Api.GetTransactionStatus(documentNr: docNo);

                var tranStatus = await desk3500Api.WaitForCardEventResponse();

                //if (tranStatus != null && tranStatus.Properties.State == "Declined")
                //{
                //    Console.WriteLine();

                //}

                await desk3500Api.CloseDoc(docNo);

                await Task.Delay(5000);

                Console.WriteLine();

                // 4. უკან დაბრუნება
                // await desk3500Api.RefundPayment(1000, "123456", "123456", "933315462707");

                // 5. ავტორიზებული ტრანზაქციის გაუქმება, რომელიც არ იყო დასრულებული
                // await desk3500Api.VoidPayment("A0000000041010");

                // 6. კავშირის დასასრული
                await desk3500Api.ClosePos();

                // 6. კავშირის დასასრული
                await desk3500Api.LockDevice();

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
