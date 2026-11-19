# Validation

Validation reports every problem at once, where `Combine` and `Chain` stop at the first. This page covers `Validate` for one value, `CombineAll` for many results, `Partition` for reporting, and how a validation failure becomes an HTTP 400 response.

## In this section

This folder has one page. The topics are below.

- [Validate](#validate): run every rule on one value.
- [CombineAll](#combineall-for-many-inputs): collect every error from many results.
- [Partition](#partition-for-reporting): split values and errors without failing.
- [A realistic command](#a-realistic-command): validation in a handler.
- [HTTP 400](#http-400-with-an-errors-list): the mapping in the ASP.NET Core package.
- [Comparison with Combine](#comparison-with-combine)

## Quick example

```csharp
using System.ComponentModel.DataAnnotations;
using Fluent.Unions;
using Fluent.Unions.Extensions;

Result<PlaceOrder> command = ParseCommand(dto).Validate(
    c => c.Quantity > 0 ? Result.Ok : new ValidationException("Quantity must be positive."),
    c => c.Email.Contains('@') ? Result.Ok : new ValidationException("Email is invalid."));
// error: AggregateException with both messages, when both rules fail
```

## When to use

- User input, where the caller should see every problem in one response.
- A value that must satisfy several independent rules.
- A list of items where each is parsed or checked on its own, and the report should list all failures.

Use `Chain` instead when a step depends on the previous one, such as loading a customer before checking its credit.

## Rules

- Every rule runs. There is no short circuit.
- A failed validation is always an `AggregateException`, even when only one rule failed. Catch that one type.
- A rule that throws, or returns an empty result, counts as a failure.
- Errors and empty results pass through without running the rules.

## Validate

`Validate(params Func<T, Result>[] rules)` runs every rule on the value of a `Result<T>`. A rule returns `Result.Ok` to pass, or an exception to fail. The exception converts to a failed `Result` implicitly.

```csharp
Result<PlaceOrder> validated = Result<PlaceOrder>.Ok(command).Validate(
    c => c.Lines.Count > 0 ? Result.Ok : new ValidationException("The order needs a line."),
    c => c.Email.Contains('@') ? Result.Ok : new ValidationException("Email is invalid."));
```

What you get back:

| Input | Output |
| --- | --- |
| ok, every rule passes | the same result |
| ok, one or more rules fail | error: `AggregateException` with every failure |
| error | the same error, rules not run |
| empty | empty, rules not run |

Because the type is always `AggregateException`, one handler works for one failure or many:

```csharp
validated.Catch((AggregateException e) => LogAndTranslate(e.InnerExceptions));
```

**Flattening.** Nested `AggregateException`s are flattened into one list. A rule can itself return a failed validation of a sub-object, and its errors join the list.

```csharp
Result<PlaceOrder> validated = Result<PlaceOrder>.Ok(command).Validate(
    c => Result<Address>.Ok(c.Address).Validate(CityIsSet, ZipIsSet), // Result<Address> converts to Result
    c => c.Lines.Count > 0 ? Result.Ok : new ValidationException("The order needs a line."));
// one AggregateException, holding the address errors and the line error side by side
```

To reuse rules, pass named methods:

```csharp
static Result HasLines(PlaceOrder c) =>
    c.Lines.Count > 0 ? Result.Ok : new ValidationException("The order needs a line.");

static Result HasValidEmail(PlaceOrder c) =>
    c.Email.Contains('@') ? Result.Ok : new ValidationException("Email is invalid.");

Result<PlaceOrder> validated = Result<PlaceOrder>.Ok(command).Validate(HasLines, HasValidEmail);
```

**Throwing and empty rules.** A rule that throws is a failure, and its exception is added to the list. A rule that returns `default` (an empty result) is a failure too, with an `InvalidOperationException("A validation rule returned an empty result.")`.

**Null rules.** Passing `null` as the rules array, or a `null` rule, throws `ArgumentNullException`.

**Tasks.** `Validate` works on `Task<Result<T>>`, so it can follow an asynchronous step. The rules are synchronous.

```csharp
Result<PlaceOrder> order = await ParseCommandAsync(request).Validate(HasLines, HasValidEmail);
```

## CombineAll for many inputs

`Validate` checks one value against many rules. `CombineAll` checks many results. It gives all values when every result is ok, or every error in an `AggregateException`. Empty results are skipped.

```csharp
Result<IReadOnlyList<OrderLine>> lines = dto.Lines
    .Select(ParseLine)          // Result<OrderLine> for each
    .CombineAll();
// ok with all lines, or an AggregateException with an error per bad line
```

Nested `AggregateException`s are flattened here as well.

## Partition for reporting

`Partition()` splits the results into `(Values, Errors)` in one pass and never fails. Use it when you want to keep the good items and report the bad ones.

```csharp
var (lines, errors) = dto.Lines.Select(ParseLine).Partition();

if (errors.Count > 0)
    logger.LogWarning("{Count} lines were skipped: {Messages}", errors.Count, errors.Select(e => e.Message));

await orders.SaveAsync(lines);
```

Empty results are skipped.

## A realistic command

A handler that parses a request, validates it as a whole, and only then runs the business logic:

```csharp
public record PlaceOrder(Guid CustomerId, string Email, IReadOnlyList<OrderLine> Lines);
public record OrderLine(string Sku, int Quantity);

static Result<PlaceOrder> ValidateCommand(PlaceOrder command) =>
    Result<PlaceOrder>.Ok(command).Validate(
        c => c.CustomerId != Guid.Empty ? Result.Ok : new ValidationException("Customer is required."),
        c => c.Email.Contains('@') ? Result.Ok : new ValidationException("Email is invalid."),
        c => c.Lines.Count > 0 ? Result.Ok : new ValidationException("The order needs at least one line."),
        c => c.Lines.All(l => l.Quantity > 0) ? Result.Ok : new ValidationException("Every quantity must be positive."));

static Task<Result<Receipt>> HandleAsync(PlaceOrder command) =>
    ValidateCommand(command)
        .ChainAwait(valid => PlaceOrderAsync(valid));
```

If the command has an invalid email and no lines, `HandleAsync` returns an error whose `AggregateException` holds two `ValidationException`s. `PlaceOrderAsync` does not run.

## HTTP 400 with an errors list

With the `Fluent.Unions.AspNetCore` package, an `AggregateException` maps to status 400 by default. The response is ProblemDetails with every message in an `errors` array.

```csharp
app.MapPost("/orders", (PlaceOrder command) => HandleAsync(command).ToHttpResult());
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more errors occurred. (Email is invalid.) (The order needs at least one line.)",
  "errors": [ "Email is invalid.", "The order needs at least one line." ],
  "traceId": "0HNQ5NHDL69CD"
}
```

The `errors` array exists only for client errors (4xx) whose error is an `AggregateException`. If you remap `AggregateException` to a 5xx status, the messages are not exposed. See [ASP.NET Core](../aspnetcore/README.md) for the mapping options.

## Comparison with Combine

| Operator | Input | On failure |
| --- | --- | --- |
| `Validate(rules)` | one `Result<T>` | `AggregateException` with every failed rule |
| `CombineAll()` | many results | `AggregateException` with every error |
| `Combine()` | many results | the first error, as is |
| `Chain(next)` | one result | the first error, as is, later steps skipped |

`Combine` is the right choice when the first error is enough, for example when loading dependencies that cannot work without each other. It returns the original exception, not an `AggregateException`. `Validate` and `CombineAll` are for reporting.

## Pitfalls

- A validation failure is an `AggregateException` even for one rule. Code that catches `ValidationException` directly will not see it. Use `Catch((AggregateException e) => ...)` and read `InnerExceptions`.
- Rules run on the same value, in order, without short circuit. Do not write a rule that depends on an earlier rule having passed. Guard against `null` inside the rule, or validate in two steps.
- A rule that throws is reported, not rethrown.
- Rules are synchronous. For asynchronous checks, use `ChainAwait` or `Task.WhenAll` with `CombineAll`.

## See also

- [Option](../option/README.md)
- [Async: concurrency](../async/concurrency-and-resilience.md#collecting-all-errors)
- [ASP.NET Core](../aspnetcore/README.md)

---

Back to index: [Docs index](../README.md) · Next: [Serialization](../serialization/README.md)
