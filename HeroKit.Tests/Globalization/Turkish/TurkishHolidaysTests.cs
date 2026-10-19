// BusinessDaysUntil is obsolete since 3.7.0 and stays tested until it is removed in 4.0.
#pragma warning disable CS0618

using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

namespace HeroKit.Tests.Turkish;

public class TurkishHolidaysTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void GetHolidays_2026_ShouldMatchTheOfficialCalendar()
    {
        IReadOnlyList<PublicHoliday> holidays = Countries.Turkey.Holidays!.GetHolidays(2026);

        // 8 fixed entries (28 October is a half day) + Ramazan (arife + 3) + Kurban (arife + 4).
        Assert.Equal(17, holidays.Count);
        Assert.Equal(holidays.OrderBy(h => h.Date), holidays);
        Assert.Contains(new PublicHoliday(D(2026, 3, 19), "Ramazan Bayramı Arifesi", true), holidays);
        Assert.Contains(new PublicHoliday(D(2026, 3, 20), "Ramazan Bayramı 1. Gün", false), holidays);
        Assert.Contains(new PublicHoliday(D(2026, 5, 26), "Kurban Bayramı Arifesi", true), holidays);
        Assert.Contains(new PublicHoliday(D(2026, 5, 30), "Kurban Bayramı 4. Gün", false), holidays);
        Assert.Contains(new PublicHoliday(D(2026, 10, 28), "Cumhuriyet Bayramı Arifesi", true), holidays);
    }

    [Theory]
    [InlineData(2022, 5, 2, 7, 9)]
    [InlineData(2023, 4, 21, 6, 28)]
    [InlineData(2024, 4, 10, 6, 16)]
    [InlineData(2025, 3, 30, 6, 6)]
    [InlineData(2026, 3, 20, 5, 27)]
    [InlineData(2027, 3, 9, 5, 16)]
    public void ReligiousHolidays_ShouldStartOnDiyanetDates(int year, int rMonth, int rDay, int kMonth, int kDay)
    {
        Assert.Equal("Ramazan Bayramı 1. Gün", D(year, rMonth, rDay).GetPublicHoliday(Countries.Turkey)?.Name);
        Assert.Equal("Kurban Bayramı 1. Gün", D(year, kMonth, kDay).GetPublicHoliday(Countries.Turkey)?.Name);
    }

    [Fact]
    public void OverlappingHolidays_ShouldKeepTheFullDay()
    {
        // 2027: Kurban Bayramı 4th day is 19 May, which is also a fixed holiday.
        IReadOnlyList<PublicHoliday> holidays = Countries.Turkey.Holidays!.GetHolidays(2027);

        Assert.Single(holidays, h => h.Date == D(2027, 5, 19));
        Assert.False(holidays.Single(h => h.Date == D(2027, 5, 19)).IsHalfDay);
    }

    [Fact]
    public void UnsupportedYears_ShouldThrow()
    {
        Assert.Equal(2022, Countries.Turkey.Holidays!.FirstSupportedYear);
        Assert.Throws<ArgumentOutOfRangeException>(() => Countries.Turkey.Holidays!.GetHolidays(2021));
        Assert.Throws<ArgumentOutOfRangeException>(() => Countries.Turkey.Holidays!.GetHolidays(2040));
        Assert.Throws<ArgumentOutOfRangeException>(() => D(2040, 1, 2).IsBusinessDay(Countries.Turkey));
        Assert.Equal(2022, Countries.Turkey.Holidays!.FirstSupportedYear);
        Assert.True(Countries.Turkey.Holidays!.LastSupportedYear >= 2027);
    }

    [Theory]
    [InlineData(2026, 10, 29, true, false, false)]   // Cumhuriyet Bayramı
    [InlineData(2026, 10, 28, false, true, true)]    // half day, Wednesday
    [InlineData(2026, 3, 19, false, true, true)]     // Ramazan arifesi, Thursday
    [InlineData(2026, 3, 20, true, false, false)]    // Ramazan 1. gün, Friday
    [InlineData(2026, 10, 31, false, false, false)]  // Saturday
    [InlineData(2026, 10, 27, false, false, true)]   // ordinary Tuesday
    public void Classification_ShouldSeparateFullAndHalfDays(int y, int m, int d, bool holiday, bool halfDay, bool businessDay)
    {
        DateOnly date = D(y, m, d);

        Assert.Equal(holiday, date.IsPublicHoliday(Countries.Turkey));
        Assert.Equal(halfDay, date.IsHalfDayHoliday(Countries.Turkey));
        Assert.Equal(businessDay, date.IsBusinessDay(Countries.Turkey));
        Assert.Equal(businessDay, date.ToDateTime(new TimeOnly(15, 0)).IsBusinessDay(Countries.Turkey));
    }

    [Theory]
    [InlineData(2026, 3, 19, 1, 2026, 3, 23)]    // skips Ramazan Bayramı and the weekend
    [InlineData(2026, 5, 22, 3, 2026, 6, 1)]     // Friday before Kurban Bayramı: 25, 26 (arife, half day), then 1 June
    [InlineData(2026, 10, 30, -2, 2026, 10, 27)] // backward over 29 October: 28, 27
    [InlineData(2026, 10, 31, 0, 2026, 10, 31)]
    public void AddBusinessDaysTr_ShouldSkipWeekendsAndHolidays(int y, int m, int d, int days, int ey, int em, int ed)
    {
        Assert.Equal(D(ey, em, ed), D(y, m, d).AddBusinessDays(days, Countries.Turkey));
    }

    [Fact]
    public void AddBusinessDaysTr_DateTime_ShouldKeepTimeAndKind()
    {
        DateTime start = new(2026, 10, 28, 9, 30, 0, DateTimeKind.Utc);

        DateTime result = start.AddBusinessDays(1, Countries.Turkey);

        Assert.Equal(new DateTime(2026, 10, 30, 9, 30, 0, DateTimeKind.Utc), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void BusinessDaysUntilTr_ShouldBeTheInverseOfAddBusinessDays()
    {
        DateOnly start = D(2026, 10, 26);

        Assert.Equal(3, start.BusinessDaysUntil(D(2026, 10, 30), Countries.Turkey));
        Assert.Equal(-3, D(2026, 10, 30).BusinessDaysUntil(start, Countries.Turkey));
        Assert.Equal(0, start.BusinessDaysUntil(start, Countries.Turkey));

        for (int n = 1; n <= 40; n++)
            Assert.Equal(n, start.BusinessDaysUntil(start.AddBusinessDays(n, Countries.Turkey), Countries.Turkey));
    }

    [Fact]
    public void SupportedYears_ShouldCoverTheCurrentYear()
    {
        // Maintenance alarm: add the next year's Diyanet dates to TurkeyHolidayCalendar before this fails.
        Assert.True(DateTime.UtcNow.Year <= Countries.Turkey.Holidays!.LastSupportedYear, $"Add Diyanet religious holiday dates for {DateTime.UtcNow.Year}.");
    }
}
