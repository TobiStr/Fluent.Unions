# Collections

This page covers the operators on `IEnumerable<Result<T>>` that turn a batch of results into one outcome or split it apart.

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

IEnumerable<Result<Customer>> customers = ids.Select(GetCustomer);
```

All five operators skip empty results. None of them counts an empty result as a value or an error.

| Operator | Returns | On errors |
| --- | --- | --- |
| `Combine()` | `Result<IReadOnlyList<T>>` | The first error wins. |
| `CombineAll()` | `Result<IReadOnlyList<T>>` | Every error, in an `AggregateException`. |
| `Partition()` | `(Values, Errors)` | Collects them, no failure. |
| `Values()` | `IEnumerable<T>` | Ignores them. |
| `Errors()` | `IEnumerable<Exception>` | Returns only them. |

## Combine

Collects all values into one result. Use it when the batch is all or nothing, such as loading every product of a cart.

```csharp
Result<IReadOnlyList<Customer>> all = customers.Combine();
```

With ids `[good, missing, good, missing]`, the result is the error of the first missing customer. `Combine` stops enumerating at the first error, so a lazy `Select` is evaluated only up to that point. A `null` sequence throws `ArgumentNullException`.

`Combine` is deliberately not called `ToList()`. Overload resolution would prefer it over `Enumerable.ToList()` and silently change what existing code returns.

## CombineAll

Like `Combine`, but reports every failure. Use it for input validation, where the user should see all problems at once.

```csharp
Result<IReadOnlyList<Customer>> allErrors = customers.CombineAll();
```

On failure, the error is an `AggregateException` that holds all errors, flattened. For the example ids it holds two inner exceptions. A single failure is wrapped as well, so you can always catch one type:

```csharp
string summary = allErrors.Match(
    list => $"{list.Count} customers loaded",
    e => e is AggregateException agg ? $"{agg.InnerExceptions.Count} failed" : e.Message,
    () => "nothing");
```

## Partition

Splits values and errors in one pass. Use it when partial success is fine: import what worked, report the rest.

```csharp
var (values, errors) = customers.Partition();

foreach (var error in errors)
    log.LogWarning(error, "Skipped a customer");
```

`values` and `errors` are `IReadOnlyList` snapshots, so enumerating them again does not call your producer again. For the example ids, `values` has 2 entries and `errors` has 2.

## Values and Errors

`Values()` and `Errors()` are lazy filters. Use them when you need only one side:

```csharp
IEnumerable<Customer> found = customers.Values();
IEnumerable<Exception> failures = customers.Errors();
```

Being lazy, they re-run the source each time you enumerate. Call `.ToList()` when the source is expensive. Use `Partition` when you need both sides, because it enumerates once.

## Asynchronous batches

`Result.WhenAll` awaits all tasks, then combines them like `Combine()`. The first error in sequence order wins, and a faulted task counts as an error.

```csharp
Result<IReadOnlyList<Order>> orders = await Result.WhenAll(ids.Select(LoadOrderAsync));
```

It also accepts `IEnumerable<Task<Result>>` and then returns the first error or `Ok`.

For every error instead of the first, use `Task.WhenAll` and `CombineAll`. A faulted task throws there, as `Task.WhenAll` always does:

```csharp
Result<IReadOnlyList<Order>> allOrderErrors =
    (await Task.WhenAll(ids.Select(LoadOrderAsync))).CombineAll();
```

Async streams have their own helpers:

```csharp
await foreach (var order in StreamOrders(ids).Values())
    log.LogInformation("{Total}", order.Total);

Result<IReadOnlyList<Order>> collected = await StreamOrders(ids).CombineAsync();
```

`StreamOrders` returns an `IAsyncEnumerable<Result<Order>>`. An exception thrown by the stream becomes the error of `CombineAsync`. See [Async](../async/README.md) for streams, timeouts and cancellation.

## Pitfalls

- `Combine` and `CombineAll` skip empty results. A batch where every result is empty gives an ok result with an empty list.
- `Result.WhenAll` reports the first error in sequence order, not the error that happened first in time.
- Tasks started by `ids.Select(LoadOrderAsync)` begin when you enumerate. Pass the sequence straight to `Result.WhenAll` so that all of them start together, instead of awaiting inside a loop.

## See also

- [Error handling](error-handling.md)
- [Patterns](patterns.md)
- [Async](../async/README.md)

---

Back to: [Result](README.md) · Next: [Patterns](patterns.md)
