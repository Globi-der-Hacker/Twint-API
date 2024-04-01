# Twint-API

> [!CAUTION]
> Es handelt sich hier nicht um eine offizielle API der TWINT AG. Die beschriebenen Aufrufe wurden durch reverse-engineering der Twint Mobile-App ermittelt und können jederzeit ändern.

## Was kann die API und was nicht?
Mit Hilfe dieser API kann ein **bestehendes** Twint Prepaid-Konto mit einer eigenen Applikation verknüpft werden. Danach können der aktuelle Kontostand sowie die letzten
Transaktionen abgefragt werden. Auch das Senden von Geld an eine Mobiltelefonnummer ist möglich.

Es kann **kein neues** Twint-Konto über die API eröffnet werden. Die API funktioniert **nur mit der Prepaid Variante**, nicht mit Twint-Konten die direkt mit deinem Bankkonto verknüpft sind.

> [!WARNING]
> Nachdem du dein Twint-Konto mit deiner eigenen Applikation verknüpft hast, kanst du nicht mehr mit deinem Handy darauf zugreifen. Du kannst aber dein Konto jederzeit wieder mit der offiziellen Twint-App auf deinem Handy verknüpfen.

## Was ist im Repository enthalten?
Das Repo enthält eine VisualStudio Solution mit zwei Projekten: TwintApi ist eine Bibliothek welche den REST-API Client enthält, WinTwint ist eine Beispielimplementation für einen Twint Client unter Windows.
Um die Beispiele zu erstellen, wird VisualStudio 2022 und .NET 8 benötigt.

## Onboarding
Um ein bestehendes Twint Prepaid-Konto zu verknüpfen, sind die folgenden Schritte notwendig. Diese müssen nur einmal durchgeführt werden.

```c#
var _api = new TwintApi.TwintApi();

await _api.StartVerifyPhoneNumber("+41791112233");
```

Im Hintergrund wird ein **GET** Request mit der Telefonnummer an folgende URL abgesetzt:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber?phoneNumber=%2B41791112233
```

Das löst das Senden einer SMS mit einem Verifizierungscode an die angegebene Nummer aus.
Mit dem über SMS empfangenen Code muss die Telefonnummer verifiziert werden:

```c#
string tan = "12345";

await _api_.CompleteVerifyPhoneNumber(tan);
```

Damit wird eine **POST** Request mit dem Code im JSON Body abgesetzt:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber
```
```JSON
{
  "tan": "12345"
}
```

Im nächsten Schritt muss auf dem Gerät eine zufällige Geräte-Id, ein selbstsigniertes CA-Zertifikat und ein damit signiertes Signging-Zertifikat erstellt werden.
ausserdem benötigen wir den PIN mit dem das bestehende Twint-Konto geschützt ist (im Beispiel unten 123456). Die Zertifikate werden später benötigt um Transaktionen zu signieren.

```c#
byte[] randomBytes = new byte[8];
u (RandomNumberGenerator rng = RandomNumberGenerator.Create())
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

Dadurch wird ein **POST** Request mit JSON Body an folgende URL abgesetzt:

```
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

Man beachte, dass die Zertifikate (ohne privaten Schlüssel) im PEM Format angegeben werden. Beim "fingerprint" handelt es sich jeweils um den SHA256 Hash des öffentlichen Schlüssels, formatiert als HEX-String.

Als Antwort erhält man eine Datenstruktur, aus der wir uns die Werte für **privateCustomerUuid**, **deviceUuid** und **devicePassword** merken müssen:
```JSON
{
  "privateCustomerUuid": "89954CD1-6175-4E0B-94C4-2E7299D7002A",
  "deviceUuid": "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2",
  "devicePassword": "dd([)62$+35*(4f]9]$+",
  "financialAccountId": "DB5265F6-ED69-47C4-B965-149EDEB45ABC"
}
```

## Login-Token
Alle weiteren API Aufrufe benötigen eine Authorisierung mittels einem JWT-Token, welches im HTTP Bearer-Header übergeben wird. Die im Kapitel **Onboarding** aufgeführten Schritte müssen bereits einmal ausgeführt worden sein.

```c#
string deviceId = "8b22a56369365885";
string devicePassword = "dd([)62$+35*(4f]9]$+";
string deviceUuid = "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2";
string pin = "123456";
string privateCustomerUuid = "89954CD1-6175-4E0B-94C4-2E7299D7002A";

var response = await _api_.GetToken(deviceId, devicePassword, deviceUuid, pin, privateCustomerUuid);
```

Damit wird ein **POST** Request mit JSON Body an folgende URL gesendet:
```
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

In der Antwort erhalten wir ein JWT-Token welches für alle weiteren API-Aufrufe benötigt wird:
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

## Abfragen des Kontostandes
Nachdem ein Login-Token erhalten wurde, kann der Kontostand folgendermassen abgefragt werden:

```c#
var response = await _api.GetBalance();
```

Der **GET** Request im Hintergrund (das JWT-Token muss im Bearer Header mitgesendet werden):
```
https://app.issuer.twint.ch/smartphone/service/v8/privateCustomers/account
```

Die Antwort enthält den Kontostand:
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

## Abfragen der letzten Transaktionen
Auch das Abfragen der Liste der Transaktionen ist erst möglich nachdem ein Login-Token erhalten wurde:

```c#
var orders = await _api.GetOrders();
```

Optional kann das Ergebnis durch die Angabe eines Datums oder der maximalen Anzahl Transaktionen begrenzt werden. Wird nichts angegeben, werden maximal die letzten 300 Transaktionen zurückgegeben.

Der **GET** Request im Hintergrund:
```
https://app.scheme.twint.ch/smartphone/service/v26/orders?since=2024-01-01T00%3A00%3A00Z&limit=300
```

Die Antwort ist eine Datenstruktur, die im Element "entries" ein Array von Transaktionen enthält:
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

## Senden von Geld
Das Senden von Geld benötigt natürlich auch ein gültiges Login-Token. Zusätzlich werden die Eckdaten der Transaktion mit dem Signing-Zertifikat signiert. Dadurch wird sichergestellt, dass niemand der sich in die verschlüsselte Verbindung eingeklinkt hat (man-in-the-middle) die Transaktion verändern kann.

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

Der **POST** Request im Hintergrund:

```
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
**certificateFingerprint** ist der SHA256 Hash des öffentlichen Schlüssels des Signing-Zertifikats, als HEX-String formatiert. **orderUuid** ist eine neue zufällige GUID. **signature** wird berechnet, indem zuerst ein XML-String mit den Eckdaten der Transaktion erstellt wird:

```XML
<Amount>0.50</Amount><Currency>CHF</Currency><Operation>WITHDRAW</Operation><AuthorizationTimestamp>2024-03-05T22:27:03</AuthorizationTimestamp><OrderUuid>B9269911-F70A-4F97-96DD-3151F1148A8D</OrderUuid>
```
Dieser String wird in ein Byte-Array umgewandelt und mit dem privaten Schlüssel des Signing-Zertifikats signiert. Die Signatur wird dann base64 codiert.

Hat alles geklappt, erhält man den Status der Transaktion zurück:

```JSON
{
  "orderUuid": "B9269911-F70A-4F97-96DD-3151F1148A8D",
  "orderState": "SUCCESSFUL",
  "customerIsUnknownSendSMS": false
}
```
