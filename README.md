            // პარამეტრები
            var baseUrl = "http://localhost:6678";
            var licenseToken = "your-license-token";
            var alias = "POS1";
            var userName = "your-username";
            var password = "your-password";

            var docNo = $"{DateTime.Now.Year}{DateTime.Now.Month}{DateTime.Now.Day}";

            // API-ის ინიცირება
            var desk3500Api = new Desk3500Api(baseUrl, licenseToken, alias, userName, password);
            
            try
            {
                var operatorId = "Cashier-5";

                // 1. POS_თან კავშირის დამყარება
                await desk3500Api.OpenPos();

                // 1. კავშირის დასასრული
                //await desk3500Api.CloseDay(operatorId);
                //return;

                Console.WriteLine();

                await desk3500Api.UnlockAndWaitForCard(100, operatorId);

                Console.WriteLine();

                // 2. გადახდის ავტორიზაცია (თანხა არის თეთრებში. 1000 ნიშნავს 10.00 ლარს)
                await desk3500Api.AuthorizePayment(100, docNo);

                Console.WriteLine();

                // 3. სტატუსის გადამოწმება (optional)
                await desk3500Api.GetTransactionStatus(documentNr: docNo);

                var tranStatus = await desk3500Api.WaitForCardEventResponse();

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
