![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.AspNetCore.svg)](https://www.nuget.org/packages/Fluent.Unions.AspNetCore) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.AspNetCore.svg)](https://www.nuget.org/packages/Fluent.Unions.AspNetCore) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/aspnetcore/)

Turns `Result`, `Result<T>` and `Option<T>` into HTTP responses, so an endpoint returns the outcome of an operation and the status code follows from it. It builds on Fluent.Unions.

## Installation

```
dotnet add package Fluent.Unions.AspNetCore
```

## Usage

Convert a result in the handler with `ToHttpResult`. A value gives 200, a succeeded `Result` gives 204, `None` gives 404, and an error gives `ProblemDetails`. Validation errors give 400 with a `ValidationProblemDetails` grouped by property.

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", async (Guid id, IOrders orders) => await orders.LoadOrderAsync(id).ToHttpResult());
app.MapDelete("/orders/{id}", (Guid id, IOrders orders) => orders.CancelAsync(id).ToHttpResult());
```

Let handlers return results directly. `WithResultMapping` works on one endpoint or a whole route group, and publishes the value type and the error responses to OpenAPI.

```csharp
var group = app.MapGroup("/v2/orders").WithResultMapping();

group.MapGet("/{id}", (Guid id, IOrders orders) => orders.LoadOrderAsync(id)); // Task<Result<Order>>
```

### With your own result type

`WithResultMapping` handles any result type, also one declared with `[FluentResult<TError>]`: a value gives 200, a succeeded value-less result gives 204, and an error gives the configured error mapping. The OpenAPI metadata has the same shape.

```csharp
group.MapGet("/customers/{id}", (Guid id) => GetCustomer(id)); // Shop.Result<Customer>
```

`ToHttpResult()` also accepts `IResult<TSelf, T, TError>` when `TError : Exception`, and maps like the default type. `ToHttpResult(onError)` works for any error type. There are no `Task` overloads for your own type, so await first. See [your own result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

By default `ValidationError` and `ValidationErrors` give 400, `KeyNotFoundException` 404, `UnauthorizedAccessException` 403 and `TimeoutException` 504. Everything else gives 500. Only validation errors show their messages to the client. Map your own exception types, including their subclasses, and choose whether the message is safe to expose:

```csharp
builder.Services.Configure<ResultHttpOptions>(o => o
    .Map<OrderShippedException>(StatusCodes.Status409Conflict, exposeMessage: true));
```

Map errors that are data with `onError`. The `switch` makes the compiler check every case.

```csharp
app.MapGet("/checkout/{id}", (int id) => Checkout(id).ToHttpResult(error => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    Unexpected => TypedResults.StatusCode(500)
}));
```

## Documentation

- [ASP.NET Core documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/aspnetcore/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
