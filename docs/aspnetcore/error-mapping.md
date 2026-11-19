# Error mapping

This page covers how an error becomes an HTTP status code and a `ProblemDetails` body, and how to change that mapping.

## Default status codes

The status code comes from the type of the exception. Without configuration:

| Exception | Status |
| --- | --- |
| `ArgumentException` (and subclasses such as `ArgumentNullException`) | 400 |
| `AggregateException`, as produced by `Validate` | 400, with every message under `errors` |
| `KeyNotFoundException` | 404 |
| `UnauthorizedAccessException` | 403 |
| `TimeoutException` | 504 |
| anything else | 500 |

`ToHttpResult` and the [endpoint filter](endpoint-filter.md) both use this table.

## Changing the mapping

`ResultHttpOptions` is configured with the options pattern. `Map<TException>(statusCode)` returns the options, so calls chain:

```csharp
using Fluent.Unions.AspNetCore;

builder.Services.Configure<ResultHttpOptions>(o => o
    .Map<OrderShippedException>(StatusCodes.Status409Conflict)
    .Map<ValidationException>(StatusCodes.Status422UnprocessableEntity));
```

A new mapping applies to the exception type and all its subclasses. Mapping an exception that already has a default replaces the default.

```csharp
// GET /shipped: an OrderShippedException error
// 409, "detail": "Order 7 has shipped."
```

If `ResultHttpOptions` is not configured, the defaults above apply. `GetStatusCode(exception)` is public if you want to look up a code yourself.

### The most specific type wins

The lookup starts at the exception's own type and walks up its base types. The first mapping it finds wins. A mapping for a subclass overrides the mapping for its base:

```csharp
builder.Services.Configure<ResultHttpOptions>(o => o.Map<ArgumentNullException>(422));
```

| Error | Status |
| --- | --- |
| `ArgumentNullException` | 422, the specific mapping |
| `ArgumentOutOfRangeException` | 400, the mapping of `ArgumentException` |

## What the response contains

An error becomes `ProblemDetails` with the mapped status code as `status`.

- **`detail` for 4xx only.** The exception message is written to `detail` when the mapped status is 400 to 499. For any other status, including every 500 and 504, the message is not exposed. This depends on the status code, not on the exception type: an exception that you map to 409 exposes its message, and an `ArgumentException` that you map to 500 does not.
- **No stack trace, ever.** Neither the stack trace nor the exception type is written.

```csharp
((Result<int>)new KeyNotFoundException("No order 7")).ToHttpResult();
// 404, "detail": "No order 7"

((Result<int>)new InvalidOperationException("Connection string secret")).ToHttpResult();
// 500, no "detail"
```

Log server errors before you convert them, because the response will not tell you what went wrong:

```csharp
LoadOrderAsync(id).OnErrorDo(e => logger.LogError(e, "Loading order {Id} failed", id)).ToHttpResult();
```

## Validation errors

`Validate` runs every rule and collects the failures in one `AggregateException`, so there is always the same error type. The mapping is on the `AggregateException`, so the status is 400 by default. Each inner exception message is listed under `errors`:

```csharp
var result = ((Result<int>)1).Validate(
    _ => new ValidationException("A"),
    _ => new ValidationException("B"));

result.ToHttpResult();
// 400, "detail": "One or more errors occurred. (A) (B)", "errors": ["A", "B"]
```

- **Map `AggregateException`, not the inner type.** `Map<ValidationException>(422)` does not change a `Validate` response, because the error is the aggregate. To change the status of validation failures, map `AggregateException`.
- **`errors` follows the status.** The list is written only when the mapped status is 4xx. If you map `AggregateException` to 500, neither `detail` nor `errors` appear.
- **`detail` repeats the messages.** It holds the aggregate's own message, which includes every inner message. Clients should read `errors`.

## ProblemDetails integration

The response is written with ASP.NET Core's `TypedResults.Problem`. When you call `AddProblemDetails()`, the body goes through `IProblemDetailsService`, so its customization applies to the errors too:

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

`Result<T, TError>` has no status table, because an error is a record and not an exception. You choose the response for each case, and the `switch` makes the compiler check that none is missing. `ResultHttpOptions` does not apply to these errors:

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

- **Messages of 4xx errors are public.** Do not put internal details into the message of an exception you map to 4xx.
- **The mapping is by exception type, not by `Result` type.** A custom `DomainException` hierarchy is mapped with one `Map<DomainException>` call and refined with `Map<NotFoundException>`.
- **Map `AggregateException`, not `ValidationException`,** to change what `Validate` returns.
- **Errors as data bypass the options in `ToHttpResult(onError)`.** `Configure<ResultHttpOptions>` has no effect there. The [endpoint filter](endpoint-filter.md#custom-result-types) wraps such an error in a `ResultErrorException`, which the options map.

## See also

- [HTTP results](http-results.md): where `ToHttpResult` is called.
- [Custom error types](../errors/custom-error-types.md): a `DomainException` hierarchy to map.
- `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs` in the repo: the status codes and the `errors` list as tests.

---

Back to: [ASP.NET Core](README.md) · Next: [Entity Framework Core](../entity-framework-core/README.md)
