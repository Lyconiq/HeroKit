---
title: C# ile IBAN doğrulama (TR IBAN)
description: C# ve .NET'te IBAN doğrulama: mod-97 algoritması, 26 karakterlik TR IBAN yapısı, boşluk ve küçük harf temizleme, banka adını bulma ve ASP.NET Core form doğrulaması.
---

# C# ile IBAN doğrulama (TR IBAN)

IBAN'ın 3. ve 4. karakterleri, numaranın geri kalanından hesaplanan kontrol haneleridir (ISO 13616, mod-97). Türkiye IBAN'ları 26 karakterdir. Bu sayfa algoritmayı anlatır, kullanıcı girişindeki boşluk ve küçük harfleri nasıl ele alacağınızı ve IBAN'dan bankayı nasıl bulacağınızı gösterir.

## Kısa cevap

```csharp
using HeroKit.Banking;

var iban = "tr33 0006 1005 1978 6457 8413 26";

iban.IsValidIban();   // true
iban.NormalizeIban(); // "TR330006100519786457841326": veritabanına bu
iban.FormatIban();    // "TR33 0006 1005 1978 6457 8413 26": ekrana bu

"TR78 0006 2000 0000 0006 2950 05".GetBank()?.Name; // "Türkiye Garanti Bankası A.Ş."
```

```bash
dotnet add package HeroKit
```

## TR IBAN'ın yapısı

| Karakter | Örnek | Anlamı |
|---|---|---|
| 1–2 | `TR` | Ülke kodu |
| 3–4 | `33` | Kontrol haneleri |
| 5–9 | `00061` | Banka kodu (EFT kodu) |
| 10 | `0` | Rezerv alan, her zaman 0 |
| 11–26 | `0519786457841326` | Hesap numarası (16 hane) |

## Mod-97 kuralı

1. Boşlukları silin, harfleri büyütün.
2. İlk 4 karakteri sona taşıyın: `0006100519786457841326TR33`.
3. Her harfi iki haneli bir sayıyla değiştirin: A = 10, B = 11 … Z = 35. `T` = 29, `R` = 27.
4. Ortaya çıkan çok uzun sayının 97'ye bölümünden kalan **1** ise IBAN geçerlidir.

Sayı `long`'a sığmayacak kadar uzundur. Kalanı hane hane hesaplamak yeterlidir; `BigInteger` gerekmez.

## Kütüphanesiz C# kodu

```csharp
static bool IsValidTurkishIban(string? value)
{
    string iban = (value ?? "").Replace(" ", "").ToUpperInvariant();
    if (iban.Length != 26 || !iban.StartsWith("TR"))
        return false;

    int remainder = 0;
    foreach (char c in iban[4..] + iban[..4])
    {
        if (char.IsAsciiDigit(c))
            remainder = (remainder * 10 + (c - '0')) % 97;
        else if (char.IsAsciiLetterUpper(c))
            remainder = (remainder * 100 + (c - 'A' + 10)) % 97;
        else
            return false;
    }

    return remainder == 1;
}
```

Bu kod Türkiye IBAN'larında HeroKit'in `IsValidIban` metoduyla 300.000 rastgele sayıda aynı sonucu verir. HeroKit ayrıca diğer ülkelerin IBAN'larını da (15–34 karakter) kabul eder.

## Sık yapılan hatalar

- **Boşlukları reddetmek.** Kullanıcılar IBAN'ı banka uygulamasından dörtlü gruplar hâlinde kopyalar. Doğrulamadan önce boşlukları silin ve harfleri büyütün. `NormalizeIban` bunu yapar ve geçersiz IBAN için `null` döner.
- **Yalnız regex kullanmak.** `^TR\d{24}$` tek bir rakamın yanlış yazılmasını yakalamaz. Mod-97 kontrolü yakalar.
- **`long` ile hesaplamaya çalışmak.** 26 karakterlik IBAN, harfler sayıya çevrilince 28 haneli olur ve `long`'a sığmaz. Kalanı adım adım hesaplayın.
- **Geçerli IBAN'ı açık hesap sanmak.** Algoritma numaranın doğru kurulduğunu söyler. Hesabın açık olduğunu ya da alıcı adının tuttuğunu söylemez. Bunu ancak bankanın hizmetleri doğrular.

## Bankayı bulmak

`GetBank`, geçerli bir IBAN'ın banka kodunu (5–9. karakterler) Türkiye Bankalar Birliği'nin EFT kodlarıyla eşleştirir:

```csharp
Bank? bank = "TR78 0006 2000 0000 0006 2950 05".GetBank();
bank?.Name; // "Türkiye Garanti Bankası A.Ş."
bank?.Code; // "00062"
```

Bilinmeyen bir kod için `null` döner. Eksik bir banka görürseniz [issue açın](https://github.com/Lyconiq/HeroKit/issues).

## ASP.NET Core form doğrulaması

```csharp
using HeroKit.Validation;

public sealed class PaymentForm
{
    [Required, Iban]
    public string? Iban { get; set; }
}
```

FluentValidation kullanıyorsanız (`dotnet add package HeroKit.FluentValidation`):

```csharp
RuleFor(x => x.Iban).NotEmpty().Iban();
```

## KVKK için maskeleme

```csharp
using HeroKit.Strings;

"TR330006100519786457841326".Mask(4); // "**********************1326"
```

## İlgili

- [TCKN doğrulama](tckn-validation.md)
- [VKN doğrulama](vkn-validation.md)
- [Ülkeler ve diller](countries-and-languages.md): diğer ülkelerin IBAN kuralları
