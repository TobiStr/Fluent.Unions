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

Add `Fluent.Unions.AspNetCore` only to web projects. It references `Fluent.Unions` and `Microsoft.AspNetCore.App`.

## Namespaces

| Namespace | Contents |
| --- | --- |
| `Fluent.Unions` | `Result`, `Result<T>`, `Option<T>`, `Loadable<T>`, `Result<T, TError>`, `Ok`, `None`, `Captured<T>`, `ValidationError(s)`, and every operator |
| `Fluent.Unions.Validation` | `Is` and the ready-made `Ensure*` rules |
| `Fluent.Unions.Abstractions` | `IResult<TSelf, T, TError>` and `IFromException<T>`, for your own result types |
| `Fluent.Unions.Serialization` | JSON converters, such as `UnionJsonConverter` |
| `Fluent.Unions.AspNetCore` | `ToHttpResult`, `WithResultMapping`, `ResultHttpOptions` |

`using Fluent.Unions;` is all most files need. The rules stay in their own namespace, so the `Ensure*` methods do not show up on every string and number unless you ask for them. A global using saves repeating it:

```csharp
global using Fluent.Unions;
```

In ASP.NET Core files that also import `Microsoft.AspNetCore.Http.HttpResults`, the case `Ok` clashes with the HTTP result `Ok` (CS0104). Add `using Ok = Fluent.Unions.Ok;` there.

If you declare your own `Result<T>`, see [Custom error types](../errors/custom-error-types.md#namespaces) for the namespace rules.

## The bundled analyzer

`Fluent.Unions` ships an analyzer. Calling a method that returns a result and ignoring it gives warning `FLU0001`:

```csharp
Save(order);      // warning FLU0001: The result of 'Save' is not used
_ = Save(order);  // fine: discarded on purpose
Save(order).Check();
```

It does not report assignments, returns, arguments, calls that end in `Check()`, `OnErrorDo(...)` or `Do(onSuccess, onError)`, or options. To raise the severity, add this to `.editorconfig`:

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
- The README in the repository root describes referencing the project directly. The package IDs above come from the project files in `src/`.

## See also

- [Quick start](quick-start.md)
- [Result](../result/README.md)

---

Back to: [Getting started](README.md) · Next: [Quick start](quick-start.md)
