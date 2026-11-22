# Installation

This page covers what you need to install, which namespaces to import, and the compiler settings worth turning on.

## Requirements

- .NET 11 SDK
- C# 15, which is the default language version for `net11.0`. You do not set `LangVersion`.

The library targets `net11.0` only. Unions do not parse at C# 14 or lower, so your project must target `net11.0` as well.

## Packages

```
dotnet add package Fluent.Unions
dotnet add package Fluent.Unions.AspNetCore
```

`Fluent.Unions` includes the source generator and the analyzers; there is nothing else to install for them.

Add `Fluent.Unions.AspNetCore` only to web projects. It references `Fluent.Unions` and `Microsoft.AspNetCore.App`.

Every other integration, such as FluentValidation, Polly, Newtonsoft.Json, Serilog or a test library, is its own package as well. The [docs index](../README.md#integrations-and-tooling) lists all of them. Add only the ones you use.

## Namespaces

| Namespace | Contents |
| --- | --- |
| `Fluent.Unions` | `Result`, `Result<T>`, `Option<T>`, `Loadable<T>`, `Ok`, `None`, `Captured<T>`, `ValidationError(s)`, `[FluentResult<TError>]`, `ExceptionCapture`, and every operator |
| `Fluent.Unions.Validation` | `Is` and the ready-made `Ensure*` rules |
| `Fluent.Unions.Abstractions` | `IFromException<T>` for the error type of your own result type, `ResultTypes`, and the result interfaces |
| `Fluent.Unions.Serialization` | The System.Text.Json converters `OptionJsonConverter`, `UnionJsonConverter`, their generic forms and `ResultJsonConverter`, plus `[UnionCaseName]` and `UnionCaseNames` |
| `Fluent.Unions.AspNetCore` | `ToHttpResult`, `WithResultMapping`, `ResultHttpOptions` |

`using Fluent.Unions;` is all most files need. The rules stay in their own namespace, so the `Ensure*` methods do not show up on every string and number unless you ask for them. A global using saves repeating it:

```csharp
global using Fluent.Unions;
```

In ASP.NET Core files that also import `Microsoft.AspNetCore.Http.HttpResults`, the case `Ok` clashes with the HTTP result `Ok` (CS0104). Add `using Ok = Fluent.Unions.Ok;` there.

If you declare your own `Result<T>` with `[FluentResult<TError>]`, put it in your root namespace. See [Your own Result type](../errors/custom-error-types.md#namespaces) for the namespace rules.

## The bundled analyzers

`Fluent.Unions` ships analyzers for the mistakes the compiler lets through. Calling a method that returns a result and ignoring it gives warning `FLU0001`:

```csharp
Save(order);      // warning FLU0001: The result of 'Save' is not used
_ = Save(order);  // fine: discarded on purpose
Save(order).Check();
```

Other rules catch an async lambda passed to `Try` or `Select`, a type such as `Result<Exception>` that cannot tell its cases apart, a result that is serialized to JSON, and more. Several come with a code fix. The source generator reports `FLU0101` to `FLU0105` for `[FluentResult]` declarations. See [Analyzer](../analyzer/README.md) for every rule. To raise a severity, add it to `.editorconfig`:

```
dotnet_diagnostic.FLU0001.severity = error
```

## Make CS8509 an error

A `switch` over a result or option that omits an arm gives warning CS8509 ("not exhaustive"). It is only a warning by default. To fail the build instead:

```xml
<PropertyGroup>
  <WarningsAsErrors>CS8509</WarningsAsErrors>
</PropertyGroup>
```

Do this early. It is the main reason to use a union: the compiler tells you when a case is not handled.

## Pitfalls

- A `default` option or result holds nothing. A `switch` over an option needs `None or null` as its last arm, not just `None`. See [Option](../option/README.md).

## See also

- [Quick start](quick-start.md)
- [Result](../result/README.md)

---

Back to: [Getting started](README.md) · Next: [Quick start](quick-start.md)
