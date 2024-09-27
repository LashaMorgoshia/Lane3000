namespace Desk3500
{
    public class Request
    {
        public Header header { get; set; }
        public Params @params { get; set; }
    }

    public class Header
    {
        public string command { get; set; }
    }

    public class Params
    {
        public int amount { get; set; }
        public int cashBackAmount { get; set; }
        public string currencyCode { get; set; }
        public string documentNr { get; set; }
        public string panL4Digit { get; set; }
    }
}
