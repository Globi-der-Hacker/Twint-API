# Twint-API
Twint payment API

## Was kann die API und was nicht?
Mit Hilfe dieser API kann ein **bestehendes** Twint Prepaid-Konto mit einer eigenen Applikation verkn�pft werden. Danach kann der aktuelle Kontostand sowie die letzten
Transaktionen abgefragt werden. Auch das Senden von Geld an eine Mobiltelefonnummer ist m�glich.

Zur Zeit kann noch **kein neues** Twint Konto �ber die API er�ffnet werden. Die API funktioniert nur mit der Prepaid Variante, nicht mit Twint-Konten die direkt mit deinem Bankkonto verkn�pft sind.

## Was ist im Repository enthalten?
Das Repo enth�lt eine VisualStudio Solution mit zwei Projekten: TwintApi ist eine Bibliothek welche den REST-API Client enth�lt, WinTwint ist eine Beispielimplementation f�r einen Twint Client unter Windows.
Um die Beispiele zu erstellen, wird VisualStudio 2022 und .NET 8 ben�tigt.

## Onboarding
Um ein bestehendes Twint Prepaid-Konto zu verkn�pfen sind folgende Schritte notwendig:
```c#
var _api = new TwintApi.TwintApi();
_api.StartVerifyPhoneNumber("+41791112233");
```

Sendet einen **GET** Request mit der Telefonnummer an folgende URL:
```
https://app.issuer.twint.ch/private/routing/v1/verifyPhoneNumber?phoneNumber=%2B41791112233
```

Mit dem �ber SMS empfangenen Code muss die Telefonnummer verifiziert werden:

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
