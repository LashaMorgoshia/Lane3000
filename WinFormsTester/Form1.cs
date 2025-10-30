using System.Threading.Tasks;

namespace WinFormsTester
{
    public partial class Form1 : Form
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

        public Form1()
        {
            InitializeComponent();

            var httpClient = new HttpClient();
            string apiBaseUrl = "http://localhost:6678/v105"; // pick from config in real env
            _paymentService = new TranslinkPaymentServiceV2(httpClient, apiBaseUrl);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Test01_Purchase();
        }

        public async Task Test01_Purchase()
        {
            decimal amount = 9.99m;
            _docNo = $"{DateTime.Now.Ticks}";

            await _paymentService.OpenPosAsync("licenseToken", _pos, "username", "password");
            await _paymentService.UnlockDeviceAsync(amount, _currCode, _operatorId, _operatorName);
            await _paymentService.AuthorizeTransactionAsync(amount, _docNo, _currCode, _panL4Digit);

            var response = await _paymentService.WaitForAuthResponse();
            if (response == null || response.Properties.State != "Approved")
                throw new Exception();

            //Assert.Equal("Approved", response.Properties.State);

            _test01OperationId = response.Properties.OperationId;
            _stan = response.Properties.STAN;
            _rrn = response.Properties.RRN;

            await _paymentService.CloseDocAsync(response.Properties.OperationId, _docNo);
            await _paymentService.LockDeviceAsync();
            await _paymentService.ClosePosAsync();
        }
    }
}
