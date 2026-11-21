![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.HotChocolate

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.HotChocolate.svg)](https://www.nuget.org/packages/Fluent.Unions.HotChocolate) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.HotChocolate.svg)](https://www.nuget.org/packages/Fluent.Unions.HotChocolate) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/hotchocolate/)

Lets HotChocolate resolvers return `Result<T>`, your own result type with a value (declared with [`[FluentResult<TError>]`](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md)) and `Option<T>`. The schema shows `T` for a result and a nullable `T` for an option, and an error in a result is rethrown so HotChocolate's error handling and mutation conventions deal with it. It builds on Fluent.Unions and HotChocolate 16.

## Installation

```
dotnet add package Fluent.Unions.HotChocolate
```

## Usage

Register it once on the request executor builder.

```csharp
using Fluent.Unions.HotChocolate;

builder.Services
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddMutationConventions()
    .AddFluentUnions();
```

Return the unions from resolvers, also inside `Task` and `ValueTask`.

```csharp
public class Query
{
    public Task<Result<Book>> GetBookAsync(int id, IBookStore store) => store.FindAsync(id);   // book(id: Int!): Book!

    public Option<Book> FindBook(string title, IBookStore store) => store.Search(title);       // findBook(title: String!): Book
}
```

An error in a result is thrown from the field, so it reaches error filters and mutation conventions as the original exception.

```csharp
public class Mutation
{
    [Error<OutOfStockException>]
    public Result<Order> PlaceOrder(string sku, IOrders orders) => orders.Place(sku);
}
```

A `ValidationErrors` error becomes one GraphQL error per `ValidationError`, with the member in `extensions.member`.

## Documentation

- [HotChocolate documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/hotchocolate/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
