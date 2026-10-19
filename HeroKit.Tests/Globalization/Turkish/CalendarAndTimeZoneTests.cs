using System.Reflection;
using HeroKit.Globalization;

namespace HeroKit.Tests.Turkish;

public class CalendarAndTimeZoneTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void CountBusinessDays_ShouldCountHalfDayHolidaysAsHalf()
    {
        // Tue 27, Wed 28 (eve of Republic Day, half day), Thu 29 (holiday), Fri 30 October 2026.
        Assert.Equal(2.5m, D(2026, 10, 26).CountBusinessDays(D(2026, 10, 30), Countries.Turkey));
        Assert.Equal(-2.5m, D(2026, 10, 30).CountBusinessDays(D(2026, 10, 26), Countries.Turkey));
        Assert.Equal(0m, D(2026, 10, 26).CountBusinessDays(D(2026, 10, 26), Countries.Turkey));
    }

    [Fact]
    public void CountBusinessDays_ShouldMatchTheObsoleteCountWithoutHalfDays()
    {
        DateOnly start = D(2026, 1, 1);
        for (DateOnly end = start; end <= D(2026, 12, 31); end = end.AddDays(17))
        {
#pragma warning disable CS0618
            int whole = start.BusinessDaysUntil(end, Countries.Turkey);
#pragma warning restore CS0618
            decimal withHalves = start.CountBusinessDays(end, Countries.Turkey);
            int halfDays = CountHalfDayWeekdays(start, end);

            Assert.Equal(whole - (halfDays * 0.5m), withHalves);
        }
    }

    [Fact]
    public void DateTimeOverloads_ShouldIgnoreTheTime()
    {
        var eve = new DateTime(2026, 10, 28, 15, 30, 0);

        Assert.True(eve.IsHalfDayHoliday(Countries.Turkey));
        Assert.Equal("Cumhuriyet Bayramı Arifesi", eve.GetPublicHoliday(Countries.Turkey)?.Name);
        Assert.Null(new DateTime(2026, 10, 27, 9, 0, 0).GetPublicHoliday(Countries.Turkey));
        Assert.Equal(2.5m, new DateTime(2026, 10, 26, 23, 0, 0).CountBusinessDays(new DateTime(2026, 10, 30, 1, 0, 0), Countries.Turkey));
    }

    [Fact]
    public void WithAdditionalHolidays_ShouldAddDaysWithoutChangingTheCountry()
    {
        var bridge = D(2026, 5, 18); // Monday before 19 May
        Country turkey = Countries.Turkey.WithAdditionalHolidays([new(bridge, "İdari izin", false)]);

        Assert.False(bridge.IsBusinessDay(turkey));
        Assert.True(bridge.IsBusinessDay(Countries.Turkey));
        Assert.Equal("İdari izin", bridge.GetPublicHoliday(turkey)?.Name);
        Assert.Equal(Countries.Turkey.Holidays!.GetHolidays(2026).Count + 1, turkey.Holidays!.GetHolidays(2026).Count);
        Assert.Equal(D(2026, 5, 20), D(2026, 5, 15).AddBusinessDays(1, turkey));
    }

    [Fact]
    public void WithAdditionalHolidays_ShouldReplaceAHolidayOnTheSameDate()
    {
        var eve = D(2026, 10, 28);
        Country turkey = Countries.Turkey.WithAdditionalHolidays([new(eve, "Şirket tatili", false)]);

        Assert.True(eve.IsPublicHoliday(turkey));
        Assert.False(eve.IsHalfDayHoliday(turkey));
        Assert.Equal(Countries.Turkey.Holidays!.GetHolidays(2026).Count, turkey.Holidays!.GetHolidays(2026).Count);
        Assert.Equal(2m, D(2026, 10, 26).CountBusinessDays(D(2026, 10, 30), turkey));
    }

    [Fact]
    public void WithAdditionalHolidays_ShouldAcceptHalfDaysAndKeepTheOrder()
    {
        Country turkey = Countries.Turkey.WithAdditionalHolidays([new(D(2026, 12, 31), "Yılbaşı arifesi", true)]);
        IReadOnlyList<PublicHoliday> holidays = turkey.Holidays!.GetHolidays(2026);

        Assert.True(D(2026, 12, 31).IsHalfDayHoliday(turkey));
        Assert.Equal(holidays.OrderBy(h => h.Date), holidays);
    }

    [Fact]
    public void WithAdditionalHolidays_ShouldRejectBadInput()
    {
        var day = D(2026, 5, 18);

        Assert.Throws<ArgumentException>(() => Countries.Turkey.WithAdditionalHolidays([new(day, "a", false), new(day, "b", false)]));
        Assert.Throws<ArgumentException>(() => Countries.Turkey.WithAdditionalHolidays([new(day, " ", false)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Countries.Turkey.WithAdditionalHolidays([new(D(1990, 1, 2), "a", false)]));
        Assert.Throws<ArgumentNullException>(() => Countries.Turkey.WithAdditionalHolidays(null!));
        Assert.Throws<ArgumentNullException>(() => Countries.Turkey.WithHolidays(null!));
    }

    [Fact]
    public void WithHolidays_ShouldUseTheGivenCalendar()
    {
        Country turkey = Countries.Turkey.WithHolidays(new FridayWeekendCalendar());

        Assert.False(D(2026, 10, 30).IsBusinessDay(turkey));  // Friday
        Assert.True(D(2026, 10, 29).IsBusinessDay(turkey));   // no holidays in this calendar
        Assert.True(D(2026, 10, 30).IsBusinessDay(Countries.Turkey));
    }

    [Fact]
    public void Copies_ShouldKeepEveryOtherRule()
    {
        Country copy = Countries.Turkey.WithAdditionalHolidays([]);

        foreach (PropertyInfo property in typeof(Country).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.Name == nameof(Country.Holidays))
                continue;

            Assert.True(Equals(property.GetValue(Countries.Turkey), property.GetValue(copy)), $"{property.Name} was not copied.");
        }
    }

    [Fact]
    public void Today_ShouldUseTheTimeZoneOfTheCountry()
    {
        // 22:30 UTC on 18 October is 01:30 on 19 October in Türkiye (UTC+3).
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 18, 22, 30, 0, TimeSpan.Zero));

        Assert.Equal(D(2026, 10, 19), Countries.Turkey.Today(clock));
        Assert.Equal(TimeSpan.FromHours(3), Countries.Turkey.Now(clock).Offset);
        Assert.Equal(1, Countries.Turkey.Now(clock).Hour);
        Assert.NotNull(Countries.Turkey.TimeZone);
    }

    [Fact]
    public void Today_ShouldRequireASingleTimeZone()
    {
        Assert.Null(Countries.UnitedStates.TimeZone);
        Assert.Throws<NotSupportedException>(() => Countries.UnitedStates.Today());
    }

    private static int CountHalfDayWeekdays(DateOnly start, DateOnly end)
    {
        int count = 0;
        for (DateOnly day = start.AddDays(1); day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && day.IsHalfDayHoliday(Countries.Turkey))
                count++;
        }

        return count;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FridayWeekendCalendar : IHolidayCalendar
    {
        public int FirstSupportedYear => 2000;

        public int LastSupportedYear => 2100;

        public bool IsWeekend(DayOfWeek day) => day is DayOfWeek.Friday or DayOfWeek.Saturday;

        public IReadOnlyList<PublicHoliday> GetHolidays(int year) => [];
    }
}
