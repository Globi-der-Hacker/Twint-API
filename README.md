# Twint-API

> [!CAUTION]
> Es handelt sich hier nicht um eine offizielle API der TWINT AG.

## Was kann die API und was nicht?
Mit Hilfe dieser API kann ein **bestehendes** Twint Prepaid-Konto mit einer eigenen Applikation verkn�pft werden. Danach k�nnen der aktuelle Kontostand sowie die letzten
Transaktionen abgefragt werden. Auch das Senden von Geld an eine Mobiltelefonnummer ist m�glich.

Zur Zeit kann noch **kein neues** Twint Konto �ber die API er�ffnet werden. Die API funktioniert nur mit der Prepaid Variante, nicht mit Twint-Konten die direkt mit deinem Bankkonto verkn�pft sind.

> [!WARNING]
> Nachdem du dein Twint-Konto mit deiner eigenen Applikation verkn�pft hast, kanst du nicht mehr mit deinem Handy darauf zugreifen. Du kannst aber dein Konto jederzeit wieder mit der offiziellen Twint-App auf deinem Handy verkn�pfen.

## Was ist im Repository enthalten?
Das Repo enth�lt eine VisualStudio Solution mit zwei Projekten: TwintApi ist eine Bibliothek welche den REST-API Client enth�lt, WinTwint ist eine Beispielimplementation f�r einen Twint Client unter Windows.
Um die Beispiele zu erstellen, wird VisualStudio 2022 und .NET 8 ben�tigt.

## Onboarding
Um ein bestehendes Twint Prepaid-Konto zu verkn�pfen, sind die folgenden Schritte notwendig. Diese m�ssen nur einmal durchgef�hrt werden.

```c#
var _api = new TwintApi.TwintApi();

await _api.StartVerifyPhoneNumber("+41791112233");
```

Im Hintergrund wird ein **GET** Request mit der Telefonnummer an folgende URL gesendet:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber?phoneNumber=%2B41791112233
```

Das l�st das Senden einer SMS mit einem Verifizierungscode an die angegebene Nummer aus.
Mit dem �ber SMS empfangenen Code muss die Telefonnummer verifiziert werden:

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

Im n�chsten Schritt muss auf dem Ger�t eine zuf�llige Ger�te-Id, ein selbstsigniertes CA-Zertifikat und ein damit signiertes Signging-Zertifikat erstellt werden.
ausserdem ben�tigen wir den PIN mit dem das bestehende Twint-Konto gesch�tzt ist (im Beispiel unten 123456).

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

Man beachte, dass die Zertifikate (ohne privaten Schl�ssel) base64-codiert im PEM Format angegeben werden. Beim "fingerprint" handelt es sich jeweils um den SHA256 Hash des �ffentlichen Schl�ssels.

Als Antwort erh�lt man eine Datenstruktur, aus der wir uns die Werte f�r **privateCustomerUuid**, **deviceUuid** und **devicePassword** merken m�ssen:
```
{
  "privateCustomerUuid": "89954CD1-6175-4E0B-94C4-2E7299D7002A",
  "deviceUuid": "46C54FEE-C9EB-4DCD-9463-D29A6CE276D2",
  "devicePassword": "dd([)62$+35*(4f]9]$+",
  "financialAccountId": "DB5265F6-ED69-47C4-B965-149EDEB45ABC"
}
```

## Login-Token
Alle weiteren API Aufrufe ben�tigen eine Authorisierung mittels einem JWT-Token, welches im HTTP Bearer-Header �bergeben wird. Die im Kapitel **Onboarding** aufgef�hrten Schritte m�ssen bereits einmal ausgef�hrt worden sein.

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

In der Antwort erhalten wir ein JWT-Token welches f�r alle weiteren API-Aufrufe ben�tigt wird:
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
