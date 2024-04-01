namespace TwintApi.Response
{
    public class PrivateCustomer
    {
        public string Category { get; set; }
        public string LockState { get; set; }
        public string IdentificationState { get; set; }
        public int AgbVersion { get; set; }
        public double CurrentAgbVersion { get; set; }
        public ShortLivedTokenClass ShortLivedToken { get; set; }
        public LongLivedTokenClass LongLivedToken { get; set; }

        public class ShortLivedTokenClass
        {
            public string Token { get; set; }
            public int TokenLifetime { get; set; }
        }

        public class LongLivedTokenClass
        {
            public string Token { get; set; }
            public int TokenLifetime { get; set; }
        }
    }
}
