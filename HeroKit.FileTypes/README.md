# HeroKit.FileTypes

Dependency-free file type detection for .NET 8 and .NET 10, Native AOT compatible.

- **Content detection** by magic bytes for about 70 formats, including the ones other libraries get wrong: DOCX/XLSX/PPTX and OpenDocument inside ZIP, HEIC, AVIF and MP4 brands, WebP, JPEG XL, WebM vs. Matroska, Java class vs. Mach-O.
- **Extension and MIME mapping** in both directions.
- **Content vs. extension** check.
- **Safe upload check** following OWASP: programs and scripts (by content and extension), hidden double extensions (`a.php.jpg`), invalid file names, SVG with script, macros (also in legacy DOC and XLS), size limits, allow-lists, and safe download names.
- **Extensible**: register your own file types and upload rules.

```bash
dotnet add package HeroKit.FileTypes
```

```csharp
using HeroKit.FileTypes;

FileTypes.Detect(stream)?.MimeType;             // "image/png" (reads at most 16 KB, restores the position)
FileTypes.GetMimeType("report.docx");           // "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
FileTypes.FromMimeType("image/x-icon")?.Extension; // "ico"
FileTypes.ContentMatchesExtension(bytes, "photo.jpg");

var policy = new UploadPolicy { AllowedTypes = [FileTypes.Jpeg, FileTypes.Png, FileTypes.Pdf] };
UploadCheckResult result = FileTypes.CheckUpload(file.OpenReadStream(), file.FileName, policy);
if (!result.IsSafe)
    return BadRequest(result.Message); // result.Rejection: DangerousContent, DoubleExtension, ExtensionMismatch, ...

// Or inject instances: new FileTypeRegistry(), new UploadValidator(policy, registry)

// Your own type, checked before the built-in types:
FileTypes.Register(new FileType("Sketch document", "application/x-sketch", FileTypeCategory.Document, "sketch")
{
    Detectors = [new ZipEntryDetector("document.json")],
    Parent = FileTypes.Zip,
});
```

Magic bytes are a first check, not a proof. For uploads, also limit the size, store files under generated names, and re-encode images or scan files when it matters.

Documentation: https://herokit.emreakdemir.dev/articles/file-types.html
