# Nullable ve AOT

## Nullable açıklamaları

[Null değer alabilen başvuru türleri](https://learn.microsoft.com/tr-tr/dotnet/csharp/nullable-references) ile `string` "asla null değil", `string?` "null olabilir" anlamına gelir; derleyici, null bir değer `NullReferenceException` fırlatmadan önce uyarır. HeroKit'in açıklamaları her metot için bu bilgiyi derleyiciye verir.

Her genel API açıklamalıdır. Null kontrol yardımcıları sonucu derleyiciye bildirir:

```csharp
string? input = GetInput();

if (input.IsNotNullOrEmpty())
    Console.WriteLine(input.Length); // uyarı yok: input burada null değil
```

## Trimming ve Native AOT

### Native AOT nedir?

Bir .NET uygulaması normalde ara kod (IL) olarak dağıtılır. Çalışma zamanı bu kodu uygulama çalışırken makine koduna çevirir; buna tam zamanında (JIT) derleme denir. **Native AOT** (ahead-of-time, önceden derleme) ile `dotnet publish` bütün uygulamayı önceden makine koduna derler. Sonuç tek bir yerel çalıştırılabilir dosyadır:

- daha hızlı açılır ve daha az bellek kullanır,
- .NET çalışma zamanı kurulu olmayan bir makinede de çalışır.

Bu en çok container'larda, serverless fonksiyonlarda, komut satırı araçlarında ve ASP.NET Core minimal API'lerde önemlidir.

Bedeli: Uygulama çalışma sırasında kod yükleyemez ya da üretemez, kullanmadığı kod da kaldırılır (**trimming**, kırpma). Yansımaya (reflection) dayanan kod bu yüzden çalışma sırasında hata verebilir. Bir kütüphane bu özelliklere ihtiyaç duymuyorsa ve yayımlama sırasında uyarı üretmiyorsa **AOT uyumludur**.

Native AOT kullanmak zorunda değilsiniz. HeroKit normal bir .NET uygulamasında da aynı şekilde çalışır. AOT uyumluluğu yalnızca, bu yolu seçerseniz HeroKit'in engel olmayacağı anlamına gelir.

### HeroKit ve Native AOT

Paket `IsAotCompatible` olarak işaretlidir. CI her pull request'te Native AOT ile bir test uygulaması yayımlar ve herhangi bir trimming uyarısında başarısız olur.

Yansımaya (reflection) dayanan bazı yardımcılar tam olarak kırpılamaz. Bunlar `[RequiresUnreferencedCode]` veya `[RequiresDynamicCode]` taşır, bu yüzden çağrı yerinde uyarı alırsınız:

- `TypesExtensions.GetExtensionMethods`, `GetExtensionMethod`, `GetRealType`

`Country`, `Language` ve `Currency` modeli düz nesnelerden ve arayüzlerden oluşur. Yansıma kullanmaz.

Dönüşümler, enum yardımcıları ve `TrySetConvertedValue` 2.0'dan beri AOT güvenlidir: yansıma yerine `IParsable<T>` ve genel enum API'lerini kullanırlar.

## Kaynaklar

- [Native AOT dağıtımına genel bakış](https://learn.microsoft.com/tr-tr/dotnet/core/deploying/native-aot/) (Microsoft Learn): Native AOT nedir, sınırları ve nasıl yayımlanır.
- [Yerel AOT için ASP.NET Core desteği](https://learn.microsoft.com/tr-tr/aspnet/core/fundamentals/native-aot)
- [Trim self-contained deployments](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained) (İngilizce)
- [Introduction to AOT warnings](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/fixing-warnings) ve [Prepare .NET libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) (İngilizce): `IsAotCompatible` bir kütüphanede neyi denetler.
