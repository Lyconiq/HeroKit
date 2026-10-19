using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HeroKit.FileTypes.AspNetCore;

/// <summary>
/// Validates an <see cref="IFormFile"/>, an <see cref="IFormFileCollection"/> or an <see cref="IEnumerable{T}"/> of <see cref="IFormFile"/>
/// with <see cref="UploadValidator"/>, allowing only the types of the given extensions. Works with MVC, Razor Pages, Blazor forms
/// and <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/>.
/// </summary>
/// <remarks>
/// <para>Every check of <see cref="UploadValidator"/> runs: file name, program or script content, content that fits the extension,
/// macros and active SVG content. Without extensions, any type that is not a program or script is allowed.</para>
/// <para>Null passes; add <see cref="RequiredAttribute"/> to require a file. The message names the file and the reason, for example
/// "photo.png: The content is a Windows executable."; set <see cref="ValidationAttribute.ErrorMessage"/> to replace it.</para>
/// <para>Types are looked up in <see cref="FileTypes.Default"/>; types added with <see cref="FileTypes.Register"/> can be used.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class ProfileForm
/// {
///     [Required, AllowedFileTypes("jpg", "png", "webp", MaxLength = 5 * 1024 * 1024)]
///     public IFormFile? Photo { get; set; }
///
///     [AllowedFileTypes("pdf")]
///     public IFormFileCollection? Documents { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AllowedFileTypesAttribute : ValidationAttribute
{
    private readonly string[] _extensions;
    private UploadValidator? _validator;

    /// <summary>
    /// Creates the attribute.
    /// </summary>
    /// <param name="extensions">The extensions of the allowed types, with or without a dot, for example "jpg" and ".png". None allows any safe type.</param>
    public AllowedFileTypesAttribute(params string[] extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);
        _extensions = extensions;
    }

    /// <summary>Gets the extensions of the allowed types.</summary>
    public IReadOnlyList<string> Extensions => _extensions;

    /// <summary>Gets or sets the largest allowed file in bytes; 0 means no limit. Defaults to 0.</summary>
    public long MaxLength { get; set; }

    /// <summary>Gets or sets whether documents with macros are accepted. Defaults to false.</summary>
    public bool AllowMacros { get; set; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">An extension is unknown, or the value is not a form file or a collection of form files.</exception>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        IEnumerable<IFormFile?> files = value switch
        {
            null => [],
            IFormFile file => [file],
            IEnumerable<IFormFile?> many => many,
            _ => throw new InvalidOperationException($"[AllowedFileTypes] needs an IFormFile or a collection of IFormFile, not {value.GetType().Name}."),
        };

        UploadValidator validator = _validator ??= CreateValidator();
        foreach (IFormFile? file in files)
        {
            if (file is null)
                continue;

            UploadCheckResult result = validator.Validate(file);
            if (!result.IsSafe)
            {
                string message = ErrorMessage is null ? $"{file.FileName}: {result.Message}" : FormatErrorMessage(validationContext.DisplayName);
                return new ValidationResult(message, validationContext.MemberName is { } member ? [member] : null);
            }
        }

        return ValidationResult.Success;
    }

    private UploadValidator CreateValidator()
    {
        FileType[]? allowed = _extensions.Length == 0
            ? null
            : [.. _extensions.Select(e => FileTypes.FromExtension(e) ?? throw new InvalidOperationException($"[AllowedFileTypes] does not know the extension '{e}'."))];

        return new UploadValidator(new UploadPolicy
        {
            AllowedTypes = allowed,
            AllowMacros = AllowMacros,
            MaxLength = MaxLength > 0 ? MaxLength : null,
        });
    }
}
