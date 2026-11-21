![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.FluentAssertions

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.FluentAssertions.svg)](https://www.nuget.org/packages/Fluent.Unions.FluentAssertions) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.FluentAssertions.svg)](https://www.nuget.org/packages/Fluent.Unions.FluentAssertions) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/fluent-assertions/)

Adds `Should()` for `Result`, `Result<T>` and `Option<T>` to FluentAssertions version 8. It builds on Fluent.Unions, and a failure message names the actual state.

> **License notice.** FluentAssertions version 8 and later is commercial software from Xceed. It is free for open-source projects and non-commercial use, and commercial use requires [a paid license](https://xceed.com/products/unit-testing/fluent-assertions/). This package itself stays under the license of Fluent.Unions, but it depends on FluentAssertions 8. For commercial work without a FluentAssertions license, use [Fluent.Unions.AwesomeAssertions](https://www.nuget.org/packages/Fluent.Unions.AwesomeAssertions), which has the same assertions for AwesomeAssertions (Apache-2.0).

## Installation

```
dotnet add package Fluent.Unions.FluentAssertions
```

## Usage

Assert on an Ok result and its value.

```csharp
using FluentAssertions;
using Fluent.Unions.FluentAssertions;

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

- [FluentAssertions documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/fluent-assertions/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
