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
| your own result type | 200 with `T` (204 without a value), 400, 500 |

The errors are always described as 400 and 500. The metadata does not list the statuses of the built-in mappings (404, 403, 504) or of your `Map<TException>` calls. Add `.Produces(...)` yourself for the statuses you return, such as 404 or 409.

## What passes through

Every other return value is left alone: strings, objects, `IResult` values such as `TypedResults.Ok(7)`, and `null`. A handler that returns `"untouched"` still answers `"untouched"`, so you can turn the filter on for a whole group and keep unrelated endpoints in it.

The filter does not catch exceptions. An exception thrown by the handler goes through ASP.NET Core's normal exception handling. If you want a thrown exception to become an error, wrap the call in `Result.Try` or `Result.TryAsync`.

## With your own result type

The filter handles any result type, including one declared with `[FluentResult<TError>]` (see [your own result type](../errors/custom-error-types.md)). A value gives 200 with the value, a value-less success gives 204, and an error gives the configured [error mapping](error-mapping.md). The OpenAPI metadata uses the same shape. The filter finds `IResult<TSelf, T, TError>` at runtime, reads the type arguments once per type and caches them.

| Handler returns | Response |
| --- | --- |
| a value | 200 with the value |
| an error that is an `Exception` | `ProblemDetails`, status from [error mapping](error-mapping.md) |
| an error that is not an `Exception` | the error is wrapped in a `ResultErrorException`, so 500 without details |
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

Data errors should always use `ToHttpResult(onError)`. Do not map `ResultErrorException` with `exposeMessage: true`: its message is the `ToString()` of the error record, with its type name and every property.

A result type must be a struct. Other structs and classes pass through.

## Pitfalls

- **Only the listed types are converted.** `Result`, `Result<T>`, `Option<T>` and every result type. Check the tables before you rely on it for another type.
- **A data error gives 500.** Use `ToHttpResult(onError)`.
- **The filter does not catch.** Wrap code that throws in `Result.TryAsync`.

## See also

- [HTTP results](http-results.md): `ToHttpResult` for each type.
- [Error mapping](error-mapping.md): status codes for errors.
- `EndpointsCanReturnResultsDirectly` in `test/Fluent.Unions.AspNetCore.Tests/HttpResultTests.cs`: the filter running against a real host.

---

Back to: [ASP.NET Core](README.md) · Next: [Error mapping](error-mapping.md)
