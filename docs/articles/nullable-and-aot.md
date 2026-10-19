# Nullable and AOT

## Nullable annotations

With [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), `string` means "never null" and `string?` means "can be null", and the compiler warns before a null value causes a `NullReferenceException`. HeroKit's annotations give the compiler this information for every method.

Every public API is annotated. Null-check helpers tell the compiler about the result:

```csharp
string? input = GetInput();

if (input.IsNotNullOrEmpty())
    Console.WriteLine(input.Length); // no warning: input is not null here
```

## Trimming and Native AOT

### What is Native AOT?

A .NET app normally ships as intermediate code (IL). The runtime turns it into machine code while the app runs; this is just-in-time (JIT) compilation. With **Native AOT** (ahead-of-time), `dotnet publish` compiles the whole app to machine code in advance. The result is one native executable that:

- starts faster and uses less memory,
- runs on a machine without the .NET runtime.

This matters most for containers, serverless functions, command-line tools and ASP.NET Core minimal APIs.

The cost: the app cannot load or generate code at run time, and code the app does not use is removed (**trimming**). Code that depends on reflection can then fail at run time. A library is **AOT compatible** when it does not need these features, so publishing produces no warnings for it.

You do not have to use Native AOT. HeroKit works the same in a normal .NET app. AOT compatibility only means that HeroKit does not stop you from choosing it.

### HeroKit and Native AOT

The package is marked `IsAotCompatible`. CI publishes a test app with Native AOT on every pull request and fails on any trimming warning.

A few reflection-based helpers cannot be fully trimmed. They carry `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`, so you get a warning at the call site:

- `TypesExtensions.GetExtensionMethods`, `GetExtensionMethod`, `GetRealType`

The `Country`, `Language` and `Currency` model is made of plain objects and interfaces. It does not use reflection.

Conversions, enum helpers and `TrySetConvertedValue` are AOT-safe since 2.0: they use `IParsable<T>` and generic enum APIs instead of reflection.

## References

- [Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) (Microsoft Learn): what Native AOT is, its limitations and how to publish.
- [ASP.NET Core support for Native AOT](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/native-aot)
- [Trim self-contained deployments](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
- [Introduction to AOT warnings](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/fixing-warnings) and [Prepare .NET libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming): what `IsAotCompatible` checks in a library.
