---
title: Validate an IBAN in C# (Turkish IBAN example)
description: How to validate an IBAN in C# and .NET. The ISO 13616 mod-97 algorithm, the 26-character Turkish IBAN, spaces and lower case in user input, finding the bank, and ASP.NET Core form validation.
---

# Validate an IBAN in C# (Turkish IBAN example)

The 3rd and 4th characters of an IBAN are check digits calculated from the rest of the number (ISO 13616, mod-97). Turkish IBANs have 26 characters. This page explains the algorithm, shows how to handle spaces and lower case in user input, and how to find the bank of an IBAN. HeroKit validates the IBANs of every country.

## Short answer

```csharp
using HeroKit.Banking;

var iban = "tr33 0006 1005 1978 6457 8413 26";

iban.IsValidIban();   // true
iban.NormalizeIban(); // "TR330006100519786457841326": store this
iban.FormatIban();    // "TR33 0006 1005 1978 6457 8413 26": show this

"TR78 0006 2000 0000 0006 2950 05".GetBank()?.Name; // "Türkiye Garanti Bankası A.Ş."
```

```bash
dotnet add package HeroKit
```

## Structure of a Turkish IBAN

| Characters | Example | Meaning |
|---|---|---|
| 1–2 | `TR` | Country code |
| 3–4 | `33` | Check digits |
| 5–9 | `00061` | Bank code (EFT code) |
| 10 | `0` | Reserved, always 0 |
| 11–26 | `0519786457841326` | Account number (16 digits) |

## The mod-97 rule

1. Remove the spaces and convert letters to upper case.
2. Move the first 4 characters to the end: `0006100519786457841326TR33`.
3. Replace each letter with a two-digit number: A = 10, B = 11 … Z = 35. `T` = 29, `R` = 27.
4. If the resulting long number modulo 97 is **1**, the IBAN is valid.

The number is too long for `long`. Calculating the remainder digit by digit is enough; `BigInteger` is not needed.

## Plain C# without a library

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

For Turkish IBANs, this code gives the same result as HeroKit's `IsValidIban` for 300,000 random numbers. HeroKit also accepts the IBANs of other countries (15 to 34 characters) and checks the national length where a country has IBAN rules.

## Common mistakes

- **Rejecting spaces.** Users copy IBANs from banking apps in groups of four. Remove the spaces and convert to upper case before the check. `NormalizeIban` does this and returns `null` for an invalid IBAN.
- **Only using a regex.** `^TR\d{24}$` does not catch a mistyped digit. The mod-97 check does.
- **Calculating with `long`.** With letters converted to numbers, a 26-character IBAN has 28 digits and does not fit in `long`. Calculate the remainder step by step.
- **Taking a valid IBAN for an open account.** The algorithm says the number is well formed. It does not say that the account is open or that the name matches. Only the bank's services can confirm that.

## Find the bank

`GetBank` reads the bank code of a valid IBAN (characters 5–9) and looks it up in the EFT codes of the Banks Association of Türkiye:

```csharp
Bank? bank = "TR78 0006 2000 0000 0006 2950 05".GetBank();
bank?.Name; // "Türkiye Garanti Bankası A.Ş."
bank?.Code; // "00062"
```

It returns `null` for an unknown code. If a bank is missing, [open an issue](https://github.com/Lyconiq/HeroKit/issues).

## ASP.NET Core form validation

```csharp
using HeroKit.Validation;

public sealed class PaymentForm
{
    [Required, Iban]
    public string? Iban { get; set; }
}
```

With FluentValidation (`dotnet add package HeroKit.FluentValidation`):

```csharp
RuleFor(x => x.Iban).NotEmpty().Iban();
```

## Masking for logs

```csharp
using HeroKit.Strings;

"TR330006100519786457841326".Mask(4); // "**********************1326"
```

## Related

- [TCKN validation](tckn-validation.md)
- [VKN validation](vkn-validation.md)
- [Countries and languages](countries-and-languages.md): IBAN rules of other countries
