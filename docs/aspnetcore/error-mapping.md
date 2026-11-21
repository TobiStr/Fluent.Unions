# Error mapping

This page covers how an error becomes an HTTP status code and a `ProblemDetails` body, and how to change that mapping.

## Default status codes

The status code comes from the type of the exception. Without configuration:

| Exception | Status |
| --- | --- |
| `ValidationError`, `ValidationErrors`, also as the `InnerException` of another error | 400, as `ValidationProblemDetails` with every message under `errors` |
| `KeyNotFoundException` | 404 |
| `UnauthorizedAccessException` | 403 |
| `TimeoutException` | 504 |
| anything else | 500 |

The defaults are deliberately narrow. `ArgumentException`, `InvalidOperationException` and `AggregateException` are not mapped, because they usually signal a bug or an infrastructure failure, not a bad request: an exception thrown inside a lambda becomes the error, and it must not turn into a 400 that shows internal text to the client.

`ToHttpResult` and the [endpoint filter](endpoint-filter.md) both use this table.

## Changing the mapping

`ResultHttpOptions` is configured with the options pattern. `Map<TException>(statusCode, exposeMessage)` returns the options, so calls chain:

```csharp
using Fluent.Unions.AspNetCore;

builder.Services.Configure<ResultHttpOptions>(o => o
    .Map<OrderShippedException>(StatusCodes.Status409Conflict, exposeMessage: true)
    .Map<ValidationErrors>(StatusCodes.Status422UnprocessableEntity));
```

A new mapping applies to the exception type and all its subclasses. Mapping an exception that already has a default replaces the default.

```csharp
// GET /shipped: an OrderShippedException error
// 409, "detail": "Order 7 has shipped."
```

If `ResultHttpOptions` is not configured, the defaults above apply.

### The most specific type wins

The lookup starts at the exception's own type and walks up its base types. The first mapping it finds wins. A mapping for a subclass overrides the mapping for its base:

```csharp
builder.Services.Configure<ResultHttpOptions>(o => o
    .Map<DomainException>(400, exposeMessage: true)
    .Map<OrderNotFoundException>(404, exposeMessage: true)
    .Map<UnexpectedDomainException>(500));
```

| Error | Status |
| --- | --- |
| `OrderNotFoundException : DomainException` | 404, the specific mapping |
| `CreditLimitExceededException : DomainException` | 400, the mapping of `DomainException` |
| `UnexpectedDomainException : DomainException` | 500, mapped explicitly, without `detail` |

Map the exception your `FromException` creates for unexpected failures, as `UnexpectedDomainException` here, to 500 without `exposeMessage`. Otherwise it inherits the mapping of `DomainException` and answers 400 with its message. Give that exception a fixed message and keep the original as `InnerException`, as in [Custom error types](../errors/custom-error-types.md).

## What the response contains

An error becomes `ProblemDetails` with the mapped status code as `status`.

- **`detail` only when you allow it.** The exception message is written to `detail` only for a type mapped with `exposeMessage: true`. The built-in mappings for 404, 403 and 504 do not expose it, and unmapped errors (500) never do. Validation errors always list their messages, because they are written for the client.
- **No stack trace, ever.** Neither the stack trace nor the exception type is written.

```csharp
((Result<int>)new KeyNotFoundException("No order 7")).ToHttpResult();
// 404, no "detail"

((Result<int>)new OrderShippedException("Order 7 has shipped.")).ToHttpResult();
// 409, "detail": "Order 7 has shipped." (mapped with exposeMessage: true)

((Result<int>)new InvalidOperationException("Connection string secret")).ToHttpResult();
// 500, no "detail"
```

Every response with a status of 500 or more is logged at `Error` level with the exception, under the category `Fluent.Unions.AspNetCore`, because the response does not tell you what went wrong. Log the error yourself only when you need more context, such as the order id:

```csharp
LoadOrderAsync(id).OnErrorDo(e => logger.LogError(e, "Loading order {Id} failed", id)).ToHttpResult();
```

The mapped exception is logged, so a 500 for a mapped `ResultErrorException` or an unmapped exception always leaves a trace. A faulted `Task<Result<T>>` is not converted: the exception goes through ASP.NET Core's exception handling, which logs it.

## Validation errors

`Ensure(predicate, message)` and the ready-made rules fail with a `ValidationError`, and `Validate` collects every failure in one `ValidationErrors`. Both become a `ValidationProblemDetails` with the messages grouped by member name:

```csharp
Result<SignUp> result = Result.From(form).Validate(
    f => f.Email.EnsureMatchesEmail(),
    f => f.Age.EnsureInRange(18, 120));

result.ToHttpResult();
// 400, "errors": { "Email": ["Email must be a valid email address."], "Age": ["Age must be between 18 and 120."] }
```

- **One shape for one failure or many.** A single `ValidationError` gives the same body with one entry.
- **Messages without a member** use the key `""`.
- **Change the status** with `Map<ValidationErrors>(422)` and `Map<ValidationError>(422)`; the body stays a validation problem.
- **A wrapped validation error stays a validation problem.** An error whose `InnerException` is a `ValidationError` or `ValidationErrors`, as `FromException` of your own result type usually makes it, is written as a validation problem with the status of the inner validation error. The wrapper's own message is not shown.
- **A mixed failure is not a validation problem.** When a rule throws another exception, `Validate` returns an `AggregateException`, which gives 500 without details.

## ProblemDetails integration

The response is written with ASP.NET Core's `Results.Problem` and `Results.ValidationProblem`. When you call `AddProblemDetails()`, the body goes through `IProblemDetailsService`, so its customization applies to the errors too:

```csharp
builder.Services.AddProblemDetails(o =>
    o.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["service"] = "shop");
```

A 409 from the example above then looks like this:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Order 7 has shipped.",
  "traceId": "00-...",
  "service": "shop"
}
```

Without `AddProblemDetails()`, errors are still written as `ProblemDetails`. Only the customization is missing.

## Errors as data

A declared result type with a record error has no status table, because the error is not an exception. You choose the response for each case, and the `switch` makes the compiler check that none is missing. `ResultHttpOptions` does not apply to these errors:

```csharp
using Microsoft.AspNetCore.Http;
using Fluent.Unions.AspNetCore;

app.MapGet("/checkout/{id}", (int id) => Checkout(id).ToHttpResult(error => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    PaymentDeclined declined => TypedResults.Problem(declined.Reason, statusCode: StatusCodes.Status402PaymentRequired),
    Unexpected => TypedResults.StatusCode(500)
}));
```

You can still produce `ProblemDetails` for a case with `TypedResults.Problem`, as the `PaymentDeclined` arm does. The rule about hiding messages for server errors is then yours to keep: do not put `Unexpected.Exception.Message` into a 500 response.

Add a case to `CheckoutError` and the `switch` no longer compiles until you map it. See [Errors as data](../errors/errors-as-data.md) for the type.

## Pitfalls

- **Exposed messages are public.** Only pass `exposeMessage: true` for exception types whose messages you write for clients.
- **The mapping is by exception type, not by `Result` type.** A custom `DomainException` hierarchy is mapped with one `Map<DomainException>` call and refined with `Map<NotFoundException>`.
- **Validation messages are public.** The messages of `ValidationError` reach the client. The built-in rules never echo the value itself.
- **Errors as data bypass the options in `ToHttpResult(onError)`.** `Configure<ResultHttpOptions>` has no effect there. The [endpoint filter](endpoint-filter.md#with-your-own-result-type) wraps such an error in a `ResultErrorException`, which the options map.

## See also

- [HTTP results](http-results.md): where `ToHttpResult` is called.
- [Custom error types](../errors/custom-error-types.md): a `DomainException` hierarchy to map.
- `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs` in the repo: the status codes and the validation problems as tests.

---

Back to: [ASP.NET Core](README.md) · Next: [Entity Framework Core](../entity-framework-core/README.md)
