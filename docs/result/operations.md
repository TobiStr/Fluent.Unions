# Operations

This page covers the operators that work on the value of a result and the ones that leave the chain. Operators for errors are on [Error handling](error-handling.md).

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;
```

| Operators | ok | error | empty |
| --- | --- | --- | --- |
| `Select`, `Chain`, `SelectMany`, `Where`, `Zip`, `Do(onNext)` | runs | passes through | passes through |
| `Finally` | runs | runs | runs |
| `DefaultIfEmpty` | passes through | passes through | becomes ok |

All of them also exist on `Task<Result<T>>`. Asynchronous lambdas use the `...Await` variants. See [Async](../async/README.md).

## Select

Projects the value. An error or an empty result passes through, and the lambda does not run. A selector that returns `null` gives an empty result.

```csharp
Result<int> length = GetCustomer(id).Select(customer => customer.Name.Length); // 3
```

On a `Result` there is no value, so the selector takes no argument and produces a `Result<T>`:

```csharp
Result<string> paid = Save(known).Select(() => "Order saved");
```

An exception thrown inside the selector becomes the error:

```csharp
Result<int> broken = GetCustomer(id).Select(c => int.Parse(c.Name)); // error: FormatException
```

## Chain and SelectMany

`Chain` calls a function that itself returns a result, and the outcome is that result, not a nested one. `SelectMany` is the LINQ name for the same operator. It exists so query syntax works.

Pass a method group:

```csharp
Result<Cart> cart = GetCustomer(id).Chain(GetCart);
Result<Order> order = GetCustomer(id).Chain(GetCart).Chain(PlaceOrder);
```

When the function returns a `Result` (no value), the chain continues as a `Result`:

```csharp
Result notified = order.Chain(Notify);
```

A lambda may return a `Result<T>` or a `Result`. A check that produces no value is a common use. To keep the value, select it back inside the lambda:

```csharp
Result<Cart> reserved = GetCustomer(id)
    .Chain(GetCart)
    .Chain(c => Reserve(c).Select(() => c));
```

On a `Result`, `Chain` takes a function without arguments, and `Select(() => value)` brings a value back:

```csharp
Result<Customer> checkedEmail = GetCustomer(id)
    .Chain(c => c.Email.Contains('@') ? Result.Ok : new ArgumentException("Invalid e-mail."))
    .Select(() => known.CustomerId)
    .Chain(GetCustomer);
```

## Where

Keeps the value when the predicate holds. Otherwise the result becomes empty, not an error. An error passes through unchanged.

```csharp
Result<Customer> company = GetCustomer(id).Where(c => c.Email.EndsWith("@example.com"));
```

To supply a value instead of staying empty, follow it with `DefaultIfEmpty`:

```csharp
Result<Customer> orGuest = GetCustomer(id)
    .Where(c => c.Email.EndsWith("@shop.test"))
    .DefaultIfEmpty(new Customer(Guid.Empty, "Guest", ""));
```

## Zip

Combines two results. The first error wins, then an empty side makes the result empty. Both sides must be ok to produce a value.

```csharp
Result<(Customer First, Cart Second)> pair =
    GetCustomer(id).Zip(GetCustomer(id).Chain(GetCart));
```

With a selector, the pair becomes one value:

```csharp
Result<decimal> total = GetCustomer(id).Chain(GetCart).Zip(
    GetCustomer(id).Chain(Shipping),
    (cart, shipping) => cart.Items.Sum(i => i.Price) + shipping);
```

`Zip` is the right tool for independent inputs. For steps that depend on each other, use `Chain`.

## Do

Runs an action with the value and returns the same result. Use it for logging and other side effects. It does not run for an error or an empty result.

```csharp
Result<Customer> logged = GetCustomer(id)
    .Do(c => log.LogInformation("Loaded {Name}", c.Name));
```

`Do(onNext, onError)` runs one action or the other:

```csharp
Result<Customer> logged = GetCustomer(id).Do(
    c => log.LogInformation("Loaded {Name}", c.Name),
    e => log.LogWarning(e, "Customer {Id} failed", id));
```

An exception thrown by the action becomes the error.

## Out

A chain hands each step only the previous value. `Out(out var x)` captures a value so that later steps can use it. The holder is a `Captured<T>`.

```csharp
Result<string> confirmation = GetCustomer(id).Out(out var customer)
    .Chain(GetCart).Out(out var cart)
    .Chain(_ => Submit(customer, cart))
    .Select(c => $"{c.Number} for {customer.Value.Name}");
```

- As an argument, `customer` converts implicitly to `Customer`.
- For members, use `.Value`. Interfaces and `object` also need `.Value` as an argument.
- After a failed or empty chain, `HasValue` is `false` and `.Value` throws `InvalidOperationException`. Check `HasValue` outside the chain.

It works in asynchronous chains without awaiting each step. The holder is filled when the task completes, before any later step runs:

```csharp
Result<string> confirmation = await GetCustomerAsync(id).Out(out var customer)
    .ChainAwait(GetCartAsync).Out(out var cart)
    .ChainAwait(_ => SubmitAsync(customer, cart))
    .Select(c => $"{c.Number} for {customer.Value.Name}");
```

`Out` exists on `Result<T>`, `Option<T>` and their `Task` versions, not on `Result`. For purely sequential steps, query syntax keeps earlier values in scope too. See [Patterns](patterns.md#using-earlier-values).

## Finally

Runs an action in every state, ok, error and empty, and returns the same result. Use it for cleanup and timing.

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
Result<Order> timed = order.Finally(() => log.LogInformation("Checkout took {Ms} ms", sw.ElapsedMilliseconds));
```

## DefaultIfEmpty

Turns an empty result into an ok one, from a value or from a factory. Ok and error pass through. It stays a result, and a factory that throws gives an error.

```csharp
Result<Cart> empty = default;
Result<Cart> filled = empty.DefaultIfEmpty(new Cart(id, []));
Result<Cart> lazy = empty.DefaultIfEmpty(() => new Cart(id, []));
```

An error is not replaced. To replace an error, use [`Catch`](error-handling.md#catch).

## GetValueOrDefault

Leaves the chain with the value. For an error or an empty result you get `default`, a fallback or the factory's result.

```csharp
Cart? maybeCart = GetCustomer(id).Chain(GetCart).GetValueOrDefault();
Cart orEmpty = GetCustomer(id).Chain(GetCart).GetValueOrDefault(new Cart(id, []));
Cart lazy = GetCustomer(id).Chain(GetCart).GetValueOrDefault(() => new Cart(id, []));
int count = await GetCustomerAsync(id).Chain(GetCart).Select(c => c.Items.Count).GetValueOrDefault(0);
```

The factory runs only when the result is not ok. An exception from the factory is not caught. It propagates, because this operator leaves the chain. All three overloads also exist on `Task<Result<T>>`.

## Unwrap

Flattens a nested result, like `Task.Unwrap()`. Nesting appears when you `Select` with a function that returns a result:

```csharp
Result<Result<Payment>> nested = order.Select(Pay);
Result<Payment> flat = nested.Unwrap();

Result<Result> nestedPlain = order.Select(Save);
Result flatPlain = nestedPlain.Unwrap();
```

`order.Chain(Pay)` gives the flat result directly. Prefer `Chain` and use `Unwrap` when you already hold a nested result.

## Match

Leaves the chain with one callback per state: value, error and empty. All three are required, so no state is forgotten.

```csharp
string text = order.Match(
    o => $"Order total {o.Total}",
    e => $"Failed: {e.Message}",
    () => "No order");

int status = Save(known).Match(() => 204, e => 500, () => 404);
```

On a `Result`, the first callback takes no argument. `Match` also works on `Task<Result<T>>`, so `await LoadOrderAsync(id).Match(...)` ends an asynchronous pipeline. An exception thrown by a callback propagates.

## Pitfalls

- `Select` with a function that returns a result nests. Use `Chain`.
- `Where` leaves the result empty, not in error. A later `Catch` does not see it. Use `DefaultIfEmpty` or `Match`.
- `Do` does not run for errors. Use `OnErrorDo` or `Do(onNext, onError)`.

## See also

- [Core concepts](core-concepts.md)
- [Error handling](error-handling.md)
- [Patterns](patterns.md)
- [Async](../async/README.md)

---

Back to: [Result](README.md) · Next: [Error handling](error-handling.md)
