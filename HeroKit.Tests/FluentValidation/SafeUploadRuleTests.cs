using FluentValidation;
using FluentValidation.Results;
using HeroKit.FileTypes;
using HeroKit.FluentValidation;

namespace HeroKit.Tests.FluentValidation;

public class SafeUploadRuleTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

    public sealed record Upload(byte[] Content, string Name)
    {
        public int OpenCount { get; private set; }

        public Stream Open()
        {
            OpenCount++;
            return new MemoryStream(Content);
        }
    }

    public sealed class Form
    {
        public Upload? Photo { get; set; }
    }

    private sealed class FormValidator : AbstractValidator<Form>
    {
        public FormValidator(UploadPolicy? policy = null) =>
            RuleFor(x => x.Photo).SafeUpload(f => f.Open(), f => f.Name, policy);
    }

    [Fact]
    public void SafeUpload_ShouldAcceptASafeFileAndNull()
    {
        Assert.True(new FormValidator().Validate(new Form { Photo = new(Png, "photo.png") }).IsValid);
        Assert.True(new FormValidator().Validate(new Form()).IsValid);
    }

    [Theory]
    [InlineData("photo.png", "The content is a Windows executable.")]
    [InlineData("photo.php.png", "The file name hides the extension '.php'.")]
    public void SafeUpload_ShouldRejectUnsafeFilesWithTheUploadMessage(string name, string expected)
    {
        byte[] content = name == "photo.png" ? Exe : Png;
        ValidationResult result = new FormValidator().Validate(new Form { Photo = new(content, name) });

        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal("Photo", failure.PropertyName);
        Assert.Equal($"'Photo' is not accepted: {expected}", failure.ErrorMessage);
    }

    [Fact]
    public void SafeUpload_ShouldUseThePolicyAndDisposeTheStream()
    {
        var upload = new Upload(Png, "photo.png");
        var policy = new UploadPolicy { AllowedTypes = [HeroKit.FileTypes.FileTypes.Jpeg] };

        ValidationResult result = new FormValidator(policy).Validate(new Form { Photo = upload });

        Assert.Contains("PNG image files are not allowed.", Assert.Single(result.Errors).ErrorMessage);
        Assert.Equal(1, upload.OpenCount);
        Assert.False(new FormValidator(new UploadPolicy { MaxLength = 4 }).Validate(new Form { Photo = new(Png, "photo.png") }).IsValid);
    }

    [Fact]
    public void SafeUpload_ShouldAllowACustomMessage()
    {
        var validator = new InlineValidator<Form>();
        validator.RuleFor(x => x.Photo).SafeUpload(f => f.Open(), f => f.Name).WithMessage("Dosya kabul edilmedi: {UploadMessage}");

        ValidationResult result = validator.Validate(new Form { Photo = new(Exe, "photo.png") });

        Assert.Equal("Dosya kabul edilmedi: The content is a Windows executable.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public void SafeUpload_ShouldRejectNullDelegates()
    {
        var validator = new InlineValidator<Form>();
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(x => x.Photo).SafeUpload(null!, f => f.Name));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(x => x.Photo).SafeUpload(f => f.Open(), null!));
    }
}
