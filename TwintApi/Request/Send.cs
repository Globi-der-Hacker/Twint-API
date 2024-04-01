namespace TwintApi.Request
{
    public class Send
    {
        public AmountClass Amount { get; set; }
        public string CertificateFingerprint { get; set; }
        public string Message { get; set; }
        public Name MoneyReceiver { get; set; }
        public string MoneyReceiverMobileNumber { get; set; }
        public Name MoneySender { get; set; }
        public string OrderUuid { get; set; }
        public string ReservationDate { get; set; }
        public bool SendMoneyEvenIfCustomerUnknown { get; set; }
        public string Signature { get; set; }

        public record AmountClass
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Name
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
