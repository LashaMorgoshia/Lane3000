using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

public class TranslinkPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private string _accessToken;

    public TranslinkPaymentService(HttpClient httpClient, string apiBaseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
    }

    public async Task<string> OpenPosAsync(string licenseToken, string alias, string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(licenseToken)) throw new ArgumentException("License token is required.", nameof(licenseToken));
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("Alias is required.", nameof(alias));
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));

        var requestData = new
        {
            licenseToken,
            alias,
            userName,
            password
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/openpos",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            _accessToken = jsonResponse.accessToken;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return _accessToken;
        }
        else
        {
            throw new Exception($"Failed to open POS connection: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task UnlockDeviceAsync(decimal amount, string currencyCode, string operatorId, string operatorName, string idleText = "Insert Card", string language = "EN", string ecrVersion = "YourSoftware-YourBank-v1.0")
    {
        var amountInCents = (int)Math.Round(amount * 100);

        var requestData = new
        {
            header = new { command = "UNLOCKDEVICE" },
            @params = new
            {
                posOperation = "AUTHORIZE",
                amount = amountInCents,
                currencyCode,
                idleText,
                language,
                ecrVersion,
                operatorId,
                operatorName,
                cardTechs = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                enabledTranSourceMedias = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                silentCardRead = false
            }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to unlock device: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task<AuthorizeResponse> AuthorizeTransactionAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
    {
        var amountInCents = (int)Math.Round(amount * 100);

        var requestData = new
        {
            header = new { command = "AUTHORIZE" },
            @params = new
            {
                amount = amountInCents,
                cashBackAmount = 0,
                currencyCode,
                documentNr,
                panL4Digit
            }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var authorizeResponse = JsonConvert.DeserializeObject<AuthorizeResponse>(responseContent);
            return authorizeResponse;
        }
        else
        {
            throw new Exception($"Failed to authorize transaction: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task ClosePosAsync()
    {
        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/closepos", null);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to close POS connection: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task<VoidResponse> VoidTransactionAsync(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID is required.", nameof(operationId));

        var requestData = new
        {
            header = new { command = "VOID" },
            @params = new
            {
                operationId
            }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var voidResponse = JsonConvert.DeserializeObject<VoidResponse>(responseContent);
            return voidResponse;
        }
        else
        {
            throw new Exception($"Failed to void transaction: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task<RefundResponse> RefundTransactionAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
    {
        var amountInCents = (int)Math.Round(amount * 100);

        var requestData = new
        {
            header = new { command = "CREDIT" },
            @params = new
            {
                amount = amountInCents,
                currencyCode,
                documentNr,
                panL4Digit,
                time = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                // STAN and RRN should be generated or retrieved appropriately
                STAN = GenerateStan(),
                RRN = GenerateRrn()
            }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var refundResponse = JsonConvert.DeserializeObject<RefundResponse>(responseContent);
            return refundResponse;
        }
        else
        {
            throw new Exception($"Failed to refund transaction: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task<CloseDayResponse> CloseDayAsync()
    {
        var requestData = new
        {
            header = new { command = "CLOSEDAY" }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var closeDayResponse = JsonConvert.DeserializeObject<CloseDayResponse>(responseContent);
            return closeDayResponse;
        }
        else
        {
            throw new Exception($"Failed to close the day: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task SendSoftwareVersionAsync(string softwareVersion)
    {
        if (string.IsNullOrWhiteSpace(softwareVersion)) throw new ArgumentException("Software version is required.", nameof(softwareVersion));

        var requestData = new
        {
            header = new { command = "SENDVERSION" },
            @params = new
            {
                ecrVersion = softwareVersion
            }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/sendversion",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to send software version: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    // Utility methods to generate STAN and RRN
    private string GenerateStan()
    {
        // Implement your logic to generate a unique STAN
        return new Random().Next(100000, 999999).ToString();
    }

    private string GenerateRrn()
    {
        // Implement your logic to generate a unique RRN
        return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }
}

// Response models
public class AuthorizeResponse
{
    public string OperationId { get; set; }
    public string Status { get; set; }
    // Add other relevant properties based on the API response
}

public class VoidResponse
{
    public string Status { get; set; }
    // Add other relevant properties
}

public class RefundResponse
{
    public string Status { get; set; }
    // Add other relevant properties
}

public class CloseDayResponse
{
    public string Status { get; set; }
    // Add other relevant properties
}
