# Dosya türleri ve güvenli yükleme

`HeroKit.FileTypes` ayrı bir pakettir. Bağımlılığı yoktur ve Native AOT ile çalışır.

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

### Boyut sınırı ve güvenli adlar

```csharp
var policy = new UploadPolicy { MaxLength = 10 * 1024 * 1024 }; // 10 MB

// Dosyayı üretilmiş bir adla saklayın; temizlenmiş özgün adı gösterim ve indirme için tutun.
string displayName = FileTypes.ToSafeFileName(file.FileName);
string storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(displayName);

// Sonra, dosya indirilirken:
return Results.File(stream, FileTypes.GetMimeType(displayName), fileDownloadName: displayName);
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
