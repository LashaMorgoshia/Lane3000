using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class TranslinkPaymentService
{
    private static readonly HttpClient client = new HttpClient();
    private string accessToken;
    private string apiBaseUrl = "http://localhost:6678/v105/";

    public async Task<string> OpenPosAsync(string licenseToken, string alias, string userName, string password)
    {
        var requestData = new
        {
            licenseToken,
            alias,
            userName,
            password
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "openpos",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(responseContent);
            accessToken = json.accessToken;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return accessToken;
        }
        else
        {
            throw new Exception("Failed to open POS connection: " + response.ReasonPhrase);
        }
    }

    public async Task UnlockDeviceAsync(decimal amount, string currencyCode, string operatorId, string operatorName)
    {
        var requestData = new
        {
            header = new { command = "UNLOCKDEVICE" },
            @params = new
            {
                posOperation = "AUTHORIZE",
                amount = (int)(amount * 100),  // Amount in cents
                currencyCode,
                idleText = "Insert Card",
                language = "EN",
                ecrVersion = "YourSoftware-YourBank-v1.0",
                operatorId,
                operatorName,
                cardTechs = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                enabledTranSourceMedias = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                silentCardRead = false
            }
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to unlock device: " + response.ReasonPhrase);
        }
    }

    public async Task AuthorizeTransactionAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
    {
        var requestData = new
        {
            header = new { command = "AUTHORIZE" },
            @params = new
            {
                amount = (int)(amount * 100),  // Amount in cents
                cashBackAmount = 0,
                currencyCode,
                documentNr,
                panL4Digit
            }
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to authorize transaction: " + response.ReasonPhrase);
        }
    }

    public async Task ClosePosAsync()
    {
        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "closepos", null);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to close POS connection: " + response.ReasonPhrase);
        }
    }

    public async Task VoidTransactionAsync(string documentNr, string operationId)
    {
        var requestData = new
        {
            header = new { command = "VOID" },
            @params = new
            {
                operationId = operationId // The operation ID from the original transaction
            }
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to void transaction: " + response.ReasonPhrase);
        }
    }

    public async Task RefundTransactionAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
    {
        var requestData = new
        {
            header = new { command = "CREDIT" },
            @params = new
            {
                amount = (int)(amount * 100),  // Amount in cents
                currencyCode = currencyCode,   // e.g., "978" for EUR
                documentNr = documentNr,       // Document number for the transaction
                panL4Digit = panL4Digit,       // Last 4 digits of the card number
                time = DateTime.UtcNow.ToString("yyyyMMddHHmmss"), // Current transaction time
                STAN = "8261",                 // System Trace Audit Number (placeholder, adjust as needed)
                RRN = "933315462707"           // Retrieval Reference Number (placeholder, adjust as needed)
            }
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to refund transaction: " + response.ReasonPhrase);
        }
    }

    public async Task CloseDayAsync()
    {
        var requestData = new
        {
            header = new { command = "CLOSEDAY" }  // Command to close the day
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to close the day: " + response.ReasonPhrase);
        }
    }

    public async Task SendSoftwareVersionAsync(string softwareVersion)
    {
        var requestData = new
        {
            header = new { command = "UNLOCKDEVICE" },  // Reuse UNLOCKDEVICE to send version
            @params = new
            {
                posOperation = "NOOPERATION",  // No operation is triggered, just version update
                amount = 0,  // No amount
                cashBackAmount = 0,
                currencyCode = "978",  // Currency code for EUR (or change accordingly)
                idleText = "Version Check",
                language = "EN",  // Language of the POS
                ecrVersion = softwareVersion,  // The software version to be sent
                operatorId = "Operator",
                operatorName = "OperatorName",
                cardTechs = new string[] { },  // Empty array as no card techs are needed here
                enabledTranSourceMedias = new string[] { },  // Empty array for no transaction source media
                silentCardRead = false
            }
        };

        HttpResponseMessage response = await client.PostAsync(apiBaseUrl + "executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to send software version: " + response.ReasonPhrase);
        }
    }


}
