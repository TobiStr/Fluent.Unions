# Verify

The package `Fluent.Unions.Verify` (namespace `Fluent.Unions.Verify`) makes [Verify](https://github.com/VerifyTests/Verify) snapshots of results, options and loadables readable. It registers one write-only converter and adds no assertions of its own.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): set up once, then snapshot a result.
- [What the snapshots look like](#what-the-snapshots-look-like): the text for each case.
- [Errors](#errors): exceptions, validation errors and errors as data.
- [License of Verify](#license-of-verify): the maintenance fee and the build check.
- [With your own result type](#with-your-own-result-type): what is written for a declared result type.
- [Rules](#rules): what is and is not covered.

## Quick example

```csharp
using System.Runtime.CompilerServices;
using Fluent.Unions.Verify;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyFluentUnions.Initialize();
}

[Test]
public Task Order() => Verify(new Order(1, Option.Some("gift"), Result.From(9.5m)));
```

The `.verified.txt` file reads:

```
{
  Id: 1,
  Note: {
    Some: gift
  },
  Price: {
    Ok: 9.5
  }
}
```

`Initialize` is safe to call more than once. It also calls `VerifierSettings.IgnoreStackTrace()`, so exceptions are written without a stack trace. That setting applies to every exception Verify writes, not only those inside a union.

## What the snapshots look like

| Value | Snapshot |
| --- | --- |
| `Result.Ok` | `Ok` |
| `Result<T>` or your own result type with a value | `{ Ok: value }` |
| a result with an error | `{ Error: ... }` |
| `Option<T>` with a value | `{ Some: value }` |
| `Option<T>` without a value | `None` |
| `Loadable<T>` not loaded | `NotLoaded` |
| `Loadable<T>` loading | `Loading` |
| `Loadable<T>` loaded | `{ Value: value }` |
| `Loadable<T>` faulted | `{ Error: ... }` |

Results, options and loadables nest with their labels: `Result.From(Option.Some(5))` is `{ Ok: { Some: 5 } }` and an option without a value inside a result is `{ Ok: None }`. A result is told apart by its state, so a result whose value has the type of its error still shows `Ok` for the value and `Error` for the error.

The same text appears when a result, option or loadable is an item of a list or a value of a dictionary. As a property of an object it appears too, except `None` and `NotLoaded`: they equal `default`, so Verify leaves the property out like any other default value. A `default` result is an error, as in the core, and shows the error the core gives it.

## Errors

An error that is an exception is written as its `Type` and `Message`.

```
{
  Error: {
    Type: InvalidOperationException,
    Message: disk full
  }
}
```

A `ValidationError` adds its `MemberName` when it has one, and a `ValidationErrors` adds its `Errors` list.

For your own result type the error is its data. When `TError` is a union of error records, the snapshot shows the record that is held, without a wrapper:

```
{
  Error: {
    Reason: insufficient funds
  }
}
```

An exception inside such a record, for example `Unexpected(Exception)`, is written by Verify itself, with its `$type`, `Type` and `Message`.

## With your own result type

Verify snapshots any result type, including one declared with `[FluentResult<TError>]` (see [Your own Result type](../errors/custom-error-types.md)). A value-less success writes `Ok`, a result with a value writes `{ Ok: ... }`, and an error is written as data as before.

## License of Verify

Verify is MIT source, but its binaries carry the Open Source Maintenance Fee EULA. Organisations with an annual gross revenue of US$10,000 or more pay a maintenance fee. Smaller users and some open source projects are exempt.

Verify flows transitively through `Fluent.Unions.Verify`, and its build check (error SC021) asks every consuming project for a license position. Set one of these MSBuild properties in your test project or in `Directory.Build.props`:

| Property | Meaning |
| --- | --- |
| `Verify_GitHubSponsorAccount` | sponsor on GitHub Sponsors |
| `Verify_SponsorshipLicensedUntil` | a time-bounded license, as `yyyy-MM` |
| `Verify_SponsorshipExemption` and `Verify_SponsorshipExemptionUntil` | an exemption such as `OpenSource` or `SmallRevenue`, with a date as `yyyy-MM` |

See the [Verify maintenance fee](https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md) for the current terms. The choice is yours, and this package does not make it for you.

## Rules

- **Call `Initialize` before the first snapshot.** A module initializer in the test project does it once for all tests.
- **Write-only.** The converter only writes snapshots. Verify never reads a result back.
- **No stack traces.** Exceptions show their type and message.
- **Only result types and the core types.** Your own unions are written by Verify's default rules. The exception is a union held directly as the value or error of a result, option or loadable, such as the error union of your own result type: it is unwrapped to its case.
>>>>

- **Verify.NUnit needs NUnit 5.** `Verify.NUnit` 33.7.1 requires NUnit 5.0.0 or later.
- **Name clash.** Inside a namespace that starts with `Fluent.Unions.Verify`, write `Verifier.Verify(...)` instead of `Verify(...)`, because the namespace hides the method.

---

Back to index: [Docs index](../README.md)
