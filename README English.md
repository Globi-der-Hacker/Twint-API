# Twint-API

![TWINT](twint-logo.svg)

> [!CAUTION]
> This is not an official API of TWINT AG. The described calls were determined through reverse-engineering of the Twint Mobile App and may change at any time.

## What can the API do, and what can't it do?

With the help of this API, an **existing** Twint Prepaid account can be linked to your own application. Afterward, the current account balance and the latest transactions can be queried. It is also possible to send money to a mobile phone number.

A **new Twint account cannot** be opened via the API. The API **only works with the prepaid version**, not with Twint accounts that are directly linked to your bank account.

> [!WARNING]
> Once you have linked your Twint account with your own application, you can no longer access it with your mobile phone. However, you can always re-link your account with the official Twint app on your phone.

## What is included in the repository?

The repository contains a Visual Studio Solution with two projects: TwintApi is a library that includes the REST API client, and WinTwint is a sample implementation for a Twint client on Windows.
To build the examples, Visual Studio 2022 and .NET 8 are required.

## Onboarding

To link an existing Twint Prepaid account, the following steps are necessary. These need to be done only once.

```c#
var _api = new TwintApi.TwintApi();

await _api.StartVerifyPhoneNumber("+41791112233");
```

In the background, a **GET** request is sent with the phone number to the following URL:

```http
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber?phoneNumber=%2B41791112233
```

This triggers the sending of an SMS with a verification code to the provided number.
The phone number must be verified with the code received via SMS:

```c#
string tan = "12345";

await _api_.CompleteVerifyPhoneNumber(tan);
```

This sends a **POST** request with the code in the JSON body:

```http
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber
```

```JSON
{
  "tan": "12345"
}
```

In the next step, a random device ID, a self-signed CA certificate and a signing certificate signed with it must be created. We need to memorise (or save) the device ID as it will be needed later.
We also need the PIN with which the existing Twint account is protected (123456 in the example below).

```c#
byte[] randomBytes = new byte[8];
using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
{
  rng.GetBytes(randomBytes);
}
string deviceId = BitConverter.ToString(randomBytes).Replace("-", string.Empty).ToLower();

CertificateHelper.CreateCertificates("c:\\temp\\ca.crt", "c:\\temp\\sign.crt", "c:\\temp\\sign_with_pk.pfx");

string pin = "123456";

var response = await _api.Reboard(
  deviceId,
  "+41791112233",
  pin,
  File.ReadAllText("c:\\temp\\ca.crt"),
  CertificateHelper.GetPublicKeyFingerprint("c:\\temp\\ca.crt"),
  File.ReadAllText("c:\\temp\\sign.crt"),
  CertificateHelper.GetPublicKeyFingerprint("c:\\temp\\sign.crt")
);
```

This sends a **POST** request with JSON body to the following URL:

```http
https://app.issuer.twint.ch/private/routing/v1/reboard
```

```JSON
{
  "caCert": {
    "certificate": "-----BEGIN CERTIFICATE-----\nMIIB3z...\n-----END CERTIFICATE-----\n",
    "fingerprint": "31f1bd3f427a9f499792f762c7b42bbce75a8479629f111ed2b73263c5ed64cd"
  },
  "deviceId": "8b22a56369365885",
  "newPhoneNumber": "+41791112233",
  "oldPhoneNumber": "+41791112233",
  "pin": "123456",
  "signingCert": {
    "certificate": "-----BEGIN CERTIFICATE-----\nMIID3DC...\n-----END CERTIFICATE-----\n",
    "fingerprint": "faba991a730e2eefcccbd2e91f49805feb76d892bd1744437e91a3c58005136f"
  }
}
```

Please note that the certificates (without private key) are specified in PEM format. The ‘fingerprint’ is the SHA256 hash of the public key, formatted as a HEX string.

The response is a data structure from which we have to memorise the values for **privateCustomerUuid**, **deviceUuid** and **devicePassword**:

```JSON
{
  "privateCustomerUuid": "89954CD1-6175-4E0B-94C4-2E7299D7002A",
  "deviceUuid": "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2",
  "devicePassword": "dd([)62$+35*(4f]9]$+",
  "financialAccountId": "DB5265F6-ED69-47C4-B965-149EDEB45ABC"
}
```

## Login-Token

All other API calls require authorisation by means of a JWT token, which is transferred in the HTTP bearer header. The steps listed in the **Onboarding** section must have already been carried out once.

```c#
string deviceId = "8b22a56369365885";
string devicePassword = "dd([)62$+35*(4f]9]$+";
string deviceUuid = "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2";
string pin = "123456";
string privateCustomerUuid = "89954CD1-6175-4E0B-94C4-2E7299D7002A";

var response = await _api_.GetToken(deviceId, devicePassword, deviceUuid, pin, privateCustomerUuid);
```

This sends a **POST** request with JSON body to the following URL:

```http
https://app.issuer.twint.ch/tokens/v2/jwt/privatecustomer/
```

```JSON
{
  "deviceId": "8b22a56369365885",
  "devicePassword": "dd([)62$+35*(4f]9]$+",
  "deviceUuid": "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2",
  "pin": "123456",
  "userUuid": "89954CD1-6175-4E0B-94C4-2E7299D7002A"
}
```

In the response, we receive a JWT token which is required for all further API calls:

```JSON
{
  "category": "CAT3A",
  "lockState": "UNLOCKED",
  "identificationState": "IDENTIFIED",
  "agbVersion": 7,
  "currentAgbVersion": 7,
  "shortLivedToken": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJtc2ciOiJ5b3UgYXJlIGxlZXQhIn0.qZ4dtnyrJrDGmeldVzjR88MKI6TdMdlCmXuEVnrSD9Y",
    "tokenLifetime": 15
  },
  "longLivedToken": {
    "token": "",
    "tokenLifetime": 0
  }
}
```

## Querying the account balance

Once a login token has been received, the account balance can be queried as follows:

```c#
var response = await _api.GetBalance();
```

The **GET** request in the background (the JWT token must be sent in the bearer header):

```http
https://app.issuer.twint.ch/smartphone/service/v8/privateCustomers/account
```

The answer contains the account balance:

```JSON
{
  "balance": {
    "amount": 1,
    "currency": "CHF"
  },
  "clientCategory": "CAT3A",
  "language": "de",
  "trackingEnabled": "NO"
}
```

## Querying the last transactions

It is also only possible to query the list of transactions after a login token has been received:

```c#
var orders = await _api.GetOrders();
```

Optionally, the result can be limited by specifying a date or the maximum number of transactions. If nothing is specified, a maximum of the last 300 transactions are returned.

The **GET** request in the background:

```http
https://app.scheme.twint.ch/smartphone/service/v26/orders?since=2024-01-01T00%3A00%3A00Z&limit=300
```

The response is a data structure that contains an array of transactions in the ‘entries’ element:

```JSON
{
  "entries": [
    {
      "ctlModTs": "2024-01-18T14:00:21Z",
      "ctlCreTs": "2024-01-18T14:00:18Z",
      "orderUuid": "C9F272E4-581F-40B7-BED5-6FB704F81513",
      "sndPhaseTs": "2024-01-18T14:00:18Z",
      "requestedAmount": 1,
      "authorizedAmount": 1,
      "paidAmount": 1,
      "currency": "CHF",
      "merchantConfirmation": false,
      "orderType": "P2P_SEND_MONEY",
      "orderState": "SUCCESSFUL",
      "transactionSide": "CREDIT",
      "p2pHasPicture": false,
      "p2pSenderMobileNr": "+41791112233",
      "p2pRecipientMobileNr": "+41791112233",
      "p2pInitiateMessage": "Hello World!",
      "p2pOrderWasResent": false,
      "voucherType": "NONE",
      "paymentAuthorizationType": "FINAL_AUTH",
      "financialAccountId": "C6514730-8ADD-4BCC-A687-2AB5ED2DE549"
    }
  ],
  "serverUpdateTime": "2024-01-18T14:00:23Z",
  "pageToken": "",
  "groups": []
}
```

## Sending money

Sending money naturally also requires a valid login token. In addition, the key data of the transaction is signed with the signing certificate. This ensures that nobody who has accessed the encrypted connection (man-in-the-middle) can change the transaction.

```c#
decimal amount = 1.00;
string message = "❤️";
string receiverNumber = "+41791112233";
string senderFirstName = "Globi";
string senderLastName = "der Hacker";

await App.Current.TwintApi.Send(
    amount,
    message,
    "",
    "",
    receiverNumber,
    senderFirstName,
    senderLastName,
    "c:\\temp\\sign_with_pk.pfx"
);
```

The **POST** request in the background:

```http
https://app.scheme.twint.ch/smartphone/service/v26/orders/p2p/send
```

```JSON
{
  "amount": {
    "amount": 0.5,
    "currency": "CHF"
  },
  "certificateFingerprint": "faba991a730e2eefcccbd2e91f49805feb76d892bd1744437e91a3c58005136f",
  "message": "❤️",
  "moneyReceiver": {
    "firstName": "",
    "lastName": ""
  },
  "moneyReceiverMobileNumber": "+41791112233",
  "moneySender": {
    "firstName": "Globi",
    "lastName": "der Hacker"
  },
  "orderUuid": "B9269911-F70A-4F97-96DD-3151F1148A8D",
  "reservationDate": "2024-03-05T22:27:03",
  "sendMoneyEvenIfCustomerUnknown": false,
  "signature": "WW91IGFyZSBsZWV0LCBidXQgR2xvYmkgaXMgdG9vIQ=="
}
```

**certificateFingerprint** is the SHA256 hash of the signing certificate's public key, formatted as a HEX string. **orderUuid** is a new random GUID. **signature** is calculated by first creating an XML string with the key data of the transaction:

```XML
<Amount>0.50</Amount><Currency>CHF</Currency><Operation>WITHDRAW</Operation><AuthorizationTimestamp>2024-03-05T22:27:03</AuthorizationTimestamp><OrderUuid>B9269911-F70A-4F97-96DD-3151F1148A8D</OrderUuid>
```

This string is converted into a byte array and signed with the private key of the signing certificate. The signature is then base64 encoded.

If everything went well, the status of the transaction is returned:

```JSON
{
  "orderUuid": "B9269911-F70A-4F97-96DD-3151F1148A8D",
  "orderState": "SUCCESSFUL",
  "customerIsUnknownSendSMS": false
}
```
