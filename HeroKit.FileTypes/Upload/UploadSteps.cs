namespace HeroKit.FileTypes;

// Rejects empty, too long or unsafe file names: path separators, colons (NTFS streams), control characters,
// characters Windows does not allow, and a leading or trailing dot or space.
internal sealed class FileNameStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        string name = inspection.FileName;
        if (!IsValid(name, inspection.Policy.MaxFileNameLength))
            return inspection.Reject(UploadRejection.InvalidFileName, "The file name is not allowed.");

        inspection.NameParts = name.Split('.');
        inspection.Extension = inspection.NameParts.Length > 1 ? inspection.NameParts[^1].ToLowerInvariant() : null;
        return null;
    }

    private static bool IsValid(string? name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > maxLength)
            return false;

        if (name[0] is '.' or ' ' || name[^1] is '.' or ' ')
            return false;

        foreach (char c in name)
        {
            if (FileNames.IsUnsafeChar(c))
                return false;
        }

        return true;
    }
}

// Rejects a program or script extension, also when it hides before the last extension ("invoice.php.jpg").
internal sealed class BlockedExtensionStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        if (inspection.Extension is { } extension && IsBlocked(extension, inspection))
            return inspection.Reject(UploadRejection.DangerousExtension, $"Files with the extension '.{extension}' are not allowed.");

        string[] parts = inspection.NameParts;
        for (int i = 1; i < parts.Length - 1; i++)
        {
            if (IsBlocked(parts[i].ToLowerInvariant(), inspection))
                return inspection.Reject(UploadRejection.DoubleExtension, $"The file name hides the extension '.{parts[i]}'.");
        }

        return null;
    }

    private static bool IsBlocked(string extension, UploadInspection inspection) =>
        inspection.Policy.BlockedExtensions.Contains(extension) || inspection.Registry.FromExtension(extension) is { IsExecutable: true };
}

internal sealed class EmptyContentStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection) =>
        inspection.Content.IsEmpty ? inspection.Reject(UploadRejection.EmptyContent, "The file is empty.") : null;
}

// Rejects a file longer than the policy allows. A stream that cannot seek has no known length and is not checked here.
internal sealed class FileLengthStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection) =>
        inspection.Policy.MaxLength is { } max && inspection.FileLength > max
            ? inspection.Reject(UploadRejection.TooLarge, $"The file is larger than {max} bytes.")
            : null;
}

// Rejects program or script content whatever the extension says. Every match counts, not only the first.
internal sealed class ExecutableContentStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        foreach (FileType match in inspection.Registry.DetectAll(inspection.Header))
        {
            if (match.IsExecutable)
                return inspection.Reject(UploadRejection.DangerousContent, match, $"The content is a {match.Name}.");
        }

        return null;
    }
}

internal sealed class KnownExtensionStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        inspection.ExtensionType = inspection.Extension is null ? null : inspection.Registry.FromExtension(inspection.Extension);
        return inspection.ExtensionType is null
            ? inspection.Reject(UploadRejection.UnknownExtension, "The file extension is missing or unknown.")
            : null;
    }
}

// Detects the content and checks it against the extension's type.
internal sealed class ContentTypeStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        FileType expected = inspection.ExtensionType!;
        FileType? detected = inspection.DetectedType = inspection.Registry.Detect(inspection.Header);

        if (detected is null)
        {
            return inspection.Policy.AllowUnknownContent
                ? null
                : inspection.Reject(UploadRejection.UnknownContent, "The content type could not be detected.");
        }

        return detected.IsCompatibleWith(expected)
            ? null
            : inspection.Reject(UploadRejection.ExtensionMismatch, $"The content is a {detected.Name}, not a {expected.Name}.");
    }
}

internal sealed class AllowListStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        IReadOnlyCollection<FileType>? allowed = inspection.Policy.AllowedTypes;
        FileType expected = inspection.ExtensionType!;
        return allowed is null || allowed.Contains(expected)
            ? null
            : inspection.Reject(UploadRejection.NotAllowed, $"{expected.Name} files are not allowed.");
    }
}

// Rejects macro-enabled documents, by content or by extension, unless the policy allows macros.
// Legacy DOC and XLS files have one type with or without macros, so their directory is searched for a VBA project.
internal sealed class MacroContentStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        if (inspection.Policy.AllowMacros)
            return null;

        bool hasMacros = inspection.DetectedType?.HasMacros == true || inspection.ExtensionType?.HasMacros == true || HasVbaProject(inspection);
        return hasMacros ? inspection.Reject(UploadRejection.ActiveContent, "The document contains macros.") : null;
    }

    private static bool HasVbaProject(UploadInspection inspection)
    {
        if (inspection.DetectedType?.IsA(FileTypes.CompoundFile) != true)
            return false;

        return inspection.Stream is { } stream
            ? CompoundFileMacros.HasMacros(stream, inspection.StreamStart)
            : CompoundFileMacros.HasMacros(inspection.Content);
    }
}

internal sealed class SvgContentStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        if (inspection.DetectedType is null || !inspection.DetectedType.IsA(FileTypes.Svg))
            return null;

        if (inspection.ContentTruncated)
            return inspection.Reject(UploadRejection.ActiveContent, "The SVG is larger than the scan limit.");

        return SvgInspector.HasActiveContent(inspection.Content)
            ? inspection.Reject(UploadRejection.ActiveContent, "The SVG contains script, event handlers, embedded HTML, entity declarations or external references.")
            : null;
    }
}

// Runs the rules of the policy, the extension point for checks of your own.
internal sealed class CustomRulesStep : IUploadStep
{
    public UploadCheckResult? Check(ref UploadInspection inspection)
    {
        foreach (IUploadRule rule in inspection.Policy.Rules)
        {
            ReadOnlySpan<byte> firstBytes = inspection.Content[..Math.Min(inspection.Content.Length, FileTypeRegistry.HeaderSize)];
            string? message = rule.Validate(firstBytes, inspection.FileName, inspection.DetectedType);
            if (message is not null)
                return inspection.Reject(UploadRejection.CustomRule, message);
        }

        return null;
    }
}
