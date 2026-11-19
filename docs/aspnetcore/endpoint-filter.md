# Endpoint filter

This page covers `WithResultMapping`, which registers an endpoint filter so handlers can return results and options directly and still get the right response.

## WithResultMapping

Without the filter, every handler calls `ToHttpResult()`. With it, the handler returns the result and the filter converts it, with the same rules as [`ToHttpResult`](http-results.md):

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", (Guid id, IOrders orders) => orders.LoadOrderAsync(id)).WithResultMapping();
```

`WithResultMapping` works on a single endpoint and on a route group. A group applies the filter to every endpoint mapped on it:

```csharp
var group = app.MapGroup("/v2/orders").WithResultMapping();

group.MapGet("/{id}", (Guid id, IOrders orders) => orders.LoadOrderAsync(id));            // Task<Result<Order>>
group.MapDelete("/{id}", (Guid id, IOrders orders) => orders.CancelAsync(id));            // Task<Result>
group.MapGet("/product/{id}", (int id, IOrders orders) => orders.FindProductAsync(id));   // Task<Option<Product>>
```

Handlers may be synchronous or asynchronous. Minimal APIs await the returned `Task` before the filter sees the value.

## What is converted

| Handler returns | Response |
| --- | --- |
| `Result<T>` with a value | 200 with the value |
| `Result<T>` with an error | `ProblemDetails`, status from [error mapping](error-mapping.md) |
| empty `Result<T>` | 404 |
| `Result` that succeeded | 204 |
| `Result` with an error, or empty | `ProblemDetails`, or 404 |
| `Option<T>` with a value | 200 with the value |
| `None` or a `default` option | 404 |

## What passes through

Every other return value is left alone: strings, objects, `IResult` values such as `TypedResults.Ok(7)`, and `null`. A handler that returns `"untouched"` still answers `"untouched"`, so you can turn the filter on for a whole group and keep unrelated endpoints in it.

The filter does not catch exceptions. An exception thrown by the handler goes through ASP.NET Core's normal exception handling. If you want a thrown exception to become an error, wrap the call in `Result.Try` or `Result.TryAsync`.

## Errors as data need explicit mapping

`Result<T, TError>` is not converted by the filter, because the library cannot know what status each of your error cases means. Convert it in the handler with `ToHttpResult(onError)`. The returned `IResult` passes through the filter untouched:

```csharp
var checkout = app.MapGroup("/checkout").WithResultMapping();

checkout.MapGet("/{id}", (int id) => Checkout(id).ToHttpResult(error => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    PaymentDeclined => TypedResults.StatusCode(402),
    Unexpected => TypedResults.StatusCode(500)
}));
```

## What is not converted

`Result<T, TError>` and your own result types are not converted, and returning one from a filtered endpoint is a mistake:

- A `Result<T, TError>` fails with status 500, because results are not JSON-serializable.
- A custom result type, such as the `Result<T>` from [custom error types](../errors/custom-error-types.md), is written as JSON. A value gives 200 with the value. An error also gives 200, with the exception serialized as JSON, including its message. Never rely on this.

Map a custom result yourself with `Match`:

```csharp
group.MapGet("/customers/{id}", (Guid id) => GetCustomer(id).Match(
    customer => (IResult)TypedResults.Ok(customer),
    error => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status404NotFound),
    () => TypedResults.NotFound()));
```

The cast on the first callback fixes the return type of `Match` to `IResult`.

## Pitfalls

- **Only `Result`, `Result<T>` and `Option<T>` are converted.** Check the table before you rely on it for another type.
- **Custom results leak.** An error in a custom result type is serialized with its message and other exception properties, under status 200.
- **The filter does not catch.** Wrap code that throws in `Result.TryAsync`.

## See also

- [HTTP results](http-results.md): `ToHttpResult` for each type.
- [Error mapping](error-mapping.md): status codes for errors.
- `EndpointsCanReturnResultsDirectly` in `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs`: the filter running against a real host.

---

Back to: [ASP.NET Core](README.md) · Next: [Error mapping](error-mapping.md)
