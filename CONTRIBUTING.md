# Contributing to HeroKit

Thank you for helping. This guide is short. Please read it before you open a PR.

## Prerequisites

- .NET 10 SDK.
- .NET 8 runtime. You need it to run the net8 tests.

## Build and test

```
dotnet build -c Release
dotnet test -c Release
```

## Conventions

- One issue per PR.
- Put `Closes #n` at the start of the PR body.
- Every fix and feature needs tests.
- Public APIs need XML docs.
- Parsing and formatting must be culture-invariant. Take a culture as a parameter if the caller must choose one.

## Public API tracking

HeroKit tracks its public API with `Microsoft.CodeAnalysis.PublicApiAnalyzers`. The build fails when the public API changes without an update to these files.

- Add every new public API to `HeroKit/PublicAPI.Unshipped.txt`.
- At release, move those entries to `HeroKit/PublicAPI.Shipped.txt`.

## Obsolete before remove

Do not remove a public API in a 1.x release. Mark it with `[Obsolete]` and name the replacement in the message. Remove it in the next major version.

## Documentation

The site at herokit.lyconiq.com is built with DocFX from `docs/` and the XML comments. Preview it locally:

```
dotnet tool install -g docfx
docfx docs/docfx.json --serve
```

The `docs.yml` workflow publishes the site when a release is published.

## Adding a country or a language

Country-specific rules live behind interfaces in `HeroKit/Globalization` (`IPhoneNumberRules`, `IVehiclePlateRules`, `IIdentifierRules`, `IHolidayCalendar`, `INumberSpeller`, `ITextRules`). To add a country, implement the rules in `HeroKit/Globalization/<Country>/`, expose a `Country` instance in `Countries` and register it in the static constructor. No existing method changes. Applications can also create and register their own `Country` at startup.

## Yearly maintenance: religious holidays

Ramazan and Kurban Bayramı dates come from the Diyanet religious days calendar (https://vakithesaplama.diyanet.gov.tr/dinigunler.php?yil=YYYY). When Diyanet publishes a new year, add its first-day dates to `HeroKit/Globalization/Turkey/TurkeyHolidayCalendar.cs` and to the test table. The test `SupportedYears_ShouldCoverTheCurrentYear` fails when the current year is missing.

## Releases

HeroKit ships at most one minor release a month. Changes collect on `master` and go out together, so users are not asked to upgrade for every small change. Security fixes ship as patch releases when they are ready. Version numbers follow [Semantic Versioning](https://semver.org): breaking changes only in a major version, after an `[Obsolete]` release. A major version line stays supported for at least 12 months after the next major version ships (see [SECURITY.md](SECURITY.md)).

1. Bump `<Version>` in `Directory.Build.props`. All packages share it.
2. Update `CHANGELOG.md`. Move the Unreleased entries under the new version.
3. Move the `PublicAPI.Unshipped.txt` entries to `PublicAPI.Shipped.txt` in every package project.
4. Merge the PR.
5. Publish a GitHub release. Name it and tag it with the version, for example `3.6.0`.

The `nuget-publish.yml` workflow then packs and tests the packages, attaches an SBOM and a build provenance attestation to the release, and pushes the packages to NuGet with Trusted Publishing.
