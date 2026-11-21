![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Verify

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Verify.svg)](https://www.nuget.org/packages/Fluent.Unions.Verify) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Verify.svg)](https://www.nuget.org/packages/Fluent.Unions.Verify) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/verify/)

Makes [Verify](https://github.com/VerifyTests/Verify) snapshots of `Result`, `Result<T>`, `Result<T, TError>`, `Option<T>` and `Loadable<T>` readable: the case and its data, with no stack trace. It builds on Fluent.Unions and Verify.

## Installation

```
dotnet add package Fluent.Unions.Verify
```

## Usage

Call `VerifyFluentUnions.Initialize()` once, for example from a module initializer in the test project.

```csharp
using System.Runtime.CompilerServices;
using Fluent.Unions.Verify;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyFluentUnions.Initialize();
}
```

Then verify results and options like any other value.

```csharp
await Verify(Result.From(42));
await Verify(Option.Some(7));
```

```
{
  Ok: 42
}
```

```
{
  Some: 7
}
```

An error is written as `{ Error: { Type, Message } }`, an `Option<T>` as `None` or `{ Some: value }`, and a `Loadable<T>` as `NotLoaded`, `Loading`, `{ Value: value }` or `{ Error: ... }`. A `Result<T, TError>` writes its error as data. `Initialize` also tells Verify to leave stack traces out of exceptions.

## License of Verify

Verify is MIT source, but its binaries carry the Open Source Maintenance Fee EULA: organisations with an annual gross revenue of US$10,000 or more pay a maintenance fee. Verify flows transitively through this package, so its build check (error SC021) asks every consuming project to declare a license position. Set one of these MSBuild properties in your test project or in `Directory.Build.props`:

- `Verify_GitHubSponsorAccount`: sponsor on GitHub Sponsors.
- `Verify_SponsorshipLicensedUntil`: a time-bounded license, as `yyyy-MM`.
- `Verify_SponsorshipExemption` with `Verify_SponsorshipExemptionUntil`: a publisher-defined exemption such as `OpenSource` or `SmallRevenue`.

See the [Verify maintenance fee](https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md) for the current terms and options.

## With your own result type

Verify snapshots any result type, including one declared with `[FluentResult<TError>]`. A value-less success writes `Ok`, a result with a value writes `{ Ok: ... }`, and an error is written as data as before. See [Your own Result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

## Documentation

- [Verify documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/verify/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
