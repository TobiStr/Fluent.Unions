# Operations

This page covers the operators that work on the value of a result and the ones that leave the chain. Operators for errors are on [Error handling](error-handling.md).

```csharp
using Fluent.Unions;
```

| Operators | ok | error |
| --- | --- | --- |
| `Select`, `Chain`, `Ensure`, `Zip`, `ChainAppend`, `Do(onSuccess)` | runs | passes through |
| `Finally` | runs | runs |

All of them also exist on `Task<Result<T>>`. Asynchronous lambdas use the `...Await` variants. See [Async](../async/README.md).

## Select

Projects the value. An error passes through, and the lambda does not run. A selector that returns `null` gives an error, because a result cannot hold `null`; return an `Option<T>` when a value may be missing.

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

### Combine with the previous value

`Chain(step, combine)` runs the step and then combines its value with the previous one. The step runs only when the result is ok, and `combine` only when both are:

```csharp
Result<string> summary = GetCustomer(id).Chain(
    GetCart,
    (customer, cart) => $"{customer.Name}: {cart.Items.Sum(i => i.Price)}");
```

`ChainAwait(step, combine)` takes an asynchronous step. To keep both values as a tuple instead, use `ChainAppend`.

## Zip

Combines two results that already exist. The first error wins. Both sides must be ok to produce a value.

```csharp
Result<(Customer First, Customer Second)> pair = GetCustomer(buyer).Zip(GetCustomer(recipient));
Result<string> line = GetCustomer(buyer).Zip(GetCustomer(recipient), (b, r) => $"{b.Name} sends to {r.Name}");
```

Both arguments are evaluated before `Zip` runs, as with any C# method call. `GetCustomer(recipient)` runs even when `GetCustomer(buyer)` failed. Use `Zip` for independent inputs. When the second call should only run after the first succeeded, or needs its value, use [`Chain(step, combine)`](#combine-with-the-previous-value), `ChainAppend` or query syntax:

```csharp
Result<string> lazy = GetCustomer(buyer).Chain(_ => GetCustomer(recipient), (b, r) => $"{b.Name} sends to {r.Name}");
```

As a rule, an argument that is a value, as in `Zip`, `CombineAll` or `Catch(fallback)`, is already evaluated. An argument that is a lambda runs only when the operator needs it.

## Ensure

Checks the value and turns a failed check into an error. An error passes through, and the predicate does not run. You always supply the message or the exception.

```csharp
Result<Cart> checkedCart = GetCustomer(id)
    .Chain(GetCart)
    .Ensure(c => c.Items.Count > 0, "The cart is empty.");
```

With a message the error is a [`ValidationError`](../validation/README.md#errors). To produce your own exception, pass a function:

```csharp
Result<Cart> stocked = GetCustomer(id)
    .Chain(GetCart)
    .Ensure(c => c.Items.Count > 0, c => new EmptyCartException(c.Id));
```

A predicate that throws becomes the error. Use `EnsureAwait` for an asynchronous predicate. Both forms also exist on `Task<Result<T>>`.

```csharp
Result<Cart> available = await GetCustomer(id)
    .Chain(GetCart)
    .EnsureAwait(c => stock.HasAllAsync(c), "Some items are out of stock.");
```

## CombineAll

Combines two to four independent results into a tuple. Unlike `Zip`, it collects every error: a `ValidationErrors` when all of them are validation failures, otherwise a flat `AggregateException`.

```csharp
Result<(Customer, Cart)> both = Result.CombineAll(GetCustomer(id), GetCustomer(id).Chain(GetCart));
```

`Result.CombineAllAsync` takes tasks, awaits all of them and turns a faulted task into a collected error:

```csharp
Result<(Customer, Cart, Shipping)> all = await Result.CombineAllAsync(
    LoadCustomerAsync(id),
    LoadCartAsync(id),
    LoadShippingAsync(id));
```

## ChainAppend

Runs a step that depends on the value and keeps both in a tuple. Later steps take the tuple elements as separate parameters, so a pipeline can collect values without nesting.

```csharp
Result<(Customer, Cart, Shipping)> context = GetCustomer(id)
    .ChainAppend(GetCart)
    .ChainAppend((customer, cart) => Shipping(cart));
```

It stops at the first error. An error passes through and the lambda does not run. `ChainAppendAwait` takes an asynchronous step. Both forms exist on `Task<Result<T>>` and work up to a four-element tuple.

## WithValue and DiscardValue

`WithValue` gives a successful `Result` a value. An error passes through. A `null` value throws `ArgumentNullException`.

```csharp
Result<Order> placed = Save(known).WithValue(order);
```

`DiscardValue` goes the other way and drops the value.

```csharp
Result saved = GetCustomer(id).Chain(GetCart).DiscardValue();
```

Both also exist on `Task` of a result.

## Do

Runs an action with the value and returns the same result. Use it for logging and other side effects. It does not run for an error.

```csharp
Result<Customer> logged = GetCustomer(id)
    .Do(c => log.LogInformation("Loaded {Name}", c.Name));
```

`Do(onSuccess, onError)` runs one action or the other:

```csharp
Result<Customer> logged = GetCustomer(id).Do(
    c => log.LogInformation("Loaded {Name}", c.Name),
    e => log.LogWarning(e, "Customer {Id} failed", id));
```

An exception thrown by the action becomes the error, even when the result was ok before. For logging and metrics, whose failure must not turn a success into an error, use `Tap`.

## Tap

`Tap`, `TapError` and `TapAwait` run an observer and return the same result, like `Do`, but never capture. An exception thrown by the observer propagates instead of replacing the result:

```csharp
Result<Customer> observed = GetCustomer(id)
    .Tap(c => log.LogInformation("Loaded {Name}", c.Name))
    .TapError(e => log.LogWarning(e, "Customer {Id} failed", id));
```

See [Do or Tap](error-handling.md#do-or-tap) for when to use which.

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
- After a failed chain, `HasValue` is `false` and `.Value` throws `InvalidOperationException`. Check `HasValue` outside the chain.

It works in asynchronous chains without awaiting each step. The holder is filled when the task completes, before any later step runs:

```csharp
Result<string> confirmation = await GetCustomerAsync(id).Out(out var customer)
    .ChainAwait(GetCartAsync).Out(out var cart)
    .ChainAwait(_ => SubmitAsync(customer, cart))
    .Select(c => $"{c.Number} for {customer.Value.Name}");
```

`Out` exists on `Result<T>`, `Option<T>` and their `Task` versions, not on `Result`. For purely sequential steps, query syntax keeps earlier values in scope too. See [Patterns](patterns.md#using-earlier-values).

## Finally

Runs an action for ok and error alike, and returns the same result. Use it for cleanup and timing.

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
Result<Order> timed = order.Finally(() => log.LogInformation("Checkout took {Ms} ms", sw.ElapsedMilliseconds));
```

## GetValueOrDefault

Leaves the chain with the value. For an error you get `default`, a fallback or the factory's result.

```csharp
Cart? maybeCart = GetCustomer(id).Chain(GetCart).GetValueOrDefault();
Cart orEmpty = GetCustomer(id).Chain(GetCart).GetValueOrDefault(new Cart(id, []));
Cart lazy = GetCustomer(id).Chain(GetCart).GetValueOrDefault(() => new Cart(id, []));
int count = await GetCustomerAsync(id).Chain(GetCart).Select(c => c.Items.Count).GetValueOrDefault(0);
```

The factory runs only when the result is not ok. An exception from the factory is not caught. It propagates, because this operator leaves the chain. All three overloads also exist on `Task<Result<T>>`.

## Unwrap

Flattens a nested result, like `Task.Unwrap()`. Nesting appears when generic code wraps a result of yours in its own, such as a cache that returns `Result<T>` and loads a value that is a result itself:

```csharp
Result<Result<Payment>> nested = cache.GetOrLoad(order.Id, () => Pay(order));
Result<Payment> flat = nested.Unwrap();
```

`Select` with a function that returns a result would nest too, but analyzer error [FLU0006](../analyzer/README.md#flu0006) rejects it. Use `order.Chain(Pay)`, which gives the flat result directly.

## Match

Leaves the chain with one callback for the value and one for the error. Both are required, so neither case is forgotten.

```csharp
string text = order.Match(
    onSuccess: o => $"Order total {o.Total}",
    onError: e => $"Failed: {e.Message}");

int status = Save(known).Match(() => 204, e => 500);
```

On a `Result`, the first callback takes no argument. `Match` also works on `Task<Result<T>>`, so `await LoadOrderAsync(id).Match(...)` ends an asynchronous pipeline. An exception thrown by a callback propagates.

## Pitfalls

- `Select` with a function that returns a result nests. Use `Chain`.
- A `default` result is an error, not a value. Initialize every result you return.
- `Do` does not run for errors. Use `OnErrorDo`, `TapError` or `Do(onSuccess, onError)`.

## See also

- [Core concepts](core-concepts.md)
- [Error handling](error-handling.md)
- [Patterns](patterns.md)
- [Async](../async/README.md)

---

Back to: [Result](README.md) · Next: [Error handling](error-handling.md)
