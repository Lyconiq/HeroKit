namespace HeroKit.FileTypes;

/// <summary>
/// Why an upload was rejected.
/// </summary>
public enum UploadRejection
{
    /// <summary>The upload passed every check.</summary>
    None,

    /// <summary>The file name is empty, too long, starts or ends with a dot or space, or contains a path separator, a colon, a control character or another character Windows does not allow.</summary>
    InvalidFileName,

    /// <summary>The extension belongs to a program or script, for example ".exe", ".js" or ".php", or is in <see cref="UploadPolicy.BlockedExtensions"/>.</summary>
    DangerousExtension,

    /// <summary>A program or script extension is hidden before the last extension, for example "invoice.php.jpg".</summary>
    DoubleExtension,

    /// <summary>The extension is missing or unknown.</summary>
    UnknownExtension,

    /// <summary>The content is empty.</summary>
    EmptyContent,

    /// <summary>The content is a program or script, whatever the extension says.</summary>
    DangerousContent,

    /// <summary>The content type could not be detected and <see cref="UploadPolicy.AllowUnknownContent"/> is false.</summary>
    UnknownContent,

    /// <summary>The content does not fit the extension.</summary>
    ExtensionMismatch,

    /// <summary>The type is not in <see cref="UploadPolicy.AllowedTypes"/>.</summary>
    NotAllowed,

    /// <summary>The SVG contains script, event handlers, embedded HTML, entity declarations or external references, or is too large to scan; or the document carries macros (a macro-enabled type, or a legacy DOC or XLS file with a VBA project) and <see cref="UploadPolicy.AllowMacros"/> is false.</summary>
    ActiveContent,

    /// <summary>A rule from <see cref="UploadPolicy.Rules"/> rejected the upload; see <see cref="UploadCheckResult.Message"/>.</summary>
    CustomRule,

    /// <summary>The file is longer than <see cref="UploadPolicy.MaxLength"/>.</summary>
    TooLarge,
}

/// <summary>
/// The result of <see cref="UploadValidator.Validate(ReadOnlySpan{byte}, string)"/>.
/// </summary>
/// <param name="Rejection">Why the upload was rejected, or <see cref="UploadRejection.None"/>.</param>
/// <param name="DetectedType">The type detected from the content, or null.</param>
/// <param name="Message">A message in English that explains the rejection, or null when the upload is safe.</param>
public readonly record struct UploadCheckResult(UploadRejection Rejection, FileType? DetectedType, string? Message)
{
    /// <summary>Gets whether the upload passed every check.</summary>
    public bool IsSafe => Rejection == UploadRejection.None;
}

/// <summary>
/// An extra check for uploads, run after the built-in checks. Add instances to <see cref="UploadPolicy.Rules"/>.
/// </summary>
/// <example>
/// <code>
/// sealed class NoEmptyPdfRule : IUploadRule
/// {
///     public string? Validate(ReadOnlySpan&lt;byte&gt; header, string fileName, FileType? detectedType) =>
///         detectedType == FileTypes.Pdf &amp;&amp; header.Length &lt; 100 ? "The PDF is too small." : null;
/// }
/// </code>
/// </example>
public interface IUploadRule
{
    /// <summary>
    /// Checks an upload.
    /// </summary>
    /// <param name="header">The first bytes of the file (the whole file when it is shorter than <see cref="FileTypes.HeaderSize"/>).</param>
    /// <param name="fileName">The file name as uploaded.</param>
    /// <param name="detectedType">The type detected from the content, or null.</param>
    /// <returns>A message that explains why the upload is rejected, or null to accept it.</returns>
    string? Validate(ReadOnlySpan<byte> header, string fileName, FileType? detectedType);
}

/// <summary>
/// Settings for <see cref="UploadValidator"/>.
/// </summary>
/// <example>
/// <code>
/// var imagesOnly = new UploadPolicy { AllowedTypes = [FileTypes.Jpeg, FileTypes.Png, FileTypes.WebP] };
/// var validator = new UploadValidator(imagesOnly);
/// UploadCheckResult result = validator.Validate(stream, file.FileName);
/// if (!result.IsSafe) return BadRequest(result.Message);
/// </code>
/// </example>
public sealed class UploadPolicy
{
    /// <summary>Gets the allowed types, or null to allow every type that is not a program or script. The extension's type must be in the list.</summary>
    public IReadOnlyCollection<FileType>? AllowedTypes { get; init; }

    /// <summary>Gets whether content that no detector recognizes is accepted when its extension is allowed. Defaults to false.</summary>
    public bool AllowUnknownContent { get; init; }

    /// <summary>
    /// Gets whether documents with macros are accepted: macro-enabled types (<see cref="FileType.HasMacros"/>, for example DOCM and XLSM)
    /// and legacy DOC and XLS files with a VBA project. Defaults to false.
    /// </summary>
    /// <remarks>
    /// A legacy file is searched for a VBA project only when its directory is available: pass the whole file as a span, or a seekable stream.
    /// Excel 4.0 (XLM) macro sheets and PowerPoint 97-2003 macros are not found.
    /// </remarks>
    public bool AllowMacros { get; init; }

    /// <summary>
    /// Gets the largest allowed file in bytes, or null for no limit. Defaults to null.
    /// </summary>
    /// <remarks>
    /// The length of a span is its length; the length of a stream is read only when the stream can seek. Also limit the request body
    /// in the web server (for example <c>[RequestSizeLimit]</c> in ASP.NET Core), so a large upload is refused before it is read.
    /// </remarks>
    public long? MaxLength { get; init; }

    /// <summary>Gets the longest allowed file name. Defaults to 255.</summary>
    public int MaxFileNameLength { get; init; } = 255;

    /// <summary>Gets the largest SVG that is scanned for active content when a stream is checked; larger SVG files are rejected. Defaults to 1 MB.</summary>
    public int MaxSvgScanLength { get; init; } = 1024 * 1024;

    /// <summary>
    /// Gets the extensions that are always rejected, in lower case without a dot. Extensions of program and script types
    /// (<see cref="FileType.IsExecutable"/>) in the registry are rejected as well. Defaults to <see cref="DefaultBlockedExtensions"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var policy = new UploadPolicy { BlockedExtensions = new HashSet&lt;string&gt;(UploadPolicy.DefaultBlockedExtensions) { "dwg" } };
    /// </code>
    /// </example>
    public IReadOnlySet<string> BlockedExtensions { get; init; } = DefaultBlockedExtensions;

    /// <summary>
    /// Gets server-side and Windows script extensions that are not built-in types: "asp", "aspx", "jsp", "cgi", "py", "config", "reg" and similar.
    /// </summary>
    public static IReadOnlySet<string> DefaultBlockedExtensions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "asp", "aspx", "ascx", "ashx", "asmx", "asa", "axd", "cer", "config", "jsp", "jspx", "cgi", "pl", "py", "pyc", "rb",
        "jse", "msc", "reg", "inf", "pif", "gadget", "application", "appref-ms", "vb", "ws", "url", "desktop", "iqy", "slk",
    };

    /// <summary>Gets extra rules that run after the built-in checks.</summary>
    public IReadOnlyList<IUploadRule> Rules { get; init; } = [];

    // Declared after DefaultBlockedExtensions: static initializers run in source order.

    /// <summary>Gets the default policy: any type that is not a program or script, with a known extension that fits the content.</summary>
    public static UploadPolicy Default { get; } = new();
}
