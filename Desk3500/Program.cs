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

            // var docNo = $"{DateTime.Now.Date.Ticks}";
            var docNo = $"{DateTime.Now.Ticks}";
            var amount = 250;

            // API-ის ინიცირება
            var desk3500Api = new Desk3500Api(baseUrl, licenseToken, alias, userName, password);
            
            try
            {
                var operatorId = "Cashier5-BOG-v3.4.5";

                // 1. POS_თან კავშირის დამყარება 638633376000000000
                await desk3500Api.OpenPos();

                //// 1.კავშირის დასასრული
                //await desk3500Api.CloseDay(operatorId);
                //await desk3500Api.WaitForDayCloseEvent();
                //return;

                Console.WriteLine();

                //await desk3500Api.UnlockAndWaitForCard(100, operatorId);

                await desk3500Api.UnlockDevice(amount, operatorId);

                await desk3500Api.WaitForCardEvent();

                Console.WriteLine();

                // 2. გადახდის ავტორიზაცია (თანხა არის თეთრებში. 1000 ნიშნავს 10.00 ლარს)
                await desk3500Api.AuthorizePayment(amount, docNo);

                var tranResult = await desk3500Api.WaitForCardEventResponse();

                Console.WriteLine();

                // await Task.Delay(3000);

                // 3. სტატუსის გადამოწმება (optional)
               //  var tranStatus = await desk3500Api.GetTransactionStatus(documentNr: docNo);

                await desk3500Api.CloseDoc(docNo);

                Console.WriteLine();

                // 4. უკან დაბრუნება
                // await desk3500Api.RefundPayment(1000, "123456", "123456", "933315462707");

                // 5. ავტორიზებული ტრანზაქციის გაუქმება, რომელიც არ იყო დასრულებული
                // await desk3500Api.VoidPayment("A0000000041010");

                // 6. კავშირის დასასრული
                await desk3500Api.LockDevice();

                // 7. კავშირის დასასრული
                await desk3500Api.ClosePos();

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
