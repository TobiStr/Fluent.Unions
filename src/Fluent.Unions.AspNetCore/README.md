![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.AspNetCore.svg)](https://www.nuget.org/packages/Fluent.Unions.AspNetCore) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.AspNetCore.svg)](https://www.nuget.org/packages/Fluent.Unions.AspNetCore) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml)

Turns `Result`, `Result<T>` and `Option<T>` into HTTP responses, so an endpoint returns the outcome of an operation and the status code follows from it. It builds on Fluent.Unions.

## Installation

```
dotnet add package Fluent.Unions.AspNetCore
```

## Usage

Convert a result in the handler with `ToHttpResult`. A value gives 200, a succeeded `Result` gives 204, an empty result or `None` gives 404, and an error gives `ProblemDetails`.

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", async (Guid id, IOrders orders) => await orders.LoadOrderAsync(id).ToHttpResult());
app.MapDelete("/orders/{id}", (Guid id, IOrders orders) => orders.CancelAsync(id).ToHttpResult());
```

Let handlers return results directly. `WithResultMapping` works on one endpoint or a whole route group.

```csharp
var group = app.MapGroup("/v2/orders").WithResultMapping();

group.MapGet("/{id}", (Guid id, IOrders orders) => orders.LoadOrderAsync(id)); // Task<Result<Order>>
```

Return a custom result type that implements `IResult<TSelf, T, TError>`, or `Result<T, TError>`, from a filtered endpoint. A value gives 200, an empty result gives 404, and an error that is not an exception gives 500 unless you map `ResultErrorException`. `ToHttpResult(onError)` takes over the mapping.

```csharp
group.MapGet("/customers/{id}", (Guid id) => GetCustomer(id)); // your own IResult type
```

Change the status code of an exception type, including its subclasses.

```csharp
builder.Services.Configure<ResultHttpOptions>(o => o
    .Map<OrderShippedException>(StatusCodes.Status409Conflict));
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
