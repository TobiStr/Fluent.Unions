![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.AwesomeAssertions

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.AwesomeAssertions.svg)](https://www.nuget.org/packages/Fluent.Unions.AwesomeAssertions) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.AwesomeAssertions.svg)](https://www.nuget.org/packages/Fluent.Unions.AwesomeAssertions) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/awesome-assertions/)

Adds `Should()` for `Result`, `Result<T>` and `Option<T>` to AwesomeAssertions. It builds on Fluent.Unions, and a failure message names the actual state.

## Installation

```
dotnet add package Fluent.Unions.AwesomeAssertions
```

## Usage

Assert on an Ok result and its value.

```csharp
using AwesomeAssertions;
using Fluent.Unions.AwesomeAssertions;

Result<Order> placed = await orders.PlaceAsync(request);
placed.Should().BeOk().Which.Total.Should().Be(49.90m);
```

Assert on the exception inside an error. `*` and `?` are wildcards in the message pattern.

```csharp
Result<Order> missing = await orders.FindAsync(unknownId);
missing.Should().BeError<NotFoundException>().WithMessage("Order * was not found");
```

Assert on an option.

```csharp
Option<string> nickname = customer.Nickname;
nickname.Should().BeSomeWithValue("ada");
```

Assert on the inner exceptions of an aggregate error, such as the output of `Validate`.

```csharp
registered.Should().BeAggregateError().WithInnerCount(2);
```

## With your own result type

`Should()` works on a result type you declare with `[FluentResult<TError>]` when it has a value. There are no assertions for the value-less `Result` of your own type. `BeError<TCase>()` also matches a case inside an error union. See [Your own Result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

```csharp
StockResult<Reservation> reserved = await stock.ReserveAsync(sku, quantity);
reserved.Should().BeError<OutOfStock>().Which.Sku.Should().Be("KB-1");
```

A `default` result counts as an error, so `BeError()` passes for it and `BeOk()` fails.

## Documentation

- [AwesomeAssertions documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/awesome-assertions/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
