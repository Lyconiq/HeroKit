---
title: C# ile TC Kimlik No (TCKN) doğrulama
description: C# ve .NET'te TC kimlik numarası doğrulama algoritması, sık yapılan hatalar, ASP.NET Core form doğrulaması ve KVKK'ya uygun maskeleme.
---

# C# ile TC Kimlik No (TCKN) doğrulama

TC kimlik numarası 11 hanelidir ve son iki hanesi ilk dokuz haneden hesaplanan kontrol haneleridir. Bu sayfa algoritmayı adım adım anlatır, sık yapılan hataları gösterir ve aynı kontrolün HeroKit ile tek satırda nasıl yapıldığını gösterir.

## Kısa cevap

```csharp
using HeroKit.Globalization.Turkey;

"10000000146".IsValidTckn(); // true
"10000000147".IsValidTckn(); // false: kontrol hanesi tutmuyor
```

```bash
dotnet add package HeroKit
```

## Kurallar

1. Tam olarak 11 hane vardır ve hepsi rakamdır.
2. İlk hane 0 olamaz.
3. **10. hane:** 1, 3, 5, 7 ve 9. hanelerin toplamının 7 katından 2, 4, 6 ve 8. hanelerin toplamı çıkarılır. Sonucun 10'a bölümünden kalan 10. haneye eşittir.
4. **11. hane:** İlk 10 hanenin toplamının 10'a bölümünden kalan 11. haneye eşittir.

Örnek: `10000000146`

| | Hesap | Sonuç |
|---|---|---|
| Tek sıralar (1, 3, 5, 7, 9) | 1 + 0 + 0 + 0 + 1 | 2 |
| Çift sıralar (2, 4, 6, 8) | 0 + 0 + 0 + 0 | 0 |
| 10. hane | (2 × 7 − 0) mod 10 | 4 ✓ |
| 11. hane | (2 + 0 + 4) mod 10 | 6 ✓ |

## Kütüphanesiz C# kodu

```csharp
static bool IsValidTckn(string? value)
{
    if (value is null || value.Length != 11 || value[0] == '0' || !value.All(char.IsAsciiDigit))
        return false;

    int[] d = value.Select(c => c - '0').ToArray();
    int odd = d[0] + d[2] + d[4] + d[6] + d[8];
    int even = d[1] + d[3] + d[5] + d[7];

    int tenth = ((odd * 7 - even) % 10 + 10) % 10; // C#'ta % negatif sonuç verebilir
    int eleventh = (odd + even + tenth) % 10;

    return d[9] == tenth && d[10] == eleventh;
}
```

Bu kod, HeroKit'in `IsValidTckn` metoduyla 300.000 rastgele sayıda aynı sonucu verir.

## Sık yapılan hatalar

- **Negatif mod.** `odd * 7 - even` negatif olabilir. C#'ta `-3 % 10` sonucu `-3`'tür, `7` değil. `+ 10) % 10` eklenmezse geçerli numaralar reddedilir.
- **Sayı tipinde saklamak.** TCKN'yi `long` ya da `int` olarak tutmayın. Bir kimlik numarasıdır, sayı değildir. Matematik işlemi yapılmaz ve `string` olarak kalmalıdır.
- **Yalnız uzunluk ya da regex kontrolü.** `^\d{11}$` yazım hatalarını yakalamaz. Kontrol haneleri bir rakamın yanlış yazılmasını yakalar.
- **Biçim kontrolünü kimlik kontrolü sanmak.** Algoritma numaranın doğru kurulduğunu söyler, numaranın birine verildiğini söylemez. Kişiyi doğrulamak için ad, soyad ve doğum yılıyla Nüfus ve Vatandaşlık İşleri'nin (NVİ) servisini kullanın.
- **Numarayı loglara yazmak.** TCKN kişisel veridir (KVKK). Loglarda ve ekranlarda maskeleyin (aşağıya bakın).

## ASP.NET Core form doğrulaması

```csharp
using HeroKit.Validation;

public sealed class CustomerForm
{
    [Required, Tckn]
    public string? IdentityNumber { get; set; }
}
```

FluentValidation kullanıyorsanız (`dotnet add package HeroKit.FluentValidation`):

```csharp
RuleFor(x => x.IdentityNumber).NotEmpty().Tckn();
```

## Doğrulanmış değer tipi: `Tckn`

Numarayı sisteme girdiği yerde bir kez doğrulayın, sonra `string` yerine `Tckn` taşıyın:

```csharp
Tckn id = Tckn.Parse("10000000146");    // geçersizse FormatException
if (Tckn.TryParse(input, out Tckn checkedId)) { /* ... */ }

public sealed record Customer(Tckn Id, string Name);           // JSON: {"Id":"10000000146"}
app.MapGet("/customers/{id}", (Tckn id) => Results.Ok(id));    // geçersiz numara 400 döner
```

## KVKK için maskeleme

```csharp
using HeroKit.Strings;

Tckn.Parse("10000000146").ToMaskedString(); // "10*******46"
"10000000146".MaskMiddle(2, 2);              // "10*******46"
```

## Test numaraları

`10000000146` geçerli bir test numarasıdır ve gerçek bir kişiye ait değildir. Kendi testleriniz için yukarıdaki kuralla yeni numaralar üretebilirsiniz. Gerçek kişilerin numaralarını test verisine koymayın.

## İlgili

- [VKN doğrulama](vkn-validation.md): şirketlerin vergi kimlik numarası. Şahıs işletmeleri vergi numarası olarak TCKN kullanır; ikisini birlikte kabul etmek için `IsValidTaxId(Countries.Turkey)` kullanın.
- [IBAN doğrulama](iban-validation.md)
- [Form doğrulama](validation.md): bütün doğrulama öznitelikleri
