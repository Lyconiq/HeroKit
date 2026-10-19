---
title: C# ile vergi kimlik numarası (VKN) doğrulama
description: C# ve .NET'te 10 haneli vergi kimlik numarası (VKN) doğrulama algoritması, örnek hesap, şahıs işletmeleri için TCKN ve ASP.NET Core form doğrulaması.
---

# C# ile vergi kimlik numarası (VKN) doğrulama

Vergi kimlik numarası (VKN) 10 hanelidir ve son hanesi ilk dokuz haneden hesaplanan kontrol hanesidir. Şirketler VKN kullanır. Şahıs işletmeleri ise vergi numarası olarak sahibinin TC kimlik numarasını kullanır. Bu sayfa algoritmayı anlatır ve iki durumu da kabul eden kontrolü gösterir.

## Kısa cevap

```csharp
using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

"4840847211".IsValidVkn();                     // true
"4840847212".IsValidVkn();                     // false
"10000000146".IsValidTaxId(Countries.Turkey);  // true: şahıs işletmesi, TCKN
"4840847211".IsValidTaxId(Countries.Turkey);   // true: şirket, VKN
```

```bash
dotnet add package HeroKit
```

## Kurallar

1. Tam olarak 10 hane vardır ve hepsi rakamdır.
2. İlk 9 hanenin her biri için, `p` hanenin sırası (1–9) olmak üzere:
   - `a = (hane + 10 − p) mod 10`
   - `b = (a × 2^(10 − p)) mod 9`
   - `a` sıfır değilken `b` sıfır çıkarsa `b = 9` alınır.
3. Bütün `b` değerleri toplanır. **10. hane** `(10 − toplam mod 10) mod 10` değerine eşittir.

Örnek: `4840847211`

| Sıra | Hane | a | 2^(10 − p) | b |
|---|---|---|---|---|
| 1 | 4 | 3 | 512 | 6 |
| 2 | 8 | 6 | 256 | 6 |
| 3 | 4 | 1 | 128 | 2 |
| 4 | 0 | 6 | 64 | 6 |
| 5 | 8 | 3 | 32 | 6 |
| 6 | 4 | 8 | 16 | 2 |
| 7 | 7 | 0 | 8 | 0 |
| 8 | 2 | 4 | 4 | 7 |
| 9 | 1 | 2 | 2 | 4 |

Toplam 39. Kontrol hanesi (10 − 9) mod 10 = **1**. Numaranın son hanesi de 1, yani numara geçerli.

## Kütüphanesiz C# kodu

```csharp
static bool IsValidVkn(string? value)
{
    if (value is null || value.Length != 10 || !value.All(char.IsAsciiDigit))
        return false;

    int sum = 0;
    for (int i = 0; i < 9; i++)                         // i = sıra − 1
    {
        int a = (value[i] - '0' + 9 - i) % 10;
        int b = a * (1 << (9 - i)) % 9;                 // 1 << n = 2^n
        if (a != 0 && b == 0)
            b = 9;

        sum += b;
    }

    return (10 - sum % 10) % 10 == value[9] - '0';
}
```

Bu kod, HeroKit'in `IsValidVkn` metoduyla 300.000 rastgele sayıda aynı sonucu verir.

## Sık yapılan hatalar

- **9 kuralını atlamak.** `a` sıfır değilken `b` sıfır çıkarsa 9 alınmalıdır. Bu kural atlanırsa geçerli numaraların bir kısmı reddedilir.
- **Şahıs işletmelerini reddetmek.** Fatura ya da cari kartında vergi numarası alanı yalnız VKN kabul ederse, şahıs işletmeleri TCKN'lerini giremez. Bu alanda `IsValidTaxId(Countries.Turkey)` ya da `[TaxId("TR")]` kullanın.
- **Sayı tipinde saklamak.** VKN `0` ile başlayabilir. `long` olarak saklanırsa baştaki sıfır kaybolur. `string` kullanın.
- **Biçim kontrolünü mükellef kontrolü sanmak.** Algoritma numaranın doğru kurulduğunu söyler. Numaranın kayıtlı bir mükellefe ait olduğunu söylemez. Bunun için Gelir İdaresi Başkanlığı'nın (GİB) sorgulama hizmetlerini kullanın.

## ASP.NET Core form doğrulaması

```csharp
using HeroKit.Validation;

public sealed class InvoiceForm
{
    [Required, TaxId("TR")]   // VKN ya da TCKN
    public string? TaxNumber { get; set; }

    [Vkn]                     // yalnız VKN
    public string? CompanyTaxNumber { get; set; }
}
```

FluentValidation kullanıyorsanız (`dotnet add package HeroKit.FluentValidation`):

```csharp
RuleFor(x => x.TaxNumber).NotEmpty().TaxId(Countries.Turkey);
RuleFor(x => x.CompanyTaxNumber).Vkn();
```

## MERSİS numarası

16 haneli MERSİS numarası "0", şirketin VKN'si ve 5 haneli bir sıra numarasından oluşur:

```csharp
"0879001756600379".IsValidMersis(); // true
```

## İlgili

- [TCKN doğrulama](tckn-validation.md)
- [IBAN doğrulama](iban-validation.md)
- [Form doğrulama](validation.md): bütün doğrulama öznitelikleri
