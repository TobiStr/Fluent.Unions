# Patterns

This page shows how the operators combine in real code, and when a result is the wrong tool.

```csharp
using Fluent.Unions;
```

## Railway-style pipelines

Each step runs only if the previous one succeeded. The first error skips everything after it, so the happy path reads top to bottom.

```csharp
Result<Confirmation> railway = GetCustomer(id)
    .Chain(GetCart)
    .Chain(PlaceOrder)
    .Do(order => Console.WriteLine(order.Id))
    .Select(order => new Confirmation($"O-{order.Id}"));
```

Put side effects in `Do`, transformations in `Select`, and steps that can fail in `Chain`. End the pipeline in one place: `Match`, a `switch`, `Check()` or `ToHttpResult()`.

## Query syntax

`Select` and `SelectMany` follow the LINQ query pattern, so `from` clauses work. A result has no `where` clause; use `Ensure` in the method chain to turn a failed check into an error. Every earlier variable stays in scope, and `let` adds computed values:

```csharp
Result<string> label =
    from customer in GetCustomer(id)
    from cart in GetCart(customer)
    let total = cart.Items.Sum(i => i.Price)
    from confirmation in Submit(customer, cart)
    select $"{confirmation.Number} for {customer.Name}, total {total}";
```

It works across tasks, with `await` around the whole query:

```csharp
Result<string> labelAsync = await (
    from customer in GetCustomerAsync(id)
    from cart in GetCartAsync(customer)
    let total = cart.Items.Sum(i => i.Price)
    from confirmation in SubmitAsync(customer, cart)
    select $"{confirmation.Number} for {customer.Name}, total {total}");
```

## Using earlier values

When a step needs a value from further back, you have three options:

| Option | Use when |
| --- | --- |
| Query syntax | The steps are sequential. No holder is needed, also across tasks. |
| `Out(out var x)` | You want a fluent chain and need one or two earlier values. See [Operations](operations.md#out). |
| Deconstruction | You write statement code with early returns. |

Query syntax is the shortest. `Out` keeps the method-chain style:

```csharp
Result<string> confirmation = GetCustomer(id).Out(out var customer)
    .Chain(GetCart).Out(out var cart)
    .Chain(_ => Submit(customer, cart))
    .Select(c => $"{c.Number} for {customer.Value.Name}");
```

Deconstruction fits when you have loops or branches. See the next section.

## Early returns

Deconstruct a result, check the error first, and return it. A result converts implicitly, so `return error;` is enough:

```csharp
static Result<Order> Place(Guid id)
{
    var (customer, error) = GetCustomer(id);
    if (error is not null) return error;

    var (cart, cartError) = GetCart(customer);
    if (cartError is not null) return cartError;

    return PlaceOrder(cart);
}
```

This is the same flow as `GetCustomer(id).Chain(GetCart).Chain(PlaceOrder)`, only longer. Use it when the steps need loops, `await` in the middle of branches, or several values at once.

## Converting between Result and Option

A missing value is not always an error. Convert at the boundary:

```csharp
Option<Customer> found = GetCustomer(id).ToOption();
Result<Customer> required = Cached(id).ToResult(() => new KeyNotFoundException($"Customer {id} not found."));
```

`ToOption()` keeps the value of an ok result and drops the error, so the error is lost. `ToResult` turns `None` into the error from the factory. `Transpose()` swaps `Option<Result<T>>` and `Result<Option<T>>`:

```csharp
Option<Result<Customer>> optionalResult = Cached(id).Select(c => GetCustomer(c.Id));
Result<Option<Customer>> transposed = optionalResult.Transpose();
```

See [Conversions](../option/conversions.md) for all cases.

## Results in service and repository layers

Return a result from methods that can fail for reasons the caller should handle. Use `Result.TryAsync` at the edge where an I/O library throws, and `ToResult` where a lookup finds nothing:

```csharp
public interface ICustomerRepository
{
    Task<Result<Customer>> FindAsync(Guid id);
    Task<Result> SaveAsync(Customer customer);
}

public class CustomerRepository : ICustomerRepository
{
    readonly Dictionary<Guid, Customer> store = new();

    public Task<Result<Customer>> FindAsync(Guid id) =>
        Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            return store.GetValueOrNone(id)
                .ToResult(() => new KeyNotFoundException($"Customer {id} not found."))
                .GetValue();
        });

    public Task<Result> SaveAsync(Customer customer) =>
        Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            store[customer.Id] = customer;
        });
}
```

The service then has no `try`/`catch` at all:

```csharp
public class CheckoutService(ICustomerRepository customers)
{
    public Task<Result<Order>> CheckoutAsync(Guid customerId) =>
        customers.FindAsync(customerId)
            .Chain(GetCart)
            .Chain(PlaceOrder);
}
```

The `GetValue()` in the repository sits inside `TryAsync`, so a failed `ToResult` is turned back into an error. The `Task.Delay` stands in for a real database call. Log an error once, at the boundary where you handle it, not in every layer.

## When not to use results

- **Programmer errors.** A `null` argument or a broken invariant should throw. Do not wrap them.
- **Absence without cause.** Use `Option<T>`. A lookup that finds nothing is not a failure.
- **Operations that cannot fail.** Return the value. A result there only adds states for callers to handle.
- **Errors that need no handling.** When every caller would just rethrow, let the exception propagate.
- **Errors with structure.** If callers must branch on many specific cases, consider errors as data: your own result type with a union error, so the compiler checks every case. See [Errors as data](../errors/errors-as-data.md).

## Pitfalls

- Ending a pipeline without looking at the result gives warning FLU0001. Assign it, return it or finish with `Check()`.
- A `switch` on a `default` result throws at runtime. Always return an initialized result.
- Mixing `Out` with a chain that may fail: read `HasValue` before `.Value` outside the chain.

## See also

- [Operations](operations.md)
- [Error handling](error-handling.md)
- [Option](../option/README.md)
- [Async](../async/README.md)

---

Back to: [Result](README.md) · Next: [Option](../option/README.md)
