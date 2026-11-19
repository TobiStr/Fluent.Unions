# Result

`Result` and `Result<T>` are unions that hold the outcome of an operation: a value (or `Ok`) when it worked, an `Exception` when it failed. The operators in `Fluent.Unions.Extensions` chain results without `try`/`catch` and without `null` checks.

## In this section

- [Core concepts](core-concepts.md): the three states, creating results, members, `switch`, deconstruction and `Check()`.
- [Operations](operations.md): `Select`, `Chain`, `Where`, `Zip`, `Do`, `Out`, `Finally`, `DefaultIfEmpty`, `GetValueOrDefault`, `Unwrap`, `Match`.
- [Error handling](error-handling.md): `Catch`, `OnErrorDo`, `Try`, `Retry`, and what still throws.
- [Collections](collections.md): `Combine`, `CombineAll`, `Partition`, `Values` and `Errors` for batches.
- [Patterns](patterns.md): railway pipelines, query syntax, early returns, service layers, and when not to use results.

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

Result<string> label = GetCustomer(id)
    .Chain(GetCart)
    .Chain(PlaceOrder)
    .Select(order => $"Order {order.Id}")
    .Catch(e => "Checkout failed");
```

## When to use

- A method can fail and the caller should look at the outcome.
- You want to combine several fallible steps into one expression.
- You want the compiler to check that every state is handled.

Use `Option<T>` instead when a missing value is normal and carries no error. See [Option](../option/README.md).

## Rules

- A result has three states: ok, error and empty (`default`). Most bugs come from forgetting the third. See [Core concepts](core-concepts.md).
- Operators on results never throw because of an exception inside your lambda. The exception becomes the error. Only `Check()` throws on purpose.
- A `null` lambda argument throws `ArgumentNullException` immediately.
- A `null` value, whether returned from a selector or converted implicitly, gives an empty result, not an ok one.
- `Match` on a result always takes three callbacks: value, error and empty.
- Ignoring a returned result gives analyzer warning FLU0001.

---

Back to index: [Docs index](../README.md) · Next: [Core concepts](core-concepts.md)
