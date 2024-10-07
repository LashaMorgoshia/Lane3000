using System;
using System.Threading.Tasks;

namespace Desk3500
{
    internal class Program
    {
        static Desk3500Api api;
        static string operatorId = "Cashier5-BOG-v3.4.5";
        static string docNo = $"{DateTime.Now.Ticks}";
        static string operationId { get; set; }

        static async Task Main(string[] args)
        {
            // პარამეტრები
            var baseUrl = "http://localhost:6678";
            var licenseToken = "your-license-token";
            var alias = "POS1";
            var userName = "your-username";
            var password = "your-password";

            // API-ის ინიცირება
            api = new Desk3500Api(baseUrl, licenseToken, alias, userName, password);
            
            try
            {
                // Init Token
                await Auth();

                // T01
                await Purchase(9.99m);

                // Dispose
                await LogOut();


                Console.WriteLine("payment completed.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// T01 - Purchase 9.99 - Ordinary Purchase - Approved
        /// </summary>
        /// <returns></returns>
        private static async Task Purchase(decimal amount)
        {
            // convert amount
            var amountInHundreds = (int)(amount * 100);

            // pay
            await Pay(amountInHundreds);

            // authorize
            await AuthorizeAmount(amountInHundreds);

            // get status
            var status = await GetStatus();
            operationId = status.Properties.OperationId;

            // close doc
            await CloseDoc();
        }

        /// <summary>
        /// auth and get token
        /// </summary>
        private static async Task Auth()
        {
            // 1. POS_თან კავშირის დამყარება 638633376000000000
            await api.OpenPos();
        }

        private static async Task DayClose()
        {
            // 1.კავშირის დასასრული
            await api.CloseDay(operatorId);
        }

        /// <summary>
        /// Unlock and wait for card tap
        /// </summary>
        /// <param name="amount"></param>
        private static async Task Pay(int amount)
        {
            await api.UnlockDevice(amount, operatorId);
            await api.WaitForCardEvent();
        }

        /// <summary>
        /// authorize amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private static async Task AuthorizeAmount(int amount)
        {
            // 2. გადახდის ავტორიზაცია (თანხა არის თეთრებში. 1000 ნიშნავს 10.00 ლარს)
            await api.AuthorizePayment(amount, docNo);
        }

        /// <summary>
        /// get trn status
        /// </summary>
        /// <returns></returns>
        private static async Task<TransactionStatus> GetStatus()
        {
            return await api.WaitForCardEventResponse();

            // 3. სტატუსის გადამოწმება (optional)
            //  var tranStatus = await desk3500Api.GetTransactionStatus(documentNr: docNo);
        }

        /// <summary>
        /// close doc and lock device
        /// </summary>
        /// <returns></returns>
        private static async Task CloseDoc()
        {
            // დოკუმენტის დახურვა
            await api.CloseDoc(docNo);
            // 6. კავშირის დასასრული
            await api.LockDevice();
        }

        private static async Task Refund(int amount)
        {
            // 4. უკან დაბრუნება
            await api.RefundPayment(amount, docNo, "123456", "933315462707");
        }

        private static async Task Void()
        {
            // 5. ავტორიზებული ტრანზაქციის გაუქმება, რომელიც არ იყო დასრულებული
            await api.VoidPayment(operationId);
        }

        /// <summary>
        /// diactivate token
        /// </summary>
        /// <returns></returns>
        private static async Task LogOut()
        {
            // 7. კავშირის დასასრული
            await api.ClosePos();
        }
    }
}
