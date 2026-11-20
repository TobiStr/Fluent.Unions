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
| a `default` `Result<T>` | `ProblemDetails` with 500 |
| `Result` that succeeded | 204 |
| `Result` with an error | `ProblemDetails` |
| `Option<T>` with a value | 200 with the value |
| `None` or a `default` option | 404 |

## OpenAPI

`WithResultMapping()` also reads the handler's return type when the endpoint is built and adds the response types to the endpoint metadata, so OpenAPI and ApiExplorer describe the real responses instead of the `Result<T>` struct:

| Handler returns | Documented responses |
| --- | --- |
| `Result<T>` or `Task<Result<T>>` | 200 with `T`, 400 `HttpValidationProblemDetails`, 500 `ProblemDetails` |
| `Result` | 204, 400, 500 |
| `Option<T>` | 200 with `T`, 404 |
| a custom result type | 200 with `T`, 400, 500 |

Add `.Produces(...)` yourself for other statuses you map, such as 404 or 409.

## What passes through

Every other return value is left alone: strings, objects, `IResult` values such as `TypedResults.Ok(7)`, and `null`. A handler that returns `"untouched"` still answers `"untouched"`, so you can turn the filter on for a whole group and keep unrelated endpoints in it.

The filter does not catch exceptions. An exception thrown by the handler goes through ASP.NET Core's normal exception handling. If you want a thrown exception to become an error, wrap the call in `Result.Try` or `Result.TryAsync`.

## Custom result types

The filter also converts a handler that returns a type implementing `IResult<TSelf, T, TError>`, including `Result<T, TError>` and your own result types. It finds the interface at runtime, reads the type arguments once per type and caches them.

| Handler returns | Response |
| --- | --- |
| a value | 200 with the value |
| an error that is an `Exception` | `ProblemDetails`, status from [error mapping](error-mapping.md) |
| an error that is not an `Exception` | the error is wrapped in a `ResultErrorException` and mapped through `ResultHttpOptions`, so 500 unless you map `ResultErrorException` |
| a `default` result | `ProblemDetails` with 500 |

The library cannot know what status each of your error cases means, so a data error is a server error by default and its content is not shown. Map the cases yourself in the handler with `ToHttpResult(onError)`. The returned `IResult` passes through the filter untouched:

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

To give every data error one status, map `ResultErrorException`. Its message names the error, so expose it only if that is safe:

```csharp
builder.Services.Configure<ResultHttpOptions>(o => o.Map<ResultErrorException>(StatusCodes.Status409Conflict, exposeMessage: true));
```

A custom result type must be a struct, as `IResult<TSelf, T, TError>` requires. Other structs and classes pass through.

## Pitfalls

- **Only the listed types are converted.** `Result`, `Result<T>`, `Option<T>` and types that implement `IResult<TSelf, T, TError>`. Check the tables before you rely on it for another type.
- **A data error gives 500 by default.** Use `ToHttpResult(onError)` or map `ResultErrorException`.
- **The filter does not catch.** Wrap code that throws in `Result.TryAsync`.

## See also

- [HTTP results](http-results.md): `ToHttpResult` for each type.
- [Error mapping](error-mapping.md): status codes for errors.
- `EndpointsCanReturnResultsDirectly` in `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs`: the filter running against a real host.

---

Back to: [ASP.NET Core](README.md) · Next: [Error mapping](error-mapping.md)
