# Twint-API

> [!CAUTION]
> Es handelt sich hier nicht um eine offizielle API der TWINT AG.

## Was kann die API und was nicht?
Mit Hilfe dieser API kann ein **bestehendes** Twint Prepaid-Konto mit einer eigenen Applikation verknüpft werden. Danach können der aktuelle Kontostand sowie die letzten
Transaktionen abgefragt werden. Auch das Senden von Geld an eine Mobiltelefonnummer ist möglich.

Zur Zeit kann noch **kein neues** Twint Konto über die API eröffnet werden. Die API funktioniert nur mit der Prepaid Variante, nicht mit Twint-Konten die direkt mit deinem Bankkonto verknüpft sind.

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

Im Hintergrund wird ein **GET** Request mit der Telefonnummer an folgende URL gesendet:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber?phoneNumber=%2B41791112233
```

Das löst das Senden einer SMS mit einem Verifizierungscode an die angegebene Nummer aus.
Mit dem über SMS empfangenen Code muss die Telefonnummer verifiziert werden:

```c#
string tan = "12345";

await _api_.CompleteVerifyPhoneNumber(tan);
```

Damit wird eine **POST** Request mit dem Code im JSON Body an folgende URL gesendet:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber
{
  "tan": "12345"
}
```

Im nächsten Schritt muss auf dem Gerät eine zufällige Geräte-Id, ein selbstsigniertes CA-Zertifikat und ein damit signiertes Signging-Zertifikat erstellt werden.
ausserdem benötigen wir den PIN mit dem das bestehende Twint-Konto geschützt ist (im Beispiel unten 123456).

```c#
byte[] randomBytes = new byte[8];
using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
{
  rng.GetBytes(randomBytes);
}
string deviceId = BitConverter.ToString(randomBytes).Replace("-", string.Empty).ToLower();

CertificateHelper.CreateCertificates("c:\\temp\\ca.crt", "c:\\temp\\sing.crt", "c:\\temp\\sing_with_pk.crt");

string pin = "123456";

var response = await _api.Reboard(
  deviceId,
  "+41791112233",
  pin,
  File.ReadAllText("c:\\temp\\ca.crt"),
  CertificateHelper.GetPublicKeyFingerprint("c:\\temp\\ca.crt"),
  File.ReadAllText("c:\\temp\\sing.crt"),
  CertificateHelper.GetPublicKeyFingerprint("c:\\temp\\sing.crt")
);
```

Dadurch wird ein **POST** Request an folgende URL abgesetzt:

```
https://app.issuer.twint.ch/private/routing/v1/reboard
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

Man beachte, dass die Zertifikate (ohne privaten Schlüssel) base64-codiert im PEM Format angegeben werden. Beim "fingerprint" handelt es sich jeweils um den SHA256 Hash des öffentlichen Schlüssels.

Als Antwort erhält man eine Datenstruktur, aus der wir uns die Werte für **privateCustomerUuid**, **deviceUuid** und **devicePassword** merken müssen:
```
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

```
https://app.issuer.twint.ch/tokens/v2/jwt/privatecustomer/
{
  "deviceId": "8b22a56369365885",
  "devicePassword": "dd([)62$+35*(4f]9]$+",
  "deviceUuid": "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2",
  "pin": "123456",
  "userUuid": "89954CD1-6175-4E0B-94C4-2E7299D7002A"
}
```

In der Antwort erhalten wir ein JWT-Token welches für alle weiteren API-Aufrufe benötigt wird:
```
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

## Abfragen der letzten Transaktionen

## Senden von Geld
