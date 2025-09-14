﻿using Newtonsoft.Json;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;

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
        return null;
    }

    public async Task UnlockDeviceAsync(decimal amount, string currencyCode, string operatorId, string operatorName, string idleText = "Insert Card", string language = "GE", string ecrVersion = "BDX-BOG-v1.0")
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
                //cardTechs = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                //enabledTranSourceMedias = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                silentCardRead = true
            }
        };

        var json = JsonConvert.SerializeObject(requestData);

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd", new StringContent(json, Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        //if (!response.IsSuccessStatusCode)
        //{
        //    throw new Exception($"Failed to unlock device: {response.ReasonPhrase}. Details: {responseContent}");
        //}

        //// Parse the response to get the operationId
        //var unlockResponse = JsonConvert.DeserializeObject<UnlockResponse>(responseContent);
        //string operationId = unlockResponse.OperationId;

        //// Wait for the ONCARD event
        //await WaitForEventAsync("ONCARD", operationId, TimeSpan.FromSeconds(5));

        await WaitForCardEvent();
    }

    public async Task UnlockDeviceWithNoOperationAsync(decimal amount, string currencyCode, string operatorId, string operatorName, string idleText = "Insert Card", string language = "GE", string ecrVersion = "BDX-BOG-v1.0")
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
                //cardTechs = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                //enabledTranSourceMedias = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                silentCardRead = true
            }
        };

        var json = JsonConvert.SerializeObject(requestData);

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd", new StringContent(json, Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();
    }

    public async Task UnlockDeviceForCreditAsync(decimal amount, string currencyCode, string operatorId, string operatorName, string idleText = "Insert Card", string language = "GE", string ecrVersion = "BDX-BOG-v1.0")
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
                //cardTechs = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                //enabledTranSourceMedias = new[] { "EmvChip", "EmvContactless", "MagnetSwipe" },
                silentCardRead = true
            }
        };

        var json = JsonConvert.SerializeObject(requestData);

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd", new StringContent(json, Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        //if (!response.IsSuccessStatusCode)
        //{
        //    throw new Exception($"Failed to unlock device: {response.ReasonPhrase}. Details: {responseContent}");
        //}

        //// Parse the response to get the operationId
        //var unlockResponse = JsonConvert.DeserializeObject<UnlockResponse>(responseContent);
        //string operationId = unlockResponse.OperationId;

        //// Wait for the ONCARD event
        //await WaitForEventAsync("ONCARD", operationId, TimeSpan.FromSeconds(5));

        await WaitForCardEvent();
    }

    public async Task CloseDocAsync(string operationId, string docNo)
    {
        var request = new
        {
            header = new
            {
                command = "CLOSEDOC"
            },
            @params = new
            {
                operations = new string[] { operationId },  // Transaction identifiers to be confirmed
                                                            //eProducts = new string[] { "A0000000041010", "A0000000041012" },  // E product transaction identifiers
                                                            //fiscalOperations = new string[] { "A0000000041010", "A0000000041012" },  // Fiscal operations to be confirmed
                documentNr = docNo  // Register receipt number
            }
        };

        string jsonRequest = JsonConvert.SerializeObject(request);
        StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd", content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Close Document Operation Response: " + responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Request error: " + e.Message);
        }
    }

    //public async Task CloseDocAsync(string operationId, string docNo)
    //{
    //    var requestData = new
    //    {
    //        header = new { command = "CLOSEDOC" },
    //        @params = new
    //        {
    //            idleText = "READY",
    //            operations = new string[] { operationId },
    //            documentNr = docNo
    //        }
    //    };

    //    var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
    //        new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

    //    var responseContent = await response.Content.ReadAsStringAsync();

    //    if (!response.IsSuccessStatusCode)
    //    {
    //        //throw new Exception($"Failed to lock device: {response.ReasonPhrase}. Details: {responseContent}");
    //    }
    //}

    public async Task LockDeviceAsync()
    {
        var requestData = new
        {
            header = new { command = "LOCKDEVICE" },
            @params = new
            {
                idleText = "READY",
            }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            //throw new Exception($"Failed to lock device: {response.ReasonPhrase}. Details: {responseContent}");
        }
    }

    public async Task WaitForCardEvent()
    {
        while (true)
        {
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/getEvent", new StringContent("{}", Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();
            if (!result.Contains("Queue empty."))
            {
                Console.WriteLine("Event Response: " + result);
                Console.WriteLine();
            }
            if (result.Contains("ONKBD") && result.Contains("kbdKey") && result.Contains("FR") && result.Contains("OK"))
            {
                Console.WriteLine("Event Response: " + result);
                break;
            }

            if (result.Contains("\"eventName\":\"ONCARD\""))
            {
                Console.WriteLine($"3. Card detected! Proceeding to authorization: {result}");
                break;
            }
        }
    }

    public async Task<AuthorizeResponse> WaitForAuthResponse()
    {
        var transactionStatus = new AuthorizeResponse();
        var printResult = new PrintResult();
        while (true)
        {
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/getEvent", new StringContent("{}", Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();

            if (result.Contains("\"eventName\":\"ONPRINT\""))
                printResult = JsonConvert.DeserializeObject<PrintResult>(result);
            

            if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
            {
                //Console.WriteLine("On Auth. Event Response: " + result);
                //Console.WriteLine();
                // Parse the JSON
                
                transactionStatus = JsonConvert.DeserializeObject<AuthorizeResponse>(result);
                transactionStatus.PrintResult = printResult;
                return transactionStatus;
            }
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

        return null;
    }

    public async Task<AuthorizeResponse> AuthorizeTransactionDeclaneAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
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

        return null;
    }

    public async Task<AuthorizeResponse> RefundTransactionAsync(string stan, string rrn, decimal amount, string documentNr, string currencyCode, string panL4Digit)
    {
        var amountInCents = (int)Math.Round(amount * 100);

        var requestData = new
        {
            header = new
            {
                command = "CREDIT"
            },
            @params = new
                   {
                       amount = amountInCents,  // Transaction amount in cents
                       currencyCode = currencyCode,  // Transaction currency code, e.g., "643" for Russian Ruble
                       documentNr = documentNr,  // Unique payment document number
                       panL4Digit = panL4Digit,  // Last 4 digits of the card number
                       time = DateTime.Now.ToString("yyyyMMddHHmmss"),
                       STAN = stan,  // System Trace Audit Number
                       RRN = rrn,  // Retrieval Reference Number
                       //ecrData = "Optional additional data"
                   }
        };

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode && !responseContent.Contains("INVALID_ARG"))
        {
            var authorizeResponse = JsonConvert.DeserializeObject<AuthorizeResponse>(responseContent);
            return authorizeResponse;
        }

        return null;
    }

    public async Task<AuthorizeResponse> AuthorizeThrowAsync(decimal amount, string documentNr, string currencyCode, string panL4Digit)
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
            return await WaitForAuthResponse();
        }
        

        return null;
    }

    public async Task ClosePosAsync()
    {
        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/closepos", null);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            //throw new Exception($"Failed to close POS connection: {response.ReasonPhrase}. Details: {responseContent}");
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
        }

        return await WaitForVoidResponse();
    }

    public async Task<VoidResponse> WaitForVoidResponse()
    {
        var transactionStatus = new VoidResponse();
        while (true)
        {
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/getEvent", new StringContent("{}", Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();
            if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
            {
                transactionStatus = JsonConvert.DeserializeObject<VoidResponse>(result);
                return transactionStatus;
            }
        }
    }

    public async Task<PrintResult> CloseDayAsync(string operatorId, string operatorName)
    {
        var requestData = new
        {
            header = new { command = "CLOSEDAY" },
            @params = new
            {
                operatorId = operatorId,
                operatorName = operatorName
            }
        };

        var printResult = new PrintResult();

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/executeposcmd",
            new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var closeDayResponse = JsonConvert.DeserializeObject<CloseDayResponse>(responseContent);
        }

        var result = "";
        while (true)
        {
            var trnStatus = await _httpClient.PostAsync($"{_apiBaseUrl}/getEvent", new StringContent("{}", Encoding.UTF8, "application/json"));
            result = await trnStatus.Content.ReadAsStringAsync();
            
            if (result.Contains("\"eventName\":\"ONPRINT\""))
            {
                printResult = JsonConvert.DeserializeObject<PrintResult>(result);
                return printResult;
            }
            //break;
        }
        return new PrintResult();
    }

    
        public async Task PerformSoftwareVersionCheck()
        {
            string versionApiUrl = _apiBaseUrl + "/getsoftwareversions";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(versionApiUrl);
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

        var requestData = new
        {
            header = new { command = "SENDVERSION" },
            @params = new
            {
                ecrVersion = softwareVersion
            }
        };

        //var response = await _httpClient.GetAsync($"{_apiBaseUrl}/getsoftwareversions",
        //    new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));

        var response = await _httpClient.GetAsync($"{_apiBaseUrl}/getsoftwareversions");

        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
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
public class UnlockResponse
{
    [JsonProperty("operationId")]
    public string OperationId { get; set; }
    // Add other relevant properties
}

public class StatusResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("events")]
    public EventData[] Events { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    // Add other relevant properties
}

public class EventData
{
    [JsonProperty("eventName")]
    public string EventName { get; set; }

    // Add other event-related properties
}

public class VoidResponse
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class RefundResponse
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class CloseDayResponse
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class AuthorizeResponse
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }
    public PrintResult PrintResult { get; set; }
}

public class Properties
{
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; }

    [JsonPropertyName("amountAuthorized")]
    public decimal AmountAuthorized { get; set; }

    [JsonPropertyName("documentNr")]
    public string DocumentNr { get; set; }

    [JsonPropertyName("cryptogram")]
    public string Cryptogram { get; set; }

    [JsonPropertyName("authCode")]
    public string AuthCode { get; set; }

    [JsonPropertyName("RRN")]
    public string RRN { get; set; }

    [JsonPropertyName("STAN")]
    public string STAN { get; set; }

    [JsonPropertyName("cardType")]
    public string CardType { get; set; }

    [JsonPropertyName("amountAdditional")]
    public decimal? AmountAdditional { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("authorizationState")]
    public string AuthorizationState { get; set; }

    [JsonPropertyName("cardName")]
    public string CardName { get; set; }

    [JsonPropertyName("APN")]
    public string APN { get; set; }

    [JsonPropertyName("AID")]
    public string AID { get; set; }

    [JsonPropertyName("CVMApplied")]
    public List<string> CVMApplied { get; set; }

    [JsonPropertyName("authCenterName")]
    public string AuthCenterName { get; set; }

    [JsonPropertyName("tranSourceMedia")]
    public string TranSourceMedia { get; set; }

    [JsonPropertyName("PAN")]
    public string PAN { get; set; }

    [JsonPropertyName("DCCResult")]
    public string DCCResult { get; set; }

    [JsonPropertyName("EcrData")]
    public string EcrData { get; set; }
}

public class Result
{
    [JsonPropertyName("resultCode")]
    public string ResultCode { get; set; }

    [JsonPropertyName("resultMessage")]
    public string ResultMessage { get; set; }

    [JsonPropertyName("resultTime")]
    public string ResultTime { get; set; }
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