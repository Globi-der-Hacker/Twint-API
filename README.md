# Twint-API

[!CAUTION]
> Es handelt sich hier nicht um eine offizielle API der TWINT AG.

## Was kann die API und was nicht?
Mit Hilfe dieser API kann ein **bestehendes** Twint Prepaid-Konto mit einer eigenen Applikation verknüpft werden. Danach kann der aktuelle Kontostand sowie die letzten
Transaktionen abgefragt werden. Auch das Senden von Geld an eine Mobiltelefonnummer ist möglich.

Zur Zeit kann noch **kein neues** Twint Konto über die API eröffnet werden. Die API funktioniert nur mit der Prepaid Variante, nicht mit Twint-Konten die direkt mit deinem Bankkonto verknüpft sind.

## Was ist im Repository enthalten?
Das Repo enthält eine VisualStudio Solution mit zwei Projekten: TwintApi ist eine Bibliothek welche den REST-API Client enthält, WinTwint ist eine Beispielimplementation für einen Twint Client unter Windows.
Um die Beispiele zu erstellen, wird VisualStudio 2022 und .NET 8 benötigt.

## Onboarding
Um ein bestehendes Twint Prepaid-Konto zu verknüpfen sind folgende Schritte notwendig:
```c#
var _api = new TwintApi.TwintApi();
_api.StartVerifyPhoneNumber("+41791112233");
```

Sendet einen **GET** Request mit der Telefonnummer an folgende URL:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber?phoneNumber=%2B41791112233
```

Mit dem über SMS empfangenen Code muss die Telefonnummer verifiziert werden:

```c#
string tan = "12345";
_api_.CompleteVerifyPhoneNumber(tan);
```

Sendet einen **POST** Request mit JSON Body an folgende URL:
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

var response = await _api.Reboard(
  deviceId,
  "+41791112233",
  "123456",
  File.ReadAllText("c:\\temp\\ca.crt"),
  CertificateHelper.GetPublicKeyFingerprint("c:\\temp\\ca.crt"),
  File.ReadAllText("c:\\temp\\sing.crt"),
  CertificateHelper.GetPublicKeyFingerprint("c:\\temp\\sing.crt")
);
```

