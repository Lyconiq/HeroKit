# HeroKit

[![NuGet](https://img.shields.io/nuget/v/HeroKit.svg)](https://www.nuget.org/packages/HeroKit)
[![NuGet downloads](https://img.shields.io/nuget/dt/HeroKit.svg)](https://www.nuget.org/packages/HeroKit)
[![Build & Test](https://github.com/Lyconiq/HeroKit/actions/workflows/build-tests.yml/badge.svg)](https://github.com/Lyconiq/HeroKit/actions/workflows/build-tests.yml)
[![Coverage](https://raw.githubusercontent.com/Lyconiq/HeroKit/badges/coverage.svg)](https://github.com/Lyconiq/HeroKit/actions/workflows/build-tests.yml)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/Lyconiq/HeroKit/badge)](https://scorecard.dev/viewer/?uri=github.com/Lyconiq/HeroKit)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Türkçe: [README.TR.md](README.TR.md) · **Documentation: [herokit.emreakdemir.dev](https://herokit.emreakdemir.dev)**

Everyday extension methods for .NET, with first-class support for Turkish data: TCKN, VKN, IBAN, phone numbers, plates, regions, public holidays and amounts in words. Country rules follow one model (`Country`, `Language`, `Currency`), so new countries can be added without new methods.

```bash
dotnet add package HeroKit
```

Targets .NET 8 and .NET 10. No dependencies. Nullable-annotated and Native AOT ready.

## Countries and languages

```csharp
using HeroKit.Banking;
using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

"10000000146".IsValidTckn();                                   // true  (T.C. Kimlik No)
"4840847211".IsValidVkn();                                     // true  (Vergi Kimlik No)
"TR330006100519786457841326".FormatIban();                     // "TR33 0006 1005 1978 6457 8413 26"
"TR78 0006 2000 0000 0006 2950 05".GetBank()?.Name;            // "Türkiye Garanti Bankası A.Ş."
"0532 123 45 67".ToE164(Countries.Turkey);                     // "+905321234567"
"34abc123".FormatVehiclePlate(Countries.Turkey);               // "34 ABC 123"
Countries.Turkey.FindRegion("34")?.Name;                       // "İstanbul"
"34710".IsValidPostalCode(Countries.Turkey);                   // true
123.50m.ToCurrencyWords(Currencies.TurkishLira);               // "yüz yirmi üç Türk lirası elli kuruş"
1234.5m.ToCurrencyString(Currencies.TurkishLira);              // "₺1.234,50"
"₺1.234,56".ToAmount(Currencies.TurkishLira);                  // 1234.56
"10000000146".MaskMiddle(2, 2);                                // "10*******46" (KVKK)
Countries.Turkey.FindDistrict("istanbul", "kadikoy")?.Name;    // "Kadıköy" (973 ilçe)
Tckn.Parse("10000000146").ToMaskedString();                   // validated value type: "10*******46"
"istanbul ılıca".ToTitleCase(Languages.Turkish);               // "İstanbul Ilıca"
new DateOnly(2026, 3, 19).AddBusinessDays(1, Countries.Turkey); // 2026-03-23 (skips the bayram and the weekend)
"ABC2026000000001".IsValidGibInvoiceNumber();                  // true  (e-Fatura / e-Arşiv)
```

Every method takes a `Country`, `Language` or `Currency`. Türkiye and the United States are built in (`Countries.UnitedStates`: NANP phone numbers, ZIP codes, SSN/EIN, federal holidays, states, `Currencies.USDollar`, `Languages.English`). To support another country, implement the rule interfaces and call `Countries.Register(new Country(...) { PhoneNumbers = ... })`: no new methods are needed. Turkish sorting also works without ICU: `names.OrderBy(n => n, Languages.Turkish.TextRules!.Comparer)`.

## Form validation

```csharp
using HeroKit.Validation;

public sealed class Customer
{
    [Required, Tckn] public string? IdentityNumber { get; set; }
    [Iban] public string? Iban { get; set; }
    [PhoneNumber("TR", MobileOnly = true)] public string? Mobile { get; set; }
    [VehiclePlate("TR")] public string? Plate { get; set; }
}
```

Also: `[Vkn]`, `[TaxId("TR")]`, `[NationalId("TR")]`, `[PostalCode("TR")]`, `[GibInvoiceNumber]`, `[Ettn]`, `[CardNumber]`. They work with ASP.NET Core, Blazor and `Validator`.

Using FluentValidation? Add [HeroKit.FluentValidation](https://www.nuget.org/packages/HeroKit.FluentValidation): `RuleFor(x => x.IdentityNumber).Tckn();`

## File types and uploads

[HeroKit.FileTypes](https://www.nuget.org/packages/HeroKit.FileTypes) is a separate, dependency-free package: content detection for about 70 types (Office inside ZIP, HEIC/AVIF/MP4 brands, WebP, JPEG XL), extension and MIME mapping, and an OWASP-style upload check.

```csharp
using HeroKit.FileTypes;

FileTypes.Detect(stream)?.MimeType;               // "image/png"
FileTypes.GetMimeType("report.docx");             // "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
FileTypes.CheckUpload(stream, "photo.php.jpg").Rejection; // UploadRejection.DoubleExtension
```

A runnable ASP.NET Core sample with uploads, `Tckn` binding and districts: [samples/HeroKit.Samples.WebApi](samples/HeroKit.Samples.WebApi).

## Conversions

Every conversion has a lenient form that returns a default value and a `Try` form. Neither throws for bad input. Parsing is culture-invariant unless you pass a provider.

```csharp
using HeroKit.Strings;

"42".ToInt();                       // 42
"abc".ToInt(-1);                    // -1
"2026-03-07".To<DateOnly>();        // any IParsable<T>
"42".TryTo(out int number);         // true
"1.234,56".ToDecimal();             // 1234.56
"Friday".ToEnum<DayOfWeek>();       // DayOfWeek.Friday
```

## More

| Area | Examples |
|---|---|
| Strings | `ToSlug`, `Truncate`, `Mask`, `Left`/`Right`, `RemoveWhitespace`, `ToTitleCase(culture)` |
| Collections | `IsNullOrEmpty`, `WhereIf` and `Page` (also on `IQueryable`), `ToReadOnlyList` |
| Dates | `StartOfWeek`, `EndOfMonth`, `IsWeekend`, `IsBetween`, `Age` (DateTime and DateOnly) |
| Prices | `RoundToStep`, `ToPsychologicalPrice`, `IncludeVat`, `ExcludeVat` |
| Cards | `IsValidCardNumber` (Luhn), `GetCardBrand` (incl. Troy) |
| Enums | `GetDescription`, `ToEnum`, `ToUnderlying<T>` |

The [guides and API reference](https://herokit.emreakdemir.dev) cover every method. Upgrading from 1.x or 2.x? See the [upgrade guide](https://herokit.emreakdemir.dev/articles/upgrading.html).

## Questions and contributions

- Questions and ideas: [GitHub Discussions](https://github.com/Lyconiq/HeroKit/discussions)
- Bugs and feature requests: [Issues](https://github.com/Lyconiq/HeroKit/issues)
- How to contribute: [CONTRIBUTING.md](CONTRIBUTING.md)

## License

MIT
