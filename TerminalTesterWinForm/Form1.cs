using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TerminalTesterWinForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }


    public class TranslinkPaymentTestsV2 
    {
        private readonly TranslinkPaymentServiceV2 _paymentService;

        // Shared test data
        private string _test01OperationId;
        private string _pos => "POS1";
        private string _docNo { get; set; } = DateTime.Now.Ticks.ToString();

        private readonly string _operatorId = "C5";
        private readonly string _operatorName = "Cashier";
        private readonly string _currCode = "981";   // GEL (as per test doc)
        private readonly string _panL4Digit = "9999";

        private string _stan { get; set; }
        private string _rrn { get; set; }

        public TranslinkPaymentTestsV2(TranslinkPaymentServiceFixture fixture)
        {
            _paymentService = fixture.PaymentService;
        }

        [Fact]
        public async Task RunTests()
        {
            // Execute in the documented order (xUnit doesn't guarantee [Fact] order by itself)
            await Test01_Purchase();
            await Test02_ManualReversal();
            await Test03_DeclinedTransaction();
            await Test04_AutomaticReversal();
            await Test05_PurchaseWithOnlinePIN();
            await Test06_DeclinedTransaction();
            await Test07_AmountsAdditional_61_00();
            await Test08_AmountsAdditional_62_00();
            await Test09_EndOfDay();
            await Test10_SendSoftwareVersion();

            Assert.True(true);
        }

        /// <summary>
        /// დღის დახურვამდე, აუცილებელია ღია დოკუმენტები დაიხუროს
        /// </summary>
        [Fact]
        public async Task Test00_CloseDocs()
        {
            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            var doc1CloseResult = await _paymentService.CloseDocAsync("201E37CA82662251", "638935380000520952");
            var doc2CloseResult = await _paymentService.CloseDocAsync("35314D3E75549B8C", "638935381152144824");
            var doc3CloseResult = await _paymentService.CloseDocAsync("BE3373A4F9E884A5", "638935382099269945");
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T01: Purchase 9.99 (Approved)
        /// </summary>
        [Fact]
        public async Task Test01_Purchase()
        {
            decimal amount = 9.99m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);
            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);

            var response = await _paymentService.WaitForAuthResponse();
            Assert.NotNull(response);
            Assert.Equal("Approved", response.Properties.State);

            _test01OperationId = response.Properties.OperationId;
            _stan = response.Properties.STAN;
            _rrn = response.Properties.RRN;

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        // T07: Refund
        [Fact]
        public async Task Test02_Refund()
        {
            // Arrange
            decimal amount = 9.99m;
            //_docNo = $"{DateTime.Now.Ticks}";
            //var stan = "28";
            //_docNo = "O1145EE77837610F4";
            //_stan = "50";
            //_rrn = "5259RR100060";

            // Console.WriteLine("Running Test T07 - Refund...");
            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceForCreditAsync(amount, _currCode, _operatorId, _operatorName); // Amount: 9.99 EUR
            var auth = await _paymentService.RefundTransactionAsync(_stan, _rrn, amount, _docNo, _currCode, _panL4Digit);  // Amount: 9.99 EUR
            var response = default(AuthorizeResponse);
            if (auth != null)
            {
                response = await _paymentService.WaitForAuthResponse();
                await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            }
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Approved", response.Properties.State);
        }

        /// <summary>
        /// T02: Manual Reversal (Void) of T01 (Approved)
        /// </summary>
        [Fact]
        public async Task Test02_ManualReversal()
        {
            decimal amount = 9.99m;
            // _test01OperationId must come from T01 when running via RunTests()
            Assert.False(string.IsNullOrWhiteSpace(_test01OperationId), "T01 must run before T02 to obtain operationId.");

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceWithNoOperationAsync(amount, _currCode, _operatorId, _operatorName);

            var response = await _paymentService.VoidTransactionAsync(_test01OperationId);
            Assert.NotNull(response);
            Assert.Equal("Approved", response.Properties.State);

            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T03: Purchase 6.51 (Online Declined). CLOSEDOC still required.
        /// </summary>
        [Fact]
        public async Task Test03_DeclinedTransaction()
        {
            decimal amount = 6.51m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);

            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
            var response = await _paymentService.WaitForAuthResponse();

            Assert.NotNull(response);
            Assert.Equal("Declined", response.Properties.State);

            // Spec requires CLOSEDOC even when Declined
            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T04: Automatic Reversal 6.66 (Declined/Reversed). CLOSEDOC required.
        /// </summary>
        [Fact]
        public async Task Test04_AutomaticReversal()
        {
            decimal amount = 6.66m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);

            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
            var response = await _paymentService.WaitForAuthResponse();

            Assert.NotNull(response);
            Assert.Equal("Declined", response.Properties.State); // test doc says Declined (Reversed)

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T05: Purchase 6.70 with Online PIN (Approved). PIN: 9999
        /// </summary>
        [Fact]
        public async Task Test05_PurchaseWithOnlinePIN()
        {
            decimal amount = 6.70m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);
            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);

            var response = await _paymentService.WaitForAuthResponse();
            Assert.NotNull(response);
            Assert.Equal("Approved", response.Properties.State);

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T06: Purchase 6.55 (Online Declined). CLOSEDOC still required.
        /// </summary>
        [Fact]
        public async Task Test06_DeclinedTransaction()
        {
            decimal amount = 6.55m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);

            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
            var response = await _paymentService.WaitForAuthResponse();

            Assert.NotNull(response);
            Assert.Equal("Declined", response.Properties.State);

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T07: Purchase 61.00 – check behavior when amountsAdditional are received (Approved).
        /// </summary>
        [Fact]
        public async Task Test07_AmountsAdditional_61_00()
        {
            decimal amount = 61.00m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);

            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
            var response = await _paymentService.WaitForAuthResponse();

            Assert.NotNull(response);
            Assert.Equal("Approved", response.Properties.State);
            // AmountsAdditional expected by the test doc for 61.00
            //Assert.True(response.Properties.AmountAdditional.HasValue, "amountsAdditional should be present for 61.00 test.");

            Assert.NotNull(response.Properties.AmountAdditional);
            Assert.NotEmpty(response.Properties.AmountAdditional);

            // Optional: verify the discount/surcharge item content
            var aa = response.Properties.AmountAdditional[0];
            Assert.Equal("981", aa.CurrencyCode);
            Assert.Equal(-100, aa.Amount); // -1.00 GEL (your sample shows a 1 GEL discount)

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T08: Purchase 62.00 – check behavior when amountsAdditional are received (Approved).
        /// </summary>
        [Fact]
        public async Task Test08_AmountsAdditional_62_00()
        {
            decimal amount = 62.00m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);

            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);
            var response = await _paymentService.WaitForAuthResponse();

            Assert.NotNull(response);
            Assert.Equal("Approved", response.Properties.State);
            // AmountsAdditional expected by the test doc for 62.00
            //Assert.True(response.Properties.AmountAdditional.HasValue, "amountsAdditional should be present for 62.00 test.");

            Assert.NotNull(response.Properties.AmountAdditional);
            Assert.NotEmpty(response.Properties.AmountAdditional);

            // Optional: verify the discount/surcharge item content
            var aa = response.Properties.AmountAdditional[0];
            Assert.Equal("981", aa.CurrencyCode);
            Assert.Equal(200, aa.Amount); // -1.00 GEL (your sample shows a 1 GEL discount)

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T09: End of Day (successful)
        /// დღის დახურვისთვის ყველა დოკუმენტი დახურული უნდა იყოს რაზეც ავტორიზაცია გაკეთდა
        /// </summary>
        [Fact]
        public async Task Test09_EndOfDay()
        {
            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            var print = await _paymentService.CloseDayAsync(_operatorId, _operatorName);
            Assert.NotNull(print);

            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        /// <summary>
        /// T10: "UNLOCKDEVICE" – send ECR name and version. We validate by making the UNLOCKDEVICE call
        /// with proper ecrVersion format and also reading /getsoftwareversions for visibility.
        /// </summary>
        [Fact]
        public async Task Test10_SendSoftwareVersion()
        {
            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");

            // Send version as part of UNLOCKDEVICE (this is what the test doc checks).
            await _paymentService.UnlockDeviceAsync(1.00m, _currCode, _operatorId, _operatorName,
                idleText: "READY", language: "GE", ecrVersion: "BDX-BOG-v1.0");

            // Optional visibility: check versions endpoint (not required for pass/fail, but useful)
            var versions = await _paymentService.SendSoftwareVersionAsync("BDX-BOG-v1.0");
            Assert.False(string.IsNullOrWhiteSpace(versions));

            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }

        // ---------------------------
        // Fixture
        // ---------------------------
        public class TranslinkPaymentServiceFixture
        {
            public TranslinkPaymentServiceV2 PaymentService { get; }

            public TranslinkPaymentServiceFixture()
            {
                var httpClient = new HttpClient();
                string apiBaseUrl = "http://localhost:6678/v105"; // pick from config in real env
                PaymentService = new TranslinkPaymentServiceV2(httpClient, apiBaseUrl);
            }
        }
    }
}
