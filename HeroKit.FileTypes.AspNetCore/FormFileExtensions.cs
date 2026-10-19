using Microsoft.AspNetCore.Http;

namespace HeroKit.FileTypes.AspNetCore;

/// <summary>
/// Upload checks for ASP.NET Core's <see cref="IFormFile"/>.
/// </summary>
/// <remarks>
/// The checks read the uploaded stream, which ASP.NET Core buffers and can seek, so the ZIP central directory and the directory
/// of legacy Office files are read too. The stream is disposed after the check; the file can be opened again to save it.
/// </remarks>
public static class FormFileExtensions
{
    /// <summary>
    /// Checks an uploaded file with a policy, or with <see cref="UploadPolicy.Default"/>.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="policy">The policy; null uses <see cref="UploadPolicy.Default"/>.</param>
    /// <returns>The result with the reason of a rejection.</returns>
    /// <example>
    /// <code>
    /// UploadCheckResult result = file.CheckUpload(new UploadPolicy { AllowedTypes = [FileTypes.Pdf] });
    /// if (!result.IsSafe) return Results.BadRequest(result.Message);
    /// </code>
    /// </example>
    public static UploadCheckResult CheckUpload(this IFormFile file, UploadPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        using Stream stream = file.OpenReadStream();
        return policy is null ? FileTypes.CheckUpload(stream, file.FileName) : FileTypes.CheckUpload(stream, file.FileName, policy);
    }

    /// <summary>
    /// Checks an uploaded file with a validator, for example one registered as a singleton.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="file">The uploaded file.</param>
    /// <returns>The result with the reason of a rejection.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/documents", (IFormFile file, UploadValidator validator) => validator.Validate(file).IsSafe ? ... : ...);
    /// </code>
    /// </example>
    public static UploadCheckResult Validate(this UploadValidator validator, IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(file);
        using Stream stream = file.OpenReadStream();
        return validator.Validate(stream, file.FileName);
    }

    /// <summary>
    /// Returns the uploaded file name made safe to save or to send as a download name. See <see cref="FileTypes.ToSafeFileName"/>.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="fallback">The name returned when nothing is left. Defaults to "file".</param>
    /// <returns>The safe file name.</returns>
    public static string GetSafeFileName(this IFormFile file, string fallback = "file")
    {
        ArgumentNullException.ThrowIfNull(file);
        return FileTypes.ToSafeFileName(file.FileName, fallback);
    }
}
