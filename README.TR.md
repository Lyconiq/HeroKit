# HeroKit

[![NuGet](https://img.shields.io/nuget/v/HeroKit.svg)](https://www.nuget.org/packages/HeroKit)
[![NuGet downloads](https://img.shields.io/nuget/dt/HeroKit.svg)](https://www.nuget.org/packages/HeroKit)
[![Build & Test](https://github.com/Lyconiq/HeroKit/actions/workflows/build-tests.yml/badge.svg)](https://github.com/Lyconiq/HeroKit/actions/workflows/build-tests.yml)
[![Coverage](https://raw.githubusercontent.com/Lyconiq/HeroKit/badges/coverage.svg)](https://github.com/Lyconiq/HeroKit/actions/workflows/build-tests.yml)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/Lyconiq/HeroKit/badge)](https://scorecard.dev/viewer/?uri=github.com/Lyconiq/HeroKit)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

English: [README.md](README.md) · **Dokümantasyon: [herokit.lyconiq.com](https://herokit.lyconiq.com)**

HeroKit, iki iş için .NET kütüphaneleridir:

- **Türkiye verisi, doğru şekilde.** Banka tespitli TCKN, VKN ve IBAN doğrulama, telefon numaraları, plakalar, il ve ilçeler, resmî tatiller ve iş günleri (yarım günler dahil), yazıyla tutar ve e-Fatura numaraları. Hepsi tek bir `Country`, `Language` ve `Currency` modeli üzerinde çalışır; başka ülkeler de aynı şekilde eklenir.
- **Güvenli dosya yükleme.** [HeroKit.FileTypes](https://www.nuget.org/packages/HeroKit.FileTypes) dosyanın gerçek türünü içeriğinden bulur; adı değiştirilmiş programları, gizli çift uzantıları (`a.php.jpg`), SVG içindeki betikleri ve Office dosyalarındaki makroları reddeder. Bağımlılığı yoktur ve tek başına kullanılır.

Çekirdek pakette ayrıca metin, dönüşüm, tarih, koleksiyon ve fiyat için günlük uzantı metotları vardır.

| Paket | Ne için |
|---|---|
| [HeroKit](https://www.nuget.org/packages/HeroKit) | Türkiye verisi, ülke modeli ve uzantı metotları |
| [HeroKit.FileTypes](https://www.nuget.org/packages/HeroKit.FileTypes) | Dosya türü tespiti ve güvenli yükleme kontrolü, bağımlılıksız |
| [HeroKit.FileTypes.AspNetCore](https://www.nuget.org/packages/HeroKit.FileTypes.AspNetCore) | `IFormFile` kontrolleri, `[AllowedFileTypes]`, minimal API filtresi |
| [HeroKit.FluentValidation](https://www.nuget.org/packages/HeroKit.FluentValidation) | Bunların hepsi için FluentValidation kuralları |

```bash
dotnet add package HeroKit
```

.NET 8 ve .NET 10'u hedefler. Bağımlılığı yoktur. Null işaretleri tamdır ve [Native AOT](https://herokit.lyconiq.com/tr/articles/nullable-and-aot.html) ile uyumludur (önceden derlenen, hızlı açılan yerel uygulama).

## Ülkeler ve diller

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
new DateOnly(2026, 3, 19).AddBusinessDays(1, Countries.Turkey); // 2026-03-23 (bayram ve hafta sonu atlanır)
"ABC2026000000001".IsValidGibInvoiceNumber();                  // true  (e-Fatura / e-Arşiv)
```

Her metot bir `Country`, `Language` ya da `Currency` alır. Türkiye ve ABD hazır gelir (`Countries.UnitedStates`: NANP telefon numaraları, ZIP kodları, SSN/EIN, federal tatiller, eyaletler, `Currencies.USDollar`, `Languages.English`). Başka bir ülkeyi desteklemek için kural arayüzlerini uygulayın ve `Countries.Register(new Country(...) { PhoneNumbers = ... })` çağırın: yeni metot gerekmez. Türkçe sıralama ICU olmadan da çalışır: `names.OrderBy(n => n, Languages.Turkish.TextRules!.Comparer)`.

## Form doğrulama

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

Ayrıca: `[Vkn]`, `[TaxId("TR")]`, `[NationalId("TR")]`, `[PostalCode("TR")]`, `[GibInvoiceNumber]`, `[Ettn]`, `[CardNumber]`. ASP.NET Core, Blazor ve `Validator` ile çalışır.

FluentValidation mı kullanıyorsunuz? [HeroKit.FluentValidation](https://www.nuget.org/packages/HeroKit.FluentValidation) paketini ekleyin: `RuleFor(x => x.IdentityNumber).Tckn();`

## Dosya türleri ve yükleme

[HeroKit.FileTypes](https://www.nuget.org/packages/HeroKit.FileTypes) ayrı ve bağımlılıksız bir pakettir: yaklaşık 70 tür için içerikten tespit (ZIP içindeki Office, HEIC/AVIF/MP4 markaları, WebP, JPEG XL), uzantı ve MIME eşlemesi ve OWASP tarzı yükleme kontrolü.

```csharp
using HeroKit.FileTypes;

FileTypes.Detect(stream)?.MimeType;               // "image/png"
FileTypes.GetMimeType("report.docx");             // "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
FileTypes.CheckUpload(stream, "photo.php.jpg").Rejection; // UploadRejection.DoubleExtension
```


ASP.NET Core için [HeroKit.FileTypes.AspNetCore](https://www.nuget.org/packages/HeroKit.FileTypes.AspNetCore) `IFormFile` yüklemelerini denetler: minimal API'ler için `ValidateUploads()` endpoint filtresi; MVC, Razor Pages ve Blazor için `[AllowedFileTypes("jpg", "png")]` özniteliği.
Yükleme, `Tckn` bağlama ve ilçeleri gösteren çalıştırılabilir ASP.NET Core örneği: [samples/HeroKit.Samples.WebApi](samples/HeroKit.Samples.WebApi).

## Dönüşümler

Her dönüşümün varsayılan değer döndüren esnek bir hâli ve bir `Try` hâli vardır. İkisi de hatalı girdide hata fırlatmaz. Bir kültür verilmezse çözümleme kültürden bağımsızdır.

```csharp
using HeroKit.Strings;

"42".ToInt();                       // 42
"abc".ToInt(-1);                    // -1
"2026-03-07".To<DateOnly>();        // her IParsable<T>
"42".TryTo(out int number);         // true
"1.234,56".ToDecimal();             // 1234.56
"Friday".ToEnum<DayOfWeek>();       // DayOfWeek.Friday
```

## Diğerleri

| Alan | Örnekler |
|---|---|
| Metin | `ToSlug`, `Truncate`, `Mask`, `Left`/`Right`, `RemoveWhitespace`, `ToTitleCase(culture)` |
| Koleksiyon | `IsNullOrEmpty`, `WhereIf` ve `Page` (`IQueryable` dahil), `ToReadOnlyList` |
| Tarih | `StartOfWeek`, `EndOfMonth`, `IsWeekend`, `IsBetween`, `Age` (DateTime ve DateOnly) |
| Fiyat | `RoundToStep`, `ToPsychologicalPrice`, `IncludeVat`, `ExcludeVat` |
| Kart | `IsValidCardNumber` (Luhn), `GetCardBrand` (Troy dahil) |
| Enum | `GetDescription`, `ToEnum`, `ToUnderlying<T>` |

Bütün metotlar [rehberlerde ve API referansında](https://herokit.lyconiq.com). 1.x ya da 2.x'ten mi geçiyorsunuz? [Yükseltme rehberine](https://herokit.lyconiq.com/tr/articles/upgrading.html) bakın.

## Sorular ve katkı

- Sorular ve fikirler: [GitHub Discussions](https://github.com/Lyconiq/HeroKit/discussions)
- Hata ve özellik talepleri: [Issues](https://github.com/Lyconiq/HeroKit/issues)
- Katkı rehberi: [CONTRIBUTING.md](CONTRIBUTING.md)

## Lisans

MIT
