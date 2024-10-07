using static TranslinkPaymentTests;

public class TranslinkPaymentTests : IClassFixture<TranslinkPaymentServiceFixture>
{
    private readonly TranslinkPaymentService _paymentService;

    // Store operation IDs from transactions for use in subsequent tests
    private string _test01OperationId;

    public TranslinkPaymentTests(TranslinkPaymentServiceFixture fixture)
    {
        _paymentService = fixture.PaymentService;
    }

    [Fact]
    public async Task Test01_Purchase()
    {
        // Arrange
        decimal amount = 9.99m;
        string currencyCode = "978"; // EUR
        string documentNr = "T01";
        string panL4Digit = "9999";

        // Act
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, currencyCode, "operatorId", "operatorName");
        var response = await _paymentService.AuthorizeTransactionAsync(amount, documentNr, currencyCode, panL4Digit);
        await _paymentService.ClosePosAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Approved", response.Status);
        _test01OperationId = response.OperationId; // Store operation ID for use in Test02
    }

    [Fact]
    public async Task Test02_ManualReversal()
    {
        // Arrange
        string operationId = _test01OperationId;

        // Act
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        var response = await _paymentService.VoidTransactionAsync(operationId);
        await _paymentService.ClosePosAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Voided", response.Status);
    }

    [Fact]
    public async Task Test03_DeclinedTransaction()
    {
        // Arrange
        decimal amount = 6.51m;
        string currencyCode = "978";
        string documentNr = "T03";
        string panL4Digit = "9999";

        // Act & Assert
        await _paymentService.OpenPosAsync("licenseToken", "alias", "username", "password");
        await _paymentService.UnlockDeviceAsync(amount, currencyCode, "operatorId", "operatorName");

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _paymentService.AuthorizeTransactionAsync(amount, documentNr, currencyCode, panL4Digit);
        });

        await _paymentService.ClosePosAsync();
    }

    // Additional tests follow the same pattern...

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
