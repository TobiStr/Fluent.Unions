# Quick start

This page walks through the main features in about ten minutes, using a small online shop: customers, carts, orders and payments. The helpers `GetCustomer`, `GetCart`, `PlaceOrder` and `LoadOrderAsync` return results. Their bodies do not matter here.

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;
```

## Return a result

Return the value or the exception directly. The union provides the implicit conversions.

```csharp
static Result<Customer> GetCustomer(Guid id)
{
    if (id == Guid.Empty)
        return new KeyNotFoundException("Customer not found.");

    return new Customer(id, "Ada", "ada@example.com");
}

static Result Ship(Order order)
{
    if (order.Total <= 0)
        return new InvalidOperationException("Nothing to ship.");

    return Result.Ok;
}
```

`Result<T>` carries a `T` or an `Exception`. `Result` carries `Ok` or an `Exception`.

## Match on the outcome

A `switch` sees both cases, so the compiler can tell you when you forget one.

```csharp
static string Describe(Result<Customer> result) => result switch
{
    Customer customer => $"{customer.Name} <{customer.Email}>",
    Exception error => error.Message
};

static string DescribeShip(Result result) => result switch
{
    Ok => "shipped",
    Exception error => error.Message
};
```

Omitting an arm is warning CS8509. [Installation](installation.md) shows how to make it an error.

## Chain steps

`Chain` calls the next step only if the previous one succeeded. The first error ends the chain.

```csharp
Result<Order> order = GetCustomer(id)
    .Chain(GetCart)
    .Chain(PlaceOrder);
```

`Select` transforms the value. `Catch` handles an error: return a value to recover. Naming the exception type handles only that type.

```csharp
Result<string> label = GetCustomer(id)
    .Select(customer => customer.Name.ToUpperInvariant())
    .Catch((KeyNotFoundException _) => "GUEST");
```

Other errors pass through `Catch` untouched. An exception thrown inside a lambda also becomes the error, so chains do not throw.

## Wrap code that throws

`Result.Try` runs code that may throw and returns the outcome as a result.

```csharp
Result<int> quantity = Result.Try(() => int.Parse(input));
Result saved = Result.Try(() => File.WriteAllText("order.txt", text));
```

`Result.TryAsync` does the same for asynchronous code.

## Look something up with Option

Use `Option<T>` when a missing value is normal, not a failure.

```csharp
Option<Product> keyboard = catalog.GetValueOrNone(1);
Option<Product> cheap = products.FirstOrNone(p => p.Price < 10m);

string name = keyboard.Match(p => p.Name, () => "unknown");
```

`catalog` is an `IReadOnlyDictionary<int, Product>`. `Dictionary<int, Product>` works too. See [Option](../option/README.md).

## An async pipeline

Every operator also works on `Task<Result<T>>`. Use the `...Await` variants when your lambda is itself asynchronous.

```csharp
Result<Confirmation> confirmation = await GetCustomerAsync(id)
    .ChainAwait(GetCartAsync)
    .ChainAwait(PlaceOrderAsync)
    .Select(order => new Confirmation($"O-{order.Id}"))
    .Catch(e => new Confirmation("none"));
```

A faulted task becomes an error result. See [Async](../async/README.md).

## An ASP.NET Core endpoint

With `Fluent.Unions.AspNetCore`, a handler turns a result into an HTTP response in one call.

```csharp
using Fluent.Unions.AspNetCore;

app.MapGet("/orders/{id}", (Guid id) => LoadOrderAsync(id).ToHttpResult());
```

A value gives 200, an empty result gives 404, and an error gives ProblemDetails with a status code chosen from the exception type. See [ASP.NET Core](../aspnetcore/README.md).

## Pitfalls

- `Catch(e => "x")` recovers and `Catch(e => new SomeException())` replaces the error. Both are the same operator.
- Ignoring a returned result gives analyzer warning FLU0001. Assign it, return it or end with `Check()`.

## See also

- [Result](../result/README.md): all operators and how errors flow
- [Option](../option/README.md)
- [Async](../async/README.md)
- [ASP.NET Core](../aspnetcore/README.md)

---

Back to: [Getting started](README.md) · Next: [Limitations](limitations.md)
