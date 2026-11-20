# FluentValidation

The package `Fluent.Unions.FluentValidation` (namespace `Fluent.Unions.FluentValidation`) runs a [FluentValidation](https://docs.fluentvalidation.net) validator and returns the outcome as a `Result<T>`, so an existing validator plugs into a result pipeline and into the HTTP 400 mapping.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): validate a command and a result.
- [What you get back](#what-you-get-back): the error shape for each state.
- [Custom result types](#custom-result-types): validate your own `IResult` type.
- [Option properties](#option-properties): rules that run only when an option has a value.
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
// error: ValidationErrors with two ValidationError, for CustomerId and Quantity

Result<PlaceOrder> parsed = ParseCommand(request);
Result<PlaceOrder> checkedCommand = await parsed.ValidateAsync(validator, cancellationToken);
// parsed with an error: the same error, the validator is not called
```

An endpoint that returns the first result with `ToHttpResult()` answers 400 with a `ValidationProblemDetails` whose `errors` are grouped by property, because the error is a `ValidationErrors` (see [Error mapping](../aspnetcore/error-mapping.md#validation-errors)).

## What you get back

| Input | Result |
| --- | --- |
| valid instance | the instance as `Result<T>` |
| one or more failures | error: a `ValidationErrors` with one `ValidationError` per failure, `MemberName` set to the property name |
| the validator throws | error: the thrown exception (cancellation propagates) |
| `Result<T>` with an error | the same error, the validator is not called |
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
| `Validate(validator)`, `ValidateAsync(validator, cancellationToken)` | `TSelf.FromError(TSelf.FromException(errors))`, with the same `ValidationErrors` as for `Result<T>` |
| `Validate(validator, map)`, `ValidateAsync(validator, map, cancellationToken)` | `TSelf.FromError(map(failures))`, where `map` receives every `ValidationFailure` |

```csharp
CheckoutResult<PlaceOrder> command = ParseCommand(request);

CheckoutResult<PlaceOrder> validated = command.Validate(
    validator,
    failures => new InvalidOrder(failures.Select(f => f.ErrorMessage).ToList()));
// invalid: the error holds an InvalidOrder with every message
```

Use the overload without `map` when `FromException` can hold an exception. Use the `map` overload when the error type is data. An error passes through and the validator is not called.

## Option properties

`RuleForSome` starts a rule for the value of an `Option<T>` property, like `RuleFor`. The rule runs only when the option has a value, so `None` is always valid:

```csharp
using FluentValidation;
using Fluent.Unions.FluentValidation;

public record UpdateProfile(Option<string> Nickname, Option<int> Age);

public class UpdateProfileValidator : AbstractValidator<UpdateProfile>
{
    public UpdateProfileValidator()
    {
        this.RuleForSome(p => p.Nickname).NotEmpty().MaximumLength(20);
        this.RuleForSome(p => p.Age).InclusiveBetween(0, 150);
    }
}
```

| `Nickname` | Outcome |
| --- | --- |
| `None` | valid, no rule runs |
| `"Ada"` | valid |
| `""` | a failure named `Nickname` |

- **Call it with `this.`** It is an extension method on `AbstractValidator<T>`, so C# needs the receiver inside the constructor.
- **Failures are named after the property.** A nested property such as `p => p.Address.Street` gives `Address.Street`, as `RuleFor` does.
- **Every validator in the chain is skipped for `None`**, including ones added after `When` or `WithMessage`.
- **To require a value**, add a separate rule: `RuleFor(p => p.Nickname).Must(n => n.HasValue)`.

## When to use

- You already have FluentValidation validators and your handlers return `Result<T>`.
- You want validation failures to become a 400 response without a `try`/`catch`.
- You want every failure in one result, like [Validate](../validation/README.md#validate).

## Rules

- **One error type for both kinds of rules.** FluentValidation failures and the rules in [Validation](../validation/README.md) both become `ValidationError`, so the HTTP mapping and your `switch` arms treat them alike.
- **Every failure counts.** A failure with `Severity.Warning` or `Severity.Info` also makes the result an error, as `IsValid` does.
- **Check the type, not the count.** The error is a `ValidationErrors` even for one failure. Read `Errors` or `ToDictionary()`.
- **The map must not return `null`.** A `null` error, or an exception thrown by `map`, becomes the error that `FromException` makes of it.

---

Back to index: [Docs index](../README.md)
