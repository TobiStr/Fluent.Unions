# Operations

This page covers the operators on `Option<T>`. They live in `Fluent.Unions` and are named after LINQ and Rx.NET.

```csharp
using Fluent.Unions;
```

Every operator that takes a value runs only when the option holds one. A `None` or `default` option passes through as `None`.

**Option operators never capture exceptions.** An option has no error state, so an exception thrown in a lambda propagates to the caller. This differs from `Result<T>`, where it becomes the error. A `null` lambda argument throws `ArgumentNullException`.

## Select

Projects the value. A `null` projection gives `None`.

```csharp
Option<Customer> customer = FindCustomer(id);

Option<string> name = customer.Select(c => c.Name);       // "Ada"
Option<int> length = customer.Select(c => c.Name.Length); // 3
Option<string> nick = customer.Select(c => c.Nickname);   // None when Nickname is null
```

## Chain and SelectMany

`Chain` links a call that returns an option. `SelectMany` is the LINQ name for the same operator, and it makes query syntax work.

```csharp
Option<Address> address = FindCustomer(id).Chain(c => FindAddress(c.AddressId));
```

A `None` anywhere stops the chain. Use `Select` when the next step returns a plain value, and `Chain` when it returns an option. `Select` with an option-returning function would give an `Option<Option<T>>`, which analyzer error [FLU0006](../analyzer/README.md#flu0006) rejects.

## Where

Keeps the value when the predicate holds, otherwise gives `None`.

```csharp
Option<Customer> adult = FindCustomer(id).Where(c => c.Age >= 18);
```

## Zip

Combines two options. The result holds a value only when both do. Both options are evaluated before `Zip` runs; when the second lookup should only run after the first found a value, use `Chain` or query syntax.

```csharp
Option<Customer> customer = FindCustomer(id);
Option<Product> product = FindProduct(sku);

Option<(Customer First, Product Second)> pair = customer.Zip(product);
Option<string> line = customer.Zip(product, (c, p) => $"{c.Name} bought {p.Name}");
```

## Do, OnNoneDo and Finally

These run side effects and return the same option.

| Operator | Runs |
| --- | --- |
| `Do(onSome)` | `onSome` with the value |
| `Do(onSome, onNone)` | `onSome` with the value, or `onNone` |
| `OnNoneDo(onNone)` | `onNone` when there is no value |
| `Finally(action)` | `action` in both cases |

```csharp
Option<Customer> customer = FindCustomer(id)
    .Do(c => logger.LogInformation("Found {Name}", c.Name))
    .OnNoneDo(() => logger.LogWarning("Customer {Id} not found", id));
```

The returned option is always a real `None` when there is no value, even if the input was `default`.

## Out

`Out(out var value)` captures the value for later steps of the chain. The holder is a `Captured<T>`, and it fills only when the option holds a value.

```csharp
Option<string> receipt = FindCustomer(id).Out(out var customer)
    .Chain(c => FindCart(c.Id))
    .Select(cart => $"{customer.Value.Name}: {cart.Items} items");
```

Inside the chain, later steps run only after a value, so `customer.Value` is safe there. Outside the chain, check `customer.HasValue` first, because `.Value` throws `InvalidOperationException` when nothing was captured. [Using earlier values](../result/patterns.md#using-earlier-values) describes `Captured<T>` in detail.

## GetValueOrDefault

Leaves the option.

| Call | Returns when there is no value |
| --- | --- |
| `GetValueOrDefault()` | `default(T)`, which is `null` for references |
| `GetValueOrDefault(fallback)` | `fallback` |
| `GetValueOrDefault(() => ...)` | the factory's result |

```csharp
string? nickname = customer.Select(c => c.Nickname).GetValueOrDefault();
string name = customer.Select(c => c.Name).GetValueOrDefault("guest");
Cart cart = FindCart(id).GetValueOrDefault(() => Cart.CreateEmpty(id));
```

The factory runs only when needed, and an exception it throws propagates.

## DefaultIfEmpty

Stays an option, but turns `None` into an option holding the fallback.

```csharp
Option<int> requested = Option.None;

Option<int> quantity = requested.DefaultIfEmpty(1); // 1
Option<Cart> cart = FindCart(id).DefaultIfEmpty(() => Cart.CreateEmpty(id));
```

Use `DefaultIfEmpty` to keep chaining. Use `GetValueOrDefault` to leave the option.

## Or

Uses another option when there is no value. The alternative is a factory, so it runs only when needed.

```csharp
Option<Customer> customer = cache.Find(id).Or(() => database.Find(id));
```

## Unwrap

Flattens `Option<Option<T>>`. A nested option comes from code that stores options, such as a cache that remembers a missing address. `None` means not cached, and an inner `None` means cached as missing:

```csharp
Option<Option<Address>> cached = addressCache.Find(id);
Option<Address> flat = cached.Unwrap();
```

## Match

Returns a value with one callback for the value and one for `None`. It is the usual way to leave a pipeline.

```csharp
string text = FindCustomer(id).Match(
    customer => $"Hello {customer.Name}",
    () => "Hello guest");
```

Exceptions from either callback propagate.

## Query syntax

`Select`, `SelectMany` and `Where` follow the LINQ query pattern, so query expressions work. Every earlier value stays in scope.

```csharp
Option<string> label =
    from customer in FindCustomer(id)
    from address in FindAddress(customer.AddressId)
    where address.Country == "DE"
    select $"{customer.Name}, {address.City}";
```

If any `from` gives `None`, or the `where` fails, the whole expression is `None`.

## Pitfalls

- A lambda that throws throws out of the chain. Wrap risky code in `Result.Try` and convert with `ToOption()` if you want the exception captured and dropped.
- `Select` with a function that returns an option nests it. Use `Chain`.
- `GetValueOrDefault()` without arguments returns `null` for references. Prefer the overload with a fallback.

## See also

- [Collections](collections.md)
- [Async task pipelines](../async/task-pipelines.md)
- [Conversions](conversions.md)

---

Back to: [Option](README.md) · Next: [Collections](collections.md)
