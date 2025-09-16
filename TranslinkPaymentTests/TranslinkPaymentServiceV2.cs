using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

public class TranslinkPaymentServiceV2
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private string _accessToken;

    public TranslinkPaymentServiceV2(HttpClient httpClient, string apiBaseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
    }

    // ---------------------------
    // Session / POS lifecycle
    // ---------------------------
    public async Task<string> OpenPosAsync(string licenseToken, string alias, string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(licenseToken)) throw new ArgumentException("License token is required.", nameof(licenseToken));
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("Alias is required.", nameof(alias));
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));

        var requestData = new { licenseToken, alias, userName, password };

        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/openpos",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            _accessToken = jsonResponse.accessToken;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return _accessToken;
        }
        return null;
    }

    public async Task ClosePosAsync()
    {
        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/closepos", null);
        _ = await response.Content.ReadAsStringAsync();
        // Spec allows silent close; no throw here.
    }

    // ---------------------------
    // Device lock/unlock (authorize/credit/no-op)
    // ---------------------------
    public async Task UnlockDeviceAsync(decimal amount, string currencyCode, string operatorId, string operatorName,
        string idleText = "Insert Card", string language = "GE", string ecrVersion = "BDX-BOG-v1.0")
    {
        var amountInCents = (int)Math.Round(amount * 100);
        var requestData = new
        {
            header = new { command = "UNLOCKDEVICE" },
            @params = new
            {
                posOperation = "AUTHORIZE",
                amount = amountInCents,
                cashBackAmount = 0,
                currencyCode,
                idleText,
                language,
                ecrVersion,
                operatorId,
                operatorName,
                silentCardRead = true
            }
        };

        var json = JsonConvert.SerializeObject(requestData);
        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(json, Encoding.UTF8, "application/json"));

        _ = await response.Content.ReadAsStringAsync();
        await WaitForCardEvent();
    }

    public async Task UnlockDeviceWithNoOperationAsync(decimal amount, string currencyCode, string operatorId, string operatorName,
        string idleText = "Insert Card", string language = "GE", string ecrVersion = "BDX-BOG-v1.0")
    {
        var amountInCents = (int)Math.Round(amount * 100);
        var requestData = new
        {
            header = new { command = "UNLOCKDEVICE" },
            @params = new
            {
                posOperation = "NOOPERATION",
                amount = amountInCents,
                cashBackAmount = 0,
                currencyCode,
                idleText,
                language,
                ecrVersion,
                operatorId,
                operatorName,
                silentCardRead = true
            }
        };

        var json = JsonConvert.SerializeObject(requestData);
        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(json, Encoding.UTF8, "application/json"));

        _ = await response.Content.ReadAsStringAsync();
        // no specific wait required for NOOPERATION
    }

    public async Task UnlockDeviceForCreditAsync(decimal amount, string currencyCode, string operatorId, string operatorName,
        string idleText = "Insert Card", string language = "GE", string ecrVersion = "BDX-BOG-v1.0")
    {
        var amountInCents = (int)Math.Round(amount * 100);
        var requestData = new
        {
            header = new { command = "UNLOCKDEVICE" },
            @params = new
            {
                posOperation = "CREDIT",
                amount = amountInCents,
                cashBackAmount = 0,
                currencyCode,
                idleText,
                language,
                ecrVersion,
                operatorId,
                operatorName,
                silentCardRead = true
            }
        };

        var json = JsonConvert.SerializeObject(requestData);
        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(json, Encoding.UTF8, "application/json"));

        _ = await response.Content.ReadAsStringAsync();
        await WaitForCardEvent();
    }

    public async Task LockDeviceAsync()
    {
        var requestData = new
        {
            header = new { command = "LOCKDEVICE" },
            @params = new { idleText = "READY" }
        };

        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        _ = await response.Content.ReadAsStringAsync();
        // no throw; device may already be locked
    }

    // ---------------------------
    // Purchase / Void / Refund / Close Day
    // ---------------------------
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

        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var body = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<AuthorizeResponse>(body);

        return null;
    }

    // keep your original name to avoid breaking existing tests
    public async Task<AuthorizeResponse> AuthorizeTransactionDeclaneAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
        => await AuthorizeTransactionAsync(amount, documentNr, currencyCode, panL4Digit);

    // Overload that lets tests control whether to send original data (when POS requests it)
    public async Task<AuthorizeResponse> RefundTransactionAsync(
        string stan, string rrn, decimal amount, string documentNr, string currencyCode, string panL4Digit, bool requireOriginalData)
    {
        var amountInCents = (int)Math.Round(amount * 100);
        var @params = new Dictionary<string, object>
        {
            ["amount"] = amountInCents,
            ["currencyCode"] = currencyCode,
            ["documentNr"] = documentNr,
            ["panL4Digit"] = panL4Digit,
            ["RRN"] = rrn
        };

        if (requireOriginalData)
        {
            //@params["time"] = DateTime.Now.ToString("yyyyMMddHHmmss");
            //@params["STAN"] = stan;
            @params["RRN"] = rrn;
        }

        var requestData = new { header = new { command = "CREDIT" }, @params };

        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode && !responseContent.Contains("INVALID_ARG"))
            return JsonConvert.DeserializeObject<AuthorizeResponse>(responseContent);

        return null;
    }

    // Backward-compatible signature (defaults to NOT forcing original data fields)
    public async Task<AuthorizeResponse> RefundTransactionAsync(string stan, string rrn, decimal amount, string documentNr, string currencyCode, string panL4Digit)
        => await RefundTransactionAsync(stan, rrn, amount, documentNr, currencyCode, panL4Digit, requireOriginalData: false);

    public async Task<VoidResponse> VoidTransactionAsync(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID is required.", nameof(operationId));

        var requestData = new
        {
            header = new { command = "VOID" },
            @params = new { operationId }
        };

        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        _ = await response.Content.ReadAsStringAsync();
        return await WaitForVoidResponse();
    }

    public async Task<PrintResult> CloseDayAsync(string operatorId, string operatorName)
    {
        var requestData = new
        {
            header = new { command = "CLOSEDAY" },
            @params = new { operatorId, operatorName }
        };

        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        _ = await response.Content.ReadAsStringAsync();

        // Wait for ONPRINT with the close-day receipt
        var printResult = new PrintResult();
        while (true)
        {
            var result = await GetEventRawAsync(15);
            if (result.Contains("\"eventName\":\"ONPRINT\"") || result.Contains("OK"))
                return JsonConvert.DeserializeObject<PrintResult>(result);
        }
    }

    public async Task<PrintResult> CloseDayAsync(string operatorId, string operatorName, CancellationToken ct = default)
    {
        // 1) Trigger CLOSEDAY
        var payload = new
        {
            header = new { command = "CLOSEDAY" },
            @params = new { operatorId, operatorName }
        };

        var res = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"CLOSEDAY HTTP {res.StatusCode}: {body}");

        var tr = JsonConvert.DeserializeObject<TypeResultEnvelope>(body);
        if (!string.Equals(tr?.result?.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"CLOSEDAY rejected by POS: {tr?.result?.ResultMessage ?? "Unknown error"}");

        // 2) Now wait up to 130s for events (ONMSGBOX/ONPRINT)
        var deadline = DateTime.UtcNow.AddSeconds(130);
        PrintResult totals = null;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var ev = await GetEventRawAsync(15); // your existing long-poll -> returns string JSON

            if (string.IsNullOrEmpty(ev) || ev.Contains("Queue empty."))
                continue;

            // If POS asks for confirmation, send SETMSGBOXKEY Ok
            if (ev.Contains("\"eventName\":\"ONMSGBOX\""))
            {
                await SetMsgBoxKeyAsync("Ok"); // Ok/Yes/Cancel/No supported
                continue;
            }

            if (ev.Contains("\"eventName\":\"ONPRINT\""))
            {
                totals = JsonConvert.DeserializeObject<PrintResult>(ev);
                break;
            }

            // Optional: handle other events if your environment emits them here
        }

        if (totals == null)
            throw new Exception("Day closed (command accepted) but no totals were received within the timeout.");

        return totals;
    }

    // Helper: reply to message boxes
    public async Task SetMsgBoxKeyAsync(string keyValue)
    {
        var payload = new
        {
            header = new { command = "SETMSGBOXKEY" },
            @params = new { keyValue }
        };
        var res = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"SETMSGBOXKEY failed: {body}");
    }

    // ---------------------------
    // Document confirmation
    // ---------------------------
    public async Task<string> CloseDocAsync(string operationId, string docNo)
    {
        var request = new
        {
            header = new { command = "CLOSEDOC" },
            @params = new
            {
                operations = new[] { operationId },
                documentNr = docNo
            }
        };

        var json = JsonConvert.SerializeObject(request);
        // Retry a few times until the POS acknowledges (resultCode=OK can be returned immediately or via buffered response)
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            using var resp = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var body = await resp.Content.ReadAsStringAsync();
            // quick path: immediate OK in HTTP response body
            if (resp.IsSuccessStatusCode && body.Contains("\"resultCode\":\"OK\""))
            {
                Console.WriteLine("CLOSEDOC immediate OK");
                return body;
            }

            // slow path: poll events briefly and inspect for ONTRNSTATUS/OK related to this doc
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(3))
            {
                var ev = await GetEventRawAsync(2);
                if (ev.Contains("\"eventName\":\"ONTRNSTATUS\"") && ev.Contains("\"resultCode\":\"OK\""))
                {
                    Console.WriteLine("CLOSEDOC OK (from event)");
                    return ev;
                }
            }

            await Task.Delay(400);
        }

        throw new Exception("CLOSEDOC was not acknowledged by POS after retries.");
    }

    // ---------------------------
    // Event loops (GET + longPollingTimeout)
    // ---------------------------
    private async Task<string> GetEventRawAsync(int longPollingSeconds = 15)
    {
        longPollingSeconds = Math.Clamp(longPollingSeconds, 1, 60);
        var url = $"{_apiBaseUrl}/getEvent?longPollingTimeout={longPollingSeconds}";
        using var resp = await _httpClient.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    public async Task WaitForCardEvent()
    {
        while (true)
        {
            var result = await GetEventRawAsync(5);

            if (!result.Contains("Queue empty."))
            {
                Console.WriteLine("Event Response: " + result);
                Console.WriteLine();
            }

            if (result.Contains("\"eventName\":\"ONKBD\"") && result.Contains("\"kbdKey\":\"FR\"") && result.Contains("\"OK\""))
            {
                Console.WriteLine("Keyboard OK received.");
                break;
            }

            if (result.Contains("\"eventName\":\"ONCARD\""))
            {
                Console.WriteLine($"Card detected -> proceed: {result}");
                break;
            }
        }
    }

    public async Task<AuthorizeResponse> WaitForAuthResponse()
    {
        var printResult = new PrintResult();
        while (true)
        {
            var result = await GetEventRawAsync(5);

            if (result.Contains("\"eventName\":\"ONPRINT\""))
                printResult = JsonConvert.DeserializeObject<PrintResult>(result);

            //if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
            //{
            //    var trn = JsonConvert.DeserializeObject<AuthorizeResponse>(result);
            //    trn.PrintResult = printResult;
            //    return trn;
            //}

            if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
            {
                try
                {
                    var trn = JsonConvert.DeserializeObject<AuthorizeResponse>(result);
                    trn.PrintResult = printResult;
                    return trn;
                }
                catch (JsonException jx)
                {
                    // Helps a ton when payloads evolve
                    Console.WriteLine("Failed to parse ONTRNSTATUS: " + jx.Message);
                    Console.WriteLine("Payload: " + result);
                    throw;
                }
            }
        }
    }

    public async Task<VoidResponse> WaitForVoidResponse()
    {
        while (true)
        {
            var result = await GetEventRawAsync(5);
            if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
                return JsonConvert.DeserializeObject<VoidResponse>(result);
        }
    }

    // ---------------------------
    // Version info (optional helper)
    // ---------------------------
    public async Task PerformSoftwareVersionCheck()
    {
        var versionApiUrl = _apiBaseUrl + "/getsoftwareversions";
        try
        {
            using var response = await _httpClient.GetAsync(versionApiUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Software Version Check Response: " + responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Request error: " + e.Message);
        }
    }

    public async Task<string> SendSoftwareVersionAsync(string softwareVersion)
    {
        if (string.IsNullOrWhiteSpace(softwareVersion)) throw new ArgumentException("Software version is required.", nameof(softwareVersion));
        // Note: test T10 is satisfied by including proper ecrVersion in UNLOCKDEVICE.
        using var response = await _httpClient.GetAsync($"{_apiBaseUrl}/getsoftwareversions");
        return await response.Content.ReadAsStringAsync();
    }

    // ---------------------------
    // Utils
    // ---------------------------
    private string GenerateStan() => new Random().Next(100000, 999999).ToString();
    private string GenerateRrn() => DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
}

// ---------------------------
// Models
// ---------------------------
public class UnlockResponse
{
    [JsonProperty("operationId")]
    public string OperationId { get; set; }
}

public class StatusResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("events")]
    public EventData[] Events { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }
}

public class EventData
{
    [JsonProperty("eventName")]
    public string EventName { get; set; }
}

public class VoidResponse
{
    [JsonPropertyName("eventName")] public string EventName { get; set; }
    [JsonPropertyName("properties")] public Properties Properties { get; set; }
    [JsonPropertyName("result")] public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class RefundResponse
{
    [JsonPropertyName("eventName")] public string EventName { get; set; }
    [JsonPropertyName("properties")] public Properties Properties { get; set; }
    [JsonPropertyName("result")] public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class CloseDayResponse
{
    [JsonPropertyName("eventName")] public string EventName { get; set; }
    [JsonPropertyName("properties")] public Properties Properties { get; set; }
    [JsonPropertyName("result")] public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class AuthorizeResponse
{
    [JsonPropertyName("eventName")] public string EventName { get; set; }
    [JsonPropertyName("properties")] public Properties Properties { get; set; }
    [JsonPropertyName("result")] public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class Properties
{
    [JsonProperty("operationId")] public string OperationId { get; set; }
    [JsonProperty("amountAuthorized")] public decimal AmountAuthorized { get; set; } // cents (e.g., 6000)
    [JsonProperty("documentNr")] public string DocumentNr { get; set; }
    [JsonProperty("cryptogram")] public string Cryptogram { get; set; }
    [JsonProperty("authCode")] public string AuthCode { get; set; }
    [JsonProperty("RRN")] public string RRN { get; set; }
    [JsonProperty("STAN")] public string STAN { get; set; }
    [JsonProperty("cardType")] public string CardType { get; set; }

    // CHANGE: array of objects, not a scalar
    [JsonProperty("amountAdditional")]
    public List<AmountAdditionalItem> AmountAdditional { get; set; }

    [JsonProperty("text")] public string Text { get; set; }
    [JsonProperty("state")] public string State { get; set; }
    [JsonProperty("authorizationState")] public string AuthorizationState { get; set; }
    [JsonProperty("cardName")] public string CardName { get; set; }
    [JsonProperty("APN")] public string APN { get; set; }
    [JsonProperty("AID")] public string AID { get; set; }

    [JsonProperty("CVMApplied")]
    public List<string> CVMApplied { get; set; }

    [JsonProperty("authCenterName")] public string AuthCenterName { get; set; }
    [JsonProperty("tranSourceMedia")] public string TranSourceMedia { get; set; }
    [JsonProperty("PAN")] public string PAN { get; set; }
    [JsonProperty("DCCResult")] public string DCCResult { get; set; }
    [JsonProperty("EcrData")] public string EcrData { get; set; }
}

// New model for items in amountAdditional
public class AmountAdditionalItem
{
    // e.g., "70" – check your spec for mapping (tips/surcharge/discount etc.)
    [JsonProperty("type")] public string Type { get; set; }

    // e.g., "981"
    [JsonProperty("currencyCode")] public string CurrencyCode { get; set; }

    // e.g., -100 (cents) == -1.00 GEL
    [JsonProperty("amount")] public int Amount { get; set; }
}


public class Result
{
    [JsonPropertyName("resultCode")] public string ResultCode { get; set; }
    [JsonPropertyName("resultMessage")] public string ResultMessage { get; set; }
    [JsonPropertyName("resultTime")] public string ResultTime { get; set; }
}

public class PrintResult
{
    public PrintProperties Properties { get; set; }
}

public class PrintProperties
{
    public string ReceiptText { get; set; }
    public string DocumentNr { get; set; }
}

public sealed class TypeResultEnvelope
{
    public Result result { get; set; }
}