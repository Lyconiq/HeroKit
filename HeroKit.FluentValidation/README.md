# HeroKit.FluentValidation

FluentValidation rules for [HeroKit](https://www.nuget.org/packages/HeroKit): Turkish identity and tax numbers, IBAN, card numbers, e-invoice numbers, MERSİS, barcodes, and phone numbers, postal codes, vehicle plates, national and tax ids of any registered country (Türkiye and the United States are built in).

```bash
dotnet add package HeroKit.FluentValidation
```

Requires FluentValidation 12.0.0 or later.

```csharp
using FluentValidation;
using HeroKit.FluentValidation;
using HeroKit.Globalization;

public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.IdentityNumber).NotEmpty().Tckn();
        RuleFor(x => x.TaxId).TaxId(Countries.Turkey);
        RuleFor(x => x.Iban).Iban();
        RuleFor(x => x.Mobile).PhoneNumber(Countries.Turkey, mobileOnly: true);
        RuleFor(x => x.PostalCode).PostalCode(Countries.Turkey);
        RuleFor(x => x.Plate).VehiclePlate(Countries.Turkey);
        RuleFor(x => x.Card).CardNumber();
        RuleFor(x => x.InvoiceNumber).GibInvoiceNumber();
        RuleFor(x => x.Mersis).Mersis();
    }
}
```

Safe file uploads (HeroKit.FileTypes): file name, program or script content, content that does not fit the extension, macros, size and allowed types.

```csharp
using HeroKit.FileTypes;

var imagesOnly = new UploadPolicy { AllowedTypes = [FileTypes.Jpeg, FileTypes.Png], MaxLength = 5 * 1024 * 1024 };
RuleFor(x => x.Photo).NotNull().SafeUpload(f => f.OpenReadStream(), f => f.FileName, imagesOnly); // IFormFile
```

Null and empty values pass; add `NotEmpty()` to require a value. Messages use `{PropertyName}` and can be replaced with `WithMessage`.

Documentation: https://herokit.lyconiq.com/articles/validation.html
