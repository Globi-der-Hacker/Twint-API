namespace TwintApi.Request
{
    public class Reboard
    {
        public CaCertClass CaCert { get; set; }
        public string DeviceId { get; set; }
        public string NewPhoneNumber { get; set; }
        public string OldPhoneNumber { get; set; }
        public string Pin { get; set; }
        public SigningCertClass SigningCert { get; set; }

        public class CaCertClass
        {
            public string Certificate { get; set; }
            public string Fingerprint { get; set; }
        }

        public class SigningCertClass
        {
            public string Certificate { get; set; }
            public string Fingerprint { get; set; }
        }
    }
}
