# Ülkeler ve diller

HeroKit 3.0 her ülke için ayrı bir metot kümesi eklemez. Ülke ya da dil alan tek bir metot kümesi vardır. Türkiye ve ABD hazır gelir. Diğer ülkeler, tanımladığınız anda aynı metotları kullanır.

## Model

| Tip | Ne tutar | Hazır olan |
|---|---|---|
| `Country` | ISO kodu, ad, dil, para birimi ve isteğe bağlı kurallar: telefon numaraları, araç plakaları, posta kodları, kimlik ve vergi numaraları, resmî tatiller, bölgeler, IBAN uzunlukları | `Countries.Turkey`, `Countries.UnitedStates` |
| `Language` | ISO kodu ve isteğe bağlı kurallar: harf büyüklüğü ve sıralama (`TextRules`), yazıyla sayı (`NumberSpeller`) | `Languages.Turkish`, `Languages.English` |
| `Currency` | ISO kodu, simge, ayırıcılar, birim adları | `Currencies.TurkishLira`, `Currencies.USDollar` |

Bütün tipler `HeroKit.Globalization` namespace'indedir. Yalnızca Türkiye'ye ait kontroller (TCKN, VKN, e-fatura) `HeroKit.Globalization.Turkey` içindedir.

```csharp
using HeroKit.Globalization;

"0532 123 45 67".IsValidPhone(Countries.Turkey);   // true
"34 ABC 123".IsValidVehiclePlate(Countries.Turkey); // true
1234.5m.ToCurrencyString(Currencies.TurkishLira);   // "₺1.234,50"
"istanbul".ToUpper(Languages.Turkish);              // "İSTANBUL"
```

Bir ülkenin her kuralı isteğe bağlıdır. Ülkede olmayan bir kurala ihtiyaç duyan metot `NotSupportedException` fırlatır.

Bu kuralların hiçbiri ICU'yu ya da işletim sisteminin kültür verisini kullanmaz. Sonuç her makinede aynıdır; `InvariantGlobalization` açıkken de.

## Kendi ülkenizi ekleyin

İhtiyacınız olan kural arayüzlerini uygulayın, bir `Country` oluşturun ve kaydedin:

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

Diğer kural özellikleri: `VehiclePlates` (`IVehiclePlateRules`), `PostalCodes`, `NationalIds`, `TaxIds` ve `IbanRules` (`IIdentifierRules`), `Holidays` (`IHolidayCalendar`), `Regions` ve `Currency`.

`Countries.Register`, ülkeyi doğrulama özniteliklerine ISO koduyla da açar: ülke kaydedildikten sonra `[PhoneNumber("XT")]` çalışır. Kaydı `Countries.Get("XT")`, `Countries.TryGet` ve `Countries.All` okur.

Yeni bir dil de aynı şekilde eklenir: kendi `ITextRules` ve `INumberSpeller` uygulamalarınızla `new Language("xx", "Ad") { TextRules = ..., NumberSpeller = ... }` oluşturun.

## Amerika Birleşik Devletleri

`Countries.UnitedStates`, `Languages.English` ve `Currencies.USDollar` aynı metotlarla çalışır:

```csharp
using HeroKit.Globalization;

var us = Countries.UnitedStates;

"(212) 555-0123".IsValidPhone(us);                  // true
"+1 212 555 0123".FormatPhone(us);                  // "(212) 555-0123"
"212.555.0123".ToE164(us);                          // "+12125550123"
"10001-1234".IsValidPostalCode(us);                 // true (ZIP ya da ZIP+4)
"123-45-6789".IsValidNationalId(us);                // true (SSN)
"12-3456789".IsValidTaxId(us);                      // true (EIN; SSN ve ITIN de kabul edilir)
us.FindRegion("ca")?.Name;                          // "California" (FIPS kodu 6)
new DateOnly(2026, 7, 2).AddBusinessDays(1, us);    // 2026-07-06 (3 Temmuz, Bağımsızlık Günü'nün resmî tatil günü)
1234.5m.ToCurrencyString(Currencies.USDollar);      // "$1,234.50"
123.50m.ToCurrencyWords(Currencies.USDollar);       // "one hundred twenty-three dollars and fifty cents"
2_500_340L.ToWords(Languages.English);              // "two million five hundred thousand three hundred forty"
```

- **Telefon numaraları** Kuzey Amerika Numaralandırma Planı'na uyar: alan kodu ve santral kodu 2–9 ile başlayan 10 haneli numara; isteğe bağlı `+1`, `1` ya da `001` öneki. ABD numaraları hattın mobil olup olmadığını göstermez; bu yüzden `IsValidMobilePhone` her geçerli numarayı kabul eder.
- **SSN, ITIN ve EIN** kontrolleri yapısaldır. Hiç verilmeyen numaraları (ör. SSN alan kodu 000, 666 ya da 900–999) reddeder, ama numaranın gerçekten verildiğini kanıtlamaz.
- **Resmî tatiller** 1986'dan itibaren 11 federal tatildir; Juneteenth 2021'den itibaren eklenir. Cumartesiye gelen tatil önceki cuma, pazara gelen tatil sonraki pazartesi kullanılır. Bu günler "… (observed)" adıyla ayrı kayıttır. Eyalet tatilleri dahil değildir.
- **Bölgeler** 50 eyalet ve District of Columbia'dır. `FindRegion` FIPS kodunu, USPS kısaltmasını ya da adı kabul eder.
- **Araç plakaları** eyalete göre değişir; bu yüzden `IsValidVehiclePlate(Countries.UnitedStates)` `NotSupportedException` fırlatır.
- **İngilizce metin kuralları** değişmez (invariant) harf dönüşümü kullanır. `Languages.English.TextRules!.Comparer` büyük/küçük harfe bakmadan sıralar, eşitlikte küçük harfi öne koyar: `apple, Apple, banana`.

## Türkiye

### Kimlik ve vergi numaraları

```csharp
using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

"10000000146".IsValidTckn();                        // true (TCKN)
"4840847211".IsValidVkn();                          // true (VKN)
"10000000146".IsValidNationalId(Countries.Turkey);  // true (TCKN)
"4840847211".IsValidTaxId(Countries.Turkey);        // true: VKN veya TCKN
"0879001756600379".IsValidMersis();                 // true: "0", VKN, 5 haneli sıra no
"firma@hs01.kep.tr".IsValidKepAddress();            // true: .kep.tr alan adlı e-posta adresi
"A12B34567".IsValidIdCardSerial();                  // true: çipli kimlik kartı seri no
```

Bunlar sağlama toplamı kontrolleridir; `IsValidMersis` MERSİS numarasının içindeki VKN'yi kontrol eder. Bir numaranın biçiminin doğru olduğunu gösterir; numaranın verilmiş olduğunu göstermez. Girdi yalnızca rakam içermelidir. Algoritmaları adım adım anlatan rehberler: [TCKN](tckn-validation.md), [VKN](vkn-validation.md).


#### Doğrulanmış TCKN değeri

`Tckn`, kontrolden geçmiş bir TCKN tutar. Değer sisteme girdiği yerde bir kez doğrulayın, sonra `string` yerine `Tckn` taşıyın:

```csharp
Tckn id = Tckn.Parse("10000000146"); // geçersiz numarada FormatException
Tckn.TryParse(input, out Tckn checkedId);
id.ToMaskedString();                 // loglar için "10*******46"

public sealed record Customer(Tckn Id, string Name); // JSON: {"Id":"10000000146", ...}; geçersiz numara deserialize edilmez
app.MapGet("/customers/{id}", (Tckn id) => ...);    // minimal API bağlar; geçersiz numara 400 döner
```

`Tckn`, `IParsable<Tckn>` uygular; JSON dönüştürücüsü (System.Text.Json kaynak üreticisi ve Native AOT dahil) ve MVC model bağlama için tip dönüştürücüsü vardır. `default(Tckn)` numara tutmaz: `IsEmpty` true'dur.

### IBAN

```csharp
using HeroKit.Banking;

var iban = "tr33 0006 1005 1978 6457 8413 26";

iban.IsValidIban();   // true
iban.NormalizeIban(); // "TR330006100519786457841326"
iban.FormatIban();    // "TR33 0006 1005 1978 6457 8413 26"

"TR78 0006 2000 0000 0006 2950 05".GetBank()?.Name; // "Türkiye Garanti Bankası A.Ş."
Countries.Turkey.Banks!.All;                        // 5 haneli EFT kodlarıyla 30 banka
```

IBAN doğrulama her ülke için ISO 13616 mod-97 kontrolüyle çalışır. Kayıtlı bir ülkenin `IbanRules` kuralı varsa uzunluğu ve sabit konumları da kontrol edilir: Türkiye IBAN'ları 26 karakter olmalıdır. Algoritma için [IBAN doğrulama rehberine](iban-validation.md) bakın.

`GetBank` geçerli bir IBAN'ın banka kodunu okur (Türkiye'de 5–9. karakterler) ve ülkenin banka listesinde (`Country.Banks`) arar. Bilinmeyen kod için null döner. Liste Türkiye Bankalar Birliği EFT kodlarını izler; eksik banka için issue açın.

### Bölgeler, plakalar ve posta kodları

```csharp
using HeroKit.Globalization;

Countries.Turkey.FindRegion("34")?.Name;        // "İstanbul"
Countries.Turkey.FindRegion(6)?.Name;           // "Ankara"
Countries.Turkey.FindRegion("sanliurfa")?.Code; // 63 (büyük-küçük harf ve aksanlar yok sayılır)
Countries.Turkey.Regions.Count;                 // 81

Countries.Turkey.GetDistricts("istanbul").Count;              // 39
Countries.Turkey.FindDistrict("istanbul", "kadikoy")?.Name;   // "Kadıköy"
Countries.Turkey.FindDistrict(1421)?.RegionCode;              // 34

"34abc123".FormatVehiclePlate(Countries.Turkey);   // "34 ABC 123"
"06 A 1234".IsValidVehiclePlate(Countries.Turkey); // true
"34 Q 1234".IsValidVehiclePlate(Countries.Turkey); // false: Q plakada kullanılan bir harf değil

"34710".IsValidPostalCode(Countries.Turkey);       // true
```

`Regions`, `Region(Code, Name)` değerlerinin listesidir. Türkiye için bölgeler 81 ildir. `Districts`, 973 ilçeyi resmî MERNİS kodlarıyla `District(Code, Name, RegionCode)` olarak, her il içinde Türk alfabesine göre sıralı tutar. Mahalle ve sokaklar dahil değildir: on binlerce satırdır ve sık değişir.

### Telefon numaraları

```csharp
"0532 123 45 67".ToE164(Countries.Turkey);          // "+905321234567"
"+905321234567".FormatPhone(Countries.Turkey);      // "0532 123 45 67"
"(0212) 555 12 34".IsValidPhone(Countries.Turkey);  // true
"0212 555 12 34".IsValidMobilePhone(Countries.Turkey); // false
```

Kabul edilen önekler: `+90`, `0090`, `90`, `0` veya öneksiz. Kontrol yapısaldır; operatör ya da alan kodu izin listesi değildir.

### Resmî tatiller ve iş günleri

```csharp
new DateOnly(2026, 10, 29).IsPublicHoliday(Countries.Turkey);   // true  (Cumhuriyet Bayramı)
new DateOnly(2026, 10, 28).IsHalfDayHoliday(Countries.Turkey);  // true  (öğleden sonra tatil)
new DateOnly(2026, 3, 20).GetPublicHoliday(Countries.Turkey)?.Name; // "Ramazan Bayramı 1. Gün"

new DateOnly(2026, 3, 19).AddBusinessDays(1, Countries.Turkey); // 2026-03-23, bayramı ve hafta sonunu atlar
new DateOnly(2026, 10, 26).BusinessDaysUntil(new DateOnly(2026, 10, 30), Countries.Turkey); // 3

foreach (PublicHoliday holiday in Countries.Turkey.Holidays!.GetHolidays(2026))
    Console.WriteLine($"{holiday.Date:dd.MM.yyyy} {holiday.Name}{(holiday.IsHalfDay ? " (yarım gün)" : "")}");
```

- Sabit tatiller 2429 sayılı Kanun'a göre belirlenir. Ramazan ve Kurban Bayramı tarihleri Diyanet takviminden gelir ve takvimin `FirstSupportedYear`–`LastSupportedYear` (2022–2027) aralığı için vardır. Diğer yıllarda eksik veri döndürmek yerine istisna fırlatılır.
- İş günleri, tam gün tatiller hariç Pazartesi'den Cuma'ya kadardır. Yarım günler (arife ve 28 Ekim) iş günü sayılır.
- İdari izin köprü günleri duruma göre ilan edilir ve dahil değildir.
- `IsBusinessDay(country)` de vardır. `IHolidayCalendar` tanımlı olan her özel ülke bu metotları alır.

### Türk lirası

```csharp
1234.5m.ToCurrencyString(Currencies.TurkishLira);                              // "₺1.234,50"
1234.5m.ToCurrencyString(Currencies.TurkishLira, CurrencyStyle.Abbreviation);  // "1.234,50 TL"
1234.5m.ToCurrencyString(Currencies.TurkishLira, CurrencyStyle.IsoCode);       // "1.234,50 TRY"
```

Para birimi kendi ayırıcılarını taşır; sonuç geçerli kültüre bağlı değildir.

### Yazıyla tutar

```csharp
123.ToWords(Languages.Turkish);                        // "yüz yirmi üç"
1_001_000L.ToWords(Languages.Turkish);                 // "bir milyon bin"
123.50m.ToCurrencyWords(Currencies.TurkishLira);
// "yüz yirmi üç Türk lirası elli kuruş"

var euro = new Currency("EUR", "€", Languages.Turkish) { UnitName = "Euro", SubunitName = "sent" };
10.20m.ToCurrencyWords(euro);
// "on Euro yirmi sent"
```

### ICU olmadan harf büyüklüğü ve sıralama

```csharp
"istanbul ılıca".ToTitleCase(Languages.Turkish);   // "İstanbul Ilıca"
"istanbul".ToUpper(Languages.Turkish);             // "İSTANBUL"
"IŞIK".ToLower(Languages.Turkish);                 // "ışık"
"IŞIK".EqualsIgnoreCase("ışık", Languages.Turkish); // true
"ISIK".EqualsIgnoreCase("isik", Languages.Turkish); // false: I, ı harfinin büyüğüdür
```

Diğer kültürler için `HeroKit.Strings` içindeki `ToTitleCase(culture)` ve `EqualsIgnoreCase(other, culture)` metotlarını kullanın.

Türkçe karşılaştırıcılar ICU olmadan Türk alfabesini izler (c, ç, g, ğ, ı, i, o, ö, s, ş, u, ü). Bu, değişmez globalleştirme (invariant globalization) ile çalışan kapsayıcılarda ve uygulamalarda da geçerlidir; oralarda kültüre dayalı karşılaştırma Türk alfabesini izlemez.

```csharp
using HeroKit.Globalization;

var comparer = Languages.Turkish.TextRules!.Comparer;

string[] names = ["Zeynep", "Çağla", "Ilgın", "İpek", "Cem", "Şule", "Oya", "Ömer", "Uğur", "Ümit", "Gül"];
Array.Sort(names, comparer);
// Cem, Çağla, Gül, Ilgın, İpek, Oya, Ömer, Şule, Uğur, Ümit, Zeynep

var sorted = customers.OrderBy(c => c.Name, comparer).ThenBy(c => c.City, comparer);

var cities = new Dictionary<string, int>(Languages.Turkish.TextRules!.IgnoreCaseComparer) { ["İzmir"] = 35 };
cities.ContainsKey("izmir"); // true
cities.ContainsKey("IZMIR"); // false: I, ı harfidir; i değil
```

`Comparer` küçük harfi önce sıralar. `IgnoreCaseComparer` büyük-küçük harfi Türkçe kurallarla yok sayar; sözlük ya da hash set karşılaştırıcısı olarak kullanılabilir.

### e-Fatura ve e-Arşiv

```csharp
using HeroKit.Globalization.Turkey;

"ABC2026000000001".IsValidGibInvoiceNumber(); // true
"ABC2026000000000".IsValidGibInvoiceNumber(); // false: sıra numarası 1'den başlar
"51d3bb18-9650-4c8e-825d-5a7f845be630".IsValidEttn(); // true

GibInvoiceNumber.TryParse("ABC2026000000001", out GibInvoiceNumber parsed); // true
var invoice = GibInvoiceNumber.Create("ABC", 2026, 41);
invoice.ToString();        // "ABC2026000000041"
invoice.Next().ToString(); // "ABC2026000000042"
```

GİB fatura numarası 16 karakterdir: 3 büyük harf ya da rakamdan (A–Z, 0–9) oluşan seri, 4 haneli yıl (2000–2099) ve 1'den başlayan 9 haneli sıra numarası. `Create` geçersiz bir parça için `ArgumentException`, `Next` sıra numarası 999.999.999'a ulaştığında `InvalidOperationException` fırlatır. ETTN, 8-4-4-4-12 biçiminde boş olmayan bir UUID'dir.

`[GibInvoiceNumber]` ve `[Ettn]` öznitelikleri ile `GibInvoiceNumber()` ve `Ettn()` FluentValidation kuralları [Form doğrulama](validation.md) sayfasında anlatılır.

#### Karekod

```csharp
if (GibInvoiceQrCode.TryParse(scannedText, out GibInvoiceQrCode? qr))
{
    qr.SellerTaxId;       // "4840847211"
    qr.Number.ToString(); // "ABC2026000000001"
    qr.Date;              // 2026-10-15
    qr.PayableAmount;     // 120.00
    qr.VatAmounts[20m];   // 20.00 (orana göre KDV)
}
```

`TryParse`, e-Fatura ya da e-Arşiv faturasının karekodundaki JSON metnini okur. Geçerli satıcı ve alıcı VKN/TCKN'si (VKN'si olmayan e-Arşiv alıcıları için "11111111111" kabul edilir), fatura numarası, ETTN ve tarih ister. Tutarlar isteğe bağlıdır; bütün alanlar `Fields` içinde de bulunur. Yalnızca metni okur; faturanın GİB'de var olup olmadığını sormaz.
