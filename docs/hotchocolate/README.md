# HotChocolate

The package `Fluent.Unions.HotChocolate` (namespace `Fluent.Unions.HotChocolate`) lets [HotChocolate](https://chillicream.com/docs/hotchocolate) 16 resolvers return `Result<T>` and `Option<T>`. A resolver can also return your own [result type](../errors/custom-error-types.md) with a value, exposed like `Result<T>`. It depends on `HotChocolate.Types` and registers a type interceptor, a field middleware and an error filter.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): a query, an optional field and a mutation.
- [What the schema shows](#what-the-schema-shows): the GraphQL type for each C# type.
- [Errors](#errors): what a failed result becomes.
- [Mutation conventions](#mutation-conventions): error unions from `[Error<T>]`.
- [Limitations](#limitations): what is not supported.
- [Rules](#rules): behavior to rely on.

## Quick example

```csharp
using Fluent.Unions.HotChocolate;

builder.Services
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddMutationConventions()
    .AddFluentUnions();

public class Query
{
    public Task<Result<Book>> GetBookAsync(int id, IBookStore store) => store.FindAsync(id);

    public Option<Book> FindBook(string title, IBookStore store) => store.Search(title);
}

public class Mutation
{
    [Error<OutOfStockException>]
    public Result<Order> PlaceOrder(string sku, IOrders orders) => orders.Place(sku);
}
```

## What the schema shows

| Resolver returns | GraphQL field type |
| --- | --- |
| `Result<Book>`, `Task<Result<Book>>`, `ValueTask<Result<Book>>` | `Book!` |
| `Result<int>` | `Int!` |
| `Result<List<Book>>` | `[Book!]!` |
| `Option<Book>`, `Task<Option<Book>>` | `Book` |
| `Option<int>` | `Int` |

`AddFluentUnions` rewrites the field type before HotChocolate builds the schema, so no `Result` or `Option` type appears in it. A field middleware unwraps the value at run time. An `Option` without a value, or a `default` option, gives `null`.

## Errors

| Resolver returns | GraphQL response |
| --- | --- |
| an error result | the original exception is thrown from the field |
| a `ValidationErrors` error | one GraphQL error per `ValidationError`, with `extensions.member` when the member is set |
| a `default` result | an `InvalidOperationException` is thrown from the field |
| a cancelled token | the request is cancelled; it is not reported as a field error |

The thrown exception goes through HotChocolate's normal pipeline. Your `IErrorFilter` sees the original exception, and HotChocolate hides the message of an unexpected exception unless you turn on exception details.

## Mutation conventions

The unwrapping runs inside the mutation convention middleware, so `[Error<TException>]` catches the exception of a failed result and fills the `errors` list of the payload.

```csharp
[Error<OutOfStockException>]
public Result<Order> PlaceOrder(string sku, IOrders orders) => orders.Place(sku);
```

```graphql
mutation { placeOrder(input: { sku: "dune" }) { order { id } errors { ... on OutOfStockError { message sku } } } }
```

`[Error<ValidationErrors>]` does not build a valid schema, because the exception exposes its `Data` dictionary and a nested `ValidationError`. Map it with your own error class that has a constructor taking `ValidationErrors` and a `Message` property.

```csharp
public class InvalidInputError(ValidationErrors errors)
{
    public string Message { get; } = errors.Message;
}

[Error<InvalidInputError>]
public Result<Order> PlaceOrder(string sku, IOrders orders) => orders.Place(sku);
```

## Limitations

- Only `Result<T>`, your own result types with a value, and `Option<T>` on object type fields are rewritten. Other union types, such as the non-generic `Result` or any value-less result, are not. HotChocolate fails when it builds the schema for the non-generic `Result`, so the mistake is not silent.
- Interface fields are not rewritten. Return the union from the implementing object type.
- Arguments and input fields of these types are not supported.
- HotChocolate removes the `Async` suffix from a field name, so `GetBookAsync` and `GetBook` collide as `book`.

## Rules

- **Errors are exceptions.** A failed result is rethrown unchanged, with its stack trace.
- **`null` is never a value.** A `default` result is an error. Use `Option<T>` for a field that may be missing.
- **Cancellation is not an error.** It is never turned into a field error or an error union entry.
- **Order of registration does not matter.** `AddFluentUnions` works before and after `AddMutationConventions`.

---

Back to index: [Docs index](../README.md)
