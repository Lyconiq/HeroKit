namespace HeroKit.Globalization.Turkey;

// Turkish IBANs: 26 characters, TR + 2 check digits + 5-digit bank code + 1 reserved digit that is always 0 + 16-character account number.
// Input is the normalized IBAN.
internal sealed class TurkeyIbanRules : IIdentifierRules
{
    public static TurkeyIbanRules Instance { get; } = new();

    public bool IsValid(string? value) =>
        value is { Length: 26 } && !value.AsSpan(4, 6).ContainsAnyExceptInRange('0', '9') && value[9] == '0';
}
