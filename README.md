# RateLimiter – Ograničavanje pristupa API endpointima

## Sadržaj
- [Opis](#opis)
- [Konfiguracija](#konfiguracija)
  - [1. Podešavanje u `appsettings.json`](#1-podešavanje-u-appsettingsjson)
  - [2. Registracija servisa](#2-registracija-servisa)
  - [3. Uključivanje middleware-a](#3-uključivanje-middleware-a)
- [Proširenje funkcionalnosti](#proširenje-funkcionalnosti)
  - [1. IRateLimitAlgorithm](#1-iratelimitalgorithm)
  - [2. IEndpointMatcher](#2-iendpointmatcher)
  - [3. IRequestStore](#3-irequeststore)
  - [Registracija custom implementacija](#registracija-custom-implementacija)
- [Testiranje pomoću RateLimiter.Demo aplikacije](#testiranje-pomoću-ratelimiterdemo-aplikacije)

## Opis

RateLimiter je .NET Core biblioteka za ograničavanje broja zahteva prema API endpointima na osnovu IP adrese klijenta. Implementirana je kao middleware u ASP.NET Core aplikacijama.

### Karakteristike
- Ograničavanje broja zahteva po IP adresi
- Globalni limiti za sve endpoint-e
- Specifični limiti za pojedinačne rute
- Konfigurisanje putem `appsettings.json` fajla ili koda
- Proširivost kroz interfejse

---

## Konfiguracija

### 1. Podešavanje u `appsettings.json`

Sekcija "RateLimiter" sadrži parametre za konfiguraciju:

```json
"RateLimiter": {
  "RequestLimiterEnabled": true,
  "DefaultRequestLimitMs": 1000,
  "DefaultRequestLimitCount": 10,
  "EndpointLimits": [
    {
      "Endpoint": "/api/products/books",
      "RequestLimitMs": 1000,
      "RequestLimitCount": 1
    },
    {
      "Endpoint": "/api/products/pencils",
      "RequestLimitMs": 500,
      "RequestLimitCount": 2
    }
  ]
}
```

**Parametri:**

| Parametar                         | Opis                                                                  | Vrednost  |
| --------------------------------- | --------------------------------------------------------------------- | --------- |
| RequestLimiterEnabled             | Uključuje rate limiter funkcionalnosti                                | `boolean` |
| DefaultRequestLimitMs             | Podrazumevani vremenski okvir na broj zahteva za sve endpoint-e       | `integer` |
| DefaultRequestLimitCount          | Limit na uzastopni broj zahteva u vremenskom okviru za sve endpoint-e | `integer` |
|                                   |                                                                       |           |
| EndpointLimits*                   | Lista limita za konkretne endpoint-e                                  |           |
| EndpointLimits/Endpoint*          | Putanja konkretnog endpoint-a                                         | `string`  |
| EndpointLimits/RequestLimitMs*    | Podrazumevani vremenski okvir na broj zahteva za endpoint             | `integer` |
| EndpointLimits/RequestLimitCount* | Limit na uzastopni broj zahteva u vremenskom okviru za endpoint       | `integer` |

---

### 2. Registracija servisa

**Korišćenje konfiguracije iz `appsettings.json` fajla:**
```csharp
builder.Services.AddIpRateLimiter(builder.Configuration);
```

**Korišćenje programskog podešavanja:**
```csharp
builder.Services.AddIpRateLimiter(options =>
{
    options.RequestLimiterEnabled = true;
    options.DefaultRequestLimitMs = 1000;
    options.DefaultRequestLimitCount = 10;
    options.EndpointLimits = new List<EndpointLimitOptions>
    {
        new EndpointLimitOptions
        {
            Endpoint = "/api/products/books",
            RequestLimitMs = 1000,
            RequestLimitCount = 1
        }
    };
});
```

---

### 3. Uključivanje middleware-a

```csharp
app.UseIpRateLimiter();
```

> **Napomena:** RateLimiter middleware treba postaviti pre Auth, CORS i sličnih komponenti.


---

## Proširenje funkcionalnosti

Biblioteka je implementirana tako da podržava proširivost. Korišćenjem definisanih interfejsa moguće je zameniti ili proširiti osnovne komponente (algoritam za limitiranje, način skladištenja podataka, logiku za poređenje putanja) sopstvenim implementacijama.

Sledeći interfejsi se mogu dodatno implementirati:

### 1. IRateLimitAlgorithm

Ovaj interfejs definiše osnovni algoritam za ograničavanje zahteva ("rate limiting").  
Podrazumevana implementacija je `FixedWindowAlgorithm`, koja broji zahteve u okviru unapred definisanog vremenskog prozora (npr. 10 sekundi). Kada broj zahteva iz iste IP adrese (ili specifične rute) premaši dozvoljeni limit u tom prozoru, dalji zahtevi se odbijaju dok prozor ne istekne.

Moguće je implementirati i druge algoritme, kao što su Sliding Window, Token Bucket, Leaky Bucket i dr.

```csharp
public interface IRateLimitAlgorithm
{
    bool ShouldLimitRequest(
        HttpContext context,
        string ipAddress, 
        string path,
        int requestLimitMs, 
        int requestLimitCount);
}
```

---

### 2. IEndpointMatcher

Ovaj interfejs definiše logiku za poređenje putanje zahteva sa konfigurisanom putanjom.
Podrazumevana implementacija je `SimpleEndpointMatcher`, koja vrši poređenje putanja kao običan "case-insensitive" string.

Primer dodatnih mehanizama za poređenje: query string parametri ili route template (npr. `/api/products/{id}`)

```csharp
public interface IEndpointMatcher
{
    bool IsMatch(string requestPath, string configuredPath);
}
```



---

### 3. IRequestStore

Ovaj interfejs definiše način čuvanja i brojanja zahteva. Podrazumevana implementacija je `InMemoryRequestStore`, koja podatke o broju zahteva čuva u memoriji procesa aplikacije.

Primer drugih opcije za skladištenje:
- Redis
- SQL baze podataka
- Drugi distribuirani key-value sistemi

```csharp
public interface IRequestStore
{
    long IncrementRequestCount(string key);
    void Cleanup(long olderThanMs);
}
```



---

### Registracija custom implementacija

Primer registracije:

```csharp
services.AddSingleton<IRateLimitAlgorithm, MyCustomAlgorithm>();
services.AddSingleton<IEndpointMatcher, MyCustomEndpointMatcher>();
services.AddSingleton<IRequestStore, MyCustomRequestStore>();

services.AddIpRateLimiter(configuration);
```

---

## Testiranje pomoću RateLimiter.Demo aplikacije

U repozitorijumu se nalazi demo aplikacija za testiranje funkcionalnosti.

Dostupni endpoint-i:

1. `GET /api/RateLimitTest` – podrazumevani limit (5 zahteva u 10 sekundi)
2. `GET /api/RateLimitTest/limited` – specifičan limit (2 zahteva u 5 sekundi)

Za testiranje se mogu koristiti:
- Swagger UI (`/swagger` u Development okruženju)
- Postman, curl i sl.

Prekoračenje limita rezultuje odgovorom `429 Too Many Requests`.
