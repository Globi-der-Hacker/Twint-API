namespace TwintApi.Response
{
    public class Order
    {
        public List<Entry> Entries { get; set; }
        public DateTime ServerUpdateTime { get; set; }
        public string PageToken { get; set; }
        public List<object> Groups { get; set; }

        public class Entry
        {
            public DateTime CtlModTs { get; set; }
            public DateTime CtlCreTs { get; set; }
            public string OrderUuid { get; set; }
            public DateTime SndPhaseTs { get; set; }
            public decimal RequestedAmount { get; set; }
            public decimal AuthorizedAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public string Currency { get; set; }
            public bool MerchantConfirmation { get; set; }
            public string OrderType { get; set; }
            public string OrderState { get; set; }
            public string TransactionSide { get; set; }
            public bool P2pHasPicture { get; set; }
            public string P2pSenderMobileNr { get; set; }
            public string P2pRecipientMobileNr { get; set; }
            public string P2pInitiateMessage { get; set; }
            public bool P2pOrderWasResent { get; set; }
            public string VoucherType { get; set; }
            public string PaymentAuthorizationType { get; set; }
            public string FinancialAccountId { get; set; }
        }
    }
}
