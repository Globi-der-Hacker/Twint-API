namespace TwintApi.Request
{
    public class PrivateCustomer
    {
        public string DeviceId { get; set; }
        public string DevicePassword { get; set; }
        public string DeviceUuid { get; set; }
        public string Pin { get; set; }
        public string UserUuid { get; set; }
        public string LongLivedToken { get; set; }
        public string ShortLivedToken { get; set; }
    }
}
