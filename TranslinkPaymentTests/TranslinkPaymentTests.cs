using static TranslinkPaymentTests;

public class TranslinkPaymentTests : IClassFixture<TranslinkPaymentServiceFixture>
{
    private readonly TranslinkPaymentService _paymentService;

    // Store operation IDs from transactions for use in subsequent tests
    private string _test01OperationId;
    private string _pos => "POS1";
    private string _docNo { get; set; } = DateTime.Now.Ticks.ToString();

    private string _operatorId = "C5";
    private string _operatorName = "Cashier";
    private string _currCode = "981";
    private string _panL4Digit = "9999";

    public TranslinkPaymentTests(TranslinkPaymentServiceFixture fixture)
    {
        _paymentService = fixture.PaymentService;
    }

    [Fact]
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

        Assert.True(true);
    }

    [Fact]
    public async Task Test01_Purchase()
    {
        // Arrange
        decimal amount = 9.99m;

        // Act
        await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);
        var response = await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Approved", response.Properties.State);
        _test01OperationId = response.Properties.OperationId; // Store operation ID for use in Test02
    }

    [Fact]
    public async Task Test02_ManualReversal()
    {
        // Arrange
        string operationId = _test01OperationId;

        // Act
        await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
        var response = await _paymentService.VoidTransactionAsync(operationId);
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Approved", response.Properties.State);
    }

    [Fact]
    public async Task Test03_DeclinedTransaction()
    {
        // Arrange
        decimal amount = 6.51m;
        
        // Act & Assert
        await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
        });

        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // T04: Automatic Reversal
    [Fact]
    public async Task Test04_AutomaticReversal()
    {
        // Arrange
        decimal amount = 6.51m;

        // Act
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName); // Amount: 6.66 EUR
        var response = await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);  // Expected: Reversed
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Approved", response.Properties.State);
    }

    // T05: Purchase with Online PIN
    [Fact]
    public async Task Test05_PurchaseWithOnlinePIN()
    {
        // Arrange
        decimal amount = 6.70m;

        // Console.WriteLine("Running Test T05 - Purchase with Online PIN...");
        await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName); // Amount: 6.70 EUR
        await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);  // Terminal will prompt for PIN
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // T06: Declined Transaction (Online Declined)
    [Fact]
    public async Task Test06_DeclinedTransaction()
    {
        // Arrange
        decimal amount = 6.55m;
        _docNo = DateTime.Now.Ticks.ToString();

        //Console.WriteLine("Running Test T06 - Declined Transaction...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName); // Amount: 6.55 EUR
        await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);  // Expected: Declined
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // T07: Refund
    [Fact]
    public async Task Test07_Refund()
    {
        Console.WriteLine("Running Test T07 - Refund...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.RefundTransactionAsync(9.99m, "T07", "978", "9999");  // Amount: 9.99 EUR
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // T08: Purchase using QR Code
    [Fact]
    public async Task Test08_QRPayment()
    {
        Console.WriteLine("Running Test T08 - QR Payment...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(9.99m, "978", "operatorId", "operatorName"); // Amount: 9.99 EUR
        await _paymentService.AuthorizeTransactionAsync(9.99m, "T08", "978", "9999");  // QR Payment
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // T09: End of Day
    [Fact]
    public async Task Test09_EndOfDay()
    {
        Console.WriteLine("Running Test T09 - End of Day...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.CloseDayAsync();  // Perform End of Day operation
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // T10: Send Software Version
    [Fact]
    public async Task Test10_SendSoftwareVersion()
    {
        Console.WriteLine("Running Test T10 - Send Software Version...");
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(0, "978", "operatorId", "operatorName");  // No amount needed
        await _paymentService.SendSoftwareVersionAsync("BDX-BOG-v1.0");
        await _paymentService.LockDeviceAsync();
        await _paymentService.ClosePosAsync();
    }

    // Fixture to initialize the payment service
    public class TranslinkPaymentServiceFixture
    {
        public TranslinkPaymentService PaymentService { get; private set; }

        public TranslinkPaymentServiceFixture()
        {
            var httpClient = new HttpClient();
            string apiBaseUrl = "http://localhost:6678/v105"; // Or retrieve from config
            PaymentService = new TranslinkPaymentService(httpClient, apiBaseUrl);
        }
    }
}

