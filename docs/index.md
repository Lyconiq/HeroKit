---
_layout: landing
_disableToc: true
---

<div class="hk-hero">
  <h1>HeroKit</h1>
  <p class="hk-tagline"><strong>Turkish data for .NET, done right</strong>: TCKN, VKN and IBAN, phone numbers, provinces, holidays and business days, amounts in words. And <strong>safe file uploads</strong> that check the real file type, not the extension. Plus everyday extension methods.</p>
  <div class="hk-install">
    <code>dotnet add package HeroKit</code>
    <button type="button" data-copy="dotnet add package HeroKit">Copy</button>
  </div>
  <div class="hk-actions">
    <a class="hk-primary" href="articles/getting-started.md">Get started</a>
    <a class="hk-secondary" href="articles/countries-and-languages.md">Turkish data</a>
    <a class="hk-secondary" href="articles/file-types.md">Safe file uploads</a>
    <a class="hk-secondary" href="api/HeroKit.Strings.yml">API reference</a>
  </div>
  <div class="hk-badges">
    <img alt="NuGet" src="https://img.shields.io/nuget/v/HeroKit.svg?color=f2a541&label=NuGet">
    <img alt="Downloads" src="https://img.shields.io/nuget/dt/HeroKit.svg?color=0a8bb0">
    <img alt="Coverage" src="https://raw.githubusercontent.com/Lyconiq/HeroKit/badges/coverage.svg">
    <img alt="License" src="https://img.shields.io/badge/license-MIT-0a8bb0.svg">
  </div>
</div>

## See it in action

```csharp
using HeroKit.Banking;
using HeroKit.Globalization;
using HeroKit.Globalization.Turkey;

"10000000146".IsValidTckn();                                // true
"TR330006100519786457841326".FormatIban();                  // "TR33 0006 1005 1978 6457 8413 26"
"0532 123 45 67".ToE164(Countries.Turkey);                  // "+905321234567"
123.50m.ToCurrencyWords(Currencies.TurkishLira);            // "yüz yirmi üç Türk lirası elli kuruş"
"istanbul ılıca".ToTitleCase(Languages.Turkish);            // "İstanbul Ilıca"
"34abc123".FormatVehiclePlate(Countries.Turkey);            // "34 ABC 123"
```

<h2 class="hk-section-title">What's inside</h2>

<div class="hk-grid">
  <a class="hk-card hk-featured" href="articles/countries-and-languages.md">
    <div class="hk-icon">🌍</div>
    <h3>Countries and languages</h3>
    <p>A Country, Language and Currency model with Türkiye built in: TCKN and VKN, IBAN, phone numbers, regions, plates, postal codes, lira, amounts in words, holidays and business days, e-invoice numbers, İ/ı-aware casing and sorting without ICU. Add your own country.</p>
  </a>
  <a class="hk-card hk-featured" href="articles/file-types.md">
    <div class="hk-icon">📁</div>
    <h3>File types and uploads</h3>
    <p>Separate package: detect about 70 types from content, map extensions and MIME types, and reject unsafe uploads: programs, double extensions, SVG with script. Add your own types.</p>
  </a>
  <a class="hk-card" href="articles/validation.md">
    <div class="hk-icon">✅</div>
    <h3>Form validation</h3>
    <p>[Tckn], [Iban], [PhoneNumber("TR")], [VehiclePlate("TR")] and more for ASP.NET Core and Blazor models. One line per field.</p>
  </a>
  <a class="hk-card" href="articles/strings.md">
    <div class="hk-icon">🔤</div>
    <h3>Strings</h3>
    <p>Slugs, title case for any culture, truncate without breaking emoji, masking, null-safe checks.</p>
  </a>
  <a class="hk-card" href="articles/conversions.md">
    <div class="hk-icon">🔁</div>
    <h3>Conversions</h3>
    <p>To&lt;T&gt; for any IParsable type, lenient ToX and checked TryToX. Culture-invariant; bad input never throws.</p>
  </a>
  <a class="hk-card" href="articles/collections-and-dates.md">
    <div class="hk-icon">📅</div>
    <h3>Collections and dates</h3>
    <p>WhereIf and paging that Entity Framework translates to SQL. Week, month and year ranges, age, weekends.</p>
  </a>
  <a class="hk-card" href="articles/prices.md">
    <div class="hk-icon">🧾</div>
    <h3>Prices, VAT and cards</h3>
    <p>Round to a step, psychological prices, VAT in and out. Card validation with Troy support.</p>
  </a>
</div>

<div class="hk-stats">
  <div><strong>0</strong><span>dependencies</span></div>
  <div><strong>.NET 8 · 10</strong><span>target frameworks</span></div>
  <div><strong>AOT</strong><span><a href="articles/nullable-and-aot.md">Native AOT ready</a></span></div>
  <div><strong>MIT</strong><span>open source</span></div>
</div>

<h2 class="hk-section-title">Built to be depended on</h2>

<div class="hk-grid">
  <div class="hk-card">
    <h3>Stable public API</h3>
    <p>Tracked with analyzers. Nothing is removed without an <code>[Obsolete]</code> release first.</p>
  </div>
  <div class="hk-card">
    <h3>Tested</h3>
    <p>Table-driven tests for every helper, run on .NET 8 and .NET 10 with a live coverage badge.</p>
  </div>
  <div class="hk-card">
    <h3>Fast</h3>
    <p>Hot paths measured with BenchmarkDotNet. Source-generated regex and single-pass string helpers.</p>
  </div>
</div>

<p style="text-align:center;margin:2.5rem 0 1rem">
  Questions or ideas? Join <a href="https://github.com/Lyconiq/HeroKit/discussions">GitHub Discussions</a>.
  Like HeroKit? <a href="https://github.com/Lyconiq/HeroKit">Give it a star ⭐</a>
</p>
