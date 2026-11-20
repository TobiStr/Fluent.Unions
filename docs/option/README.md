# Option

`Option<T>` is a value that may be missing, where missing is normal and not an error. It holds a `T` or `None`. It is a struct, so there is no `null` option.

## In this section

- [Core concepts](core-concepts.md): what an option is, how to create one, its members, and how to match it.
- [Operations](operations.md): `Select`, `Chain`, `Where`, `Zip`, `Or`, `Match` and the other operators.
- [Collections](collections.md): `FirstOrNone`, `GetValueOrNone`, `Values`, `Combine` and friends.
- [Conversions](conversions.md): between options, nullable types and results.
- [Patterns](patterns.md): replacing `null` returns, fallbacks, optional configuration and DTO fields.

## Quick example

```csharp
using Fluent.Unions;

static Option<Customer> FindCustomer(Guid id) =>
    customers.GetValueOrNone(id);

string label = FindCustomer(id)
    .Select(customer => customer.Name)
    .GetValueOrDefault("guest"); // "Ada", or "guest" when the customer does not exist
```

## When to use

- A lookup that finds nothing is a normal outcome: a cache miss, a customer without a nickname, an empty cart.
- You want the caller to handle the missing case, and you do not want to rely on `null` checks.
- Use a `Result<T>` instead when the failure has a reason the caller should see. See [patterns](patterns.md#option-result-or-nullable).

## Rules

- Write `None or null` or `_` in a `switch`, not only `None`. A `default` option holds neither case. See [core concepts](core-concepts.md#matching-an-option).
- `0`, `false` and `""` are values. Only `null` and `None` mean no value.
- Option operators never catch exceptions. An exception thrown in a lambda, or a faulted task, propagates.
- `SingleOrNone` throws when there are several elements, as `SingleOrDefault` does.
- `Option.Some(null)` throws `ArgumentNullException`. Use `ToOption()` when the value may be `null`.

---

Back to index: [Docs index](../README.md) · Next: [Core concepts](core-concepts.md)
