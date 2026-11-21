![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Xunit

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Xunit.svg)](https://www.nuget.org/packages/Fluent.Unions.Xunit) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Xunit.svg)](https://www.nuget.org/packages/Fluent.Unions.Xunit) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/testing/)

Adds `ResultAssert` and `OptionAssert` for `Result`, `Result<T>` and `Option<T>` to xUnit v3. The methods return the value or the error, and a failure message names the actual state.

## Installation

```
dotnet add package Fluent.Unions.Xunit
```

## Usage

```csharp
using Fluent.Unions.Xunit;

Result<Order> placed = await orders.PlaceAsync(request);
Order order = ResultAssert.Ok(placed);
// passes when the order was placed, and returns it

Result missing = await orders.DeleteAsync(unknownId);
NotFoundException error = ResultAssert.Error<NotFoundException>(missing);
// passes when the error is a NotFoundException, and returns it

OptionAssert.None(customer.Nickname);
// passes when the customer has no nickname
```

The assertions are static methods and not part of `Assert`, because the partial `Assert` class of `xunit.v3.assert.source` is compiled into each test project and cannot be extended from another assembly.

## With your own result type

`ResultAssert.Ok` and `ResultAssert.Error` accept a result type you declare with `[FluentResult<TError>]`, with a value and without (the value-less `Result`), and fail with the same messages. There is no `Error<TException>` for your own types, because the untyped `Error` already returns your declared error type. See [Your own Result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

## Documentation

- [Testing documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/testing/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
