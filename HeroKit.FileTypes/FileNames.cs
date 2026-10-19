namespace HeroKit.FileTypes;

// File name parsing shared by lookups and upload checks.
internal static class FileNames
{
    public const int MaxLength = 255;

    // Device names Windows reserves in any folder and with any extension.
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    // Path separators, the colon of NTFS streams, control characters and the other characters Windows does not allow in a name.
    public static bool IsUnsafeChar(char c) =>
        c < 0x20 || c == 0x7F || c is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|';

    public static string ToSafe(string? fileName, string fallback)
    {
        string name = fileName ?? string.Empty;
        int separator = name.LastIndexOfAny(['/', '\\']);
        if (separator >= 0)
            name = name[(separator + 1)..];

        Span<char> buffer = name.Length <= 256 ? stackalloc char[name.Length] : new char[name.Length];
        int length = 0;
        foreach (char c in name)
        {
            if (char.IsWhiteSpace(c))
            {
                if (length > 0 && buffer[length - 1] != ' ')
                    buffer[length++] = ' ';
            }
            else
            {
                buffer[length++] = IsUnsafeChar(c) ? '_' : c;
            }
        }

        string safe = buffer[..length].ToString().Trim(' ', '.');
        if (safe.Length == 0)
            return fallback;

        int dot = safe.IndexOf('.');
        if (ReservedNames.Contains(dot < 0 ? safe : safe[..dot]))
            safe = "_" + safe;

        return Shorten(safe);
    }

    // Cuts the name to MaxLength characters and keeps a short extension, without splitting a surrogate pair.
    private static string Shorten(string name)
    {
        if (name.Length <= MaxLength)
            return name;

        int dot = name.LastIndexOf('.');
        string extension = dot > 0 && name.Length - dot <= 16 ? name[dot..] : string.Empty;
        int keep = MaxLength - extension.Length;
        if (char.IsHighSurrogate(name[keep - 1]))
            keep--;

        return name[..keep].TrimEnd(' ', '.') + extension;
    }

    // The text after the last dot of the file name, in lower case; a value without a dot is taken as an extension.
    public static string? GetExtension(string? fileNameOrExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrExtension))
            return null;

        string value = fileNameOrExtension.Trim();
        int separator = value.LastIndexOfAny(['/', '\\']);
        string name = separator >= 0 ? value[(separator + 1)..] : value;
        int dot = name.LastIndexOf('.');
        string extension = dot >= 0 ? name[(dot + 1)..] : name;
        return extension.Length == 0 ? null : extension.ToLowerInvariant();
    }
}
