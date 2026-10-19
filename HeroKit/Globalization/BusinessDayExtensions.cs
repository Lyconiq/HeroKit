namespace HeroKit.Globalization;

/// <summary>
/// Public holiday and business day helpers based on the <see cref="IHolidayCalendar"/> of a <see cref="Country"/>.
/// </summary>
/// <remarks>
/// Business days are non-weekend days without a full-day holiday. Half-day holidays count as business days.
/// Methods throw <see cref="ArgumentOutOfRangeException"/> for years the calendar does not cover and
/// <see cref="NotSupportedException"/> when the country has no calendar.
/// </remarks>
public static class BusinessDayExtensions
{
    /// <summary>
    /// Returns the public holiday on a date, including half days.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <param name="country">The country.</param>
    /// <returns>The holiday, or null.</returns>
    /// <example>
    /// <code>
    /// new DateOnly(2026, 3, 20).GetPublicHoliday(Countries.Turkey)?.Name; // "Ramazan Bayramı 1. Gün"
    /// </code>
    /// </example>
    public static PublicHoliday? GetPublicHoliday(this DateOnly value, Country country)
    {
        foreach (PublicHoliday holiday in Calendar(country).GetHolidays(value.Year))
        {
            if (holiday.Date == value)
                return holiday;
        }

        return null;
    }

    /// <summary>
    /// Checks whether a date is a full-day public holiday.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <param name="country">The country.</param>
    /// <returns>True for full-day holidays; false for half days and ordinary days.</returns>
    public static bool IsPublicHoliday(this DateOnly value, Country country) => value.GetPublicHoliday(country) is { IsHalfDay: false };

    /// <summary>
    /// Checks whether a date is a full-day public holiday. The time part is ignored.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <param name="country">The country.</param>
    /// <returns>True for full-day holidays; otherwise false.</returns>
    public static bool IsPublicHoliday(this DateTime value, Country country) => DateOnly.FromDateTime(value).IsPublicHoliday(country);

    /// <summary>
    /// Checks whether a date is a half-day holiday, for example the eve (arife) of a Turkish religious holiday.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <param name="country">The country.</param>
    /// <returns>True for half-day holidays; otherwise false.</returns>
    public static bool IsHalfDayHoliday(this DateOnly value, Country country) => value.GetPublicHoliday(country) is { IsHalfDay: true };

    /// <summary>
    /// Checks whether a date is a business day.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <param name="country">The country.</param>
    /// <returns>True for business days; otherwise false.</returns>
    public static bool IsBusinessDay(this DateOnly value, Country country) =>
        !Calendar(country).IsWeekend(value.DayOfWeek) && !value.IsPublicHoliday(country);

    /// <summary>
    /// Checks whether a date is a business day. The time part is ignored.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <param name="country">The country.</param>
    /// <returns>True for business days; otherwise false.</returns>
    public static bool IsBusinessDay(this DateTime value, Country country) => DateOnly.FromDateTime(value).IsBusinessDay(country);

    /// <summary>
    /// Moves a date by a number of business days. Negative values move backward.
    /// </summary>
    /// <param name="value">The start date. It does not need to be a business day.</param>
    /// <param name="days">The number of business days to move.</param>
    /// <param name="country">The country.</param>
    /// <returns>The resulting business day, or <paramref name="value"/> when <paramref name="days"/> is zero.</returns>
    /// <example>
    /// <code>
    /// new DateOnly(2026, 3, 19).AddBusinessDays(1, Countries.Turkey); // 2026-03-23
    /// </code>
    /// </example>
    public static DateOnly AddBusinessDays(this DateOnly value, int days, Country country)
    {
        int step = Math.Sign(days);
        DateOnly current = value;

        for (int remaining = Math.Abs(days); remaining > 0;)
        {
            current = current.AddDays(step);

            if (current.IsBusinessDay(country))
                remaining--;
        }

        return current;
    }

    /// <summary>
    /// Moves a date by a number of business days. The time part and <see cref="DateTimeKind"/> are kept.
    /// </summary>
    /// <param name="value">The start date and time.</param>
    /// <param name="days">The number of business days to move.</param>
    /// <param name="country">The country.</param>
    /// <returns>The resulting date and time.</returns>
    public static DateTime AddBusinessDays(this DateTime value, int days, Country country) =>
        DateOnly.FromDateTime(value).AddBusinessDays(days, country).ToDateTime(TimeOnly.FromDateTime(value), value.Kind);

    /// <summary>
    /// Counts the business days after <paramref name="start"/> up to and including <paramref name="end"/>. A half-day holiday counts as a full day.
    /// </summary>
    /// <param name="start">The start date (not counted).</param>
    /// <param name="end">The end date (counted if it is a business day).</param>
    /// <param name="country">The country.</param>
    /// <returns>The number of business days; negative when <paramref name="end"/> is earlier.</returns>
    [Obsolete("Use CountBusinessDays, which counts a half-day holiday as 0.5. BusinessDaysUntil will be removed in 4.0.")]
    public static int BusinessDaysUntil(this DateOnly start, DateOnly end, Country country)
    {
        if (end < start)
            return -end.BusinessDaysUntil(start, country);

        int count = 0;

        for (DateOnly day = start.AddDays(1); day <= end; day = day.AddDays(1))
        {
            if (day.IsBusinessDay(country))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Counts the business days after <paramref name="start"/> up to and including <paramref name="end"/>.
    /// A half-day holiday on a weekday, for example the eve (arife) of a Turkish religious holiday, counts as 0.5.
    /// </summary>
    /// <param name="start">The start date (not counted).</param>
    /// <param name="end">The end date (counted if it is a business day).</param>
    /// <param name="country">The country.</param>
    /// <returns>The number of business days; negative when <paramref name="end"/> is earlier.</returns>
    /// <example>
    /// <code>
    /// // Tue 27, Wed 28 (eve of Republic Day, half day), Thu 29 (holiday), Fri 30 October 2026
    /// new DateOnly(2026, 10, 26).CountBusinessDays(new DateOnly(2026, 10, 30), Countries.Turkey); // 2.5
    /// </code>
    /// </example>
    public static decimal CountBusinessDays(this DateOnly start, DateOnly end, Country country)
    {
        if (end < start)
            return -end.CountBusinessDays(start, country);

        IHolidayCalendar calendar = Calendar(country);
        decimal count = 0;

        for (DateOnly day = start.AddDays(1); day <= end; day = day.AddDays(1))
        {
            if (calendar.IsWeekend(day.DayOfWeek))
                continue;

            count += day.GetPublicHoliday(country) switch
            {
                null => 1m,
                { IsHalfDay: true } => 0.5m,
                _ => 0m,
            };
        }

        return count;
    }

    /// <summary>
    /// Counts the business days between the dates of two <see cref="DateTime"/> values. See <see cref="CountBusinessDays(DateOnly, DateOnly, Country)"/>; the time parts are ignored.
    /// </summary>
    /// <param name="start">The start (its date is not counted).</param>
    /// <param name="end">The end (its date is counted if it is a business day).</param>
    /// <param name="country">The country.</param>
    /// <returns>The number of business days; negative when <paramref name="end"/> is earlier.</returns>
    public static decimal CountBusinessDays(this DateTime start, DateTime end, Country country) =>
        DateOnly.FromDateTime(start).CountBusinessDays(DateOnly.FromDateTime(end), country);

    /// <summary>
    /// Returns the public holiday on the date of a <see cref="DateTime"/>, including half days. The time part is ignored.
    /// </summary>
    /// <param name="value">The date and time.</param>
    /// <param name="country">The country.</param>
    /// <returns>The holiday, or null.</returns>
    public static PublicHoliday? GetPublicHoliday(this DateTime value, Country country) => DateOnly.FromDateTime(value).GetPublicHoliday(country);

    /// <summary>
    /// Checks whether the date of a <see cref="DateTime"/> is a half-day holiday. The time part is ignored.
    /// </summary>
    /// <param name="value">The date and time.</param>
    /// <param name="country">The country.</param>
    /// <returns>True for half-day holidays; otherwise false.</returns>
    public static bool IsHalfDayHoliday(this DateTime value, Country country) => DateOnly.FromDateTime(value).IsHalfDayHoliday(country);

    private static IHolidayCalendar Calendar(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);
        return country.Require(country.Holidays, "holiday calendar");
    }
}
