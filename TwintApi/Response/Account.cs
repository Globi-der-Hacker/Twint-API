namespace TwintApi.Response
{
    public class Account
    {
        public BalanceClass Balance { get; set; }
        public string ClientCategory { get; set; }
        public string Language { get; set; }
        public string TrackingEnabled { get; set; }

        public class BalanceClass
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
        }
    }
}
