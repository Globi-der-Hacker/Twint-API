using RestSharp;

using TwintApi.Crypto;

namespace TwintApi
{
    public class TwintApi
    {
        private RestClient client;
        private System.Net.CookieContainer cookies = new System.Net.CookieContainer();

        private string bearerToken;
        private DateTime tokenLifetime;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public TwintApi()
        {
            var options = new RestClientOptions("https://app.issuer.twint.ch/");
            client = new RestClient(options);
        }

        /// <summary>
        /// Start verifying a phone. This will trigger am SMS sent to the given phone number.
        /// </summary>
        /// <param name="phoneNumber">phone number</param>
        public async Task StartVerifyPhoneNumber(string phoneNumber)
        {
            var request = new RestRequest("private/routing/v1/verifyPhoneNumber").AddQueryParameter("phoneNumber", phoneNumber);
            request.CookieContainer = cookies;
            var response = await client.ExecuteGetAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }
        }

        /// <summary>
        /// Complete verifying a phone number by entering the code received in an SMS.
        /// </summary>
        /// <param name="tan">code from SMS</param>
        /// <returns>VerifyPhoneNumber</returns>
        public async Task<Response.VerifyPhoneNumber> CompleteVerifyPhoneNumber(string tan)
        {
            var body = new Request.VerifyPhoneNumber
            {
                Tan = tan
            };

            var request = new RestRequest("private/routing/v1/verifyPhoneNumber", Method.Post).AddJsonBody(body);
            request.CookieContainer = cookies;
            var response = await client.ExecutePostAsync<Response.VerifyPhoneNumber>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            return response.Data;
        }

        /// <summary>
        /// Link a device to an existing account.
        /// </summary>
        /// <param name="deviceId">unique device id</param>
        /// <param name="phoneNumber">phone number</param>
        /// <param name="pin">Twint PIN</param>
        /// <param name="caCert">CA certificate (base64 encoded)</param>
        /// <param name="caCertFingerprint">SHA256 hash of certificate public key</param>
        /// <param name="signingCert">signing certificate (base64 encoded)</param>
        /// <param name="signingCertFingerprint">SHA256 hash of certificate public key</param>
        /// <returns>Reboard</returns>
        public async Task<Response.Reboard> Reboard(string deviceId, string phoneNumber, string pin, string caCert, string caCertFingerprint, string signingCert, string signingCertFingerprint)
        {
            var body = new Request.Reboard
            {
                CaCert = new Request.Reboard.CaCertClass
                {
                    Certificate = caCert,
                    Fingerprint = caCertFingerprint
                },
                DeviceId = deviceId,
                NewPhoneNumber = phoneNumber,
                OldPhoneNumber = phoneNumber,
                Pin = pin,
                SigningCert = new Request.Reboard.SigningCertClass
                {
                    Certificate = signingCert,
                    Fingerprint = signingCertFingerprint
                }
            };

            var request = new RestRequest("private/routing/v1/reboard").AddJsonBody(body);
            request.CookieContainer = cookies;
            var response = await client.ExecutePostAsync<Response.Reboard>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            return response.Data;
        }

        /// <summary>
        /// Get the bearer token for authentication.
        /// </summary>
        /// <param name="deviceId">unique device id</param>
        /// <param name="devicePassword">device password</param>
        /// <param name="deviceUuid">device uuid</param>
        /// <param name="pin">Twint PIN</param>
        /// <param name="userUuid">unique user id</param>
        /// <returns>PrivateCustomer</returns>
        public async Task<Response.PrivateCustomer> GetToken(string deviceId, string devicePassword, string deviceUuid, string pin, string userUuid)
        {
            var body = new Request.PrivateCustomer
            {
                DeviceId = deviceId,
                DevicePassword = devicePassword,
                DeviceUuid = deviceUuid,
                Pin = pin,
                UserUuid = userUuid
            };

            var request = new RestRequest("tokens/v2/jwt/privatecustomer").AddJsonBody(body);
            var response = await client.ExecutePostAsync<Response.PrivateCustomer>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            bearerToken = response.Data.ShortLivedToken.Token;
            tokenLifetime = DateTime.UtcNow.AddMinutes(response.Data.ShortLivedToken.TokenLifetime);

            return response.Data;
        }

        /// <summary>
        /// Query account balance.
        /// </summary>
        /// <returns>Account</returns>
        public async Task<Response.Account> GetBalance()
        {
            var request = new RestRequest("smartphone/service/v8/privateCustomers/account");
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            var response = await client.ExecuteGetAsync<Response.Account>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            return response.Data;
        }

        /// <summary>
        /// Query user name.
        /// </summary>
        /// <returns>Response.Name</returns>
        public async Task<Response.Name> GetName()
        {
            var request = new RestRequest("smartphone/twint-issuer/v1/customers/names");
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            var response = await client.ExecuteGetAsync<Response.Name>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            return response.Data;
        }

        /// <summary>
        /// Query list of recent transactions.
        /// </summary>
        /// <param name="since">timestamp after which transactions should be returned</param>
        /// <param name="limit">maximum number of transactions returned</param>
        /// <returns>list of transactions</returns>
        public async Task<List<Response.Order.Entry>> GetOrders(DateTime? since = null, int limit = 300)
        {
            string sinceStr = (since ?? DateTime.MinValue).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var request = new RestRequest("smartphone/service/v26/orders")
                .AddQueryParameter("since", sinceStr)
                .AddQueryParameter("limit", limit);
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            var response = await client.ExecuteGetAsync<Response.Order>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            return response.Data.Entries;
        }

        /// <summary>
        /// Send money.
        /// </summary>
        /// <param name="amount">amount in CHF</param>
        /// <param name="message">optionally a message to the receiver</param>
        /// <param name="receiverFirstName">receiver first name</param>
        /// <param name="receiverLastName">receiver last name</param>
        /// <param name="receiverNumber">receiver phone number</param>
        /// <param name="senderFirstName">sender first name</param>
        /// <param name="senderLastName">sender last name</param>
        /// <param name="signatureCert">signature certificate including private key (base64 encoded)</param>
        /// <returns>Send</returns>
        public async Task<Response.Send> Send(decimal amount, string message, string receiverFirstName, string receiverLastName, string receiverNumber, string senderFirstName, string senderLastName, string signatureCert)
        {
            // create a new id for this transaction
            Guid orderUid = Guid.NewGuid();
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            // this XML fragment is signed with the signing certificates private key
            string xmlToSign = $"<Amount>{amount:0.00}</Amount><Currency>CHF</Currency><Operation>WITHDRAW</Operation><AuthorizationTimestamp>{timestamp}</AuthorizationTimestamp><OrderUuid>{orderUid}</OrderUuid>";

            var body = new Request.Send
            {
                Amount = new Request.Send.AmountClass
                {
                    Amount = amount,
                    Currency = "CHF"
                },
                CertificateFingerprint = CertificateHelper.GetPublicKeyFingerprint(signatureCert),
                Message = message,
                MoneyReceiver = new Request.Send.Name
                {
                    FirstName = receiverFirstName,
                    LastName = receiverLastName
                },
                MoneyReceiverMobileNumber = receiverNumber,
                MoneySender = new Request.Send.Name
                {
                    FirstName = senderFirstName,
                    LastName = senderLastName
                },
                OrderUuid = orderUid.ToString(),
                ReservationDate = timestamp,
                SendMoneyEvenIfCustomerUnknown = false,
                Signature = CertificateHelper.Sign(signatureCert, xmlToSign)
            };

            var request = new RestRequest("smartphone/service/v26/orders/p2p/send", Method.Post).AddJsonBody(body);
            request.AddHeader("Authorization", "Bearer " + bearerToken);

            var response = await client.ExecutePostAsync<Response.Send>(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new TwintException(response);
            }

            return response.Data;
        }

        /// <summary>
        /// Gets the bearer token.
        /// </summary>
        public string BearerToken => bearerToken;

        /// <summary>
        /// Gets a formatted version of the bearer token for displaying.
        /// </summary>
        public string BearerTokenForDisplay => (bearerToken ?? "").Length > 64 ? bearerToken.Substring(0, 64) + "..." : "";
    }
}
