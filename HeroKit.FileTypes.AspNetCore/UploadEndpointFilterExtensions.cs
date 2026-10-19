using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HeroKit.FileTypes.AspNetCore;

/// <summary>
/// An endpoint filter for minimal APIs that checks every uploaded file before the handler runs.
/// </summary>
public static class UploadEndpointFilterExtensions
{
    /// <summary>
    /// Checks every <see cref="IFormFile"/> and <see cref="IFormFileCollection"/> argument of the endpoint with <see cref="UploadValidator"/>.
    /// When a file is rejected, the handler does not run and the response is 400 with a validation problem
    /// (<c>application/problem+json</c>) that lists each rejected file under its form field name.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint or route group builder.</typeparam>
    /// <param name="builder">The endpoint or route group.</param>
    /// <param name="policy">The policy; null uses <see cref="UploadPolicy.Default"/>.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/documents", (IFormFile file) => Results.Ok(file.GetSafeFileName()))
    ///    .ValidateUploads(new UploadPolicy { AllowedTypes = [FileTypes.Pdf], MaxLength = 10 * 1024 * 1024 });
    /// </code>
    /// </example>
    public static TBuilder ValidateUploads<TBuilder>(this TBuilder builder, UploadPolicy? policy = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        var validator = new UploadValidator(policy ?? UploadPolicy.Default);

        return builder.AddEndpointFilter(async (context, next) =>
        {
            Dictionary<string, string[]>? errors = null;
            foreach (object? argument in context.Arguments)
            {
                IEnumerable<IFormFile> files = argument switch
                {
                    IFormFile file => [file],
                    IFormFileCollection collection => collection,
                    _ => [],
                };

                foreach (IFormFile file in files)
                {
                    UploadCheckResult result = validator.Validate(file);
                    if (!result.IsSafe)
                        Add(errors ??= [], file.Name, $"{file.FileName}: {result.Message}");
                }
            }

            return errors is null ? await next(context).ConfigureAwait(false) : Results.ValidationProblem(errors);
        });
    }

    private static void Add(Dictionary<string, string[]> errors, string field, string message) =>
        errors[field] = errors.TryGetValue(field, out string[]? existing) ? [.. existing, message] : [message];
}
