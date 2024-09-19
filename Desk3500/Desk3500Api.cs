/*
 * Author: Lasha Morgoshia
 * Created On: 18.09.2024
 * POS Terminal Integration Protocol for DESK 3500
 *
 */


using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine("POS Connection Opened: " + result);
            // Parse and store accessToken from the response
            accessToken = "extracted-access-token"; // Update this based on response parsing
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
            Console.WriteLine("POS Connection Closed: " + result);
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

            var response = await SendPostRequest(url, requestJson);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Payment Authorized: " + result);
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

        public async Task GetTransactionStatus(string documentNr = "", string operationId = "", string cryptogram = "")
        {
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
            Console.WriteLine("Transaction Status: " + result);
        }
    }
}
