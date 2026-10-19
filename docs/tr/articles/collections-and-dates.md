# Koleksiyonlar ve tarihler

## Koleksiyonlar

```csharp
using HeroKit.Enumerables;

items.IsNullOrEmpty();                               // null güvenli; tembel (lazy) bir dizide en fazla bir öğe okur
products.WhereIf(onlyInStock, p => p.Stock > 0);     // yalnızca koşul true ise filtreler
query.OrderBy(x => x.Id).Page(pageNumber: 2, pageSize: 20); // 1 tabanlı sayfalama
source.ToReadOnlyList();                             // salt okunur kopya
```

`WhereIf` ve `Page`, `IQueryable<T>` üzerinde de çalışır; bu yüzden Entity Framework bunları SQL'e çevirir.

## Tarihler

```csharp
using HeroKit.DateTimes;

var date = new DateTime(2024, 2, 15, 13, 45, 0);

date.StartOfMonth();     // 2024-02-01 00:00:00
date.EndOfMonth();       // 2024-02-29 23:59:59.9999999
date.StartOfWeek();      // Pazartesi 2024-02-12
date.IsWeekend();        // false
new DateTime(1990, 2, 15).Age(asOf: date); // 34

date.AsFileName("Monthly Report"); // "monthly-report-2024-02-15-13-45-00"
```

## İş günleri

İş günleri ve resmî tatiller, tatil takvimi olan bir `Country` alır:

```csharp
using HeroKit.Globalization;

var day = new DateOnly(2026, 3, 19);

day.IsBusinessDay(Countries.Turkey);               // true
day.AddBusinessDays(1, Countries.Turkey);          // 2026-03-23, bayramı ve hafta sonunu atlar
day.CountBusinessDays(new DateOnly(2026, 3, 27), Countries.Turkey); // day'den sonra 27 Mart dahil iş günü sayısı; yarım gün 0,5
new DateOnly(2026, 10, 29).IsPublicHoliday(Countries.Turkey); // true
```

Tatiller, yarım günler ve başka bir ülke için takvim eklemek için bkz. [Ülkeler ve diller](countries-and-languages.md).

Sonuçlar girdinin `DateTimeKind` değerini korur. Hafta, ay, hafta sonu, aralık ve yaş yardımcıları için `DateOnly` aşırı yüklemeleri de vardır.
