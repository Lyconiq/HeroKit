---
title: Validate a Turkish identity number (TCKN) in C#
description: How to validate a Turkish identity number (TC Kimlik No, TCKN) in C# and .NET. The checksum algorithm, common mistakes, ASP.NET Core form validation and masking for logs.
---

# Validate a Turkish identity number (TCKN) in C#

The Turkish identity number (TC Kimlik No, TCKN) has 11 digits. The last two digits are check digits calculated from the first nine. This page explains the algorithm step by step, shows common mistakes, and shows the same check in one line with HeroKit.

## Short answer

```csharp
using HeroKit.Globalization.Turkey;

"10000000146".IsValidTckn(); // true
"10000000147".IsValidTckn(); // false: wrong check digit
```

```bash
dotnet add package HeroKit
```

## Rules

1. The number has exactly 11 digits.
2. The first digit is not 0.
3. **10th digit:** take 7 times the sum of the digits at positions 1, 3, 5, 7 and 9, and subtract the sum of the digits at positions 2, 4, 6 and 8. The result modulo 10 is the 10th digit.
4. **11th digit:** the sum of the first 10 digits modulo 10 is the 11th digit.

Example: `10000000146`

| | Calculation | Result |
|---|---|---|
| Odd positions (1, 3, 5, 7, 9) | 1 + 0 + 0 + 0 + 1 | 2 |
| Even positions (2, 4, 6, 8) | 0 + 0 + 0 + 0 | 0 |
| 10th digit | (2 × 7 − 0) mod 10 | 4 ✓ |
| 11th digit | (2 + 0 + 4) mod 10 | 6 ✓ |

## Plain C# without a library

```csharp
static bool IsValidTckn(string? value)
{
    if (value is null || value.Length != 11 || value[0] == '0' || !value.All(char.IsAsciiDigit))
        return false;

    int[] d = value.Select(c => c - '0').ToArray();
    int odd = d[0] + d[2] + d[4] + d[6] + d[8];
    int even = d[1] + d[3] + d[5] + d[7];

    int tenth = ((odd * 7 - even) % 10 + 10) % 10; // % can return a negative number in C#
    int eleventh = (odd + even + tenth) % 10;

    return d[9] == tenth && d[10] == eleventh;
}
```

This code gives the same result as HeroKit's `IsValidTckn` for 300,000 random numbers.

## Common mistakes

- **Negative modulo.** `odd * 7 - even` can be negative. In C#, `-3 % 10` is `-3`, not `7`. Without `+ 10) % 10`, valid numbers are rejected.
- **Storing it as a number.** Do not store a TCKN as `long` or `int`. It is an identifier, not a quantity. Keep it as a `string`.
- **Only checking the length or a regex.** `^\d{11}$` does not catch a mistyped digit. The check digits do.
- **Taking a well-formed number for a real person.** The algorithm says the number is well formed, not that it is issued. To verify a person, use the service of the General Directorate of Population and Citizenship Affairs (NVİ) with the name, surname and birth year.
- **Writing it to logs.** A TCKN is personal data under KVKK, the Turkish data protection law. Mask it in logs and on screens (see below).

## ASP.NET Core form validation

```csharp
using HeroKit.Validation;

public sealed class CustomerForm
{
    [Required, Tckn]
    public string? IdentityNumber { get; set; }
}
```

With FluentValidation (`dotnet add package HeroKit.FluentValidation`):

```csharp
RuleFor(x => x.IdentityNumber).NotEmpty().Tckn();
```

## A validated value type: `Tckn`

Validate the number once where it enters the system, then pass `Tckn` instead of `string`:

```csharp
Tckn id = Tckn.Parse("10000000146");    // FormatException for an invalid number
if (Tckn.TryParse(input, out Tckn checkedId)) { /* ... */ }

public sealed record Customer(Tckn Id, string Name);           // JSON: {"Id":"10000000146"}
app.MapGet("/customers/{id}", (Tckn id) => Results.Ok(id));    // an invalid number returns 400
```

## Masking for logs

```csharp
using HeroKit.Strings;

Tckn.Parse("10000000146").ToMaskedString(); // "10*******46"
"10000000146".MaskMiddle(2, 2);              // "10*******46"
```

## Test numbers

`10000000146` is a valid test number and does not belong to a real person. Generate more test numbers with the rules above. Do not put real people's numbers into test data.

## Related

- [VKN validation](vkn-validation.md): the tax number of companies. Sole proprietors use their TCKN as tax number; accept both with `IsValidTaxId(Countries.Turkey)`.
- [IBAN validation](iban-validation.md)
- [Form validation](validation.md): every validation attribute
