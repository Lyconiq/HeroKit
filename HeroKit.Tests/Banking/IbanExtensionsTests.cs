using HeroKit.Banking;

namespace HeroKit.Tests.Banking;

public class IbanExtensionsTests
{
    [Theory]
    [InlineData("TR330006100519786457841326")]
    [InlineData("TR33 0006 1005 1978 6457 8413 26")]
    [InlineData("tr33 0006 1005 1978 6457 8413 26")]
    [InlineData("tr330006100519786457841326")]
    [InlineData("DE89370400440532013000")]
    [InlineData("GB82WEST12345698765432")]
    public void IsValidIban_ValidInput_ReturnsTrue(string value) => Assert.True(value.IsValidIban());

    [Theory]
    [InlineData("TR330006100519786457841327")] // wrong checksum
    [InlineData("DE89370400440532013001")] // wrong checksum
    [InlineData("TR3300061005197864578413")] // TR too short
    [InlineData("TR33000610051978645784132600")] // TR too long
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("33TR0006100519786457841326")] // letters and digits swapped
    [InlineData("TRAB0006100519786457841326")] // letters in check digits
    [InlineData("DE8937040044053201300")] // length 21 with broken checksum
    [InlineData("DE89370400440")] // 13 characters
    [InlineData("DE893704004405")] // 14 characters
    [InlineData("DE89370400440532013000DE89370400440532013")] // longer than 34
    [InlineData("GB82WEST1234569876543!")] // invalid character
    public void IsValidIban_InvalidInput_ReturnsFalse(string? value) => Assert.False(value.IsValidIban());

    [Fact]
    public void IsValidIban_Length35_ReturnsFalse() =>
        Assert.False(new string('1', 35).Insert(0, "DE00").Substring(0, 35).IsValidIban());

    [Theory]
    [InlineData("0006110519786457841326", false)] // reserved digit (10th character) is 1
    [InlineData("0006190519786457841326", false)] // reserved digit is 9
    [InlineData("0006A00519786457841326", false)] // letter in the bank code
    [InlineData("1006100519786457841326", true)]  // bank code that starts with 1, reserved digit 0
    [InlineData("0006100519786457841326", true)]
    public void IsValidIban_TurkishBankCodeAndReservedDigit(string bban, bool expected)
    {
        // Recompute the check digits so that only the Turkish rule decides.
        var check = 98 - Mod97("TR00" + bban);
        var iban = $"TR{check:00}{bban}";
        Assert.Equal(26, iban.Length);
        Assert.Equal(expected, iban.IsValidIban());
    }

    [Theory]
    [InlineData("TR330006100519786457841326", "TR33 0006 1005 1978 6457 8413 26")]
    [InlineData("tr33 0006 1005 1978 6457 8413 26", "TR33 0006 1005 1978 6457 8413 26")]
    [InlineData("DE89370400440532013000", "DE89 3704 0044 0532 0130 00")]
    [InlineData("GB82WEST12345698765432", "GB82 WEST 1234 5698 7654 32")]
    [InlineData("TR330006100519786457841327", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void FormatIban_ReturnsExpected(string? value, string? expected) => Assert.Equal(expected, value.FormatIban());

    [Theory]
    [InlineData("TR330006100519786457841326", "TR330006100519786457841326")]
    [InlineData("tr33 0006 1005 1978 6457 8413 26", "TR330006100519786457841326")]
    [InlineData("gb82 west 1234 5698 7654 32", "GB82WEST12345698765432")]
    [InlineData("TR330006100519786457841327", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void NormalizeIban_ReturnsExpected(string? value, string? expected) => Assert.Equal(expected, value.NormalizeIban());

    private static int Mod97(string iban)
    {
        var rotated = iban[4..] + iban[..4];
        var remainder = 0;
        foreach (var c in rotated)
        {
            remainder = char.IsDigit(c)
                ? ((remainder * 10) + (c - '0')) % 97
                : ((remainder * 100) + (c - 'A' + 10)) % 97;
        }

        return remainder;
    }
}
