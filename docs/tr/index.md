---
_layout: landing
_disableToc: true
---

<div class="hk-hero">
  <h1>HeroKit</h1>
  <p class="hk-tagline">.NET için günlük uzantı metotları ve <strong>Türkiye verisi için birinci sınıf destek</strong>: TCKN, VKN, IBAN, telefon numaraları ve yazıyla tutar. Türkiye ve ABD'nin hazır geldiği tek bir ülke ve dil modeli; yeni metot olmadan kendi ülkenizi ekleyin.</p>
  <div class="hk-install">
    <code>dotnet add package HeroKit</code>
    <button type="button" data-copy="dotnet add package HeroKit">Kopyala</button>
  </div>
  <div class="hk-actions">
    <a class="hk-primary" href="articles/getting-started.md">Başlayın</a>
    <a class="hk-secondary" href="articles/countries-and-languages.md">Ülkeler ve diller</a>
    <a class="hk-secondary" href="../api/HeroKit.Strings.yml">API referansı</a>
  </div>
  <div class="hk-badges">
    <img alt="NuGet" src="https://img.shields.io/nuget/v/HeroKit.svg?color=f2a541&label=NuGet">
    <img alt="Downloads" src="https://img.shields.io/nuget/dt/HeroKit.svg?color=0a8bb0">
    <img alt="Coverage" src="https://raw.githubusercontent.com/Lyconiq/HeroKit/badges/coverage.svg">
    <img alt="License" src="https://img.shields.io/badge/license-MIT-0a8bb0.svg">
  </div>
</div>

## Çalışırken görün

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

<h2 class="hk-section-title">İçinde neler var</h2>

<div class="hk-grid">
  <a class="hk-card hk-featured" href="articles/countries-and-languages.md">
    <div class="hk-icon">🌍</div>
    <h3>Ülkeler ve diller</h3>
    <p>Türkiye'nin hazır geldiği Country, Language ve Currency modeli: TCKN ve VKN, IBAN, telefon numaraları, bölgeler, plakalar, posta kodları, lira, yazıyla tutar, resmî tatiller ve iş günleri, e-fatura numaraları, İ/ı duyarlı büyük-küçük harf dönüşümü ve ICU olmadan sıralama. Kendi ülkenizi ekleyin.</p>
  </a>
  <a class="hk-card" href="articles/validation.md">
    <div class="hk-icon">✅</div>
    <h3>Form doğrulama</h3>
    <p>ASP.NET Core ve Blazor modelleri için [Tckn], [Iban], [PhoneNumber("TR")], [VehiclePlate("TR")] ve daha fazlası. Alan başına tek satır.</p>
  </a>
  <a class="hk-card" href="articles/strings.md">
    <div class="hk-icon">🔤</div>
    <h3>Metinler</h3>
    <p>Slug, her kültür için baş harfleri büyütme, emojiyi bozmadan kısaltma, maskeleme, null güvenli kontroller.</p>
  </a>
  <a class="hk-card" href="articles/conversions.md">
    <div class="hk-icon">🔁</div>
    <h3>Dönüşümler</h3>
    <p>Her IParsable tipi için To&lt;T&gt;, toleranslı ToX ve denetimli TryToX. Kültürden bağımsız; hatalı girdi asla istisna fırlatmaz.</p>
  </a>
  <a class="hk-card" href="articles/collections-and-dates.md">
    <div class="hk-icon">📅</div>
    <h3>Koleksiyonlar ve tarihler</h3>
    <p>Entity Framework'ün SQL'e çevirdiği WhereIf ve sayfalama. Hafta, ay ve yıl aralıkları, yaş, hafta sonu.</p>
  </a>
  <a class="hk-card" href="articles/prices.md">
    <div class="hk-icon">🧾</div>
    <h3>Fiyat, KDV ve kartlar</h3>
    <p>Adıma yuvarlama, psikolojik fiyatlar, KDV ekleme ve çıkarma. Troy destekli kart doğrulama.</p>
  </a>
  <a class="hk-card" href="articles/file-types.md">
    <div class="hk-icon">📁</div>
    <h3>Dosya türleri ve yükleme</h3>
    <p>Ayrı paket: içerikten yaklaşık 70 tür tespiti, uzantı ve MIME eşlemesi, güvensiz yüklemeleri reddetme: programlar, çift uzantı, betikli SVG. Kendi türlerinizi ekleyin.</p>
  </a>
</div>

<div class="hk-stats">
  <div><strong>0</strong><span>bağımlılık</span></div>
  <div><strong>.NET 8 · 10</strong><span>hedef çerçeveler</span></div>
  <div><strong>AOT</strong><span>Native AOT'ye hazır</span></div>
  <div><strong>MIT</strong><span>açık kaynak</span></div>
</div>

<h2 class="hk-section-title">Güvenilmek için tasarlandı</h2>

<div class="hk-grid">
  <div class="hk-card">
    <h3>Kararlı genel API</h3>
    <p>Analizörlerle takip edilir. Önce <code>[Obsolete]</code> bir sürüm yayımlanmadan hiçbir şey kaldırılmaz.</p>
  </div>
  <div class="hk-card">
    <h3>Test edilmiş</h3>
    <p>Her yardımcı için tablo tabanlı testler; .NET 8 ve .NET 10 üzerinde çalışır, canlı kapsam rozeti vardır.</p>
  </div>
  <div class="hk-card">
    <h3>Hızlı</h3>
    <p>Kritik yollar BenchmarkDotNet ile ölçülür. Kaynak üretimli regex ve tek geçişli metin yardımcıları.</p>
  </div>
</div>

<p style="text-align:center;margin:2.5rem 0 1rem">
  Sorularınız ya da fikirleriniz mi var? <a href="https://github.com/Lyconiq/HeroKit/discussions">GitHub Discussions</a>'a katılın.
  HeroKit'i beğendiniz mi? <a href="https://github.com/Lyconiq/HeroKit">Yıldız verin ⭐</a>
</p>
