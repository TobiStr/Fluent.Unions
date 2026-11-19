# HTTP results

This page covers `ToHttpResult`, the extension methods in `Fluent.Unions.AspNetCore` that turn a result or an option into an `IResult` for a minimal API endpoint or a controller action.

## Overloads

| Receiver | Value | Error | Empty or `None` |
| --- | --- | --- | --- |
| `Result<T>` | 200 with the value | `ProblemDetails`, status from [error mapping](error-mapping.md) | 404 |
| `Result` | 204 | `ProblemDetails` | 404 |
| `Option<T>` | 200 with the value | not applicable | 404 |
| `Result<T, TError>` with `onError` | 200 with the value | the response from `onError` | 404 |

Each of them has a `Task` overload, so a pipeline stays one expression:

| Receiver | Task overload |
| --- | --- |
| `Task<Result<T>>` | `ToHttpResult()` |
| `Task<Result>` | `ToHttpResult()` |
| `Task<Option<T>>` | `ToHttpResult()` |
| `Task<Result<T, TError>>` | `ToHttpResult(onError)` |

The value is written as JSON with `TypedResults.Ok`. The 404 for an empty result has no body.

## Result and Option

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", async (Guid id, IOrders orders) => await orders.LoadOrderAsync(id).ToHttpResult());
app.MapDelete("/orders/{id}", (Guid id, IOrders orders) => orders.CancelAsync(id).ToHttpResult());
app.MapGet("/products/{id}", (int id, IOrders orders) => orders.FindProductAsync(id).ToHttpResult());
```

- `LoadOrderAsync` returns `Task<Result<Order>>`. A found order is 200, a `KeyNotFoundException` error is 404 with `ProblemDetails`.
- `CancelAsync` returns `Task<Result>`. Success is 204 with no body.
- `FindProductAsync` returns `Task<Option<Product>>`. `None` is 404. An option has no error state, so it never gives `ProblemDetails`.

Handlers can return the `Task<IResult>` directly. The `await` in the first line is optional: `(Guid id, IOrders o) => o.LoadOrderAsync(id).ToHttpResult()` returns a `Task<IResult>`, which minimal APIs await.

## A faulted task

The `Task<Result<T>>` and `Task<Result>` overloads treat a faulted task as an error result. An exception that escapes the operation becomes `ProblemDetails`, usually 500 without a message:

```csharp
Task.FromException<Result<int>>(new InvalidOperationException("secret")).ToHttpResult();
// 500, ProblemDetails without a detail
```

The `Task<Option<T>>` and `Task<Result<T, TError>>` overloads do not settle the task. A faulted task there throws as usual and the exception goes through ASP.NET Core's exception handling.

## Errors as data

`Result<T, TError>` has no default mapping, because an error is a record and not an exception. Pass `onError`, and a `switch` makes the compiler check every case:

```csharp
app.MapGet("/checkout/{id}", (int id) => Checkout(id).ToHttpResult(error => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    PaymentDeclined declined => TypedResults.Problem(declined.Reason, statusCode: StatusCodes.Status402PaymentRequired),
    Unexpected => TypedResults.StatusCode(500)
}));

app.MapGet("/checkout-async/{id}", async (int id) => await Task.FromResult(Checkout(id)).ToHttpResult(ToResponse));
```

Pass a method group when several endpoints share the mapping:

```csharp
static IResult ToResponse(CheckoutError error) => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    PaymentDeclined => TypedResults.StatusCode(402),
    Unexpected => TypedResults.StatusCode(500)
};
```

`onError` must not be `null`: `ToHttpResult` throws `ArgumentNullException`. The arms may return different `TypedResults` types, because `onError` returns `IResult`. See [Error mapping](error-mapping.md#errors-as-data) for more.

## Controllers

ASP.NET Core MVC executes an `IResult` returned from an action, so the same extension works in a controller:

```csharp
using Microsoft.AspNetCore.Mvc;
using Fluent.Unions;
using Fluent.Unions.AspNetCore;
using Fluent.Unions.Extensions;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrders orders) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IResult> Get(Guid id) => await orders.LoadOrderAsync(id).ToHttpResult();

    [HttpPost]
    public IResult Create([FromBody] string input) => orders.Parse(input)
        .Validate(
            o => o.Number.Length > 0 ? Result.Ok : new ValidationException("Number is required."),
            o => o.Number.Length < 20 ? Result.Ok : new ValidationException("Number is too long."))
        .ToHttpResult();
}
```

An empty `input` makes `Validate` fail with an `AggregateException`, which gives 400 with the messages under `errors`. See [Error mapping](error-mapping.md#validation-errors).

## Pitfalls

- **A result is not serializable.** Do not return a `Result<T>` from a minimal API without `ToHttpResult` or the [endpoint filter](endpoint-filter.md). `JsonSerializer` throws for it.
- **Empty is 404.** A `Where` that filters the value out gives an empty result, and so a 404 and not an error.
- **`Task<Result<T, TError>>` needs `onError` as well.** There is no overload without it.
- **Only 4xx messages are exposed.** A `500` never shows the exception message, so log the error before you convert it, for example with `OnErrorDo`.

## See also

- [Endpoint filter](endpoint-filter.md): return results straight from handlers.
- [Error mapping](error-mapping.md): the status code table.
- [Errors as data](../errors/errors-as-data.md): declaring `Result<T, TError>`.
- `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs` in the repo: the status codes as tests.

---

Back to: [ASP.NET Core](README.md) · Next: [Endpoint filter](endpoint-filter.md)
