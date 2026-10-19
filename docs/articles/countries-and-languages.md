# Countries and languages

HeroKit 3.0 does not add a new set of methods for every country. It has one set of methods that take a country or a language. Türkiye and the United States are built in. Other countries use the same methods once you describe them.

## The model

| Type | What it holds | Built in |
|---|---|---|
| `Country` | ISO code, name, language, currency and optional rules: phone numbers, vehicle plates, postal codes, national and tax ids, public holidays, regions, IBAN lengths | `Countries.Turkey`, `Countries.UnitedStates` |
| `Language` | ISO code and optional rules: casing and sorting (`TextRules`), numbers in words (`NumberSpeller`) | `Languages.Turkish`, `Languages.English` |
| `Currency` | ISO code, symbol, separators, unit names | `Currencies.TurkishLira`, `Currencies.USDollar` |

All types are in the `HeroKit.Globalization` namespace. Türkiye-only checks (TCKN, VKN, e-invoice) are in `HeroKit.Globalization.Turkey`.

```csharp
using HeroKit.Globalization;

"0532 123 45 67".IsValidPhone(Countries.Turkey);   // true
"34 ABC 123".IsValidVehiclePlate(Countries.Turkey); // true
1234.5m.ToCurrencyString(Currencies.TurkishLira);   // "₺1.234,50"
"istanbul".ToUpper(Languages.Turkish);              // "İSTANBUL"
```

Every rule of a country is optional. A method that needs a rule the country does not have throws `NotSupportedException`.

None of these rules use ICU or the culture data of the operating system. The results are the same on every machine, also with `InvariantGlobalization`.

## Add your own country

Implement the rule interfaces you need, create a `Country` and register it:

```csharp
using HeroKit.Globalization;

public sealed class SixDigitPhoneRules : IPhoneNumberRules
{
    public bool IsValid(string? value) => value is { Length: 6 } && value.All(char.IsAsciiDigit);
    public bool IsMobile(string? value) => IsValid(value) && value![0] == '9';
    public string? ToE164(string? value) => IsValid(value) ? "+999" + value : null;
    public string? Format(string? value) => IsValid(value) ? $"{value![..3]} {value[3..]}" : null;
}

var testland = new Country("XT", "Testland", Languages.Turkish)
{
    PhoneNumbers = new SixDigitPhoneRules(),
};

Countries.Register(testland);

"912345".IsValidPhone(testland);        // true
"912345".IsValidMobilePhone(testland);  // true
"912345".ToE164(testland);              // "+999912345"
"912345".FormatPhone(testland);         // "912 345"
```

The other rule properties are `VehiclePlates` (`IVehiclePlateRules`), `PostalCodes`, `NationalIds`, `TaxIds` and `IbanRules` (`IIdentifierRules`), `Holidays` (`IHolidayCalendar`), `Regions` and `Currency`.

`Countries.Register` also makes the country available to the validation attributes by its ISO code: `[PhoneNumber("XT")]` works after the country is registered. `Countries.Get("XT")`, `Countries.TryGet` and `Countries.All` read the registry.

A new language works the same way: create `new Language("xx", "Name") { TextRules = ..., NumberSpeller = ... }` with your own `ITextRules` and `INumberSpeller`.

## United States

`Countries.UnitedStates`, `Languages.English` and `Currencies.USDollar` use the same methods:

```csharp
using HeroKit.Globalization;

var us = Countries.UnitedStates;

"(212) 555-0123".IsValidPhone(us);                  // true
"+1 212 555 0123".FormatPhone(us);                  // "(212) 555-0123"
"212.555.0123".ToE164(us);                          // "+12125550123"
"10001-1234".IsValidPostalCode(us);                 // true (ZIP or ZIP+4)
"123-45-6789".IsValidNationalId(us);                // true (SSN)
"12-3456789".IsValidTaxId(us);                      // true (EIN; SSN and ITIN are also accepted)
us.FindRegion("ca")?.Name;                          // "California" (FIPS code 6)
new DateOnly(2026, 7, 2).AddBusinessDays(1, us);    // 2026-07-06 (July 3 is the observed Independence Day)
1234.5m.ToCurrencyString(Currencies.USDollar);      // "$1,234.50"
123.50m.ToCurrencyWords(Currencies.USDollar);       // "one hundred twenty-three dollars and fifty cents"
2_500_340L.ToWords(Languages.English);              // "two million five hundred thousand three hundred forty"
```

- **Phone numbers** follow the North American Numbering Plan: a 10-digit number whose area code and exchange start with 2–9, with an optional `+1`, `1` or `001` prefix. US numbers do not show whether a line is mobile, so `IsValidMobilePhone` accepts every valid number.
- **SSN, ITIN and EIN** checks are structural. They reject numbers that are never issued (for example SSN area 000, 666 or 900–999), but they cannot prove that a number was issued.
- **Public holidays** are the 11 federal holidays from 1986 on, with Juneteenth from 2021. A holiday on a Saturday is observed on the Friday before, a holiday on a Sunday on the Monday after. Observed days are separate entries named "… (observed)". State holidays are not included.
- **Regions** are the 50 states and the District of Columbia. `FindRegion` accepts the FIPS code, the USPS abbreviation or the name.
- **Vehicle plates** differ by state, so `IsValidVehiclePlate(Countries.UnitedStates)` throws `NotSupportedException`.
- **English text rules** use invariant casing. `Languages.English.TextRules!.Comparer` sorts case-insensitively and puts lower case first: `apple, Apple, banana`.

## Türkiye

### Identity and tax numbers

```csharp
using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

"10000000146".IsValidTckn();                        // true (TCKN)
"4840847211".IsValidVkn();                          // true (VKN)
"10000000146".IsValidNationalId(Countries.Turkey);  // true (TCKN)
"4840847211".IsValidTaxId(Countries.Turkey);        // true: VKN or TCKN
"0879001756600379".IsValidMersis();                 // true: "0", VKN, 5-digit serial
"firma@hs01.kep.tr".IsValidKepAddress();            // true: e-mail address on a .kep.tr domain
"A12B34567".IsValidIdCardSerial();                  // true: serial of the chip identity card
```

These are checksum checks; `IsValidMersis` checks the VKN inside the MERSİS number. They confirm that a number is well formed, not that it is issued. Input must contain digits only. Step-by-step guides with the algorithms: [TCKN](tckn-validation.md), [VKN](vkn-validation.md).


#### A validated TCKN value

`Tckn` holds a TCKN that has passed the check. Validate once where the value enters the system, then pass `Tckn` instead of `string`:

```csharp
Tckn id = Tckn.Parse("10000000146"); // FormatException for an invalid number
Tckn.TryParse(input, out Tckn checkedId);
id.ToMaskedString();                 // "10*******46" for logs

public sealed record Customer(Tckn Id, string Name); // JSON: {"Id":"10000000146", ...}; an invalid number fails deserialization
app.MapGet("/customers/{id}", (Tckn id) => ...);    // minimal APIs bind it; an invalid number returns 400
```

`Tckn` implements `IParsable<Tckn>` and has a JSON converter (also for the System.Text.Json source generator and Native AOT) and a type converter for MVC model binding. `default(Tckn)` holds no number: `IsEmpty` is true.

### IBAN

```csharp
using HeroKit.Banking;

var iban = "tr33 0006 1005 1978 6457 8413 26";

iban.IsValidIban();   // true
iban.NormalizeIban(); // "TR330006100519786457841326"
iban.FormatIban();    // "TR33 0006 1005 1978 6457 8413 26"

"TR78 0006 2000 0000 0006 2950 05".GetBank()?.Name; // "Türkiye Garanti Bankası A.Ş."
Countries.Turkey.Banks!.All;                        // 30 banks with their 5-digit EFT codes
```

IBAN validation works for every country with the ISO 13616 mod-97 check. When a registered country has `IbanRules`, its length and fixed positions are checked too: Türkiye IBANs must have 26 characters. See the [IBAN validation guide](iban-validation.md) for the algorithm.

`GetBank` reads the bank code of a valid IBAN (positions 5–9 for Türkiye) and looks it up in the bank directory of the country (`Country.Banks`). It returns null for an unknown code. The list follows the EFT codes of the Banks Association of Türkiye; open an issue when a bank is missing.

### Regions, plates and postal codes

```csharp
using HeroKit.Globalization;

Countries.Turkey.FindRegion("34")?.Name;        // "İstanbul"
Countries.Turkey.FindRegion(6)?.Name;           // "Ankara"
Countries.Turkey.FindRegion("sanliurfa")?.Code; // 63 (case and diacritics ignored)
Countries.Turkey.Regions.Count;                 // 81

Countries.Turkey.GetDistricts("istanbul").Count;              // 39
Countries.Turkey.FindDistrict("istanbul", "kadikoy")?.Name;   // "Kadıköy"
Countries.Turkey.FindDistrict(1421)?.RegionCode;              // 34

"34abc123".FormatVehiclePlate(Countries.Turkey);   // "34 ABC 123"
"06 A 1234".IsValidVehiclePlate(Countries.Turkey); // true
"34 Q 1234".IsValidVehiclePlate(Countries.Turkey); // false: Q is not a plate letter

"34710".IsValidPostalCode(Countries.Turkey);       // true
```

`Regions` is a list of `Region(Code, Name)`. For Türkiye the regions are the 81 provinces. `Districts` holds the 973 districts (ilçe) as `District(Code, Name, RegionCode)` with their official MERNİS codes, in Turkish alphabetical order per province. Neighbourhoods and streets are not included: they are tens of thousands of rows and change often.

### Phone numbers

```csharp
"0532 123 45 67".ToE164(Countries.Turkey);          // "+905321234567"
"+905321234567".FormatPhone(Countries.Turkey);      // "0532 123 45 67"
"(0212) 555 12 34".IsValidPhone(Countries.Turkey);  // true
"0212 555 12 34".IsValidMobilePhone(Countries.Turkey); // false
```

Accepted prefixes: `+90`, `0090`, `90`, `0` or none. The check is structural, not an operator or area-code allow-list.

### Public holidays and business days

```csharp
new DateOnly(2026, 10, 29).IsPublicHoliday(Countries.Turkey);   // true  (Cumhuriyet Bayramı)
new DateOnly(2026, 10, 28).IsHalfDayHoliday(Countries.Turkey);  // true  (afternoon off)
new DateOnly(2026, 3, 20).GetPublicHoliday(Countries.Turkey)?.Name; // "Ramazan Bayramı 1. Gün"

new DateOnly(2026, 3, 19).AddBusinessDays(1, Countries.Turkey); // 2026-03-23, skips the bayram and the weekend
new DateOnly(2026, 10, 26).CountBusinessDays(new DateOnly(2026, 10, 30), Countries.Turkey); // 2.5: 28 October is a half day

foreach (PublicHoliday holiday in Countries.Turkey.Holidays!.GetHolidays(2026))
    Console.WriteLine($"{holiday.Date:dd.MM.yyyy} {holiday.Name}{(holiday.IsHalfDay ? " (yarım gün)" : "")}");
```

- Fixed holidays follow Law No. 2429. Ramazan and Kurban Bayramı dates come from the Diyanet calendar and are available for `FirstSupportedYear`–`LastSupportedYear` of the calendar (2022–2027). Other years throw instead of returning incomplete data.
- Business days are Monday to Friday without full-day holidays. A half day (arife and 28 October) is a business day for `IsBusinessDay` and `AddBusinessDays`, and counts as 0.5 in `CountBusinessDays`. `BusinessDaysUntil` (obsolete since 3.7.0) counts it as a full day.
- Administrative bridge days (idari izin) are announced case by case and are not built in. Add them, or your company's holidays, to a copy of the country:

```csharp
using HeroKit.Globalization;

Country turkeyWithLeave = Countries.Turkey.WithAdditionalHolidays(
[
    new PublicHoliday(new DateOnly(2026, 5, 18), "İdari izin", IsHalfDay: false),
    new PublicHoliday(new DateOnly(2026, 12, 31), "Yılbaşı arifesi", IsHalfDay: true),
]);

new DateOnly(2026, 5, 18).IsBusinessDay(turkeyWithLeave);  // false: 18 May is added as administrative leave
new DateOnly(2026, 5, 18).IsBusinessDay(Countries.Turkey); // true: Countries.Turkey itself does not change
```

  A day you add replaces a holiday on the same date. `WithHolidays(calendar)` swaps the whole calendar for your own `IHolidayCalendar`. Register the copy as a singleton and use it everywhere instead of `Countries.Turkey`.
- `IsBusinessDay(country)` is also available. A custom country gets all of these methods when it has an `IHolidayCalendar`.

### Today in Türkiye

A server usually runs in UTC. Between midnight in Türkiye and 03:00 UTC, `DateTime.Today` on the server still shows yesterday. `Country.Today()` uses the country's time zone:

```csharp
using HeroKit.Globalization;

Countries.Turkey.Today();                                  // today's date in Türkiye
Countries.Turkey.Now();                                    // e.g. 2026-10-19 01:30 +03:00
Countries.Turkey.Today().IsBusinessDay(Countries.Turkey);
Countries.Turkey.Today(TimeProvider.System);              // pass a fake TimeProvider in tests
```

`TimeZone` is Europe/Istanbul. A country with more than one time zone (the United States) has no `TimeZone`, and `Today()` throws `NotSupportedException`.

### Turkish lira

```csharp
1234.5m.ToCurrencyString(Currencies.TurkishLira);                              // "₺1.234,50"
1234.5m.ToCurrencyString(Currencies.TurkishLira, CurrencyStyle.Abbreviation);  // "1.234,50 TL"
1234.5m.ToCurrencyString(Currencies.TurkishLira, CurrencyStyle.IsoCode);       // "1.234,50 TRY"
```

The currency carries its own separators, so the result does not depend on the current culture.

### Amounts in words

```csharp
123.ToWords(Languages.Turkish);                        // "yüz yirmi üç"
1_001_000L.ToWords(Languages.Turkish);                 // "bir milyon bin"
123.50m.ToCurrencyWords(Currencies.TurkishLira);
// "yüz yirmi üç Türk lirası elli kuruş"

var euro = new Currency("EUR", "€", Languages.Turkish) { UnitName = "Euro", SubunitName = "sent" };
10.20m.ToCurrencyWords(euro);
// "on Euro yirmi sent"
```

### Casing and sorting without ICU

```csharp
"istanbul ılıca".ToTitleCase(Languages.Turkish);   // "İstanbul Ilıca"
"istanbul".ToUpper(Languages.Turkish);             // "İSTANBUL"
"IŞIK".ToLower(Languages.Turkish);                 // "ışık"
"IŞIK".EqualsIgnoreCase("ışık", Languages.Turkish); // true
"ISIK".EqualsIgnoreCase("isik", Languages.Turkish); // false: I is the capital of ı
```

For other cultures use `ToTitleCase(culture)` and `EqualsIgnoreCase(other, culture)` from `HeroKit.Strings`.

The Turkish comparers follow the Turkish alphabet (c, ç, g, ğ, ı, i, o, ö, s, ş, u, ü) without ICU. This also works in containers and apps that run with invariant globalization, where culture-based comparison does not follow the Turkish alphabet.

```csharp
using HeroKit.Globalization;

var comparer = Languages.Turkish.TextRules!.Comparer;

string[] names = ["Zeynep", "Çağla", "Ilgın", "İpek", "Cem", "Şule", "Oya", "Ömer", "Uğur", "Ümit", "Gül"];
Array.Sort(names, comparer);
// Cem, Çağla, Gül, Ilgın, İpek, Oya, Ömer, Şule, Uğur, Ümit, Zeynep

var sorted = customers.OrderBy(c => c.Name, comparer).ThenBy(c => c.City, comparer);

var cities = new Dictionary<string, int>(Languages.Turkish.TextRules!.IgnoreCaseComparer) { ["İzmir"] = 35 };
cities.ContainsKey("izmir"); // true
cities.ContainsKey("IZMIR"); // false: I is ı, not i
```

`Comparer` sorts lower case first. `IgnoreCaseComparer` ignores case with the Turkish rules and can be used as a dictionary or hash set comparer.

### e-Fatura and e-Arşiv

```csharp
using HeroKit.Globalization.Turkey;

"ABC2026000000001".IsValidGibInvoiceNumber(); // true
"ABC2026000000000".IsValidGibInvoiceNumber(); // false: the sequence starts at 1
"51d3bb18-9650-4c8e-825d-5a7f845be630".IsValidEttn(); // true

GibInvoiceNumber.TryParse("ABC2026000000001", out GibInvoiceNumber parsed); // true
var invoice = GibInvoiceNumber.Create("ABC", 2026, 41);
invoice.ToString();        // "ABC2026000000041"
invoice.Next().ToString(); // "ABC2026000000042"
```

A GİB invoice number has 16 characters: a series of 3 upper-case letters or digits (A–Z, 0–9), a 4-digit year (2000–2099) and a 9-digit sequence from 1. `Create` throws `ArgumentException` for an invalid part; `Next` throws `InvalidOperationException` when the sequence reaches 999,999,999. An ETTN is a non-empty UUID in the 8-4-4-4-12 format.

The `[GibInvoiceNumber]` and `[Ettn]` attributes and the `GibInvoiceNumber()` and `Ettn()` FluentValidation rules are described in [Form validation](validation.md).

#### QR code (karekod)

```csharp
if (GibInvoiceQrCode.TryParse(scannedText, out GibInvoiceQrCode? qr))
{
    qr.SellerTaxId;       // "4840847211"
    qr.Number.ToString(); // "ABC2026000000001"
    qr.Date;              // 2026-10-15
    qr.PayableAmount;     // 120.00
    qr.VatAmounts[20m];   // 20.00 (VAT per rate)
}
```

`TryParse` reads the JSON text in the QR code of an e-Fatura or e-Arşiv invoice. It requires a valid seller and buyer VKN or TCKN ("11111111111" is accepted for e-Arşiv buyers without one), invoice number, ETTN and date. Amounts are optional; every field is also in `Fields`. It reads the text only and does not ask GİB whether the invoice exists.
