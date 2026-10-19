using System.Buffers.Binary;
using System.Text;
using HeroKit.FileTypes;
using static HeroKit.Tests.FileTypes.FileTypesTests;
using FT = HeroKit.FileTypes.FileTypes;

namespace HeroKit.Tests.FileTypes;

public class LegacyOfficeAndLimitsTests
{
    private const int SectorSize = 512;
    private const uint EndOfChain = 0xFFFFFFFE;

    [Fact]
    public void CheckUpload_ShouldAcceptLegacyDocumentsWithoutMacros()
    {
        byte[] doc = CompoundFile(["WordDocument", "1Table"]);

        Assert.True(FT.CheckUpload(doc, "report.doc").IsSafe);
        Assert.True(FT.CheckUpload(new MemoryStream(doc), "report.doc").IsSafe);
    }

    [Theory]
    [InlineData("Macros", "report.doc")]
    [InlineData("_VBA_PROJECT_CUR", "budget.xls")]
    [InlineData("macros", "report.dot")]
    public void CheckUpload_ShouldRejectLegacyDocumentsWithAVbaProject(string storage, string fileName)
    {
        byte[] file = CompoundFile(["WordDocument", "1Table"], storage);

        Assert.Equal(UploadRejection.ActiveContent, FT.CheckUpload(file, fileName).Rejection);
        Assert.Equal(UploadRejection.ActiveContent, FT.CheckUpload(new MemoryStream(file), fileName).Rejection);
        Assert.True(FT.CheckUpload(file, fileName, new UploadPolicy { AllowMacros = true }).IsSafe);
    }

    [Fact]
    public void CheckUpload_ShouldFollowTheDirectoryChainBeyondTheHeader()
    {
        // 50 KB of other sectors first, then a directory of three sectors with the VBA project in the last one.
        string[] streams = Enumerable.Range(0, 10).Select(i => $"Stream{i}").ToArray();
        byte[] xls = CompoundFile(streams, "_VBA_PROJECT_CUR", fillerSectors: 100);

        Assert.True(xls.Length > FT.HeaderSize);
        Assert.Equal(UploadRejection.ActiveContent, FT.CheckUpload(new MemoryStream(xls), "budget.xls").Rejection);
        Assert.Equal(UploadRejection.ActiveContent, FT.CheckUpload(xls, "budget.xls").Rejection);
    }

    [Fact]
    public void CheckUpload_ShouldStopOnADamagedDirectoryChain()
    {
        byte[] doc = CompoundFile(["WordDocument"]);
        // The directory sector points to itself in the allocation table.
        BinaryPrimitives.WriteUInt32LittleEndian(doc.AsSpan(SectorSize + 4), 1);

        Assert.True(FT.CheckUpload(doc, "report.doc").IsSafe);
        Assert.True(FT.CheckUpload(doc[..600], "report.doc").IsSafe);
    }

    [Fact]
    public void CheckUpload_ShouldRejectFilesLongerThanMaxLength()
    {
        byte[] png = Bytes("89 50 4E 47 0D 0A 1A 0A 00 00 00 0D 49 48 44 52");
        var policy = new UploadPolicy { MaxLength = 10 };

        Assert.Equal(UploadRejection.TooLarge, FT.CheckUpload(png, "photo.png", policy).Rejection);
        Assert.Equal(UploadRejection.TooLarge, FT.CheckUpload(new MemoryStream(png), "photo.png", policy).Rejection);
        Assert.True(FT.CheckUpload(png, "photo.png", new UploadPolicy { MaxLength = png.Length }).IsSafe);
        Assert.True(FT.CheckUpload(new NonSeekableStream(new MemoryStream(png)), "photo.png", policy).IsSafe);
        Assert.True(FT.CheckUpload(png, "photo.png").IsSafe);
    }

    [Theory]
    [InlineData("photo.png", "photo.png")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\temp\\rapor?.pdf", "rapor_.pdf")]
    [InlineData("a<b>c:d*e|f\".txt", "a_b_c_d_e_f_.txt")]
    [InlineData("  Fatura \t Ekim .pdf ", "Fatura Ekim .pdf")]
    [InlineData("tab\u0001name.txt", "tab_name.txt")]
    [InlineData("...hidden...", "hidden")]
    [InlineData("con", "_con")]
    [InlineData("NUL.txt", "_NUL.txt")]
    [InlineData("console.txt", "console.txt")]
    [InlineData("Fotoğraf Şubat.JPG", "Fotoğraf Şubat.JPG")]
    [InlineData("", "file")]
    [InlineData(null, "file")]
    [InlineData("/", "file")]
    [InlineData(" . ", "file")]
    public void ToSafeFileName_ShouldRemovePathsAndUnsafeCharacters(string? input, string expected) =>
        Assert.Equal(expected, FT.ToSafeFileName(input));

    [Fact]
    public void ToSafeFileName_ShouldShortenLongNamesAndKeepTheExtension()
    {
        string safe = FT.ToSafeFileName(new string('a', 300) + ".pdf");

        Assert.Equal(255, safe.Length);
        Assert.EndsWith("a.pdf", safe);
        Assert.Equal("___", FT.ToSafeFileName("???"));
        Assert.Equal("upload", FT.ToSafeFileName("", "upload"));
        Assert.Throws<ArgumentException>(() => FT.ToSafeFileName("a.txt", " "));
    }

    [Fact]
    public void ToSafeFileName_ShouldNotSplitSurrogatePairs()
    {
        string safe = FT.ToSafeFileName(new string('a', 250) + "😀😀😀😀.png");

        Assert.True(safe.Length <= 255);
        Assert.False(char.IsHighSurrogate(safe[^5]));
        Assert.EndsWith(".png", safe);
    }

    // Builds a version 3 compound file: a header, one allocation table sector, filler sectors, then the directory chain.
    // The directory holds the root entry, a stream per name and, when given, a storage named macroStorage at the end.
    private static byte[] CompoundFile(string[] streams, string? macroStorage = null, int fillerSectors = 0)
    {
        var entries = new List<(string Name, byte Type)> { ("Root Entry", 5) };
        entries.AddRange(streams.Select(name => (name, (byte)2)));
        if (macroStorage is not null)
            entries.Add((macroStorage, 1));

        int directorySectors = (entries.Count + 3) / 4;
        int firstDirectory = 1 + fillerSectors;
        int sectorCount = firstDirectory + directorySectors;
        byte[] file = new byte[SectorSize * (sectorCount + 1)];
        Span<byte> header = file.AsSpan(0, SectorSize);

        Bytes("D0 CF 11 E0 A1 B1 1A E1").CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x18..], 0x3E);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x1A..], 3);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x1C..], 0xFFFE);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x1E..], 9);
        BinaryPrimitives.WriteUInt16LittleEndian(header[0x20..], 6);
        BinaryPrimitives.WriteUInt32LittleEndian(header[0x2C..], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header[0x30..], (uint)firstDirectory);
        BinaryPrimitives.WriteUInt32LittleEndian(header[0x38..], 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(header[0x3C..], EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(header[0x44..], EndOfChain);
        for (int i = 0; i < 109; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(header[(0x4C + i * 4)..], i == 0 ? 0u : 0xFFFFFFFF);

        Span<byte> table = Sector(file, 0);
        table.Fill(0xFF);
        BinaryPrimitives.WriteUInt32LittleEndian(table, 0xFFFFFFFD);
        for (int i = 1; i <= fillerSectors; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(table[(i * 4)..], EndOfChain);

        for (int i = 0; i < directorySectors; i++)
        {
            uint next = i == directorySectors - 1 ? EndOfChain : (uint)(firstDirectory + i + 1);
            BinaryPrimitives.WriteUInt32LittleEndian(table[((firstDirectory + i) * 4)..], next);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Span<byte> entry = Sector(file, firstDirectory + i / 4).Slice(i % 4 * 128, 128);
            byte[] name = Encoding.Unicode.GetBytes(entries[i].Name + "\0");
            name.CopyTo(entry);
            BinaryPrimitives.WriteUInt16LittleEndian(entry[0x40..], (ushort)name.Length);
            entry[0x42] = entries[i].Type;
        }

        return file;
    }

    private static Span<byte> Sector(byte[] file, int index) => file.AsSpan((index + 1) * SectorSize, SectorSize);
}
