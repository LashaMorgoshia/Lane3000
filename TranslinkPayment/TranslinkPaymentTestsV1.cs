using System;
using System.Threading.Tasks;

public class TranslinkPaymentTestsV1
{
    private readonly TranslinkPaymentService _paymentService;

    public TranslinkPaymentTestsV1(TranslinkPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task RunTests()
    {
        await Test01_Purchase();
        await Test02_ManualReversal();
        await Test03_DeclinedTransaction();
        await Test04_AutomaticReversal();
        await Test05_PurchaseWithOnlinePIN();
        await Test06_DeclinedTransaction();
        await Test07_Refund();
        await Test08_QRPayment();
        await Test09_EndOfDay();
        await Test10_SendSoftwareVersion();
    }

    // T01: Purchase with Contactless Card
    public async Task Test01_Purchase()
    {
        Console.WriteLine("Running Test T01 - Purchase...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(9.99m, "978", "operatorId", "operatorName"); // Amount: 9.99 EUR
        await _paymentService.AuthorizeTransactionAsync(9.99m, "T01", "978", "9999");
        await _paymentService.ClosePosAsync();
    }

    // T02: Manual Reversal (Void) for the previous transaction
    public async Task Test02_ManualReversal()
    {
        Console.WriteLine("Running Test T02 - Manual Reversal...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.VoidTransactionAsync("T01", "A0000000041010");  // Void transaction for T01
        await _paymentService.ClosePosAsync();
    }

    // T03: Declined Transaction
    public async Task Test03_DeclinedTransaction()
    {
        Console.WriteLine("Running Test T03 - Declined Transaction...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(6.51m, "978", "operatorId", "operatorName"); // Amount: 6.51 EUR
        await _paymentService.AuthorizeTransactionAsync(6.51m, "T03", "978", "9999");  // Expected: Declined
        await _paymentService.ClosePosAsync();
    }

    // T04: Automatic Reversal
    public async Task Test04_AutomaticReversal()
    {
        Console.WriteLine("Running Test T04 - Automatic Reversal...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(6.66m, "978", "operatorId", "operatorName"); // Amount: 6.66 EUR
        await _paymentService.AuthorizeTransactionAsync(6.66m, "T04", "978", "9999");  // Expected: Reversed
        await _paymentService.ClosePosAsync();
    }

    // T05: Purchase with Online PIN
    public async Task Test05_PurchaseWithOnlinePIN()
    {
        Console.WriteLine("Running Test T05 - Purchase with Online PIN...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(6.70m, "978", "operatorId", "operatorName"); // Amount: 6.70 EUR
        await _paymentService.AuthorizeTransactionAsync(6.70m, "T05", "978", "9999");  // Terminal will prompt for PIN
        await _paymentService.ClosePosAsync();
    }

    // T06: Declined Transaction (Online Declined)
    public async Task Test06_DeclinedTransaction()
    {
        Console.WriteLine("Running Test T06 - Declined Transaction...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(6.55m, "978", "operatorId", "operatorName"); // Amount: 6.55 EUR
        await _paymentService.AuthorizeTransactionAsync(6.55m, "T06", "978", "9999");  // Expected: Declined
        await _paymentService.ClosePosAsync();
    }

    // T07: Refund
    public async Task Test07_Refund()
    {
        Console.WriteLine("Running Test T07 - Refund...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.RefundTransactionAsync(9.99m, "T07", "978", "9999");  // Amount: 9.99 EUR
        await _paymentService.ClosePosAsync();
    }

    // T08: Purchase using QR Code
    public async Task Test08_QRPayment()
    {
        Console.WriteLine("Running Test T08 - QR Payment...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(9.99m, "978", "operatorId", "operatorName"); // Amount: 9.99 EUR
        await _paymentService.AuthorizeTransactionAsync(9.99m, "T08", "978", "9999");  // QR Payment
        await _paymentService.ClosePosAsync();
    }

    // T09: End of Day
    public async Task Test09_EndOfDay()
    {
        Console.WriteLine("Running Test T09 - End of Day...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.CloseDayAsync();  // Perform End of Day operation
        await _paymentService.ClosePosAsync();
    }

    // T10: Send Software Version
    public async Task Test10_SendSoftwareVersion()
    {
        Console.WriteLine("Running Test T10 - Send Software Version...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(0, "978", "operatorId", "operatorName");  // No amount needed
        await _paymentService.SendSoftwareVersionAsync("YourSoftware-YourBank-v1.0");
        await _paymentService.ClosePosAsync();
    }
}
