# FluentAssertions

The package `Fluent.Unions.FluentAssertions` (namespace `Fluent.Unions.FluentAssertions`) adds `Should()` for `Result`, `Result<T>`, `Option<T>` and custom result types to [FluentAssertions](https://fluentassertions.com) version 8. It checks the state of a result, its value, and the exception inside an error.

> **License notice.** FluentAssertions version 8 and later is commercial software from Xceed. It is free for open-source projects and non-commercial use, and commercial use requires [a paid license](https://xceed.com/products/unit-testing/fluent-assertions/). The `Fluent.Unions.FluentAssertions` package itself stays under the license of Fluent.Unions, but it depends on FluentAssertions 8. If you need a free license for commercial work, use [Fluent.Unions.AwesomeAssertions](../awesome-assertions/README.md), which has the same assertions for AwesomeAssertions (Apache-2.0).

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): assert on an Ok result, an error and an option.
- [Result assertions](#result-assertions): the state and value checks.
- [Option assertions](#option-assertions): some, none and the value.
- [Custom result types](#custom-result-types): assertions for your own result type (declared with `[FluentResult<TError>]`).
- [Error assertions](#error-assertions): the exception type and message, and aggregate errors.
- [When to use](#when-to-use): where it helps.
- [Rules](#rules): `default` results, failure messages and chaining.

## Quick example

```csharp
using FluentAssertions;
using Fluent.Unions.FluentAssertions;

Result<Order> placed = await orders.PlaceAsync(request);
placed.Should().BeOk().Which.Total.Should().Be(49.90m);
// passes when the order was placed with that total

Result<Order> missing = await orders.FindAsync(unknownId);
missing.Should().BeError<NotFoundException>().WithMessage("Order * was not found");
// passes when the error is a NotFoundException with a matching message

Option<string> nickname = customer.Nickname;
nickname.Should().BeSomeWithValue("ada");
// passes when the customer set the nickname "ada"
```

## Result assertions

`Should()` on a `Result` returns `ResultAssertions`. On a `Result<T>` it returns `ResultAssertions<T>`. Every method takes an optional `because` text and format arguments, as in FluentAssertions.

| Method | Passes when | On |
| --- | --- | --- |
| `BeOk()` | the result is Ok | both. On `Result<T>` it returns the value through `Which`. |
| `BeError()` | the result is an error | both |
| `BeError<TException>()` | the error is a `TException` or a derived type | both |
| `BeError<TException>(predicate)` | the error is a `TException` and the predicate is true | both |
| `BeAggregateError()` | the error is an `AggregateException` | both |
| `BeOkWithValue(value)` | the result is Ok and holds an equal value | `Result<T>` |
>>>>

| `BeOkMatching(predicate)` | the result is Ok and the value matches the predicate | `Result<T>` |
| `BeOkSatisfying(assertion)` | the result is Ok and the assertion runs on the value | `Result<T>` |

For validation failures use `BeError<ValidationErrors>()` and read `Which.Errors`.

`BeError`, `BeError<TException>` and `BeError<TException>(predicate)` return `ErrorAssertions<TException>`. `BeAggregateError` returns `AggregateErrorAssertions`.

## Option assertions

`Should()` on an `Option<T>` returns `OptionAssertions<T>`.

| Method | Passes when |
| --- | --- |
| `BeSome()` | the option has a value, which `Which` returns |
| `BeSomeWithValue(value)` | the option has an equal value |
| `NotBeSomeWithValue(value)` | the option is none or has a different value |
| `BeSomeMatching(predicate)` | the option has a value that matches the predicate |
| `BeSomeSatisfying(assertion)` | the option has a value and the assertion runs on it |
| `BeNone()` | the option has no value. A `default` option counts as none. |

## Custom result types

With your own result type, declared with `[FluentResult<TError>]` (see [Your own Result type](../errors/custom-error-types.md)), `Should()` returns `CustomResultAssertions<TSelf, T, TError>`. This works when the type has a value. There are no assertions for the value-less `Result` of your own type. The concrete `Should()` overloads for `Result` and `Result<T>` still apply to those types.

| Method | Passes when |
| --- | --- |
| `BeOk()` | the result holds a value, which `Which` returns |
| `BeOkWithValue(value)` | the result holds an equal value |
| `BeOkMatching(predicate)` | the result holds a value that matches the predicate |
| `BeError()` | the result is an error, which `Which` returns as `TError` |
| `BeError<TCase>()` | the error is a `TCase`, or the error union holds a `TCase`. `Which` returns the `TCase`. |
| `BeError<TCase>(predicate)` | the error is a `TCase` and the predicate is true |

```csharp
StockResult<Reservation> reserved = await stock.ReserveAsync(sku, quantity);

reserved.Should().BeOk().Which.Quantity.Should().Be(2);
// passes when the reservation holds a quantity of 2

reserved.Should().BeError<OutOfStock>(error => error.Sku == "KB-1");
// passes when the error union holds an OutOfStock for that sku
```

An error that is not an exception is named in a failure message by the type of its case and its content.

## Error assertions

`ErrorAssertions<TException>` works on the exception inside an error. `Which` returns the exception, and `And` continues on the same assertions.

| Method | Passes when |
| --- | --- |
| `WithMessage(pattern)` | the message matches the pattern. `*` and `?` are wildcards and case is ignored. |
| `WithMessageContaining(text)` | the message contains the text, ignoring case |

`AggregateErrorAssertions` works on the flattened inner exceptions. `Validate`, `CombineAll` and `Combine` produce an `AggregateException` when at least one failure is not a `ValidationError`. When every failure is a `ValidationError`, they produce `ValidationErrors` instead, so use `BeError<ValidationErrors>()` and read `Which.Errors`.

| Method | Passes when |
| --- | --- |
| `WithInnerCount(count)` | there are exactly that many inner exceptions |
| `WithInner<TException>()` | an inner exception is a `TException`. `Which` returns the first match. |
| `WithInnerMessage(pattern)` | the message of any inner exception matches the pattern |

```csharp
Result<Customer> registered = Result<Customer>.Ok(new Customer("", "not-an-email"))
    .Validate(c => c.Name.EnsureNotEmpty(), c => c.Email.EnsureMatchesEmail());

registered.Should().BeError<ValidationErrors>().Which.Errors.Should().HaveCount(2);
// passes when both rules failed

Result<Customer> checkedOnline = Result<Customer>.Ok(new Customer("", "ada@example.com"))
    .Validate(c => c.Name.EnsureNotEmpty(), c => emailService.Verify(c.Email));

checkedOnline.Should().BeAggregateError().WithInner<ValidationError>();
// passes when the name rule failed and the email service returned an error that is not a ValidationError
```

## When to use

- You test code that returns `Result`, `Result<T>`, `Option<T>` or your own result type and want the failure message to name the actual state instead of `Value is Exception` checks.
- You already use FluentAssertions. For NUnit constraints alone, `Assert.That(result.IsSuccess, Is.True)` still works.

## Rules

- **`default` is an error.** A `default` result passes `BeError()` and fails `BeOk()`, like everywhere else in the library.
- **The message names the state.** A failure shows whether the result was Ok or an error, with the exception type and message.
- **None includes `default`.** `BeNone()` passes for `default(Option<T>)`, and `BeSome()` fails for it.
- **Predicates are expressions.** `BeOkMatching`, `BeSomeMatching`, `BeError<TException>(predicate)` and `BeError<TCase>(predicate)` take an `Expression<Func<..>>`, so the failure message shows the condition.
>>>>

- **Not covered yet.** `Loadable<T>` has no assertions.

---

Back to index: [Docs index](../README.md)
