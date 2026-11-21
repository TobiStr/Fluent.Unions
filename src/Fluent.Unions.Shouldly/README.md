![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Shouldly

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Shouldly.svg)](https://www.nuget.org/packages/Fluent.Unions.Shouldly) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Shouldly.svg)](https://www.nuget.org/packages/Fluent.Unions.Shouldly) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/testing/)

Adds `ShouldBeOk`, `ShouldBeError`, `ShouldBeSome` and `ShouldBeNone` for `Result`, `Result<T>` and `Option<T>` to Shouldly. The methods return the value or the error, so a test can continue, and a failure message names the actual state.

## Installation

```
dotnet add package Fluent.Unions.Shouldly
```

## Usage

```csharp
using Shouldly;
using Fluent.Unions.Shouldly;

Result<Order> placed = await orders.PlaceAsync(request);
placed.ShouldBeOk().Total.ShouldBe(49.90m);
// passes when the order was placed with that total

Result missing = await orders.DeleteAsync(unknownId);
missing.ShouldBeError<NotFoundException>().Message.ShouldContain("was not found");
// passes when the error is a NotFoundException

customer.Nickname.ShouldBeNone();
// passes when the customer has no nickname
```

## With your own result type

`ShouldBeOk()` and `ShouldBeError()` accept a result type you declare with `[FluentResult<TError>]`, with a value and without (the value-less `Result`), and fail with the same messages. `ShouldBeError()` returns your declared error type. When that is an error union, `ShouldBeError<NotFound>()` returns the case, and fails for another case. See [Your own Result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

## Documentation

- [Testing documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/testing/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
