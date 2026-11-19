![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions

`Result`, `Result<T>` and `Option<T>` for .NET, built on the C# 15 `union` keyword. A method returns either its value or an error, and the compiler checks that callers handle both. Fluent operators are named after LINQ and Rx.NET, so a whole flow reads as one expression.

## Installation

```
dotnet add package Fluent.Unions
```

Requires .NET 11. C# 15 is the default language version for `net11.0`.

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
    .Where(cart => cart.Items.Count > 0)
    .Chain(PlaceOrder)
    .OnErrorDo(error => logger.LogWarning(error, "Checkout failed"));
```

**Wrap code you do not control.** Turn `null` and exceptions into results at the boundary:

```csharp
Result<Customer> customer = repository.Find(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));
Result<int> quantity = Result.Try(() => int.Parse(input));
```

**Validate input and get every error at once.** Ready-made rules name the property in their message:

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

**Answer HTTP requests in one line** with [Fluent.Unions.AspNetCore](https://www.nuget.org/packages/Fluent.Unions.AspNetCore):

```csharp
app.MapGet("/orders/{id}", (Guid id) => LoadOrderAsync(id).ToHttpResult());   // 200, 404, or ProblemDetails
```

## Integrations

| Package | Purpose |
| --- | --- |
| [Fluent.Unions.AspNetCore](https://www.nuget.org/packages/Fluent.Unions.AspNetCore) | HTTP responses with ProblemDetails |
| [Fluent.Unions.FluentValidation](https://www.nuget.org/packages/Fluent.Unions.FluentValidation) | FluentValidation validators as results |
| [Fluent.Unions.Polly](https://www.nuget.org/packages/Fluent.Unions.Polly) | Polly strategies that handle results |
| [Fluent.Unions.OpenApi](https://www.nuget.org/packages/Fluent.Unions.OpenApi) | `Option<T>` as nullable `T` in OpenAPI documents |
| [Fluent.Unions.AwesomeAssertions](https://www.nuget.org/packages/Fluent.Unions.AwesomeAssertions) | Assertions for results and options |

## Documentation

- [Docs](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/README.md): every concern with several examples
- [Reference](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/reference.md): every type and operator on one page
- [Source and issues](https://github.com/TobiStr/Fluent.Unions)
