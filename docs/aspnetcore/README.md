# ASP.NET Core

The package `Fluent.Unions.AspNetCore` (namespace `Fluent.Unions.AspNetCore`) turns results and options into HTTP responses, so an endpoint returns the outcome of an operation and the status code follows from it.

## In this section

- [HTTP results](http-results.md): `ToHttpResult` for each type, the `Task` overloads, and minimal API and controller usage.
- [Endpoint filter](endpoint-filter.md): `WithResultMapping` lets handlers return results directly, on one endpoint or a whole route group.
- [Error mapping](error-mapping.md): which exception gives which status code, how to change it, and how `ProblemDetails` is written.

## Quick example

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", async (Guid id, IOrders orders) => await orders.LoadOrderAsync(id).ToHttpResult());
```

`LoadOrderAsync` returns a `Task<Result<Order>>`. The endpoint answers with the table below.

| Input | Response |
| --- | --- |
| `Result<T>` or `Option<T>` with a value | 200 with the value as JSON |
| `Result` that succeeded | 204 |
| empty result or `None` | 404 with an empty body |
| error | `ProblemDetails` with the status code mapped from the exception type |
| `Result<T, TError>` with a value | 200 with the value |
| `Result<T, TError>` with an error | the response you return from the `onError` callback |

## When to use

- Your handlers already return `Result<T>` or `Option<T>`, and you want one consistent way to turn them into responses.
- You want errors as `ProblemDetails` without writing a `try`/`catch` in every endpoint.
- You model errors as data and want the compiler to check that each case has a response.

## Rules

- **Add the package, not just the library.** `Fluent.Unions.AspNetCore` references `Fluent.Unions`. It needs the `Microsoft.AspNetCore.App` framework.
- **Server errors hide their message.** The exception message becomes `detail` only for status codes 400 to 499.
- **The filter handles three types.** `Result`, `Result<T>` and `Option<T>`. Return a custom result type or `Result<T, TError>` from a filtered endpoint and it is not mapped. See [Endpoint filter](endpoint-filter.md#what-is-not-converted).
- **Results are not JSON.** `JsonSerializer` refuses `Result<T>`. Always convert before the response is written.
- **Errors as data need a mapping.** `Result<T, TError>` has no default, so the compiler checks every case.

---

Back to index: [Docs index](../README.md) · Next: [HTTP results](http-results.md)
