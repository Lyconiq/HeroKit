# Dosya türleri ve güvenli yükleme

`HeroKit.FileTypes` ayrı bir pakettir. Bağımlılığı yoktur ve [Native AOT](nullable-and-aot.md) ile çalışır.

```bash
dotnet add package HeroKit.FileTypes
```

## Dosyanın türünü bulma

```csharp
using HeroKit.FileTypes;

using FileStream stream = File.OpenRead(path);
FileType? type = FileTypes.Detect(stream); // en fazla 16 KB okur ve konumu geri alır
type?.MimeType;  // "image/png"
type?.Extension; // "png"
type?.Category;  // FileTypeCategory.Image
```

Tespit, dosyanın başındaki imza baytlarını (magic bytes) okur. Yaklaşık 70 tür hazır gelir:

| Grup | Türler |
|---|---|
| Resimler | PNG, JPEG, GIF, WebP, BMP, TIFF, ICO, PSD, AVIF, HEIC, HEIF, JPEG XL, SVG |
| Belgeler | PDF, RTF, DOCX, XLSX, PPTX, DOCM, XLSM, PPTM, ODT, ODS, ODP, EPUB, eski Office (OLE) |
| Arşivler | ZIP, 7z, RAR, gzip, bzip2, xz, Zstandard, tar |
| Ses ve video | MP3, AAC, FLAC, Ogg, WAV, M4A, MIDI, MP4, M4V, MOV, 3GP, WebM, Matroska, AVI, MPEG, FLV |
| Fontlar | WOFF, WOFF2, TTF, OTF |
| Programlar | Windows çalıştırılabilir dosyaları, ELF, Mach-O, Java class ve JAR, Android APK ve DEX, WebAssembly, Windows kısayolları |
| Diğer | SQLite, Parquet, glTF binary |
| Metin | JSON, XML, HTML, `#!` satırlı kabuk betikleri, düz metin |

Bazı türler sabit baytlardan fazlasını ister; HeroKit bunları ele alır:

- **Office ve OpenDocument dosyaları ZIP'tir.** HeroKit kayıt adlarını (`word/`, `xl/`, `ppt/`) ve OpenDocument ile EPUB dosyalarının `mimetype` kaydını okur; hiçbir şeyi açmaz. Konumlanabilir (seekable) bir stream'de dosyanın sonundaki ZIP içindekiler listesini (central directory, en fazla 1 MB) de okur; böylece büyük dosyalar da tanınır. Yalnız ilk baytları içeren bir span'de ya da konumlanamayan bir stream'de başlığın ötesindeki kayıtlar görünmez ve sonuç `FileTypes.Zip` olabilir.
- **Makrolu Office dosyaları** (DOCM, XLSM, PPTM) VBA proje kayıtlarından tanınır ve `HasMacros` değerleri true'dur.
- **MP4, MOV, M4A, 3GP, HEIC ve AVIF aynı kabı kullanır.** HeroKit `ftyp` kutusundaki markaları okur; birden çok eşleşmede hata vermez.
- **WebM ile Matroska, Java class dosyaları ile Mach-O evrensel dosyaları** aynı baytlarla başlar. HeroKit onları ayıran baytları okur.
- **Metin türleri** başlangıçlarından tanınır: JSON için `{` ya da `[`, `<?xml`, `<svg`, `<html`. CSV, YAML ve Markdown düz metin gibi görünür; bu yüzden `FileTypes.PlainText` olarak bulunur.

`DetectAll` bütün eşleşmeleri döner, örneğin DOCX ve ZIP. `Detect(ReadOnlySpan<byte>)` elinizdeki baytlarla çalışır; daha uzun bir span ZIP dedektörünün daha çok kayıt görmesini sağlar.

## Uzantılar ve MIME türleri

```csharp
FileTypes.GetMimeType("report.docx");              // "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
FileTypes.GetMimeType("unknown.xyz");              // "application/octet-stream"
FileTypes.FromExtension(".JPEG")?.MimeType;        // "image/jpeg"
FileTypes.FromMimeType("image/x-icon")?.Extension; // "ico" (diğer adlar da kabul edilir)
FileTypes.FromMimeType("application/json; charset=utf-8"); // FileTypes.Json
```

## Hazır dosya türleri

Her hazır tür, `FileTypes` sınıfının `FileType` döndüren statik bir özelliğidir. `FileTypes.BuiltIn` 89 türün hepsini tespit sırasıyla listeler.

```csharp
using HeroKit.FileTypes;

FileType msi = FileTypes.Msi;                     // her çağrıda aynı nesne
msi.Name;                                         // "Windows installer"
msi.Category;                                     // FileTypeCategory.Executable
msi.MimeType;                                     // "application/x-msi"
msi.OtherMimeTypes;                               // ["application/x-ms-installer"]
msi.Extensions;                                   // ["msi", "msp"]
msi.IsExecutable;                                 // true: yüklemesi reddedilir
msi.IsA(FileTypes.CompoundFile);                  // true: MSI bir OLE compound dosyasıdır

FileTypes.FromExtension("setup.msi") == FileTypes.Msi;                   // true
FileTypes.FromMimeType("application/x-ms-installer") == FileTypes.Msi;   // true
FileTypes.Detect(msiBytes) == FileTypes.CompoundFile;                    // true: içerikte MSI'ya özgü bir imza yok
FileTypes.ContentMatchesExtension(msiBytes, "setup.msi");                // true
FileTypes.CheckUpload(msiBytes, "setup.msi").Rejection;                  // UploadRejection.DangerousExtension
```

Getter statik bir özelliktir: `FileTypes.Msi` her zaman aynı `FileType` nesnesini döndürür, bu yüzden `==` ile karşılaştırılabilir. `msiBytes`, bir `.msi` dosyasının ilk baytlarını temsil eder.

Bazı biçimler aynı kabı paylaşır. Eski Office dosyaları, Outlook iletileri ve MSI kurulum dosyaları OLE compound dosyasıdır; içerikleri `CompoundFile` olarak tespit edilir, hangisi olduğunu uzantı söyler. Aşağıdaki notlar bunun nerede geçerli olduğunu gösterir.

#### Arşiv

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `Bzip2` | Bzip2 archive | .bz2, .tbz2 | `application/x-bzip2` |  |
| `Gzip` | Gzip archive | .gz, .tgz | `application/gzip` |  |
| `Rar` | RAR archive | .rar | `application/vnd.rar` |  |
| `SevenZip` | 7-Zip archive | .7z | `application/x-7z-compressed` |  |
| `Tar` | Tar archive | .tar | `application/x-tar` |  |
| `Xz` | XZ archive | .xz, .txz | `application/x-xz` |  |
| `Zip` | ZIP archive | .zip | `application/zip` |  |
| `Zstandard` | Zstandard archive | .zst | `application/zstd` |  |

#### Ses

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `Aac` | AAC audio | .aac | `audio/aac` |  |
| `Flac` | FLAC audio | .flac | `audio/flac` |  |
| `M4a` | MPEG-4 audio | .m4a, .m4b | `audio/mp4` |  |
| `Midi` | MIDI audio | .mid, .midi | `audio/midi` |  |
| `Mp3` | MP3 audio | .mp3 | `audio/mpeg` |  |
| `Ogg` | Ogg audio | .ogg, .oga, .opus | `audio/ogg` |  |
| `Wav` | WAV audio | .wav | `audio/wav` |  |

#### Belge

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `CompoundFile` | OLE compound file | — | `application/x-ole-storage` |  |
| `Doc` | Word 97-2003 document | .doc, .dot | `application/msword` | içerik `CompoundFile` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Docm` | Macro-enabled Word document | .docm, .dotm | `application/vnd.ms-word.document.macroenabled.12` | makrolu, `AllowMacros` yoksa reddedilir; `Zip` üzerine kurulu |
| `Docx` | Word document | .docx, .dotx | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `Zip` üzerine kurulu |
| `Epub` | EPUB e-book | .epub | `application/epub+zip` | `Zip` üzerine kurulu |
| `Msg` | Outlook message | .msg | `application/vnd.ms-outlook` | içerik `CompoundFile` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Odp` | OpenDocument presentation | .odp | `application/vnd.oasis.opendocument.presentation` | `Zip` üzerine kurulu |
| `Ods` | OpenDocument spreadsheet | .ods | `application/vnd.oasis.opendocument.spreadsheet` | `Zip` üzerine kurulu |
| `Odt` | OpenDocument text | .odt | `application/vnd.oasis.opendocument.text` | `Zip` üzerine kurulu |
| `Pdf` | PDF document | .pdf | `application/pdf` |  |
| `Ppt` | PowerPoint 97-2003 presentation | .ppt, .pps, .pot | `application/vnd.ms-powerpoint` | içerik `CompoundFile` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Pptm` | Macro-enabled PowerPoint presentation | .pptm, .potm, .ppsm, .ppam | `application/vnd.ms-powerpoint.presentation.macroenabled.12` | makrolu, `AllowMacros` yoksa reddedilir; `Zip` üzerine kurulu |
| `Pptx` | PowerPoint presentation | .pptx, .ppsx | `application/vnd.openxmlformats-officedocument.presentationml.presentation` | `Zip` üzerine kurulu |
| `Rtf` | Rich Text Format document | .rtf | `application/rtf` |  |
| `Xls` | Excel 97-2003 workbook | .xls, .xlt | `application/vnd.ms-excel` | içerik `CompoundFile` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Xlsm` | Macro-enabled Excel workbook | .xlsm, .xltm, .xlam | `application/vnd.ms-excel.sheet.macroenabled.12` | makrolu, `AllowMacros` yoksa reddedilir; `Zip` üzerine kurulu |
| `Xlsx` | Excel workbook | .xlsx, .xltx | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | `Zip` üzerine kurulu |

#### Program

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `Apk` | Android package | .apk, .aab | `application/vnd.android.package-archive` | program ya da betik, yüklemede reddedilir; `Zip` üzerine kurulu |
| `Dex` | Android Dalvik executable | .dex | `application/vnd.android.dex` | program ya da betik, yüklemede reddedilir |
| `Elf` | ELF executable | .elf, .so | `application/x-executable` | program ya da betik, yüklemede reddedilir |
| `Jar` | Java archive | .jar, .war, .ear | `application/java-archive` | program ya da betik, yüklemede reddedilir; `Zip` üzerine kurulu |
| `JavaClass` | Java class file | .class | `application/java-vm` | program ya da betik, yüklemede reddedilir |
| `MachO` | Mach-O executable | .dylib | `application/x-mach-binary` | program ya da betik, yüklemede reddedilir |
| `Msi` | Windows installer | .msi, .msp | `application/x-msi` | program ya da betik, yüklemede reddedilir; içerik `CompoundFile` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `WebAssembly` | WebAssembly module | .wasm | `application/wasm` | program ya da betik, yüklemede reddedilir |
| `WindowsExecutable` | Windows executable | .exe, .dll, .sys, .scr, .com, .cpl, .ocx | `application/vnd.microsoft.portable-executable` | program ya da betik, yüklemede reddedilir |
| `WindowsShortcut` | Windows shortcut | .lnk | `application/x-ms-shortcut` | program ya da betik, yüklemede reddedilir |

#### Yazı tipi

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `OpenType` | OpenType font | .otf | `font/otf` |  |
| `TrueType` | TrueType font | .ttf | `font/ttf` |  |
| `Woff` | WOFF font | .woff | `font/woff` |  |
| `Woff2` | WOFF2 font | .woff2 | `font/woff2` |  |

#### Görsel

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
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
| `Svg` | SVG image | .svg, .svgz | `image/svg+xml` | `Xml` üzerine kurulu |
| `Tiff` | TIFF image | .tif, .tiff | `image/tiff` |  |
| `WebP` | WebP image | .webp | `image/webp` |  |

#### Diğer

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `Gltf` | glTF model | .gltf | `model/gltf+json` | içerik `Json` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `GltfBinary` | glTF binary model | .glb | `model/gltf-binary` |  |
| `Parquet` | Apache Parquet file | .parquet | `application/vnd.apache.parquet` |  |
| `Sqlite` | SQLite database | .sqlite, .sqlite3, .db | `application/vnd.sqlite3` |  |

#### Betik

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `Batch` | Windows batch file | .bat, .cmd | `application/x-bat` | program ya da betik, yüklemede reddedilir; içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Html` | HTML document | .html, .htm, .xhtml, .shtml, .hta | `text/html` | program ya da betik, yüklemede reddedilir; `PlainText` üzerine kurulu |
| `JavaScript` | JavaScript | .js, .mjs, .cjs | `text/javascript` | program ya da betik, yüklemede reddedilir; içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Php` | PHP script | .php, .phtml, .php3, .php4, .php5, .php7, .php8, .phar | `application/x-httpd-php` | program ya da betik, yüklemede reddedilir; içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `PowerShell` | PowerShell script | .ps1, .psm1, .psd1 | `application/x-powershell` | program ya da betik, yüklemede reddedilir; içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `ShellScript` | Shell script | .sh, .bash, .zsh, .ksh, .csh | `application/x-sh` | program ya da betik, yüklemede reddedilir; `PlainText` üzerine kurulu |
| `VbScript` | VBScript file | .vbs, .vbe, .wsf, .wsh | `text/vbscript` | program ya da betik, yüklemede reddedilir; içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |

#### Metin

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
|---|---|---|---|---|
| `Calendar` | iCalendar file | .ics, .ifb | `text/calendar` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Css` | CSS stylesheet | .css | `text/css` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Csv` | CSV file | .csv | `text/csv` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Json` | JSON document | .json, .map, .geojson, .webmanifest | `application/json` | `PlainText` üzerine kurulu |
| `Markdown` | Markdown document | .md, .markdown | `text/markdown` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `PlainText` | Plain text | .txt, .text, .log | `text/plain` |  |
| `Tsv` | TSV file | .tsv | `text/tab-separated-values` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `VCard` | vCard file | .vcf, .vcard | `text/vcard` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |
| `Xml` | XML document | .xml, .xsd, .xsl, .xslt, .rss, .atom | `application/xml` | `PlainText` üzerine kurulu |
| `Yaml` | YAML document | .yaml, .yml | `application/yaml` | içerik `PlainText` olarak tespit edilir; hangisi olduğunu uzantı söyler |

#### Video

| Özellik | Ad | Uzantılar | MIME türü | Notlar |
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

## İçerik uzantıya uyuyor mu?

```csharp
FileTypes.ContentMatchesExtension(stream, "photo.jpg"); // dosya PNG ya da program ise false
```

Daha genel bir tür, daha özel bir uzantı için kabul edilir: kayıtları başlığın ötesinde kalan bir `.docx` ZIP'tir, bir `.csv` düz metindir. Bu bağları `FileType.Parent` tanımlar, `IsA` izler.

## Güvenli yükleme

```csharp
var policy = new UploadPolicy { AllowedTypes = [FileTypes.Jpeg, FileTypes.Png, FileTypes.Pdf] };

UploadCheckResult result = FileTypes.CheckUpload(file.OpenReadStream(), file.FileName, policy);
if (!result.IsSafe)
    return BadRequest(result.Message);
```

`CheckUpload` OWASP dosya yükleme önerilerini izler. Şunları reddeder:

| Ret | Örnek |
|---|---|
| `InvalidFileName` | `../a.png`, `a.png:evil.exe`, `.htaccess`, kontrol karakteri içeren ya da nokta ile biten adlar |
| `DangerousExtension` | `.exe`, `.js`, `.php`, `.aspx`, `.html`, `web.config` |
| `DoubleExtension` | `invoice.php.jpg`, `photo.exe.png` |
| `UnknownExtension` | uzantı yok ya da HeroKit bilmiyor |
| `EmptyContent` | boş dosya |
| `TooLarge` | `MaxLength` bayttan uzun dosya (varsayılan: sınır yok) |
| `DangerousContent` | uzantısı ne derse desin program ya da betik; örneğin `photo.png` adlı bir Windows programı |
| `UnknownContent` | hiçbir dedektörün tanımadığı içerik (`AllowUnknownContent` ile izin verilir) |
| `ExtensionMismatch` | `photo.png` adlı bir PDF |
| `NotAllowed` | `AllowedTypes` dışındaki tür |
| `ActiveContent` | betik, olay işleyicisi, `javascript:` bağlantısı, `foreignObject`, entity tanımı ya da dış kaynak içeren SVG; `AllowMacros` true değilse makrolu Office belgeleri (DOCM, XLSM ya da VBA projesi olan eski DOC, XLS) |
| `CustomRule` | sizin `Rules` kurallarınızdan biri |

Stream overload'u SVG'nin tamamını `MaxSvgScanLength` (1 MB) sınırına kadar okur; böylece sondaki bir betik de bulunur. `.docx` kılığındaki makrolu bir belge, makro kaydı büyük bir dosyanın sonunda olsa bile `ExtensionMismatch` ile reddedilir. Eski Word ve Excel dosyalarının (DOC, XLS) ayrı bir makrolu türü yoktur; HeroKit bu dosyaların dizinini okur ve VBA projesi olanı reddeder. Dosyanın tamamını span olarak ya da konumlanabilir (seekable) bir stream olarak verin; yalnız dizin okunur. Excel 4.0 makro sayfaları ve PowerPoint 97-2003 makroları bulunmaz.

`UploadValidator` ve `Tckn` kullanan eksiksiz bir ASP.NET Core minimal API örneği [samples/HeroKit.Samples.WebApi](https://github.com/Lyconiq/HeroKit/tree/master/samples/HeroKit.Samples.WebApi) klasöründedir.

### ASP.NET Core

`dotnet add package HeroKit.FileTypes.AspNetCore`, `IFormFile` için denetimler ekler:

```csharp
using HeroKit.FileTypes;
using HeroKit.FileTypes.AspNetCore;

// Minimal API'ler: reddedilen dosya, handler çalışmadan 400 (validation problem) döner.
app.MapPost("/documents", (IFormFile file) => Results.Ok(file.GetSafeFileName()))
   .ValidateUploads(new UploadPolicy { AllowedTypes = [FileTypes.Pdf], MaxLength = 10 * 1024 * 1024 });

// MVC, Razor Pages ve Blazor modelleri
public sealed class ProfileForm
{
    [Required, AllowedFileTypes("jpg", "png", "webp", MaxLength = 5 * 1024 * 1024)]
    public IFormFile? Photo { get; set; }
}

// Her yerde
var pdfOnly = new UploadPolicy { AllowedTypes = [FileTypes.Pdf] };
UploadCheckResult result = file.CheckUpload(pdfOnly); // ya da singleton bir UploadValidator ile validator.Validate(file)
```

`ValidateUploads` route group'larda da çalışır ve `IFormFileCollection` parametrelerini de denetler. FluentValidation kullanıyorsanız HeroKit.FluentValidation'daki `RuleFor(x => x.Photo).SafeUpload(f => f.OpenReadStream(), f => f.FileName, policy)` kuralını kullanın.

### Boyut sınırı ve güvenli adlar

```csharp
using HeroKit.FileTypes;

// file, yüklenen IFormFile'dır.
var policy = new UploadPolicy { AllowedTypes = [FileTypes.Pdf], MaxLength = 10 * 1024 * 1024 }; // en çok 10 MB PDF

using Stream upload = file.OpenReadStream();
UploadCheckResult result = FileTypes.CheckUpload(upload, file.FileName, policy);
if (!result.IsSafe)
    return Results.BadRequest(result.Message); // örneğin "The file is larger than 10485760 bytes."

// Dosyayı üretilmiş bir adla saklayın; temizlenmiş özgün adı gösterim ve indirme için tutun.
string displayName = FileTypes.ToSafeFileName(file.FileName); // "C:\temp\rapor?.pdf" için "rapor_.pdf"
string storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(displayName);

// Sonra, dosya indirilirken (storedFile, saklanan dosyanın stream'idir):
return Results.File(storedFile, FileTypes.GetMimeType(displayName), fileDownloadName: displayName);
```

`MaxLength`, span'in ya da konumlanabilir bir stream'in uzunluğunu denetler. Web sunucusunun istek boyutu sınırını da ayarlayın (örneğin `[RequestSizeLimit]` ya da Kestrel'in `MaxRequestBodySize` ayarı); böylece büyük bir yükleme okunmadan reddedilir.

`ToSafeFileName` yolu atar, Windows dosya adlarında izin verilmeyen karakterleri `_` ile değiştirir, baştaki ve sondaki nokta ile boşlukları siler, `CON` gibi Windows aygıt adlarını korumaya alır ve adı 255 karaktere kısaltır. Türkçe dahil her dilin harfleri kalır. `fileDownloadName` verildiğinde ASP.NET Core `Content-Disposition: attachment` başlığını kendisi yazar ve ASCII dışı adları kodlar (`filename*=UTF-8''...`); bu başlığı elle kurmayın.

Bu kontrol ilk savunma hattıdır. Yükleme boyutunu da sınırlayın, dosyaları web kökünün dışında üretilmiş adlarla saklayın, mümkünse `Content-Disposition: attachment` ile sunun; gerektiğinde resimleri yeniden kodlayın ya da dosyaları tarayın.

## Bağımlılık enjeksiyonu ve kendi kayıt defteriniz

Statik `FileTypes` metotları ortak kayıt defteri `FileTypes.Default`'u kullanır. Bağımlılık enjeksiyonu, testler ya da kiracıya özel türler için örnek oluşturun:

```csharp
var registry = new FileTypeRegistry();            // hazır türler; yalnız kendi listeniz için new FileTypeRegistry(types)
registry.Register(myType);                         // FileTypes.Default'u değiştirmez

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

İki tip de thread-safe'tir. `UploadValidator` kontrollerini bir zincir olarak çalıştırır: her kontrol tek işi olan küçük bir adımdır, sizin `IUploadRule` kurallarınız son adımlardır. `UploadPolicy.BlockedExtensions` içindeki uzantılar, program ve betik türlerinin uzantılarıyla birlikte her zaman reddedilir; varsayılan listeyi `new HashSet<string>(UploadPolicy.DefaultBlockedExtensions) { "dwg" }` ile genişletin.

## Kendi türlerinizi ve kurallarınızı ekleyin

Bir ya da daha çok dedektörle tür kaydedin. Kayıtlı türler hazır türlerden önce denenir; uzantıları ve MIME türleri öncelik kazanır.

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

| Dedektör | Eşleştirdiği |
|---|---|
| `MagicNumber` | bir konumdaki sabit baytlar; `??` her baytla eşleşir |
| `ZipEntryDetector` | ZIP kayıt adı öneki ya da OpenDocument tarzı `mimetype` kaydı |
| `IsoMediaDetector` | MP4 tarzı dosyaların `ftyp` markaları |
| kendi sınıfınız | `IFileTypeDetector.IsMatch(ReadOnlySpan<byte>)` uygulayın |

Yüklemelere `IUploadRule` ile kontrol ekleyin. Kurallar hazır kontrollerden sonra çalışır:

```csharp
sealed class MinimumImageSizeRule : IUploadRule
{
    public string? Validate(ReadOnlySpan<byte> header, string fileName, FileType? detectedType) =>
        detectedType?.Category == FileTypeCategory.Image && header.Length < 100 ? "Resim çok küçük." : null;
}

var policy = new UploadPolicy { Rules = [new MinimumImageSizeRule()] };
```
