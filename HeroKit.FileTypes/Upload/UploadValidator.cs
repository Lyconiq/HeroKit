using System.Buffers;

namespace HeroKit.FileTypes;

/// <summary>
/// Checks whether an uploaded file is safe to accept, following the OWASP file upload advice.
/// </summary>
/// <remarks>
/// <para>The checks run as a chain, in this order, and the first rejection stops it: file name, program or script extension
/// (also hidden as in "a.php.jpg"), empty content, <see cref="UploadPolicy.MaxLength"/>, program or script content, known extension, detected content and whether it fits
/// the extension, <see cref="UploadPolicy.AllowedTypes"/>, macros in Office documents (also VBA projects in legacy DOC and XLS files), active content in SVG, then the rules in <see cref="UploadPolicy.Rules"/>.</para>
/// <para>An instance is immutable and thread-safe; register one per policy as a singleton. The check does not replace a request size limit in the web server,
/// generated file names, re-encoding images or virus scanning.</para>
/// </remarks>
/// <example>
/// <code>
/// var validator = new UploadValidator(new UploadPolicy { AllowedTypes = [FileTypes.Jpeg, FileTypes.Png] });
///
/// UploadCheckResult result = validator.Validate(file.OpenReadStream(), file.FileName);
/// if (!result.IsSafe)
///     return BadRequest(result.Message);
/// </code>
/// </example>
public sealed class UploadValidator
{
    private static readonly IUploadStep[] Steps =
    [
        new FileNameStep(),
        new BlockedExtensionStep(),
        new EmptyContentStep(),
        new FileLengthStep(),
        new ExecutableContentStep(),
        new KnownExtensionStep(),
        new ContentTypeStep(),
        new AllowListStep(),
        new MacroContentStep(),
        new SvgContentStep(),
        new CustomRulesStep(),
    ];

    /// <summary>
    /// Creates a validator with <see cref="UploadPolicy.Default"/> and <see cref="FileTypes.Default"/>.
    /// </summary>
    public UploadValidator()
        : this(UploadPolicy.Default, FileTypes.Default)
    {
    }

    /// <summary>
    /// Creates a validator with a policy and <see cref="FileTypes.Default"/>.
    /// </summary>
    /// <param name="policy">The policy.</param>
    public UploadValidator(UploadPolicy policy)
        : this(policy, FileTypes.Default)
    {
    }

    /// <summary>
    /// Creates a validator with a policy and a registry of file types.
    /// </summary>
    /// <param name="policy">The policy.</param>
    /// <param name="registry">The registry used for detection and extension lookups.</param>
    public UploadValidator(UploadPolicy policy, FileTypeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(registry);
        Policy = policy;
        Registry = registry;
    }

    /// <summary>Gets the policy.</summary>
    public UploadPolicy Policy { get; }

    /// <summary>Gets the registry used for detection and extension lookups.</summary>
    public FileTypeRegistry Registry { get; }

    /// <summary>
    /// Checks an uploaded file from its bytes.
    /// </summary>
    /// <remarks>
    /// An SVG is scanned only as far as <paramref name="content"/> goes; pass the whole file or use <see cref="Validate(Stream, string)"/>.
    /// </remarks>
    /// <param name="content">The file content, or at least its first <see cref="FileTypeRegistry.HeaderSize"/> bytes.</param>
    /// <param name="fileName">The file name as uploaded.</param>
    /// <returns>The result with the reason of a rejection.</returns>
    /// <example>
    /// <code>
    /// validator.Validate(pngBytes, "photo.png").IsSafe;        // true
    /// validator.Validate(exeBytes, "photo.png").Rejection;     // DangerousContent
    /// validator.Validate(pngBytes, "photo.php.png").Rejection; // DoubleExtension
    /// </code>
    /// </example>
    public UploadCheckResult Validate(ReadOnlySpan<byte> content, string fileName) =>
        Run(new UploadInspection(content, content[..Math.Min(content.Length, FileTypeRegistry.HeaderSize)], contentTruncated: false, fileName, Policy, Registry));

    /// <summary>
    /// Checks an uploaded file from a stream. Reads like <see cref="FileTypeRegistry.Detect(Stream)"/> (the header and, for ZIP files,
    /// the central directory), or the whole file up to <see cref="UploadPolicy.MaxSvgScanLength"/> for SVG, and restores the position of a seekable stream.
    /// </summary>
    /// <param name="stream">The stream, positioned at the start of the file.</param>
    /// <param name="fileName">The file name as uploaded.</param>
    /// <returns>The result with the reason of a rejection.</returns>
    public UploadCheckResult Validate(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        long start = stream.CanSeek ? stream.Position : 0;
        long? fileLength = stream.CanSeek ? stream.Length - start : null;
        Stream? seekable = stream.CanSeek ? stream : null;
        DetectionBytes bytes = HeaderReader.ReadDetectionBytes(stream, start);
        byte[]? contentBuffer = null;
        try
        {
            ReadOnlySpan<byte> header = bytes.Header;
            bool moreToRead = !bytes.HasDirectory && header.Length == FileTypeRegistry.HeaderSize;
            if (!moreToRead || Registry.Detect(header)?.IsA(FileTypes.Svg) != true)
                return Run(new UploadInspection(header, bytes.Span, contentTruncated: false, fileName, Policy, Registry, fileLength, seekable, start));

            // Read the rest of the SVG, one byte past the limit to notice a larger file.
            int read = header.Length;
            int limit = Math.Max(Policy.MaxSvgScanLength, read);
            contentBuffer = ArrayPool<byte>.Shared.Rent(limit + 1);
            header.CopyTo(contentBuffer);
            int total = read + stream.ReadAtLeast(contentBuffer.AsSpan(read, limit + 1 - read), limit + 1 - read, throwOnEndOfStream: false);

            return Run(new UploadInspection(
                contentBuffer.AsSpan(0, Math.Min(total, limit)), header, contentTruncated: total > limit, fileName, Policy, Registry, fileLength, seekable, start));
        }
        finally
        {
            bytes.Return();
            if (contentBuffer is not null)
                ArrayPool<byte>.Shared.Return(contentBuffer);

            if (stream.CanSeek)
                stream.Position = start;
        }
    }

    private static UploadCheckResult Run(UploadInspection inspection)
    {
        foreach (IUploadStep step in Steps)
        {
            if (step.Check(ref inspection) is { } rejection)
                return rejection;
        }

        return new UploadCheckResult(UploadRejection.None, inspection.DetectedType, null);
    }
}
