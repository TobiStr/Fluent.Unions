# AwesomeAssertions

The package `Fluent.Unions.AwesomeAssertions` (namespace `Fluent.Unions.AwesomeAssertions`) adds `Should()` for `Result`, `Result<T>` and `Option<T>` to [AwesomeAssertions](https://awesomeassertions.org). It checks the state of a result, its value, and the exception inside an error.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): assert on an Ok result, an error and an option.
- [Result assertions](#result-assertions): the state and value checks.
- [Option assertions](#option-assertions): some, none and the value.
- [Error assertions](#error-assertions): the exception type and message, and aggregate errors.
- [When to use](#when-to-use): where it helps.
- [Rules](#rules): empty results, failure messages and chaining.

## Quick example

```csharp
using AwesomeAssertions;
using Fluent.Unions.AwesomeAssertions;

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

`Should()` on a `Result` returns `ResultAssertions`. On a `Result<T>` it returns `ResultAssertions<T>`. Every method takes an optional `because` text and format arguments, as in AwesomeAssertions.

| Method | Passes when | On |
| --- | --- | --- |
| `BeOk()` | the result is Ok | both. On `Result<T>` it returns the value through `Which`. |
| `BeError()` | the result is an error | both |
| `BeError<TException>()` | the error is a `TException` or a derived type | both |
| `BeError<TException>(predicate)` | the error is a `TException` and the predicate is true | both |
| `BeAggregateError()` | the error is an `AggregateException` | both |
| `BeOkWithValue(value)` | the result is Ok and holds an equal value | `Result<T>` |
| `BeOkMatching(predicate)` | the result is Ok and the value matches the predicate | `Result<T>` |
| `BeOkSatisfying(assertion)` | the result is Ok and the assertion runs on the value | `Result<T>` |
| `BeEmpty()` | the result is empty: neither a value nor an error | `Result<T>` |

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

## Error assertions

`ErrorAssertions<TException>` works on the exception inside an error. `Which` returns the exception, and `And` continues on the same assertions.

| Method | Passes when |
| --- | --- |
| `WithMessage(pattern)` | the message matches the pattern. `*` and `?` are wildcards and case is ignored. |
| `WithMessageContaining(text)` | the message contains the text, ignoring case |

`AggregateErrorAssertions` works on the flattened inner exceptions, so it fits the output of `Validate` and `CombineAll`.

| Method | Passes when |
| --- | --- |
| `WithInnerCount(count)` | there are exactly that many inner exceptions |
| `WithInner<TException>()` | an inner exception is a `TException`. `Which` returns the first match. |
| `WithInnerMessage(pattern)` | the message of any inner exception matches the pattern |

```csharp
Result<Customer> registered = Result<Customer>.Ok(new Customer("", "not-an-email"))
    .Validate(c => c.Name.EnsureNotEmpty(), c => c.Email.EnsureMatchesEmail());

registered.Should().BeAggregateError().WithInnerCount(2);
// passes when both rules failed
```

## When to use

- You test code that returns `Result`, `Result<T>` or `Option<T>` and want the failure message to name the actual state instead of `Value is Exception` checks.
- You already use AwesomeAssertions. For NUnit constraints alone, `Assert.That(result.Success, Is.True)` still works.

## Rules

- **Empty is its own state.** Every Ok and error assertion on an empty `Result<T>` fails and says the result was empty. Only `BeEmpty()` passes for it.
- **The message names the state.** A failure shows whether the result was Ok, empty or an error, with the exception type and message.
- **None includes `default`.** `BeNone()` passes for `default(Option<T>)`, and `BeSome()` fails for it.
- **Predicates are expressions.** `BeOkMatching` and `BeError<TException>(predicate)` take an `Expression<Func<..>>`, so the failure message shows the condition.
- **Not covered yet.** `Result<T, TError>` and `Loadable<T>` have no assertions.

---

Back to index: [Docs index](../README.md)
