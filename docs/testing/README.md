# Testing

The packages `Fluent.Unions.Shouldly`, `Fluent.Unions.TUnit`, `Fluent.Unions.NUnit` and `Fluent.Unions.Xunit` add assertions for `Result`, `Result<T>` and `Option<T>` to Shouldly, TUnit, NUnit and xUnit v3. Each package offers the same vocabulary in the style of its host library, and a failure message names the actual state. For AwesomeAssertions and FluentAssertions see [AwesomeAssertions](../awesome-assertions/README.md) and [FluentAssertions](../fluent-assertions/README.md).

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): the same check in all four libraries.
- [Vocabulary](#vocabulary): what each package offers.
- [Shouldly](#shouldly): extension methods that return the value.
- [TUnit](#tunit): generated assertions on `Assert.That`.
- [NUnit](#nunit): constraints from `IsResult` and `IsOption`.
- [xUnit](#xunit): static `ResultAssert` and `OptionAssert`.
- [With your own result type](#with-your-own-result-type): what changes for a declared result type.
- [Failure messages](#failure-messages): what a failed check says.
- [When to use](#when-to-use): where it helps.
- [Rules](#rules): `default` values, returned values and limits.

## Quick example

```csharp
Result<Order> placed = await orders.PlaceAsync(request);

placed.ShouldBeOk().Total.ShouldBe(49.90m);                                      // Shouldly
Order order = await Assert.That(placed).IsOk();                                   // TUnit
Assert.That(placed, IsResult.OkWith(Has.Property("Total").EqualTo(49.90m)));      // NUnit
Order ordered = ResultAssert.Ok(placed);                                          // xUnit
// every line passes when the order was placed
```

## Vocabulary

| Check | Shouldly | TUnit | NUnit | xUnit |
| --- | --- | --- | --- | --- |
| Ok | `ShouldBeOk()` | `IsOk()` | `IsResult.Ok` | `ResultAssert.Ok(result)` |
| Ok with a value check | `ShouldBeOk()` returns the value | `IsOk()` returns the value | `IsResult.OkWith(constraint)` | `ResultAssert.Ok(result)` returns the value |
| Error | `ShouldBeError()` | `IsError()` | `IsResult.Error` | `ResultAssert.Error(result)` |
| Error of a type | `ShouldBeError<TException>()` | `IsError<TException>()` | `IsResult.ErrorOfType<TError>()` | `ResultAssert.Error<TException>(result)` |
| Error with a check | `ShouldBeError()` returns the exception | `IsError()` returns the exception | `IsResult.ErrorWith(constraint)` | `ResultAssert.Error(result)` returns the exception |
| Some | `ShouldBeSome()` | `IsSome()` | `IsOption.Some` | `OptionAssert.Some(option)` |
| Some with a value check | `ShouldBeSome()` returns the value | `IsSome()` returns the value | `IsOption.SomeWith(constraint)` | `OptionAssert.Some(option)` returns the value |
| None | `ShouldBeNone()` | `IsNone()` | `IsOption.None` | `OptionAssert.None(option)` |

## Shouldly

Package `Fluent.Unions.Shouldly`, namespace `Fluent.Unions.Shouldly`. The methods are extensions on `Result`, `Result<T>` and `Option<T>`. Each takes an optional custom message, which a failure appends. `ShouldBeOk()` and `ShouldBeSome()` return the value and `ShouldBeError()` returns the exception, so a test continues with Shouldly's own checks.

```csharp
using Shouldly;
using Fluent.Unions.Shouldly;

Result<Order> missing = await orders.FindAsync(unknownId);
missing.ShouldBeError().ShouldBeOfType<NotFoundException>().Message.ShouldContain("was not found");
// passes when the error is a NotFoundException with that message

Result removed = await orders.DeleteAsync(unknownId);
removed.ShouldBeError<NotFoundException>();
// passes when the error is a NotFoundException or a derived type
```

On a `Result<T>`, `ShouldBeError<TException>()` binds to the overload for any result type (an extension on `IUnion`). It passes for the same exception type or a derived type and returns it, like the overload on `Result`.

```csharp
Result<Order> missing = await orders.FindAsync(unknownId);
missing.ShouldBeError<NotFoundException>().Message.ShouldContain("was not found");
```

## TUnit

Package `Fluent.Unions.TUnit`. The assertions use the TUnit source generator: a method marked with `[GenerateAssertion]` becomes an extension on `Assert.That`. The generated methods live in the namespace `TUnit.Assertions.Extensions`, which TUnit projects import globally. Awaiting `IsOk()`, `IsError()` or `IsSome()` returns the value or the exception.

```csharp
Result<Order> placed = await orders.PlaceAsync(request);
Order order = await Assert.That(placed).IsOk();
// passes when the order was placed, and returns it

Result removed = await orders.DeleteAsync(unknownId);
NotFoundException error = await Assert.That(removed).IsError<NotFoundException>();
// passes when the error is a NotFoundException, and returns it

await Assert.That(customer.Nickname).IsNone();
// passes when the customer has no nickname
```

A chained `And` continues on the result itself and not on the returned value. Await the assertion to get the value, then assert on it. `IsError<TException>()` exists on `Result` only. On a `Result<T>`, await `IsError()` and check the returned exception with `IsTypeOf`, or assign the result to a `Result` variable first.

The package depends on `TUnit.Assertions` only, so the assertions also run in a test project of another framework. A TUnit test project needs the Microsoft.Testing.Platform runner mode of `dotnet test` on .NET 10 SDK and later, which is set in `global.json`.

## NUnit

Package `Fluent.Unions.NUnit`, namespace `Fluent.Unions.NUnit`. `Is` is a static class of NUnit and cannot be extended, so the constraints come from `IsResult` and `IsOption`. They return a `UnionConstraint` for `Assert.That`, which composes with `&`, `|` and `And` as usual. `OkWith`, `ErrorWith` and `SomeWith` take any NUnit constraint for the value or the exception. The package targets NUnit 4 and later.

```csharp
using Fluent.Unions.NUnit;

Result<Order> placed = await orders.PlaceAsync(request);
Assert.That(placed, IsResult.OkWith(Has.Property("Total").EqualTo(49.90m)));
// passes when the order was placed with that total

Result<Order> missing = await orders.FindAsync(unknownId);
Assert.That(missing, IsResult.ErrorWith(Has.Message.Contains("was not found")));
Assert.That(missing, IsResult.ErrorOfType<NotFoundException>());
// passes when the error has that message and type

Assert.That(customer.Nickname, IsOption.SomeWith(Is.EqualTo("ada")));
// passes when the customer has the nickname "ada"
```

A value that is not a `Result`, `Result<T>` or `Option<T>` fails with a message that says so.

## xUnit

Package `Fluent.Unions.Xunit`, namespace `Fluent.Unions.Xunit`. The assertions are static methods of `ResultAssert` and `OptionAssert`, on xUnit v3 (`xunit.v3.assert`). They are not members of `Assert`: the partial `Assert` class of `xunit.v3.assert.source` is compiled into each test project, so a library cannot add to it. Each method returns the value or the exception.

```csharp
using Fluent.Unions.Xunit;

Result<Order> placed = await orders.PlaceAsync(request);
Order order = ResultAssert.Ok(placed);
// passes when the order was placed, and returns it

NotFoundException error = ResultAssert.Error<NotFoundException>(missing);
// passes when the error is a NotFoundException, and returns it

string nickname = OptionAssert.Some(customer.Nickname);
// passes when the customer has a nickname, and returns it
```

`ResultAssert.Error<TException>(result)` takes a `Result`, and a `Result<T>` converts to it implicitly, so it works for both. Inside the namespace `Fluent.Unions.Xunit` the name `Xunit` refers to that namespace, so write `global::Xunit.Assert` or an alias there.

## With your own result type

A result type you declare with `[FluentResult<TError>]` (see [Your own Result type](../errors/custom-error-types.md)) works in all four packages, with these differences.

- **Shouldly and xUnit.** `ShouldBeOk`, `ShouldBeError`, `ResultAssert.Ok` and `ResultAssert.Error` accept any result type, with a value and without (the value-less `Result`), with the same messages. The untyped form returns your declared error type. When that is an error union, `result.ShouldBeError<NotFound>()` and `ResultAssert.Error<NotFound>(result)` return the case, or the declared error type itself, and fail for another case.
- **TUnit.** Assert through `AsResult()`, because C# does not allow the result interface as a type argument: `await Assert.That(result.AsResult()).IsOk()` and `.IsError()`. The default types keep `Assert.That(result).IsOk()`. For an error union, `result.AsResult().ErrorCase<NotFound>()` returns an `Option<NotFound>`: `var notFound = await Assert.That(result.AsResult().ErrorCase<NotFound>()).IsSome()`. The assertion methods behind `Assert.That(...)` are not public extensions, so `result.IsOk();` does not compile and cannot silently assert nothing.
- **NUnit.** The `IsResult` constraints recognize any result type. An error that is not an exception prints as `failed with Type: ...`. `IsResult.ErrorOfType<TError>()` and `IsResult.ErrorWith(...)` look inside an error union: `ErrorOfType<NotFound>()` passes for `AppError(NotFound)`, and the constraint of `ErrorWith` applies to the case.

## Failure messages

A failed check names the state it found.

| Check | Message |
| --- | --- |
| Ok on an error | `Expected an ok result, but it failed with TimeoutException: too slow` |
| Error on an Ok | `Expected an error result, but it was ok with value 42` |
| Error of a type on another error | `Expected an error of type ArgumentException, but it failed with TimeoutException: too slow` |
| Some on none | `Expected an option with a value, but it was none` |
| None on some | `Expected an option without a value, but it was some with value ada` |

NUnit prints `Expected: an ok result` and `But was: failed with TimeoutException: too slow`. TUnit prints `to be an ok result` and `it failed with TimeoutException: too slow`.

## When to use

- You test code that returns `Result`, `Result<T>` or `Option<T>` and want a failure to say which state it was, instead of `IsSuccess` being false.
- You want the value or the exception back to keep asserting on it.
- You use AwesomeAssertions or FluentAssertions. Then see [AwesomeAssertions](../awesome-assertions/README.md) or [FluentAssertions](../fluent-assertions/README.md).
>>>>


## Rules

- **`default` is an error.** A `default` result passes the error checks and fails the Ok checks, like everywhere else in the library.
- **None includes `default`.** A `default` option passes the none checks and fails the some checks.
- **Cancellation is not special.** An error is checked like any other exception, including `OperationCanceledException` inside an error.
- **Not covered yet.** `Loadable<T>` has no assertions in these packages.

---

Back to index: [Docs index](../README.md)
