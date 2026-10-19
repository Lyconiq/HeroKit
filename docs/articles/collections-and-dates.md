# Collections and dates

## Collections

```csharp
using HeroKit.Enumerables;

items.IsNullOrEmpty();                               // null-safe; reads at most one element of a lazy sequence
products.WhereIf(onlyInStock, p => p.Stock > 0);     // filter only when the condition is true
query.OrderBy(x => x.Id).Page(pageNumber: 2, pageSize: 20); // 1-based paging
source.ToReadOnlyList();                             // read-only copy
```

`WhereIf` and `Page` also work on `IQueryable<T>`, so Entity Framework translates them to SQL.

## Dates

```csharp
using HeroKit.DateTimes;

var date = new DateTime(2024, 2, 15, 13, 45, 0);

date.StartOfMonth();     // 2024-02-01 00:00:00
date.EndOfMonth();       // 2024-02-29 23:59:59.9999999
date.StartOfWeek();      // Monday 2024-02-12
date.IsWeekend();        // false
new DateTime(1990, 2, 15).Age(asOf: date); // 34

date.AsFileName("Monthly Report"); // "monthly-report-2024-02-15-13-45-00"
```

## Business days

Business days and public holidays take a `Country` with a holiday calendar:

```csharp
using HeroKit.Globalization;

var day = new DateOnly(2026, 3, 19);

day.IsBusinessDay(Countries.Turkey);               // true
day.AddBusinessDays(1, Countries.Turkey);          // 2026-03-23, skips the bayram and the weekend
day.CountBusinessDays(new DateOnly(2026, 3, 27), Countries.Turkey); // business days after day, up to and including 27 March; a half day counts 0.5
new DateOnly(2026, 10, 29).IsPublicHoliday(Countries.Turkey); // true
```

See [Countries and languages](countries-and-languages.md) for holidays, half days and how to add a calendar for another country.

Results keep the `DateTimeKind` of the input. `DateOnly` overloads are available for week, month, weekend, range and age helpers.
