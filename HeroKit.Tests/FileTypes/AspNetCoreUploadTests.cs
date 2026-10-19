using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using HeroKit.FileTypes;
using HeroKit.FileTypes.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using FT = HeroKit.FileTypes.FileTypes;

namespace HeroKit.Tests.FileTypes;

public class AspNetCoreUploadTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] Pdf = "%PDF-1.7\n%âãÏÓ\n"u8.ToArray();
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

    private static FormFile File(byte[] content, string fileName, string field = "file") =>
        new(new MemoryStream(content), 0, content.Length, field, fileName);

    [Fact]
    public void CheckUpload_ShouldCheckFormFiles()
    {
        Assert.True(File(Png, "photo.png").CheckUpload().IsSafe);
        Assert.Equal(UploadRejection.DangerousContent, File(Exe, "photo.png").CheckUpload().Rejection);
        Assert.Equal(UploadRejection.NotAllowed, File(Png, "photo.png").CheckUpload(new UploadPolicy { AllowedTypes = [FT.Pdf] }).Rejection);
        Assert.Equal(UploadRejection.TooLarge, new UploadValidator(new UploadPolicy { MaxLength = 4 }).Validate(File(Png, "photo.png")).Rejection);
    }

    [Fact]
    public void GetSafeFileName_ShouldCleanTheUploadedName()
    {
        Assert.Equal("rapor_.pdf", File(Pdf, "C:\\temp\\rapor?.pdf").GetSafeFileName());
        Assert.Equal("upload", File(Pdf, "...").GetSafeFileName("upload"));
    }

    public sealed class ProfileForm
    {
        [AllowedFileTypes("jpg", ".png", MaxLength = 1024)]
        public IFormFile? Photo { get; set; }

        [AllowedFileTypes("pdf")]
        public IFormFileCollection? Documents { get; set; }

        [AllowedFileTypes]
        public IReadOnlyList<IFormFile>? Anything { get; set; }
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void AllowedFileTypes_ShouldAcceptAllowedFilesAndNull()
    {
        var documents = new FormFileCollection { File(Pdf, "a.pdf", "Documents"), File(Pdf, "b.pdf", "Documents") };

        Assert.Empty(Validate(new ProfileForm()));
        Assert.Empty(Validate(new ProfileForm { Photo = File(Png, "photo.png"), Documents = documents, Anything = [File(Pdf, "x.pdf")] }));
    }

    [Fact]
    public void AllowedFileTypes_ShouldRejectWithFileNameAndReason()
    {
        var documents = new FormFileCollection { File(Pdf, "a.pdf"), File(Png, "b.png") };
        List<ValidationResult> results = Validate(new ProfileForm { Photo = File(Exe, "photo.png"), Documents = documents });

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.ErrorMessage == "photo.png: The content is a Windows executable." && r.MemberNames.Single() == "Photo");
        Assert.Contains(results, r => r.ErrorMessage == "b.png: PNG image files are not allowed." && r.MemberNames.Single() == "Documents");
    }

    [Fact]
    public void AllowedFileTypes_ShouldApplyMaxLengthAndACustomMessage()
    {
        var big = new byte[2048];
        Png.CopyTo(big, 0);
        Assert.Contains("larger than 1024 bytes", Validate(new ProfileForm { Photo = File(big, "photo.png") }).Single().ErrorMessage);

        var attribute = new AllowedFileTypesAttribute("png") { ErrorMessage = "{0} kabul edilmedi." };
        var context = new ValidationContext(new object()) { DisplayName = "Fotoğraf", MemberName = "Photo" };
        Assert.Equal("Fotoğraf kabul edilmedi.", attribute.GetValidationResult(File(Exe, "a.png"), context)?.ErrorMessage);
    }

    [Fact]
    public void AllowedFileTypes_ShouldReportMisuse()
    {
        var context = new ValidationContext(new object());
        Assert.Throws<InvalidOperationException>(() => new AllowedFileTypesAttribute("nosuchext").GetValidationResult(File(Png, "a.png"), context));
        Assert.Throws<InvalidOperationException>(() => new AllowedFileTypesAttribute("png").GetValidationResult("a string", context));
    }

    [Fact]
    public async Task ValidateUploads_ShouldStopRejectedFilesBeforeTheHandler()
    {
        await using WebApplication app = await StartAppAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage ok = await client.PostAsync("/one", Form(("file", Pdf, "invoice.pdf")));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("invoice.pdf", await ok.Content.ReadFromJsonAsync<string>());

        HttpResponseMessage bad = await client.PostAsync("/one", Form(("file", Exe, "invoice.pdf")));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var problem = await bad.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["invoice.pdf: The content is a Windows executable."], problem!.Errors["file"]);
    }

    [Fact]
    public async Task ValidateUploads_ShouldCheckEveryFileOfACollectionAndRouteGroups()
    {
        await using WebApplication app = await StartAppAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage bad = await client.PostAsync("/group/many", Form(("files", Pdf, "a.pdf"), ("files", Png, "b.png"), ("files", Exe, "c.pdf")));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var problem = await bad.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(2, problem!.Errors["files"].Length);

        HttpResponseMessage ok = await client.PostAsync("/group/many", Form(("files", Pdf, "a.pdf"), ("files", Pdf, "b.pdf")));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    private static async Task<WebApplication> StartAppAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();
        var pdfOnly = new UploadPolicy { AllowedTypes = [FT.Pdf] };

        app.MapPost("/one", (IFormFile file) => Results.Ok(file.GetSafeFileName())).DisableAntiforgery().ValidateUploads(pdfOnly);
        app.MapGroup("/group").ValidateUploads(pdfOnly).MapPost("/many", (IFormFileCollection files) => Results.Ok(files.Count)).DisableAntiforgery();

        await app.StartAsync();
        return app;
    }

    private static MultipartFormDataContent Form(params (string Field, byte[] Content, string FileName)[] files)
    {
        var form = new MultipartFormDataContent();
        foreach ((string field, byte[] content, string fileName) in files)
            form.Add(new ByteArrayContent(content), field, fileName);

        return form;
    }
}
