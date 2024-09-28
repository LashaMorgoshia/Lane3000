using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Desk3500
{
    public class TransactionStatus
    {
        [JsonPropertyName("eventName")]
        public string EventName { get; set; }

        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }

        [JsonPropertyName("result")]
        public Result Result { get; set; }
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
}
