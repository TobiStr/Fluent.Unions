# HTTP results

This page covers `ToHttpResult`, the extension methods in `Fluent.Unions.AspNetCore` that turn a result or an option into an `IResult` for a minimal API endpoint or a controller action.

## Overloads

| Receiver | Value | Error or `None` |
| --- | --- | --- |
| `Result<T>` | 200 with the value | `ProblemDetails`, status from [error mapping](error-mapping.md) |
| `Result` | 204 | `ProblemDetails` |
| `Option<T>` | 200 with the value | 404 for `None` |
| your own result type, `ToHttpResult()` with `TError : Exception` | 200 with the value | `ProblemDetails`, status from [error mapping](error-mapping.md) |
| your own result type, with `onError` | 200 with the value | the response from `onError` |

Each of them except your own result type has a `Task` overload, so a pipeline stays one expression:

| Receiver | Task overload |
| --- | --- |
| `Task<Result<T>>` | `ToHttpResult()` |
| `Task<Result>` | `ToHttpResult()` |
| `Task<Option<T>>` | `ToHttpResult()` |

The value is written as JSON with `TypedResults.Ok`. The 404 for `None` has no body.

## Result and Option

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", async (Guid id, IOrders orders) => await orders.LoadOrderAsync(id).ToHttpResult());
app.MapDelete("/orders/{id}", (Guid id, IOrders orders) => orders.CancelAsync(id).ToHttpResult());
app.MapGet("/products/{id}", (int id, IOrders orders) => orders.FindProductAsync(id).ToHttpResult());
```

- `LoadOrderAsync` returns `Task<Result<Order>>`. A found order is 200, a `KeyNotFoundException` error is 404 with `ProblemDetails` (without the message, unless you map it with `exposeMessage: true`).
- `CancelAsync` returns `Task<Result>`. Success is 204 with no body.
- `FindProductAsync` returns `Task<Option<Product>>`. `None` is 404. An option has no error state, so it never gives `ProblemDetails`.

Handlers can return the `Task<IResult>` directly. The `await` in the first line is optional: `(Guid id, IOrders o) => o.LoadOrderAsync(id).ToHttpResult()` returns a `Task<IResult>`, which minimal APIs await.

## A faulted task

None of the `Task` overloads settles the task. A faulted task throws as usual, and the exception goes through ASP.NET Core's exception handling, which logs it and gives 500:

```csharp
await Task.FromException<Result<int>>(new InvalidOperationException("secret")).ToHttpResult();
// throws InvalidOperationException
```

Wrap code that throws in `Result.TryAsync` if the exception should become an error result instead.

A `default` result of your own type is not mapped through your `FromException`. It gives `ProblemDetails` with 500 and is logged, like the [endpoint filter](endpoint-filter.md) does.

## Errors as data

A result type whose error is a record has no default mapping, because the error is not an exception. Pass `onError`, and a `switch` makes the compiler check every case:

```csharp
app.MapGet("/checkout/{id}", (int id) => Checkout(id).ToHttpResult(error => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    PaymentDeclined declined => TypedResults.Problem(declined.Reason, statusCode: StatusCodes.Status402PaymentRequired),
    Unexpected => TypedResults.StatusCode(500)
}));

app.MapGet("/checkout-async/{id}", async (int id) => (await CheckoutAsync(id)).ToHttpResult(ToResponse));
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

## With your own result type

A result type declared with `[FluentResult<TError>]` (see [your own result type](../errors/custom-error-types.md)) has two overloads. Both take any type that implements `IResult<TSelf, T, TError>`, which the generator does for you. Do not implement the interface by hand. `ToHttpResult<TSelf, T, TError>(this IResult<TSelf, T, TError>)` needs `TError : Exception` and maps like the default `Result<T>`. `ToHttpResult(onError)` works for any error type: a value gives 200 and an error gives the response from `onError`.

```csharp
app.MapGet("/customers/{id}", (Guid id) => GetCustomer(id).ToHttpResult(error => error switch
{
    NotFoundException => TypedResults.NotFound(),
    _ => TypedResults.StatusCode(500)
}));
```

There is no `Task` overload for your own type. `IResult` in the signature is `Microsoft.AspNetCore.Http.IResult`. `Fluent.Unions.Abstractions.IResult<TSelf, T, TError>` has three type arguments, so the names do not clash, but an alias such as `using HttpResult = Microsoft.AspNetCore.Http.IResult;` can make a file easier to read.

## Controllers

ASP.NET Core MVC executes an `IResult` returned from an action, so the same extension works in a controller:

```csharp
using Microsoft.AspNetCore.Mvc;
using Fluent.Unions;
using Fluent.Unions.AspNetCore;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrders orders) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IResult> Get(Guid id) => await orders.LoadOrderAsync(id).ToHttpResult();

    [HttpPost]
    public IResult Create([FromBody] string input) => orders.Parse(input)
        .Validate(
            o => o.Number.Length > 0 ? Result.Ok : new ValidationError("Number is required.", "Number"),
            o => o.Number.Length < 20 ? Result.Ok : new ValidationError("Number is too long.", "Number"))
        .ToHttpResult();
}
```

An empty `input` makes `Validate` fail with a `ValidationErrors`, which gives 400 with a `ValidationProblemDetails` that lists the messages under `errors.Number`. See [Error mapping](error-mapping.md#validation-errors).

## Pitfalls

- **A result is not serializable.** Do not return a `Result<T>` from a minimal API without `ToHttpResult` or the [endpoint filter](endpoint-filter.md). `JsonSerializer` throws for it.
- **Only `None` is 404.** A result never gives 404 by itself. Return a `KeyNotFoundException` error, or use `Option<T>`, for "not found".
- **Your own result type has no `Task` overload.** Await the task, then call `ToHttpResult()` or `ToHttpResult(onError)` on the result.
- **Messages are hidden by default.** Only validation errors and types mapped with `exposeMessage: true` show their message, so log the error before you convert it, for example with `OnErrorDo`.

## See also

- [Endpoint filter](endpoint-filter.md): return results straight from handlers.
- [Error mapping](error-mapping.md): the status code table.
- [Errors as data](../errors/errors-as-data.md): an error union with an exhaustive `switch`.
- `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs` in the repo: the status codes as tests.

---

Back to: [ASP.NET Core](README.md) · Next: [Endpoint filter](endpoint-filter.md)
