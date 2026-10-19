namespace HeroKit.Globalization;

/// <summary>
/// A country and its rules. Predefined countries are in <see cref="Countries"/>.
/// </summary>
/// <remarks>
/// Every rule is optional. Methods that need a rule the country does not have throw <see cref="NotSupportedException"/>.
/// To support another country, implement the rule interfaces, create an instance and register it with <see cref="Countries.Register"/>.
/// </remarks>
/// <param name="code">The ISO 3166-1 alpha-2 code, for example "TR".</param>
/// <param name="name">The English name, for example "Türkiye".</param>
/// <param name="language">The primary language, used for region names.</param>
public sealed class Country(string code, string name, Language language)
{
    /// <summary>Gets the ISO 3166-1 alpha-2 code.</summary>
    public string Code { get; } = code;

    /// <summary>Gets the English name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the primary language.</summary>
    public Language Language { get; } = language;

    /// <summary>Gets the national currency, or null.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Gets the phone number rules, or null.</summary>
    public IPhoneNumberRules? PhoneNumbers { get; init; }

    /// <summary>Gets the vehicle plate rules, or null.</summary>
    public IVehiclePlateRules? VehiclePlates { get; init; }

    /// <summary>Gets the postal code rules, or null.</summary>
    public IIdentifierRules? PostalCodes { get; init; }

    /// <summary>Gets the national identity number rules, or null.</summary>
    public IIdentifierRules? NationalIds { get; init; }

    /// <summary>Gets the tax identifier rules, or null.</summary>
    public IIdentifierRules? TaxIds { get; init; }

    /// <summary>Gets the public holiday calendar, or null.</summary>
    public IHolidayCalendar? Holidays { get; init; }

    /// <summary>
    /// Gets the national IBAN rules (length and fixed positions), or null. IBAN validation applies them to IBANs with this country code.
    /// </summary>
    public IIdentifierRules? IbanRules { get; init; }

    /// <summary>Gets the banks and how to find them from an IBAN, or null.</summary>
    public IBankDirectory? Banks { get; init; }

    /// <summary>Gets the first-level administrative regions, for example the 81 Turkish provinces.</summary>
    public IReadOnlyList<Region> Regions { get; init; } = [];

    /// <summary>Gets the second-level administrative units, for example the 973 Turkish districts (ilçe).</summary>
    public IReadOnlyList<District> Districts { get; init; } = [];

    /// <summary>
    /// Gets the time zone of the country, or null when the country has more than one (for example the United States).
    /// </summary>
    public TimeZoneInfo? TimeZone { get; init; }

    /// <summary>
    /// Gets the current date and time in the country's <see cref="TimeZone"/>, also when the server runs in UTC.
    /// </summary>
    /// <param name="timeProvider">The clock, for tests; null uses <see cref="TimeProvider.System"/>.</param>
    /// <returns>The current local date and time with its UTC offset.</returns>
    /// <exception cref="NotSupportedException">The country has no single time zone.</exception>
    /// <example>
    /// <code>
    /// Countries.Turkey.Now(); // 2026-10-19 09:30:00 +03:00 while the server clock shows 06:30 UTC
    /// </code>
    /// </example>
    public DateTimeOffset Now(TimeProvider? timeProvider = null)
    {
        TimeZoneInfo zone = Require(TimeZone, "time zone");
        return TimeZoneInfo.ConvertTime((timeProvider ?? TimeProvider.System).GetUtcNow(), zone);
    }

    /// <summary>
    /// Gets today's date in the country's <see cref="TimeZone"/>. Between midnight in Türkiye and 03:00 UTC, a server in UTC still shows yesterday.
    /// </summary>
    /// <param name="timeProvider">The clock, for tests; null uses <see cref="TimeProvider.System"/>.</param>
    /// <returns>Today's date in the country.</returns>
    /// <exception cref="NotSupportedException">The country has no single time zone.</exception>
    /// <example>
    /// <code>
    /// Countries.Turkey.Today().IsBusinessDay(Countries.Turkey);
    /// </code>
    /// </example>
    public DateOnly Today(TimeProvider? timeProvider = null) => DateOnly.FromDateTime(Now(timeProvider).DateTime);

    /// <summary>
    /// Returns a copy of the country with another holiday calendar. The country itself does not change.
    /// </summary>
    /// <param name="holidays">The holiday calendar of the copy.</param>
    /// <returns>A copy with every other rule of this country.</returns>
    public Country WithHolidays(IHolidayCalendar holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        return CopyWith(holidays);
    }

    /// <summary>
    /// Returns a copy of the country whose holiday calendar also contains the given days, for example administrative leave
    /// ("idari izin") or company holidays. A given day replaces a holiday of the country on the same date. The country itself does not change.
    /// </summary>
    /// <param name="holidays">The days to add; set <see cref="PublicHoliday.IsHalfDay"/> for an afternoon off.</param>
    /// <returns>A copy with every other rule of this country.</returns>
    /// <exception cref="NotSupportedException">The country has no holiday calendar.</exception>
    /// <exception cref="ArgumentException">A date is given more than once, or a name is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A date is outside the supported years of the calendar.</exception>
    /// <example>
    /// <code>
    /// Country turkeyWithLeave = Countries.Turkey.WithAdditionalHolidays([new(new DateOnly(2026, 5, 18), "İdari izin", false)]);
    /// new DateOnly(2026, 5, 18).IsBusinessDay(turkeyWithLeave); // false: 18 May is added as administrative leave
    /// </code>
    /// </example>
    public Country WithAdditionalHolidays(IEnumerable<PublicHoliday> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        return CopyWith(new AdditionalHolidayCalendar(Require(Holidays, "holiday calendar"), holidays));
    }

    // Copies every rule; keep in sync with the properties above (a test compares them by reflection).
    private Country CopyWith(IHolidayCalendar holidays) => new(Code, Name, Language)
    {
        Currency = Currency,
        PhoneNumbers = PhoneNumbers,
        VehiclePlates = VehiclePlates,
        PostalCodes = PostalCodes,
        NationalIds = NationalIds,
        TaxIds = TaxIds,
        Holidays = holidays,
        IbanRules = IbanRules,
        Banks = Banks,
        Regions = Regions,
        Districts = Districts,
        TimeZone = TimeZone,
    };

    /// <summary>
    /// Returns the districts of a region.
    /// </summary>
    /// <param name="regionCode">The region code, for example 34.</param>
    /// <returns>The districts in the order of <see cref="Districts"/>; empty for an unknown region.</returns>
    /// <example>
    /// <code>
    /// Countries.Turkey.GetDistricts(34).Count; // 39
    /// </code>
    /// </example>
    public IReadOnlyList<District> GetDistricts(int regionCode) => [.. Districts.Where(d => d.RegionCode == regionCode)];

    /// <summary>
    /// Returns the districts of a region found by <see cref="FindRegion(string?)"/>.
    /// </summary>
    /// <param name="regionCodeOrName">The region code or name, for example "34" or "istanbul".</param>
    /// <returns>The districts; empty for an unknown region.</returns>
    public IReadOnlyList<District> GetDistricts(string? regionCodeOrName) =>
        FindRegion(regionCodeOrName) is { } region ? GetDistricts(region.Code) : [];

    /// <summary>
    /// Finds a district by its official code.
    /// </summary>
    /// <param name="code">The district code, for example 1757.</param>
    /// <returns>The district, or null.</returns>
    public District? FindDistrict(int code)
    {
        foreach (District district in Districts)
        {
            if (district.Code == code)
                return district;
        }

        return null;
    }

    /// <summary>
    /// Finds a district by its name within a region, ignoring case and diacritics.
    /// </summary>
    /// <param name="regionCodeOrName">The region code or name, for example "34" or "istanbul".</param>
    /// <param name="districtName">The district name, for example "kadikoy".</param>
    /// <returns>The district, or null if the region or the district is unknown.</returns>
    /// <example>
    /// <code>
    /// Countries.Turkey.FindDistrict("istanbul", "kadikoy")?.Name; // "Kadıköy"
    /// </code>
    /// </example>
    public District? FindDistrict(string? regionCodeOrName, string? districtName)
    {
        if (string.IsNullOrWhiteSpace(districtName) || FindRegion(regionCodeOrName) is not { } region)
            return null;

        string key = TextFolding.Fold(districtName.Trim(), Language.TextRules);
        foreach (District district in Districts)
        {
            if (district.RegionCode == region.Code && TextFolding.Fold(district.Name, Language.TextRules) == key)
                return district;
        }

        return null;
    }

    /// <summary>
    /// Finds a region by its code.
    /// </summary>
    /// <param name="code">The region code.</param>
    /// <returns>The region, or null.</returns>
    public Region? FindRegion(int code)
    {
        foreach (Region region in Regions)
        {
            if (region.Code == code)
                return region;
        }

        return null;
    }

    /// <summary>
    /// Finds a region by its code ("34", "06"), its abbreviation ("CA") or its name, ignoring case and diacritics ("istanbul", "Sanliurfa").
    /// </summary>
    /// <param name="codeOrName">The code or the name.</param>
    /// <returns>The region, or null.</returns>
    /// <example>
    /// <code>
    /// Countries.Turkey.FindRegion("34")?.Name;        // "İstanbul"
    /// Countries.Turkey.FindRegion("sanliurfa")?.Code; // 63
    /// Countries.UnitedStates.FindRegion("ca")?.Name;   // "California"
    /// </code>
    /// </example>
    public Region? FindRegion(string? codeOrName)
    {
        if (string.IsNullOrWhiteSpace(codeOrName))
            return null;

        string trimmed = codeOrName.Trim();

        if (int.TryParse(trimmed, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int code))
            return FindRegion(code);

        foreach (Region region in Regions)
        {
            if (region.Abbreviation is not null && string.Equals(region.Abbreviation, trimmed, StringComparison.OrdinalIgnoreCase))
                return region;
        }

        string key = TextFolding.Fold(trimmed, Language.TextRules);

        foreach (Region region in Regions)
        {
            if (TextFolding.Fold(region.Name, Language.TextRules) == key)
                return region;
        }

        return null;
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    internal T Require<T>(T? rules, string feature) where T : class =>
        rules ?? throw new NotSupportedException($"{Name} has no {feature} rules.");
}
