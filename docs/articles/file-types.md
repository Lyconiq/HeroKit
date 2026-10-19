# File types and safe uploads

`HeroKit.FileTypes` is a separate package. It has no dependencies and works with [Native AOT](nullable-and-aot.md).

```bash
dotnet add package HeroKit.FileTypes
```

## Detect the type of a file

```csharp
using HeroKit.FileTypes;

using FileStream stream = File.OpenRead(path);
FileType? type = FileTypes.Detect(stream); // reads at most 16 KB and restores the position
type?.MimeType;  // "image/png"
type?.Extension; // "png"
type?.Category;  // FileTypeCategory.Image
```

Detection reads the magic bytes at the start of the file. About 70 types are built in:

| Group | Types |
|---|---|
| Images | PNG, JPEG, GIF, WebP, BMP, TIFF, ICO, PSD, AVIF, HEIC, HEIF, JPEG XL, SVG |
| Documents | PDF, RTF, DOCX, XLSX, PPTX, DOCM, XLSM, PPTM, ODT, ODS, ODP, EPUB, legacy Office (OLE) |
| Archives | ZIP, 7z, RAR, gzip, bzip2, xz, Zstandard, tar |
| Audio and video | MP3, AAC, FLAC, Ogg, WAV, M4A, MIDI, MP4, M4V, MOV, 3GP, WebM, Matroska, AVI, MPEG, FLV |
| Fonts | WOFF, WOFF2, TTF, OTF |
| Programs | Windows executables, ELF, Mach-O, Java class and JAR, Android APK and DEX, WebAssembly, Windows shortcuts |
| Other | SQLite, Parquet, glTF binary |
| Text | JSON, XML, HTML, shell scripts with a `#!` line, plain text |

Some types need more than fixed bytes, and HeroKit handles them:

- **Office and OpenDocument files are ZIP files.** HeroKit reads the entry names (`word/`, `xl/`, `ppt/`) and the `mimetype` entry of OpenDocument and EPUB files, without decompressing anything. From a seekable stream it also reads the ZIP central directory at the end of the file (at most 1 MB), so large files are recognized too. From a span with only the first bytes, or from a stream that cannot seek, entries beyond the header are not seen and the result can be `FileTypes.Zip`.
- **Macro-enabled Office files** (DOCM, XLSM, PPTM) are recognized by their VBA project entry and have `HasMacros` set.
- **MP4, MOV, M4A, 3GP, HEIC and AVIF share one container.** HeroKit reads the brands of the `ftyp` box and never fails on several matches.
- **WebM and Matroska, Java class files and Mach-O universal binaries** share their first bytes. HeroKit reads the bytes that tell them apart.
- **Text types** are recognized by their start: `{` or `[` for JSON, `<?xml`, `<svg`, `<html`. CSV, YAML and Markdown look like plain text, so they are detected as `FileTypes.PlainText`.

`DetectAll` returns every match, for example DOCX and ZIP. `Detect(ReadOnlySpan<byte>)` works on bytes you already have; a longer span lets the ZIP detector see more entries.

## Extensions and MIME types

```csharp
FileTypes.GetMimeType("report.docx");              // "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
FileTypes.GetMimeType("unknown.xyz");              // "application/octet-stream"
FileTypes.FromExtension(".JPEG")?.MimeType;        // "image/jpeg"
FileTypes.FromMimeType("image/x-icon")?.Extension; // "ico" (aliases are accepted)
FileTypes.FromMimeType("application/json; charset=utf-8"); // FileTypes.Json
```

## Built-in file types

Every built-in type is a static property of `FileTypes` that returns a `FileType`. `FileTypes.BuiltIn` lists all 89 of them in detection order.

```csharp
using HeroKit.FileTypes;

FileType msi = FileTypes.Msi;                     // the same instance on every call
msi.Name;                                         // "Windows installer"
msi.Category;                                     // FileTypeCategory.Executable
msi.MimeType;                                     // "application/x-msi"
msi.OtherMimeTypes;                               // ["application/x-ms-installer"]
msi.Extensions;                                   // ["msi", "msp"]
msi.IsExecutable;                                 // true: uploads of it are rejected
msi.IsA(FileTypes.CompoundFile);                  // true: an MSI is an OLE compound file

FileTypes.FromExtension("setup.msi") == FileTypes.Msi;                   // true
FileTypes.FromMimeType("application/x-ms-installer") == FileTypes.Msi;   // true
FileTypes.Detect(msiBytes) == FileTypes.CompoundFile;                    // true: the content has no MSI-only signature
FileTypes.ContentMatchesExtension(msiBytes, "setup.msi");                // true
FileTypes.CheckUpload(msiBytes, "setup.msi").Rejection;                  // UploadRejection.DangerousExtension
```

The getter is a static property: `FileTypes.Msi` always returns the same `FileType`, so compare with `==`. `msiBytes` stands for the first bytes of an `.msi` file.

Some formats share one container. Legacy Office files, Outlook messages and MSI installers are all OLE compound files, so their content detects as `CompoundFile` and the extension tells which one it is. The notes below say when this applies.

#### Archive

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Bzip2` | Bzip2 archive | .bz2, .tbz2 | `application/x-bzip2` |  |
| `Gzip` | Gzip archive | .gz, .tgz | `application/gzip` |  |
| `Rar` | RAR archive | .rar | `application/vnd.rar` |  |
| `SevenZip` | 7-Zip archive | .7z | `application/x-7z-compressed` |  |
| `Tar` | Tar archive | .tar | `application/x-tar` |  |
| `Xz` | XZ archive | .xz, .txz | `application/x-xz` |  |
| `Zip` | ZIP archive | .zip | `application/zip` |  |
| `Zstandard` | Zstandard archive | .zst | `application/zstd` |  |

#### Audio

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Aac` | AAC audio | .aac | `audio/aac` |  |
| `Flac` | FLAC audio | .flac | `audio/flac` |  |
| `M4a` | MPEG-4 audio | .m4a, .m4b | `audio/mp4` |  |
| `Midi` | MIDI audio | .mid, .midi | `audio/midi` |  |
| `Mp3` | MP3 audio | .mp3 | `audio/mpeg` |  |
| `Ogg` | Ogg audio | .ogg, .oga, .opus | `audio/ogg` |  |
| `Wav` | WAV audio | .wav | `audio/wav` |  |

#### Document

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `CompoundFile` | OLE compound file | — | `application/x-ole-storage` |  |
| `Doc` | Word 97-2003 document | .doc, .dot | `application/msword` | content detects as `CompoundFile`; the extension tells which |
| `Docm` | Macro-enabled Word document | .docm, .dotm | `application/vnd.ms-word.document.macroenabled.12` | has macros, rejected unless `AllowMacros`; built on `Zip` |
| `Docx` | Word document | .docx, .dotx | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | built on `Zip` |
| `Epub` | EPUB e-book | .epub | `application/epub+zip` | built on `Zip` |
| `Msg` | Outlook message | .msg | `application/vnd.ms-outlook` | content detects as `CompoundFile`; the extension tells which |
| `Odp` | OpenDocument presentation | .odp | `application/vnd.oasis.opendocument.presentation` | built on `Zip` |
| `Ods` | OpenDocument spreadsheet | .ods | `application/vnd.oasis.opendocument.spreadsheet` | built on `Zip` |
| `Odt` | OpenDocument text | .odt | `application/vnd.oasis.opendocument.text` | built on `Zip` |
| `Pdf` | PDF document | .pdf | `application/pdf` |  |
| `Ppt` | PowerPoint 97-2003 presentation | .ppt, .pps, .pot | `application/vnd.ms-powerpoint` | content detects as `CompoundFile`; the extension tells which |
| `Pptm` | Macro-enabled PowerPoint presentation | .pptm, .potm, .ppsm, .ppam | `application/vnd.ms-powerpoint.presentation.macroenabled.12` | has macros, rejected unless `AllowMacros`; built on `Zip` |
| `Pptx` | PowerPoint presentation | .pptx, .ppsx | `application/vnd.openxmlformats-officedocument.presentationml.presentation` | built on `Zip` |
| `Rtf` | Rich Text Format document | .rtf | `application/rtf` |  |
| `Xls` | Excel 97-2003 workbook | .xls, .xlt | `application/vnd.ms-excel` | content detects as `CompoundFile`; the extension tells which |
| `Xlsm` | Macro-enabled Excel workbook | .xlsm, .xltm, .xlam | `application/vnd.ms-excel.sheet.macroenabled.12` | has macros, rejected unless `AllowMacros`; built on `Zip` |
| `Xlsx` | Excel workbook | .xlsx, .xltx | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | built on `Zip` |

#### Executable

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Apk` | Android package | .apk, .aab | `application/vnd.android.package-archive` | program or script, rejected in uploads; built on `Zip` |
| `Dex` | Android Dalvik executable | .dex | `application/vnd.android.dex` | program or script, rejected in uploads |
| `Elf` | ELF executable | .elf, .so | `application/x-executable` | program or script, rejected in uploads |
| `Jar` | Java archive | .jar, .war, .ear | `application/java-archive` | program or script, rejected in uploads; built on `Zip` |
| `JavaClass` | Java class file | .class | `application/java-vm` | program or script, rejected in uploads |
| `MachO` | Mach-O executable | .dylib | `application/x-mach-binary` | program or script, rejected in uploads |
| `Msi` | Windows installer | .msi, .msp | `application/x-msi` | program or script, rejected in uploads; content detects as `CompoundFile`; the extension tells which |
| `WebAssembly` | WebAssembly module | .wasm | `application/wasm` | program or script, rejected in uploads |
| `WindowsExecutable` | Windows executable | .exe, .dll, .sys, .scr, .com, .cpl, .ocx | `application/vnd.microsoft.portable-executable` | program or script, rejected in uploads |
| `WindowsShortcut` | Windows shortcut | .lnk | `application/x-ms-shortcut` | program or script, rejected in uploads |

#### Font

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `OpenType` | OpenType font | .otf | `font/otf` |  |
| `TrueType` | TrueType font | .ttf | `font/ttf` |  |
| `Woff` | WOFF font | .woff | `font/woff` |  |
| `Woff2` | WOFF2 font | .woff2 | `font/woff2` |  |

#### Image

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Avif` | AVIF image | .avif | `image/avif` |  |
| `Bmp` | BMP image | .bmp, .dib | `image/bmp` |  |
| `Gif` | GIF image | .gif | `image/gif` |  |
| `Heic` | HEIC image | .heic | `image/heic` |  |
| `Heif` | HEIF image | .heif | `image/heif` |  |
| `Icon` | Windows icon | .ico | `image/vnd.microsoft.icon` |  |
| `Jpeg` | JPEG image | .jpg, .jpeg, .jpe, .jfif | `image/jpeg` |  |
| `JpegXl` | JPEG XL image | .jxl | `image/jxl` |  |
| `Png` | PNG image | .png, .apng | `image/png` |  |
| `Psd` | Photoshop document | .psd | `image/vnd.adobe.photoshop` |  |
| `Svg` | SVG image | .svg, .svgz | `image/svg+xml` | built on `Xml` |
| `Tiff` | TIFF image | .tif, .tiff | `image/tiff` |  |
| `WebP` | WebP image | .webp | `image/webp` |  |

#### Other

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Gltf` | glTF model | .gltf | `model/gltf+json` | content detects as `Json`; the extension tells which |
| `GltfBinary` | glTF binary model | .glb | `model/gltf-binary` |  |
| `Parquet` | Apache Parquet file | .parquet | `application/vnd.apache.parquet` |  |
| `Sqlite` | SQLite database | .sqlite, .sqlite3, .db | `application/vnd.sqlite3` |  |

#### Script

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Batch` | Windows batch file | .bat, .cmd | `application/x-bat` | program or script, rejected in uploads; content detects as `PlainText`; the extension tells which |
| `Html` | HTML document | .html, .htm, .xhtml, .shtml, .hta | `text/html` | program or script, rejected in uploads; built on `PlainText` |
| `JavaScript` | JavaScript | .js, .mjs, .cjs | `text/javascript` | program or script, rejected in uploads; content detects as `PlainText`; the extension tells which |
| `Php` | PHP script | .php, .phtml, .php3, .php4, .php5, .php7, .php8, .phar | `application/x-httpd-php` | program or script, rejected in uploads; content detects as `PlainText`; the extension tells which |
| `PowerShell` | PowerShell script | .ps1, .psm1, .psd1 | `application/x-powershell` | program or script, rejected in uploads; content detects as `PlainText`; the extension tells which |
| `ShellScript` | Shell script | .sh, .bash, .zsh, .ksh, .csh | `application/x-sh` | program or script, rejected in uploads; built on `PlainText` |
| `VbScript` | VBScript file | .vbs, .vbe, .wsf, .wsh | `text/vbscript` | program or script, rejected in uploads; content detects as `PlainText`; the extension tells which |

#### Text

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Calendar` | iCalendar file | .ics, .ifb | `text/calendar` | content detects as `PlainText`; the extension tells which |
| `Css` | CSS stylesheet | .css | `text/css` | content detects as `PlainText`; the extension tells which |
| `Csv` | CSV file | .csv | `text/csv` | content detects as `PlainText`; the extension tells which |
| `Json` | JSON document | .json, .map, .geojson, .webmanifest | `application/json` | built on `PlainText` |
| `Markdown` | Markdown document | .md, .markdown | `text/markdown` | content detects as `PlainText`; the extension tells which |
| `PlainText` | Plain text | .txt, .text, .log | `text/plain` |  |
| `Tsv` | TSV file | .tsv | `text/tab-separated-values` | content detects as `PlainText`; the extension tells which |
| `VCard` | vCard file | .vcf, .vcard | `text/vcard` | content detects as `PlainText`; the extension tells which |
| `Xml` | XML document | .xml, .xsd, .xsl, .xslt, .rss, .atom | `application/xml` | built on `PlainText` |
| `Yaml` | YAML document | .yaml, .yml | `application/yaml` | content detects as `PlainText`; the extension tells which |

#### Video

| Property | Name | Extensions | MIME type | Notes |
|---|---|---|---|---|
| `Avi` | AVI video | .avi | `video/x-msvideo` |  |
| `Flv` | Flash video | .flv | `video/x-flv` |  |
| `M4v` | MPEG-4 video (iTunes) | .m4v | `video/x-m4v` |  |
| `Matroska` | Matroska video | .mkv, .mka, .mk3d | `video/x-matroska` |  |
| `Mp4` | MPEG-4 video | .mp4, .m4p | `video/mp4` |  |
| `Mpeg` | MPEG video | .mpg, .mpeg, .m2p, .vob | `video/mpeg` |  |
| `QuickTime` | QuickTime video | .mov, .qt | `video/quicktime` |  |
| `ThreeGp` | 3GPP video | .3gp, .3g2 | `video/3gpp` |  |
| `WebM` | WebM video | .webm | `video/webm` |  |

## Does the content fit the extension?

```csharp
FileTypes.ContentMatchesExtension(stream, "photo.jpg"); // false when the file is a PNG or a program
```

A more general detected type is accepted for a more specific extension: a `.docx` whose entries are beyond the header is a ZIP, and a `.csv` is plain text. `FileType.Parent` describes these links, and `IsA` follows them.

## Safe uploads

```csharp
var policy = new UploadPolicy { AllowedTypes = [FileTypes.Jpeg, FileTypes.Png, FileTypes.Pdf] };

UploadCheckResult result = FileTypes.CheckUpload(file.OpenReadStream(), file.FileName, policy);
if (!result.IsSafe)
    return BadRequest(result.Message);
```

`CheckUpload` follows the OWASP file upload advice. It rejects:

| Rejection | Example |
|---|---|
| `InvalidFileName` | `../a.png`, `a.png:evil.exe`, `.htaccess`, names with control characters or ending in a dot |
| `DangerousExtension` | `.exe`, `.js`, `.php`, `.aspx`, `.html`, `web.config` |
| `DoubleExtension` | `invoice.php.jpg`, `photo.exe.png` |
| `UnknownExtension` | no extension, or one HeroKit does not know |
| `EmptyContent` | an empty file |
| `TooLarge` | a file longer than `MaxLength` bytes (no limit by default) |
| `DangerousContent` | a program or script whatever the extension says, for example a Windows executable named `photo.png` |
| `UnknownContent` | content no detector recognizes (allow it with `AllowUnknownContent`) |
| `ExtensionMismatch` | a PDF named `photo.png` |
| `NotAllowed` | a type outside `AllowedTypes` |
| `ActiveContent` | SVG with script, event handlers, `javascript:` links, `foreignObject`, entity declarations or external references; Office documents with macros (DOCM, XLSM, or a legacy DOC or XLS with a VBA project) unless `AllowMacros` is true |
| `CustomRule` | one of your `Rules` |

The stream overload reads the whole SVG up to `MaxSvgScanLength` (1 MB) so a script at the end is found. A macro-enabled document disguised as `.docx` is rejected as `ExtensionMismatch`, also when the macro entry is at the end of a large file. Legacy Word and Excel files (DOC, XLS) have no separate macro type, so HeroKit reads their directory and rejects a file with a VBA project. Pass the whole file as a span, or a seekable stream; only the directory is read. Excel 4.0 macro sheets and PowerPoint 97-2003 macros are not found.

A complete ASP.NET Core minimal API that uses `UploadValidator` and `Tckn` is in [samples/HeroKit.Samples.WebApi](https://github.com/Lyconiq/HeroKit/tree/master/samples/HeroKit.Samples.WebApi).

### ASP.NET Core

`dotnet add package HeroKit.FileTypes.AspNetCore` adds checks for `IFormFile`:

```csharp
using HeroKit.FileTypes;
using HeroKit.FileTypes.AspNetCore;

// Minimal APIs: a rejected file returns 400 (validation problem) before the handler runs.
app.MapPost("/documents", (IFormFile file) => Results.Ok(file.GetSafeFileName()))
   .ValidateUploads(new UploadPolicy { AllowedTypes = [FileTypes.Pdf], MaxLength = 10 * 1024 * 1024 });

// MVC, Razor Pages and Blazor models
public sealed class ProfileForm
{
    [Required, AllowedFileTypes("jpg", "png", "webp", MaxLength = 5 * 1024 * 1024)]
    public IFormFile? Photo { get; set; }
}

// Anywhere
var pdfOnly = new UploadPolicy { AllowedTypes = [FileTypes.Pdf] };
UploadCheckResult result = file.CheckUpload(pdfOnly); // or validator.Validate(file) with a singleton UploadValidator
```

`ValidateUploads` also works on route groups and checks `IFormFileCollection` arguments. With FluentValidation, use `RuleFor(x => x.Photo).SafeUpload(f => f.OpenReadStream(), f => f.FileName, policy)` from HeroKit.FluentValidation.

### Size limit and safe names

```csharp
using HeroKit.FileTypes;

// file is the uploaded IFormFile.
var policy = new UploadPolicy { AllowedTypes = [FileTypes.Pdf], MaxLength = 10 * 1024 * 1024 }; // PDF up to 10 MB

using Stream upload = file.OpenReadStream();
UploadCheckResult result = FileTypes.CheckUpload(upload, file.FileName, policy);
if (!result.IsSafe)
    return Results.BadRequest(result.Message); // for example "The file is larger than 10485760 bytes."

// Store under a generated name; keep the cleaned original name for display and downloads.
string displayName = FileTypes.ToSafeFileName(file.FileName); // "rapor_.pdf" for "C:\temp\rapor?.pdf"
string storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(displayName);

// Later, when the file is downloaded (storedFile is the stream of the stored file):
return Results.File(storedFile, FileTypes.GetMimeType(displayName), fileDownloadName: displayName);
```

`MaxLength` checks the length of a span, or of a stream that can seek. Also set the request size limit of the web server (for example `[RequestSizeLimit]` or Kestrel's `MaxRequestBodySize`), so a large upload is refused before it is read.

`ToSafeFileName` drops any path, replaces characters that are not allowed in Windows file names with `_`, trims dots and spaces, guards Windows device names such as `CON`, and shortens the name to 255 characters. Letters of every language stay. With `fileDownloadName`, ASP.NET Core writes the `Content-Disposition: attachment` header and encodes non-ASCII names (`filename*=UTF-8''...`), so do not build that header by hand.

The check is a first line of defense. Also limit the upload size, store files under generated names outside the web root, serve them with `Content-Disposition: attachment` where possible, and re-encode images or scan files when it matters.

## Dependency injection and your own registry

The static `FileTypes` methods use the shared registry `FileTypes.Default`. For dependency injection, tests or per-tenant types, create instances:

```csharp
var registry = new FileTypeRegistry();            // the built-in types; new FileTypeRegistry(types) for only your list
registry.Register(myType);                         // does not change FileTypes.Default

builder.Services.AddSingleton(registry);
builder.Services.AddSingleton(new UploadValidator(new UploadPolicy { AllowedTypes = [FileTypes.Pdf] }, registry));

public sealed class DocumentsController(UploadValidator validator) : ControllerBase
{
    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        UploadCheckResult result = validator.Validate(file.OpenReadStream(), file.FileName);
        return result.IsSafe ? Ok() : BadRequest(result.Message);
    }
}
```

Both types are thread-safe. `UploadValidator` runs its checks as a chain: each check is a small step with one job, and your `IUploadRule` instances are the last steps. Extensions in `UploadPolicy.BlockedExtensions` are always rejected, together with the extensions of program and script types; extend the default list with `new HashSet<string>(UploadPolicy.DefaultBlockedExtensions) { "dwg" }`.

## Add your own types and rules

Register a type with one or more detectors. Registered types are checked before the built-in types, and their extensions and MIME types win.

```csharp
FileTypes.Register(new FileType("Sketch document", "application/x-sketch", FileTypeCategory.Document, "sketch")
{
    Detectors = [new ZipEntryDetector("document.json")],
    Parent = FileTypes.Zip,
});

FileTypes.Register(new FileType("Canon raw image", "image/x-canon-cr3", FileTypeCategory.Image, "cr3")
{
    Detectors = [new IsoMediaDetector("crx ")],
});

FileTypes.Register(new FileType("My format", "application/x-my-format", FileTypeCategory.Other, "myf")
{
    Detectors = [new MagicNumber("4D 59 46 ?? 01"), MagicNumber.FromText("MYF2")],
});
```

| Detector | Matches |
|---|---|
| `MagicNumber` | fixed bytes at an offset; `??` matches any byte |
| `ZipEntryDetector` | a ZIP entry name prefix, or an OpenDocument-style `mimetype` entry |
| `IsoMediaDetector` | `ftyp` brands of MP4-style files |
| your class | implement `IFileTypeDetector.IsMatch(ReadOnlySpan<byte>)` |

Add checks to uploads with `IUploadRule`. Rules run after the built-in checks:

```csharp
sealed class MinimumImageSizeRule : IUploadRule
{
    public string? Validate(ReadOnlySpan<byte> header, string fileName, FileType? detectedType) =>
        detectedType?.Category == FileTypeCategory.Image && header.Length < 100 ? "The image is too small." : null;
}

var policy = new UploadPolicy { Rules = [new MinimumImageSizeRule()] };
```
