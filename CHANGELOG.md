# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Package descriptions and tags lead with what each package is for: Turkish data for HeroKit, safe file uploads for HeroKit.FileTypes.

## [3.7.0] - 2026-10-19

### Added
- `CountBusinessDays(start, end, country)` for `DateOnly` and `DateTime`: counts a half-day holiday on a weekday (arife, 28 October) as 0.5 and returns a `decimal`.
- `Country.WithAdditionalHolidays(...)` returns a copy of a country whose calendar also has your days, for example administrative leave (idari izin) or company holidays; a day replaces a holiday on the same date. `Country.WithHolidays(calendar)` swaps the whole calendar. The country itself does not change.
- `Country.TimeZone`, `Country.Now(timeProvider)` and `Country.Today(timeProvider)`: the current date in the country, also on a server that runs in UTC. Türkiye uses Europe/Istanbul; the United States has no single time zone and throws `NotSupportedException`.
- `DateTime` overloads of `GetPublicHoliday` and `IsHalfDayHoliday`.
- New package HeroKit.FileTypes.AspNetCore: `IFormFile.CheckUpload(policy)`, `UploadValidator.Validate(IFormFile)`, `IFormFile.GetSafeFileName()`, the `[AllowedFileTypes("jpg", "png", MaxLength = ...)]` attribute for MVC, Razor Pages and Blazor models (also for `IFormFileCollection`), and the `ValidateUploads(policy)` endpoint filter for minimal APIs and route groups, which returns 400 with a validation problem before the handler runs.
- HeroKit.FluentValidation: `SafeUpload(openRead, fileName, policy)` checks an uploaded file of any type (`IFormFile`, `IBrowserFile` or your own) with `UploadValidator`. The package now depends on HeroKit.FileTypes. The README states the minimum FluentValidation version (12.0.0).

### Deprecated
- `BusinessDaysUntil` counts a half-day holiday as a full business day. Use `CountBusinessDays`. `BusinessDaysUntil` will be removed in 4.0.

### Fixed
- Turkish IBANs: the reserved digit (10th character) must be 0, as the format requires. Before, the 5th character (the first digit of the bank code) was checked instead. A Turkish IBAN with a non-zero reserved digit is now invalid; one with a bank code that starts with another digit than 0 is now valid.

### Changed
- The documentation moved to https://herokit.lyconiq.com. The old address redirects.

## [3.6.0] - 2026-10-19

### Added
- Each GitHub release carries an SPDX software bill of materials per package and a signed build provenance attestation for every `.nupkg`. Verify a package with `gh attestation verify <file>.nupkg --repo Lyconiq/HeroKit`.
- HeroKit.FileTypes: legacy Word and Excel files (DOC, XLS) with a VBA project are rejected as `ActiveContent` unless `UploadPolicy.AllowMacros` is true. Only the file's directory is read, from the whole span or a seekable stream.
- HeroKit.FileTypes: `UploadPolicy.MaxLength` and the `UploadRejection.TooLarge` reason.
- HeroKit.FileTypes: `FileTypes.ToSafeFileName` turns an uploaded name into one that is safe to save or to send as a download name. The guide shows how to serve downloads with `Content-Disposition`.
- `SECURITY.md` with supported versions and private vulnerability reporting, a release and support policy in `CONTRIBUTING.md`, and an OpenSSF Scorecard check with a README badge.

### Changed
- HeroKit.FileTypes: a legacy DOC or XLS upload with macros that 3.5.0 accepted is now rejected. Set `AllowMacros = true` to accept it as before.
- The repository moved to the Lyconiq organization (github.com/Lyconiq/HeroKit). Old links redirect. Packages now set `Company` to Lyconiq.

## [3.5.0] - 2026-10-16

### Added
- HeroKit.FileTypes: macro-enabled Office types `Docm`, `Xlsm` and `Pptm` (`FileType.HasMacros`). `UploadValidator` rejects macros unless `UploadPolicy.AllowMacros` is true; a macro document named `.docx` is an extension mismatch.
- HeroKit.FileTypes: detection from a seekable stream also reads the ZIP central directory, so DOCX, XLSX, PPTX, APK, JAR and macro entries are found in large files. A whole ZIP file passed as a span is read the same way.
- A runnable ASP.NET Core minimal API sample (`samples/HeroKit.Samples.WebApi`) with uploads, `Tckn` route binding, districts and amounts, covered by integration tests.

### Changed
- HeroKit.FileTypes: the extensions `docm`, `dotm`, `xlsm`, `xltm`, `pptm` and `potm` now map to the macro-enabled types instead of DOCX, XLSX and PPTX.

## [3.4.0] - 2026-10-16

### Added
- Districts: `Country.Districts`, `GetDistricts(region)`, `FindDistrict(region, name)` and `FindDistrict(code)`; Türkiye has its 973 districts (ilçe) with official MERNİS codes. New `District` type.
- `Tckn` value type: validated once, then passed around. `Parse`, `TryParse`, `IParsable<Tckn>`, `ToMaskedString`, JSON converter (also for source generation and Native AOT) and type converter.
- `IsValidKepAddress()` with `[KepAddress]` and the `KepAddress()` FluentValidation rule.
- `IsValidIdCardSerial()` with `[IdCardSerial]` and the `IdCardSerial()` FluentValidation rule.

### Quality
- Native AOT smoke test with 23 checks, including JSON source generation with `Tckn`.

## [3.3.0] - 2026-10-16

### Added
- New package **HeroKit.FileTypes**, dependency-free and Native AOT compatible:
  - `FileTypes.Detect` / `DetectAsync` / `DetectAll` recognize about 70 types from magic bytes, including DOCX/XLSX/PPTX, OpenDocument, EPUB, APK and JAR inside ZIP, MP4/MOV/M4A/3GP/HEIC/AVIF by `ftyp` brand, WebP, JPEG XL, WebM vs. Matroska and Java class vs. Mach-O. Streams are read up to 16 KB and their position is restored.
  - `FromExtension`, `FromMimeType` (with aliases) and `GetMimeType` for 86 types.
  - `ContentMatchesExtension` accepts a more general detected type for a more specific extension (`FileType.Parent`).
  - `CheckUpload` with `UploadPolicy` rejects invalid file names, program and script extensions, hidden double extensions, program or script content, unknown or mismatching content, types outside an allow-list and SVG with active content.
  - Your own types (`MagicNumber`, `ZipEntryDetector`, `IsoMediaDetector`, `IFileTypeDetector`) and upload rules (`IUploadRule`), `UploadPolicy.BlockedExtensions`.
  - `FileTypeRegistry` and `UploadValidator` instances for dependency injection and isolated type sets; the static `FileTypes` methods use the shared `FileTypes.Default`.

### Fixed
- The HeroKit.FluentValidation package readme showed method names that 3.0 removed.

## [3.2.0] - 2026-10-15

### Added
- Bank from IBAN: `GetBank()` and `Countries.Turkey.Banks` (30 banks by EFT code). New `Bank` type and `IBankDirectory` rule, so other countries can add their banks.
- `GibInvoiceQrCode.TryParse` reads the QR code (karekod) of e-Fatura and e-Arşiv invoices: tax ids, number, ETTN, date, totals and VAT per rate.
- `IsValidMersis()`, `[Mersis]` and the `Mersis()` FluentValidation rule.
- `IsValidGtin()` (EAN-8, UPC-A, EAN-13, GTIN-14), `[Gtin]` and the `Gtin()` FluentValidation rule.
- `TryToAmount` / `ToAmount` read amounts with the separators of a currency ("₺1.234,56", "1.234,56 TL", "$1,234.56") and reject wrong digit grouping.
- Masking for personal data (KVKK, GDPR): `MaskMiddle`, `MaskEmail`, `MaskName`.

### Quality
- Tests for every new helper, also under invariant globalization; Native AOT smoke test with 21 checks.

## [3.1.0] - 2026-10-15

### Added
- United States: `Countries.UnitedStates` with NANP phone numbers, ZIP and ZIP+4 codes, SSN (`IsValidNationalId`), SSN, ITIN or EIN (`IsValidTaxId`), the 11 federal holidays with observed days, and the 50 states plus DC (FIPS codes and USPS abbreviations). Vehicle plates differ by state and are not supported.
- `Languages.English`: numbers in American English words and ICU-free casing and sorting.
- `Currencies.USDollar`: "$1,234.50", "one hundred twenty-three dollars and fifty cents".
- `Currency.UnitNamePlural`, `SubunitNamePlural` and `SubunitConjunction` for languages that inflect unit names. Turkish output is unchanged.
- `Region.Abbreviation`; `Country.FindRegion` also matches it ("CA").
- Attributes and FluentValidation rules resolve "US" without extra setup.

### Quality
- Tests for `IsValidNationalId` (TCKN) and for every United States rule, also under invariant globalization; Native AOT smoke test with 20 checks.

## [3.0.0] - 2026-10-15

Breaking release. Country- and language-specific behavior is selected with objects instead of method names, so new countries and languages need no new methods. See the [upgrade guide](https://herokit.lyconiq.com/articles/upgrading.html).

### Changed (breaking)
- New model in `HeroKit.Globalization`: `Country` / `Countries`, `Language` / `Languages`, `Currency` / `Currencies` with rule interfaces `IPhoneNumberRules`, `IVehiclePlateRules`, `IIdentifierRules`, `IHolidayCalendar`, `INumberSpeller`, `ITextRules` (#104).
- Türkiye is `Countries.Turkey`, `Languages.Turkish` and `Currencies.TurkishLira`. The `HeroKit.Turkish` namespace is removed.
- Methods lose their `Tr` / `Turkish` names and take the country or language:
  - `IsValidPhoneTr`, `IsValidMobileTr`, `ToE164Tr`, `FormatPhoneTr` → `IsValidPhone`, `IsValidMobilePhone`, `ToE164`, `FormatPhone` (`Countries.Turkey`).
  - `IsValidPlateTr`, `FormatPlateTr`, `IsValidPostalCodeTr`, `IsValidTaxIdTr` → `IsValidVehiclePlate`, `FormatVehiclePlate`, `IsValidPostalCode`, `IsValidTaxId`; new `IsValidNationalId`.
  - `ToProvinceName`, `ToProvinceCode`, `TurkishProvinces` → `Countries.Turkey.FindRegion(...)`, `Countries.Turkey.Regions`.
  - `TurkishHolidays` and the `…Tr` business day methods → `Countries.Turkey.Holidays`, `IsPublicHoliday`, `IsHalfDayHoliday`, `GetPublicHoliday`, `IsBusinessDay`, `AddBusinessDays`, `BusinessDaysUntil` (`Countries.Turkey`).
  - `ToTurkishWords`, `ToTurkishCurrencyWords`, `ToTurkishLira` → `ToWords(Languages.Turkish)`, `ToCurrencyWords(Currencies.TurkishLira)`, `ToCurrencyString(Currencies.TurkishLira, CurrencyStyle)`.
  - `ToTitleCaseTr`, `EqualsTr` → `ToTitleCase(Languages.Turkish)`, `EqualsIgnoreCase(other, Languages.Turkish)`; new `ToUpper` / `ToLower(Languages.Turkish)`.
- Attributes take an ISO country code: `[PhoneNumber("TR", MobileOnly = …)]`, `[VehiclePlate("TR")]`, `[PostalCode("TR")]`, `[TaxId("TR")]`, new `[NationalId("TR")]`. FluentValidation rules take a `Country`: `PhoneNumber(Countries.Turkey, mobileOnly)`, `VehiclePlate`, `PostalCode`, `TaxId`, `NationalId`.
- `RemoveAccent` and `ClearTurkishChars` → `RemoveDiacritics`.
- IBAN national rules come from the registered country (`Country.IbanRules`) instead of a hard-coded check.

### Added
- Countries can be added without changing HeroKit: implement the rule interfaces and call `Countries.Register`. Attributes resolve countries by ISO code.
- e-Fatura / e-Arşiv: `GibInvoiceNumber` (parse, create, next number, format), `IsValidGibInvoiceNumber`, `IsValidEttn`, `[GibInvoiceNumber]`, `[Ettn]`, matching FluentValidation rules (#96).
- Turkish alphabetical sorting without ICU: `Languages.Turkish.TextRules.Comparer` and `IgnoreCaseComparer` (#97).
- `CurrencyStyle.IsoCode` ("1.234,50 TRY").
- Turkish documentation site (#95).

### Fixed
- Text rules, sorting and `RemoveDiacritics` no longer depend on ICU, so they work with invariant globalization (containers).
- `ToSlug` no longer returns leading or trailing dashes: `"  ---  "` becomes `""` and `"- Hello -"` becomes `"hello"`.

### Quality
- Test coverage 98% lines and 96% branches (#98); Native AOT smoke test with 19 checks in CI.

## 2.x (2.0.0 – 2.2.0) - 2026-10-15

Superseded by 3.0.0. Highlights:
- One error policy for conversions: lenient `ToX(defaultValue)` and checked `TryToX(out …)`; generic `To<T>` / `TryTo<T>` for any `IParsable<T>`.
- Consistent names (`IsEven`, `ToMonthName`, `ToText`, `CopyFrom`, `ToBase64Url`, …) and removal of obsolete helpers.
- Validation attributes and the `HeroKit.FluentValidation` package.
- Provinces, plates, postal codes, card numbers with Troy, lira formatting, enum descriptions, public holidays and business days (Diyanet calendar 2022–2027).

## 1.x (1.0.0 – 1.3.0) - 2026-10-15

Superseded by 3.0.0. Highlights:
- First stable release for .NET 8 and .NET 10; culture-invariant parsing and many bug fixes.
- Nullable annotations, trimming and Native AOT support, public API tracking, benchmarks.
- TCKN/VKN, IBAN, Turkish phone numbers, amounts in words, Turkish casing.
- Enumerable, date, price/VAT and string helpers; documentation site.

## Pre-releases (2023 – 2024)

1.0.0-alpha to 1.0.0-alpha.6: the first array, boolean, string, type and conversion helpers (contributors: @emreakdemir, @jepozdemir).

[Unreleased]: https://github.com/Lyconiq/HeroKit/compare/3.7.0...HEAD
[3.7.0]: https://github.com/Lyconiq/HeroKit/compare/3.6.0...3.7.0
[3.6.0]: https://github.com/Lyconiq/HeroKit/compare/3.5.0...3.6.0
[3.5.0]: https://github.com/Lyconiq/HeroKit/compare/3.4.0...3.5.0
[3.4.0]: https://github.com/Lyconiq/HeroKit/compare/3.3.0...3.4.0
[3.3.0]: https://github.com/Lyconiq/HeroKit/compare/3.2.0...3.3.0
[3.2.0]: https://github.com/Lyconiq/HeroKit/compare/3.1.0...3.2.0
[3.1.0]: https://github.com/Lyconiq/HeroKit/compare/3.0.0...3.1.0
[3.0.0]: https://github.com/Lyconiq/HeroKit/compare/2.2.0...3.0.0
