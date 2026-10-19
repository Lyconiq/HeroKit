# HeroKit.FileTypes.AspNetCore

ASP.NET Core integration for [HeroKit.FileTypes](https://www.nuget.org/packages/HeroKit.FileTypes): safe upload checks for `IFormFile` in minimal APIs, MVC, Razor Pages and Blazor.

```bash
dotnet add package HeroKit.FileTypes.AspNetCore
```

Every check of `UploadValidator` runs: invalid file names, program or script content (also behind an image extension), hidden double extensions (`a.php.jpg`), content that does not fit the extension, macros in Office documents, script in SVG, size and allowed types.

## Minimal APIs

```csharp
using HeroKit.FileTypes;
using HeroKit.FileTypes.AspNetCore;

app.MapPost("/documents", (IFormFile file) => Results.Ok(file.GetSafeFileName()))
   .ValidateUploads(new UploadPolicy { AllowedTypes = [FileTypes.Pdf], MaxLength = 10 * 1024 * 1024 });
```

A rejected file stops the request before the handler runs. The response is 400 with a validation problem:

```json
{ "errors": { "file": ["invoice.pdf: The content is a Windows executable."] } }
```

`ValidateUploads` also works on a route group and checks `IFormFileCollection` arguments.

## MVC, Razor Pages and Blazor forms

```csharp
public sealed class ProfileForm
{
    [Required, AllowedFileTypes("jpg", "png", "webp", MaxLength = 5 * 1024 * 1024)]
    public IFormFile? Photo { get; set; }
}
```

## Anywhere

```csharp
UploadCheckResult result = file.CheckUpload(policy);   // or validator.Validate(file) with a singleton UploadValidator
string displayName = file.GetSafeFileName();            // safe to store and to send as a download name
```

Documentation: https://herokit.lyconiq.com/articles/file-types.html
