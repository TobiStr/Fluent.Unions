![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.svg)](https://www.nuget.org/packages/Fluent.Unions) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.svg)](https://www.nuget.org/packages/Fluent.Unions) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/)

`Result`, `Result<T>` and `Option<T>` for .NET, built on the C# 15 `union` keyword. A method returns either its value or an error, and the compiler checks that callers handle both. Fluent operators are named after LINQ and Rx.NET, so a whole flow reads as one expression.

- **Your own Result in one line.** `[FluentResult<TError>]` on a `partial struct` lets the source generator build a complete `Result<T>` around your error type, with every operator and `Task` overload. Make the error a union and the compiler checks that every case is handled.
- **Built for hot paths.** Results store their value without boxing, and a chain of operators with static lambdas allocates nothing.
- **Every outcome in the signature.** `Option<T>` replaces `null`, and failures are typed values instead of hidden exceptions. What a method can return is visible in its type and checked by the compiler, so flows stay flat, readable and free of `null` checks and `try`/`catch`.

## Installation

```
dotnet add package Fluent.Unions
```

Requires .NET 11. C# 15 is the default language version for `net11.0`.

## Choose your Result

Use the default `Result<T>`, whose error is any `Exception`, with `using Fluent.Unions;`. Or declare your own in your root namespace, with the error type you want:

```csharp
using Fluent.Unions;
using Fluent.Unions.Abstractions;

namespace Shop;

public class DomainException(string message, Exception? inner = null)
    : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new UnexpectedDomainException(exception);
}

public sealed class UnexpectedDomainException(Exception inner)
    : DomainException("An unexpected error occurred.", inner);

[FluentResult<DomainException>]
public readonly partial struct Result<T>;
```

The generator adds the union cases, the value-less `Result` with `Result.From` and `Result.Try`, and every operator of the default type. The error type can also be a union of your error cases; the compiler then checks that a `switch` handles each one. Results store their value without boxing, so a `Result<int>` and a chain of `Select`, `Chain`, `Ensure` and `Do` allocate nothing.

## Common scenarios

**Return a value or an error, and handle both.** The compiler reports a `switch` that misses a case:

```csharp
Result<Customer> GetCustomer(Guid id) =>
    customers.TryGetValue(id, out var customer) ? customer : new KeyNotFoundException($"No customer {id}.");

string greeting = GetCustomer(id) switch
{
    Customer customer => $"Hello {customer.Name}",
    Exception error => error.Message
};
```

**Chain steps.** The first error skips the rest, and exceptions thrown inside a step become the error:

```csharp
Result<Order> order = GetCustomer(id)
    .Chain(GetCart)
    .Ensure(cart => cart.Items.Count > 0, "The cart is empty.")
    .Chain(PlaceOrder)
    .OnErrorDo(error => logger.LogWarning(error, "Checkout failed"));
```

**Wrap code you do not control.** Turn `null` and exceptions into results at the boundary:

```csharp
Result<Customer> customer = repository.Find(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));
Result<int> quantity = Result.Try(() => int.Parse(input));
```

**Validate input and get every error at once.** The ready-made rules in `Fluent.Unions.Validation` are part of the core package and name the property in their message:

```csharp
Result<SignUp> signUp = Result.From(form).Validate(
    f => f.Email.EnsureMatchesEmail(),          // "Email must be a valid email address."
    f => f.Age.EnsureInRange(18, 120),          // "Age must be between 18 and 120."
    f => f.Password.EnsureLongerThanOrEqual(12));
```

**Model missing values with `Option<T>`** instead of `null`:

```csharp
Option<Customer> cached = cache.GetValueOrNone(id).Or(() => FindInDatabase(id));
string name = cached.Select(c => c.Name).GetValueOrDefault("Guest");
```

**Stay fluent across `await`.** Every operator has a `Task` overload, and `...Await` operators take asynchronous steps:

```csharp
Result<decimal> total = await LoadCustomerAsync(id)
    .ChainAwait(customer => LoadCartAsync(customer.CartId))
    .Select(cart => cart.Total);
```

## Integrations

Each integration is its own package. Add only the ones you use.

| Area | Package | Purpose |
| --- | --- | --- |
| Web APIs | [Fluent.Unions.AspNetCore](https://www.nuget.org/packages/Fluent.Unions.AspNetCore) | HTTP responses with ProblemDetails, validation errors as 400 |
|  | [Fluent.Unions.OpenApi](https://www.nuget.org/packages/Fluent.Unions.OpenApi) | `Option<T>` as nullable `T` in Microsoft.AspNetCore.OpenApi documents |
|  | [Fluent.Unions.Swashbuckle](https://www.nuget.org/packages/Fluent.Unions.Swashbuckle) | `Option<T>` as nullable `T` in Swashbuckle documents |
|  | [Fluent.Unions.NSwag](https://www.nuget.org/packages/Fluent.Unions.NSwag) | `Option<T>` as nullable `T` in NSwag documents |
|  | [Fluent.Unions.HotChocolate](https://www.nuget.org/packages/Fluent.Unions.HotChocolate) | GraphQL resolvers that return `Result<T>` and `Option<T>` |
|  | [Fluent.Unions.Grpc](https://www.nuget.org/packages/Fluent.Unions.Grpc) | gRPC calls as results, errors as status codes |
|  | [Fluent.Unions.Refit](https://www.nuget.org/packages/Fluent.Unions.Refit) | Refit API responses as results, a 404 as `None` |
| Validation and messaging | [Fluent.Unions.FluentValidation](https://www.nuget.org/packages/Fluent.Unions.FluentValidation) | FluentValidation validators as results, rules for options |
|  | [Fluent.Unions.Mediator](https://www.nuget.org/packages/Fluent.Unions.Mediator) | Mediator pipeline behaviors that return a failed result |
|  | [Fluent.Unions.MediatR](https://www.nuget.org/packages/Fluent.Unions.MediatR) | MediatR pipeline behaviors that return a failed result |
|  | [Fluent.Unions.Polly](https://www.nuget.org/packages/Fluent.Unions.Polly) | Polly strategies that handle results |
| Data and serialization | [Fluent.Unions.Dapper](https://www.nuget.org/packages/Fluent.Unions.Dapper) | `Option<T>` to and from database `NULL` |
|  | [Fluent.Unions.MongoDB](https://www.nuget.org/packages/Fluent.Unions.MongoDB) | `Option<T>` as a value or BSON null |
|  | [Fluent.Unions.NewtonsoftJson](https://www.nuget.org/packages/Fluent.Unions.NewtonsoftJson) | Newtonsoft.Json converters for options, unions and results |
|  | [Fluent.Unions.MessagePack](https://www.nuget.org/packages/Fluent.Unions.MessagePack) | MessagePack formatter and resolver for `Option<T>` |
|  | [Fluent.Unions.Orleans](https://www.nuget.org/packages/Fluent.Unions.Orleans) | `Option<T>` in Orleans grain calls and state |
|  | [Fluent.Unions.Mapster](https://www.nuget.org/packages/Fluent.Unions.Mapster) | Mapster mappings between `Option<T>` and `T?` |
|  | [Fluent.Unions.AutoMapper](https://www.nuget.org/packages/Fluent.Unions.AutoMapper) | AutoMapper mappings between `Option<T>` and `T?` |
| Logging | [Fluent.Unions.Serilog](https://www.nuget.org/packages/Fluent.Unions.Serilog) | Results, options and loadables as structured log values |
| Testing | [Fluent.Unions.AwesomeAssertions](https://www.nuget.org/packages/Fluent.Unions.AwesomeAssertions) | AwesomeAssertions for results and options |
|  | [Fluent.Unions.FluentAssertions](https://www.nuget.org/packages/Fluent.Unions.FluentAssertions) | FluentAssertions 8 for results and options |
|  | [Fluent.Unions.Shouldly](https://www.nuget.org/packages/Fluent.Unions.Shouldly) | Shouldly assertions for results and options |
|  | [Fluent.Unions.NUnit](https://www.nuget.org/packages/Fluent.Unions.NUnit) | NUnit constraints for results and options |
|  | [Fluent.Unions.Xunit](https://www.nuget.org/packages/Fluent.Unions.Xunit) | xUnit v3 assertions for results and options |
|  | [Fluent.Unions.TUnit](https://www.nuget.org/packages/Fluent.Unions.TUnit) | TUnit assertions for results and options |
|  | [Fluent.Unions.Verify](https://www.nuget.org/packages/Fluent.Unions.Verify) | Readable Verify snapshots of results, options and loadables |

## Documentation

- [Docs](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/README.md): every concern with several examples
- [Reference](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/reference.md): every type and operator on one page
- [Source and issues](https://github.com/TobiStr/Fluent.Unions)
