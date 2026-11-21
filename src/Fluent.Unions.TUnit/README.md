![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.TUnit

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.TUnit.svg)](https://www.nuget.org/packages/Fluent.Unions.TUnit) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.TUnit.svg)](https://www.nuget.org/packages/Fluent.Unions.TUnit) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/testing/)

Adds `IsOk`, `IsError`, `IsSome` and `IsNone` for `Result`, `Result<T>` and `Option<T>` to TUnit assertions. Awaiting an assertion returns the value or the error, and a failure message names the actual state.

## Installation

```
dotnet add package Fluent.Unions.TUnit
```

## Usage

```csharp
using TUnit.Assertions.Extensions;

Result<Order> placed = await orders.PlaceAsync(request);
Order order = await Assert.That(placed).IsOk();
// passes when the order was placed, and returns it

Result missing = await orders.DeleteAsync(unknownId);
NotFoundException error = await Assert.That(missing).IsError<NotFoundException>();
// passes when the error is a NotFoundException, and returns it

await Assert.That(customer.Nickname).IsNone();
// passes when the customer has no nickname
```

The extension methods are generated into `TUnit.Assertions.Extensions`, which TUnit projects import globally. The package depends on TUnit.Assertions only, so the assertions also work in a test project of another framework.

## With your own result type

Assert a result type you declare with `[FluentResult<TError>]` through `AsResult()`. C# does not allow the result interface as a type argument, so the generated assertions cannot take your type directly. The default types keep `Assert.That(result).IsOk()`. See [Your own Result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

```csharp
await Assert.That(result.AsResult()).IsOk();
await Assert.That(result.AsResult()).IsError();
NotFound notFound = await Assert.That(result.AsResult().ErrorCase<NotFound>()).IsSome(); // a case of an error union
```

The assertion methods behind `Assert.That(...)` are not public extensions, so `result.IsOk();` does not compile instead of asserting nothing.

## Documentation

- [Testing documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/testing/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
