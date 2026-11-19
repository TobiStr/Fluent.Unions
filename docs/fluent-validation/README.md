# FluentValidation

The package `Fluent.Unions.FluentValidation` (namespace `Fluent.Unions.FluentValidation`) runs a [FluentValidation](https://docs.fluentvalidation.net) validator and returns the outcome as a `Result<T>`, so an existing validator plugs into a result pipeline and into the HTTP 400 mapping.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): validate a command and a result.
- [What you get back](#what-you-get-back): the error shape for each state.
- [Custom result types](#custom-result-types): validate your own `IResult` type.
- [When to use](#when-to-use): where it helps.
- [Rules](#rules): name clash, severity and what is not covered.

## Quick example

```csharp
using FluentValidation;
using Fluent.Unions.FluentValidation;

public class PlaceOrderValidator : AbstractValidator<PlaceOrder>
{
    public PlaceOrderValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.Quantity).GreaterThan(0);
    }
}

var validator = new PlaceOrderValidator();

Result<PlaceOrder> command = validator.ValidateToResult(new PlaceOrder(Guid.Empty, 0));
// error: AggregateException with two ValidationException

Result<PlaceOrder> parsed = ParseCommand(request);
Result<PlaceOrder> checkedCommand = await parsed.ValidateAsync(validator, cancellationToken);
// parsed with an error: the same error, the validator is not called
```

An endpoint that returns the first result with `ToHttpResult()` answers 400 with both messages in `errors`, because the error is an `AggregateException` (see [Error mapping](../aspnetcore/error-mapping.md)).

## What you get back

| Input | Result |
| --- | --- |
| valid instance | the instance as `Result<T>` |
| one or more failures | error: an `AggregateException` with one `FluentValidation.ValidationException` per failure |
| `Result<T>` with an error | the same error, the validator is not called |
| empty `Result<T>` | empty, the validator is not called |
| `null` instance | `ArgumentNullException` |

The aggregate is always flattened, also for a single failure, the same as `Validate`. Each inner exception carries the failure in its `Errors`, so `PropertyName`, `ErrorCode`, `AttemptedValue` and `Severity` stay available.

| Member | On |
| --- | --- |
| `ValidateToResult(instance)` | `IValidator<T>` |
| `ValidateToResultAsync(instance, cancellationToken)` | `IValidator<T>` |
| `Validate(validator)` | `Result<T>` |
| `ValidateAsync(validator, cancellationToken)` | `Result<T>` |

## Custom result types

`Validate` and `ValidateAsync` also work on a type that implements `IResult<TSelf, T, TError>`, including `Result<T, TError>`. The concrete `Result<T>` overloads still apply to `Result<T>`. They return your own type and call the validator only for a value.

| Member | A failure becomes |
| --- | --- |
| `Validate(validator)`, `ValidateAsync(validator, cancellationToken)` | `TSelf.FromError(TSelf.FromException(aggregate))`, with the same `AggregateException` of `ValidationException` as for `Result<T>` |
| `Validate(validator, map)`, `ValidateAsync(validator, map, cancellationToken)` | `TSelf.FromError(map(failures))`, where `map` receives every `ValidationFailure` |

```csharp
CheckoutResult<PlaceOrder> command = ParseCommand(request);

CheckoutResult<PlaceOrder> validated = command.Validate(
    validator,
    failures => new InvalidOrder(failures.Select(f => f.ErrorMessage).ToList()));
// invalid: the error holds an InvalidOrder with every message
```

Use the overload without `map` when `FromException` can hold an exception. Use the `map` overload when the error type is data. An error or empty result passes through and the validator is not called.

## When to use

- You already have FluentValidation validators and your handlers return `Result<T>`.
- You want validation failures to become a 400 response without a `try`/`catch`.
- You want every failure in one result, like [Validate](../validation/README.md#validate).

## Rules

- **Name clash.** `ValidationException` exists in FluentValidation and in `System.ComponentModel.DataAnnotations`, which the rules in [Validation](../validation/README.md) use. Alias one of them when a file needs both: `using FluentValidationException = FluentValidation.ValidationException;`.
- **Every failure counts.** A failure with `Severity.Warning` or `Severity.Info` also makes the result an error, as `IsValid` does.
- **Check the type, not the count.** The error is an `AggregateException` even for one failure. Use `GetError()` and read `InnerExceptions`.
- **The map must not return `null`.** The `map` overload throws an `InvalidOperationException` for a `null` error. An exception from `map` is not captured.
- **Options are not covered.** `WhenSome` for `Option<T>` properties is not part of the package yet.

---

Back to index: [Docs index](../README.md)
