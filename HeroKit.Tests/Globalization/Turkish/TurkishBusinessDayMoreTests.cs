// BusinessDaysUntil is obsolete since 3.7.0 and stays tested until it is removed in 4.0.
#pragma warning disable CS0618

using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

namespace HeroKit.Tests.Turkish;

public class TurkishBusinessDayMoreTests
{
    [Fact]
    public void IsPublicHolidayTr_DateTime_ShouldIgnoreTimePart()
    {
        Assert.True(new DateTime(2026, 10, 29, 15, 30, 0).IsPublicHoliday(Countries.Turkey));
    }

    [Fact]
    public void IsPublicHolidayTr_DateTime_ShouldReturnFalse_ForOrdinaryDay()
    {
        Assert.False(new DateTime(2026, 10, 27, 9, 0, 0).IsPublicHoliday(Countries.Turkey));
    }

    [Fact]
    public void IsHalfDayHolidayTr_ShouldReturnTrue_ForRepublicDayEve()
    {
        Assert.True(new DateOnly(2026, 10, 28).IsHalfDayHoliday(Countries.Turkey));
    }

    [Fact]
    public void IsHalfDayHolidayTr_ShouldReturnFalse_ForFullHoliday()
    {
        Assert.False(new DateOnly(2026, 10, 29).IsHalfDayHoliday(Countries.Turkey));
    }

    [Fact]
    public void GetPublicHolidayTr_ShouldReturnHoliday_OnHolidayDate()
    {
        Assert.NotNull(new DateOnly(2026, 10, 29).GetPublicHoliday(Countries.Turkey));
    }

    [Fact]
    public void GetPublicHolidayTr_ShouldReturnNull_OnOrdinaryDay()
    {
        Assert.Null(new DateOnly(2026, 10, 27).GetPublicHoliday(Countries.Turkey));
    }

    [Fact]
    public void IsBusinessDayTr_DateTime_ShouldReturnFalse_OnWeekend()
    {
        Assert.False(new DateTime(2026, 10, 31, 12, 0, 0).IsBusinessDay(Countries.Turkey));
    }

    [Fact]
    public void IsBusinessDayTr_DateTime_ShouldReturnTrue_OnWeekday()
    {
        Assert.True(new DateTime(2026, 10, 27, 12, 0, 0).IsBusinessDay(Countries.Turkey));
    }

    [Fact]
    public void AddBusinessDaysTr_ShouldSkipWeekendAndHoliday_WhenMovingForward()
    {
        DateOnly result = new DateOnly(2026, 10, 28).AddBusinessDays(1, Countries.Turkey);

        Assert.Equal(new DateOnly(2026, 10, 30), result);
    }

    [Fact]
    public void AddBusinessDaysTr_ShouldMoveBackward_WithNegativeDays()
    {
        DateOnly result = new DateOnly(2026, 10, 30).AddBusinessDays(-1, Countries.Turkey);

        Assert.Equal(new DateOnly(2026, 10, 28), result);
    }

    [Fact]
    public void AddBusinessDaysTr_ShouldReturnSameDate_ForZero()
    {
        Assert.Equal(new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 31).AddBusinessDays(0, Countries.Turkey));
    }

    [Fact]
    public void AddBusinessDaysTr_DateTime_ShouldKeepTimeAndKind()
    {
        var start = new DateTime(2026, 10, 27, 14, 45, 10, DateTimeKind.Utc);

        DateTime result = start.AddBusinessDays(1, Countries.Turkey);

        Assert.Equal(new DateTime(2026, 10, 28, 14, 45, 10, DateTimeKind.Utc), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void BusinessDaysUntilTr_ShouldCountForward()
    {
        Assert.Equal(1, new DateOnly(2026, 10, 27).BusinessDaysUntil(new DateOnly(2026, 10, 28), Countries.Turkey));
    }

    [Fact]
    public void BusinessDaysUntilTr_ShouldNegate_WhenEndIsBeforeStart()
    {
        Assert.Equal(-1, new DateOnly(2026, 10, 28).BusinessDaysUntil(new DateOnly(2026, 10, 27), Countries.Turkey));
    }

    [Fact]
    public void BusinessDaysUntilTr_ShouldReturnZero_ForSameDay()
    {
        Assert.Equal(0, new DateOnly(2026, 10, 27).BusinessDaysUntil(new DateOnly(2026, 10, 27), Countries.Turkey));
    }
}
