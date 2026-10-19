---
title: Validate a Turkish tax number (VKN) in C#
description: How to validate a 10-digit Turkish tax number (Vergi Kimlik Numarası, VKN) in C# and .NET. The checksum algorithm with a worked example, sole proprietors with a TCKN, and ASP.NET Core form validation.
---

# Validate a Turkish tax number (VKN) in C#

The Turkish tax number (Vergi Kimlik Numarası, VKN) has 10 digits. The last digit is a check digit calculated from the first nine. Companies use a VKN. Sole proprietors use the owner's identity number (TCKN) as tax number. This page explains the algorithm and shows a check that accepts both.

## Short answer

```csharp
using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

"4840847211".IsValidVkn();                     // true
"4840847212".IsValidVkn();                     // false
"10000000146".IsValidTaxId(Countries.Turkey);  // true: sole proprietor, TCKN
"4840847211".IsValidTaxId(Countries.Turkey);   // true: company, VKN
```

```bash
dotnet add package HeroKit
```

## Rules

1. The number has exactly 10 digits.
2. For each of the first 9 digits, with `p` the position (1–9):
   - `a = (digit + 10 − p) mod 10`
   - `b = (a × 2^(10 − p)) mod 9`
   - if `a` is not zero and `b` is zero, `b = 9`.
3. Add up all `b` values. The **10th digit** is `(10 − sum mod 10) mod 10`.

Example: `4840847211`

| Position | Digit | a | 2^(10 − p) | b |
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

The sum is 39. The check digit is (10 − 9) mod 10 = **1**, which is the last digit, so the number is valid.

## Plain C# without a library

```csharp
static bool IsValidVkn(string? value)
{
    if (value is null || value.Length != 10 || !value.All(char.IsAsciiDigit))
        return false;

    int sum = 0;
    for (int i = 0; i < 9; i++)                         // i = position − 1
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

This code gives the same result as HeroKit's `IsValidVkn` for 300,000 random numbers.

## Common mistakes

- **Missing the 9 rule.** When `a` is not zero and `b` is zero, `b` must be 9. Without this rule, some valid numbers are rejected.
- **Rejecting sole proprietors.** If an invoice or customer form accepts only a VKN, sole proprietors cannot enter their TCKN. Use `IsValidTaxId(Countries.Turkey)` or `[TaxId("TR")]` for that field.
- **Storing it as a number.** A VKN can start with 0. Stored as `long`, the leading zero is lost. Use `string`.
- **Taking a well-formed number for a registered taxpayer.** The algorithm says the number is well formed. It does not say that a taxpayer with this number is registered. Use the services of the Revenue Administration (GİB) for that.

## ASP.NET Core form validation

```csharp
using HeroKit.Validation;

public sealed class InvoiceForm
{
    [Required, TaxId("TR")]   // VKN or TCKN
    public string? TaxNumber { get; set; }

    [Vkn]                     // VKN only
    public string? CompanyTaxNumber { get; set; }
}
```

With FluentValidation (`dotnet add package HeroKit.FluentValidation`):

```csharp
RuleFor(x => x.TaxNumber).NotEmpty().TaxId(Countries.Turkey);
RuleFor(x => x.CompanyTaxNumber).Vkn();
```

## MERSİS number

The 16-digit MERSİS number is "0", the company's VKN and a 5-digit serial:

```csharp
"0879001756600379".IsValidMersis(); // true
```

## Related

- [TCKN validation](tckn-validation.md)
- [IBAN validation](iban-validation.md)
- [Form validation](validation.md): every validation attribute
