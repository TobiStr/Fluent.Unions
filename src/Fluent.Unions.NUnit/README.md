![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.NUnit

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.NUnit.svg)](https://www.nuget.org/packages/Fluent.Unions.NUnit) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.NUnit.svg)](https://www.nuget.org/packages/Fluent.Unions.NUnit) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/testing/)

Adds constraints for the state of a `Result`, `Result<T>` and `Option<T>` to NUnit: `IsResult.Ok`, `IsResult.Error` and `IsOption.Some`, among others. A failure message names the actual state. It targets NUnit 4 and later.

## Installation

```
dotnet add package Fluent.Unions.NUnit
```

## Usage

```csharp
using Fluent.Unions.NUnit;

Result<Order> placed = await orders.PlaceAsync(request);
Assert.That(placed, IsResult.OkWith(Has.Property("Total").EqualTo(49.90m)));
// passes when the order was placed with that total

Result missing = await orders.DeleteAsync(unknownId);
Assert.That(missing, IsResult.ErrorOfType<NotFoundException>());
// passes when the error is a NotFoundException

Assert.That(customer.Nickname, IsOption.None);
// passes when the customer has no nickname
```

## With your own result type

The `IsResult` constraints recognize a result type you declare with `[FluentResult<TError>]`. An error that is not an exception prints as `failed with Type: ...`. See [Your own Result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

## Documentation

- [Testing documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/testing/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
