
    // პარამეტრები
    var baseUrl = "https://your-terminal-url";

    var licenseToken = "your-license-token";

    var alias = "POS1";

    var userName = "your-username";

    var password = "your-password";

    // API-ის ინიცირება
    var desk3500Api = new Desk3500Api(baseUrl, licenseToken, alias, userName, password);

    try
    {
    

        // 1. POS_თან კავშირის დამყარება
        await desk3500Api.OpenPos();

        // 2. გადახდის ავტორიზაცია (თანხა არის თეთრებში. 1000 ნიშნავს 10.00 ლარს)
        await desk3500Api.AuthorizePayment(1000, "123456");

        // 3. სტატუსის გადამოწმება (optional)
        await desk3500Api.GetTransactionStatus(documentNr: "123456");

        // 4. უკან დაბრუნება
        // await desk3500Api.RefundPayment(1000, "123456", "123456", "933315462707");

        // 5. ავტორიზებული ტრანზაქციის გაუქმება, რომელიც არ იყო დასრულებული
        // await desk3500Api.VoidPayment("A0000000041010");

        // 6. კავშირის დასასრული
        await desk3500Api.ClosePos();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }
    
