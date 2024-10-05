/*
 * Author: Lasha Morgoshia
 * Created On: 18.09.2024
 * POS Terminal Integration Protocol for DESK 3500
 *
 */


using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Desk3500
{
    public class Desk3500Api
    {
        private readonly string baseUrl;
        private readonly string licenseToken;
        private readonly string alias;
        private readonly string userName;
        private readonly string password;
        private string accessToken;

        public Desk3500Api(string baseUrl, string licenseToken, string alias, string userName, string password)
        {
            this.baseUrl = baseUrl;
            this.licenseToken = licenseToken;
            this.alias = alias;
            this.userName = userName;
            this.password = password;
        }

        private async Task<HttpResponseMessage> SendPostRequest(string url, string contentJson)
        {
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                }

                var content = new StringContent(contentJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error: {response.StatusCode}, {error}");
                }
            }
        }

        public async Task OpenPos()
        {
            var url = $"{baseUrl}/v105/openpos";
            var requestJson = $@"
        {{
            ""licenseToken"": ""{licenseToken}"",
            ""alias"": ""{alias}"",
            ""userName"": ""{userName}"",
            ""password"": ""{password}""
        }}";

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(result))
            {
                // Extract the access token
                accessToken = doc.RootElement.GetProperty("accessToken").GetString();
                Console.WriteLine("1. Access Token: " + accessToken);
            }
            // accessToken = "extracted-access-token"; // Update this based on response parsing
        }

        public async Task WaitForCardEvent()
        {
            var url = $"{baseUrl}/v105/getEvent";

            while (true)
            {
                var response = await SendPostRequest(url, "{}");
                var result = await response.Content.ReadAsStringAsync();
                if (!result.Contains("Queue empty."))
                {
                    Console.WriteLine("Event Response: " + result);
                    Console.WriteLine();
                }

                if (result.Contains("\"eventName\":\"ONCARD\""))
                {
                    Console.WriteLine($"3. Card detected! Proceeding to authorization: {result}");
                    break;
                }
                // No need for Task.Delay here if using long polling
                await Task.Delay(1000);
            }
        }

        public async Task<TransactionStatus> WaitForCardEventResponse()
        {
            var url = $"{baseUrl}/v105/getEvent";
            var transactionStatus = new TransactionStatus();
            var printResult = new PrintResult() ;
            while (true)
            {
                var response = await SendPostRequest(url, "{}");
                var result = await response.Content.ReadAsStringAsync();

                if (result.Contains("\"eventName\":\"ONPRINT\""))
                {
                    Console.WriteLine("On Auth:");
                    Console.WriteLine(result);
                    printResult = JsonConvert.DeserializeObject<PrintResult>(result);
                }

                if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
                {
                    Console.WriteLine("On Auth. Event Response: " + result);
                    Console.WriteLine();
                    // Parse the JSON
                    transactionStatus = JsonConvert.DeserializeObject<TransactionStatus>(result);
                    // Output parsed values
                    Console.WriteLine($"Event Name: {transactionStatus.EventName}");
                    Console.WriteLine($"Document Number: {transactionStatus.Properties.DocumentNr}");
                    Console.WriteLine($"State: {transactionStatus.Properties.State}");
                    Console.WriteLine($"Result Code: {transactionStatus.Result.ResultCode}");
                    Console.WriteLine($"Result Message: {transactionStatus.Result.ResultMessage}");
                    break;
                }

                // No need for Task.Delay here if using long polling
                // break;
                await Task.Delay(1000);
            }
            transactionStatus.PrintResult = printResult;
            return transactionStatus;
        }

        // Method to unlock the device and wait for the card
        public async Task UnlockAndWaitForCard(int amount, string ecrVersion)
        {
            await UnlockDevice(amount, ecrVersion);
            await WaitForCardEvent(); // Wait for the card touch
        }

        // Unlock POS for transactions
        public async Task<string> UnlockDevice(int amount, string ecrVersion, string currencyCode = "981", string idleText = "Insert Card")
        {
            var url = $"{baseUrl}/v105/executeposcmd";
            var requestJson = $@"
            {{
                ""header"": {{
                    ""command"": ""UNLOCKDEVICE""
                }},
                ""params"": {{
                    ""posOperation"": ""AUTHORIZE"",
                    ""amount"": {amount},
                    ""cashBackAmount"": 0,
                    ""currencyCode"": ""{currencyCode}"",
                    ""language"": ""GE"",
                    ""idleText"": ""{idleText}"",
                    ""ecrVersion"": ""{ecrVersion}""
                }}
            }}";

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("2. Device Unlocked: " + result);
            return result;
        }

        public async Task CloseDoc(string docNo)
        {
            try
            {
                var url = $"{baseUrl}/v105/executeposcmd";
                var requestJson = $@"
        {{
            ""header"": {{
                ""command"": ""CLOSEDOC""
            }},
            ""params"": {{
                ""documentNr"": ""{docNo}""
            }}
        }}";

                var response = await SendPostRequest(url, requestJson);
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("7. Close Doc: " + result);
            }
            catch(Exception ex) {
                Console.WriteLine("7. CloseDoc: " + ex.Message);
            }
        }

        public async Task CloseDay(string operatorId)
        {
            try
            {
                var url = $"{baseUrl}/v105/executeposcmd";
                var requestJson = $@"
        {{
            ""header"": {{
                ""command"": ""CLOSEDAY""
            }},
            ""params"": {{
                ""operatorId"": ""{operatorId}"",
""operatorName"": ""Anna"",
            }}
        }}";

                var response = await SendPostRequest(url, requestJson);
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("8. Close Doc: " + result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("8. CloseDoc: " + ex.Message);
            }
        }

        public async Task LockDevice()
        {
            try
            {
                var url = $"{baseUrl}/v105/executeposcmd";
                var requestJson = $@"
        {{
            ""header"": {{
                ""command"": ""LOCKDEVICE""
            }},
            ""params"": {{
                ""idleText"": ""READY""
            }}
        }}";

                var response = await SendPostRequest(url, requestJson);
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("8. Close Doc: " + result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("8. CloseDoc: " + ex.Message);
            }
        }

        public async Task ClosePos()
        {
            var url = $"{baseUrl}/v105/closepos";
            var requestJson = $@"
        {{
            ""accessToken"": ""{accessToken}""
        }}";

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("10. POS Connection Closed: " + result);
        }

        public static string CreateJson(int amount, string currencyCode, string documentNr, string panL4Digit)
        {
            var request = new Request
            {
                header = new Header
                {
                    command = "AUTHORIZE"
                },
                @params = new Params
                {
                    amount = amount,
                    cashBackAmount = 0,
                    currencyCode = currencyCode,
                    documentNr = documentNr,
                    panL4Digit = panL4Digit
                }
            };

            return JsonConvert.SerializeObject(request, Formatting.None);
        }

        public async Task AuthorizePayment(int amount, string documentNr, string currencyCode = "981", string panL4Digit = "")
        {
                var url = $"{baseUrl}/v105/executeposcmd";
            var requestJson = $@"
            {{
                ""header"": {{
                    ""command"": ""AUTHORIZE""
                }},
                ""params"": {{
                    ""amount"": {amount},
                    ""cashBackAmount"": 0,
                    ""currencyCode"": ""{currencyCode}"",
                    ""documentNr"": ""{documentNr}"",
                    ""panL4Digit"": ""{panL4Digit}""
                }}
            }}";

            // var requestJson = CreateJson(amount, currencyCode, "DOC123456", "1234");

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("4. Payment Authorized: " + result);
        }

        public async Task RefundPayment(int amount, string documentNr, string stan, string rrn, string currencyCode = "981")
        {
            var url = $"{baseUrl}/v105/executeposcmd";
            var requestJson = $@"
        {{
            ""header"": {{
                ""command"": ""CREDIT""
            }},
            ""params"": {{
                ""amount"": {amount},
                ""currencyCode"": ""{currencyCode}"",
                ""documentNr"": ""{documentNr}"",
                ""STAN"": ""{stan}"",
                ""RRN"": ""{rrn}""
            }}
        }}";

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Refund Processed: " + result);
        }

        public async Task VoidPayment(string operationId)
        {
            var url = $"{baseUrl}/v105/executeposcmd";
            var requestJson = $@"
        {{
            ""header"": {{
                ""command"": ""VOID""
            }},
            ""params"": {{
                ""operationId"": ""{operationId}""
            }}
        }}";

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Transaction Voided: " + result);
        }

        public async Task<TransactionStatus> GetTransactionStatus(string documentNr = "", string operationId = "", string cryptogram = "")
        {
            var transactionStatus = new TransactionStatus();
            var url = $"{baseUrl}/v105/executeposcmd";
            var requestJson = $@"
        {{
            ""header"": {{
                ""command"": ""GETTRNSTATUS""
            }},
            ""params"": {{
                {(string.IsNullOrEmpty(documentNr) ? "" : $@"""documentNr"": ""{documentNr}"",")}
                {(string.IsNullOrEmpty(operationId) ? "" : $@"""operationId"": ""{operationId}"",")}
                {(string.IsNullOrEmpty(cryptogram) ? "" : $@"""cryptogram"": ""{cryptogram}""")}
            }}
        }}";

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("5. Transaction Status: " + result);

            // transactionStatus = await GetTranStatusResponse();
            // transactionStatus = await GetTranStatusResponse();
            return transactionStatus;
        }

        public async Task<TransactionStatus> GetTranStatusResponse()
        {
            var url = $"{baseUrl}/v105/getEvent";
            var transactionStatus = new TransactionStatus();

            while (true)
            {

                var response = await SendPostRequest(url, "{}");
                var result = await response.Content.ReadAsStringAsync();


                if (result.Contains("\"eventName\":\"ONTRNSTATUS\""))
                {
                    Console.WriteLine("6. Event Response: " + result);
                    Console.WriteLine();
                    // Parse the JSON
                    transactionStatus = JsonConvert.DeserializeObject<TransactionStatus>(result);
                    // Output parsed values
                    Console.WriteLine($"Event Name: {transactionStatus.EventName}");
                    Console.WriteLine($"Document Number: {transactionStatus.Properties.DocumentNr}");
                    Console.WriteLine($"State: {transactionStatus.Properties.State}");
                    Console.WriteLine($"Result Code: {transactionStatus.Result.ResultCode}");
                    Console.WriteLine($"Result Message: {transactionStatus.Result.ResultMessage}");
                    break;
                }

                // No need for Task.Delay here if using long polling
                // break;
                // await Task.Delay(1000);

            }
            return transactionStatus;
        }
    }
}
