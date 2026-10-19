using System.Collections.Concurrent;

namespace HeroKit.Globalization;

// Decorator: adds days to another holiday calendar, for example administrative leave ("idari izin") or company holidays.
// An added day replaces a holiday of the inner calendar on the same date. Weekends and the supported years come from the inner calendar.
internal sealed class AdditionalHolidayCalendar : IHolidayCalendar
{
    private readonly IHolidayCalendar _inner;
    private readonly Dictionary<int, PublicHoliday[]> _added;
    private readonly ConcurrentDictionary<int, IReadOnlyList<PublicHoliday>> _cache = new();

    public AdditionalHolidayCalendar(IHolidayCalendar inner, IEnumerable<PublicHoliday> holidays)
    {
        _inner = inner;
        PublicHoliday[] added = [.. holidays];

        var dates = new HashSet<DateOnly>();
        foreach (PublicHoliday holiday in added)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(holiday.Name, nameof(holidays));
            if (!dates.Add(holiday.Date))
                throw new ArgumentException($"The date {holiday.Date:yyyy-MM-dd} is added more than once.", nameof(holidays));

            if (holiday.Date.Year < inner.FirstSupportedYear || holiday.Date.Year > inner.LastSupportedYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(holidays), holiday.Date, $"The calendar supports {inner.FirstSupportedYear}-{inner.LastSupportedYear}.");
            }
        }

        _added = added.GroupBy(h => h.Date.Year).ToDictionary(g => g.Key, g => g.ToArray());
    }

    public int FirstSupportedYear => _inner.FirstSupportedYear;

    public int LastSupportedYear => _inner.LastSupportedYear;

    public bool IsWeekend(DayOfWeek day) => _inner.IsWeekend(day);

    public IReadOnlyList<PublicHoliday> GetHolidays(int year)
    {
        IReadOnlyList<PublicHoliday> holidays = _inner.GetHolidays(year);
        if (!_added.TryGetValue(year, out PublicHoliday[]? added))
            return holidays;

        return _cache.GetOrAdd(year, _ =>
        {
            var byDate = holidays.ToDictionary(h => h.Date);
            foreach (PublicHoliday holiday in added)
                byDate[holiday.Date] = holiday;

            return [.. byDate.Values.OrderBy(h => h.Date)];
        });
    }
}
