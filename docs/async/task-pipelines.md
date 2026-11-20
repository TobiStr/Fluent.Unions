# Task pipelines

This page covers the `Task` overloads of the operators, the `...Await` variants, async query syntax, `Out`, `Match` and cancellation.

```csharp
using Fluent.Unions;
```

## Task overloads

The operators exist on `Task<Result<T>>`, `Task<Result>` and `Task<Option<T>>` with the same names and behavior as on the plain types. They take synchronous lambdas, and they await the task for you.

```csharp
Result<string> name = await LoadCustomerAsync(id)       // Task<Result<Customer>>
    .Ensure(customer => customer.Age >= 18, "The customer must be an adult.")
    .Do(customer => logger.LogInformation("Found {Name}", customer.Name))
    .Select(customer => customer.Name)
    .Catch(e => "unknown");
```

For options:

```csharp
Option<string> nickname = await FindCustomerAsync(id)   // Task<Option<Customer>>
    .Select(customer => customer.Nickname)
    .DefaultIfEmpty("none");
```

**A faulted task becomes an error result.** Every result operator, and `Result.TryAsync`, turns the task's exception into the error. A cancelled task is the exception: its `OperationCanceledException` propagates, as everywhere else in .NET.

```csharp
Result<int> result = await Task.FromException<Result<int>>(new IOException("disk"))
    .Select(n => n + 1);
// error: disk
```

**A faulted task stays faulted for options.** An option has no error state, so `await` throws the original exception.

```csharp
Option<int> option = await Task.FromException<Option<int>>(new IOException("disk"))
    .Select(n => n + 1);
// throws IOException
```

Use `Result.TryAsync` at the start of a pipeline when the source throws and you want a result:

```csharp
Result<Order> order = await Result.TryAsync(() => LoadOrderAsync(id))
    .Select(order => order with { Status = "Seen" });
```

## The Await variants

A lambda that itself returns a `Task` needs the `...Await` variant, named after System.Linq.Async. They exist on `Result<T>`, `Result`, `Task<Result<T>>` and `Task<Result>`. For options, they exist on `Option<T>` and `Task<Option<T>>` for the value operators.

| Operator | Lambda | On results | On options |
| --- | --- | --- | --- |
| `SelectAwait` | `T -> Task<TOut>` | yes | yes |
| `ChainAwait` / `SelectManyAwait` | `T -> Task<Result<TOut>>` | yes | yes (`Task<Option<TOut>>`) |
| `WhereAwait` | `T -> Task<bool>` | yes | yes |
| `DoAwait` | `T -> Task` | yes | yes |
| `CatchAwait` | `Exception -> Task<Result<T>>` | yes | no, options have no errors |
| `FinallyAwait` | `() -> Task` | yes | no |

```csharp
Result<Address> address = await LoadCustomerAsync(id)
    .ChainAwait(customer => LoadAddressAsync(customer.AddressId))
    .DoAwait(address => cache.StoreAsync(address))
    .CatchAwait((KeyNotFoundException _) => LoadDefaultAddressAsync());
```

`CatchAwait` also has a typed overload, like `Catch`, that handles only that exception type.

```csharp
Result<Order> order = await LoadOrderAsync(id)
    .FinallyAwait(() => metrics.FlushAsync());
```

`FinallyAwait` runs in every state.

For options:

```csharp
Option<Address> address = await FindCustomerAsync(id)
    .ChainAwait(customer => FindAddressAsync(customer.AddressId))
    .WhereAwait(address => IsDeliverableAsync(address));
```

`Task<Option<T>>` has no `Unwrap`. Use `ChainAwait` instead of `SelectAwait` when the step returns an option.

## Async query syntax

Query expressions work across tasks, for results and options. Every earlier value stays in scope.

```csharp
Result<string> label = await (
    from customer in LoadCustomerAsync(id)
    from address in LoadAddressAsync(customer.AddressId)
    select $"{customer.Name}, {address.City}");
```

```csharp
Option<string> label = await (
    from customer in FindCustomerAsync(id)
    from address in FindAddressAsync(customer.AddressId)
    where address.Country == "DE"
    select $"{customer.Name}, {address.City}");
```

Wrap the whole query in parentheses before `await`.

## Out in async chains

`Out(out var value)` works on tasks without awaiting each step. The holder is assigned at once and fills when the task completes, before any later step runs.

```csharp
Result<string> confirmation = await LoadCustomerAsync(id).Out(out var customer)
    .ChainAwait(c => LoadCartAsync(c.Id)).Out(out var cart)
    .ChainAwait(_ => SubmitAsync(customer, cart));
```

Reading `customer.Value` before the task completes, or after a failed step, throws `InvalidOperationException`. Check `HasValue` outside the chain. For purely sequential steps, query syntax is simpler.

## Match at the end of a pipeline

`Match` on a `Task` ends the pipeline and returns `Task<TOut>`. On results it takes three callbacks, for ok, error and empty. On options it takes two.

```csharp
IResult response = await LoadCustomerAsync(id).Match(
    customer => Results.Ok(customer),
    error => Results.Problem(error.Message),
    () => Results.NotFound());
```

```csharp
string text = await FindCustomerAsync(id).Match(
    customer => $"Hello {customer.Name}",
    () => "Hello guest");
```

Exceptions thrown by `Match` callbacks propagate. `GetValueOrDefault` has `Task` overloads for the same purpose. `Check()` on a `Task<Result<T>>` throws the error: `await LoadCustomerAsync(id).Check()`.

## CancellationToken

The operators take no token, because they do no I/O. Capture it in your lambdas and pass it to the asynchronous methods you call.

```csharp
async Task<Result<Customer>> LoadAsync(Guid id, CancellationToken ct) =>
    await LoadCustomerAsync(id, ct)
        .ChainAwait(customer => EnrichAsync(customer, ct));
```

- A cancelled operation throws `OperationCanceledException` out of the pipeline, for results and options alike. It is never turned into an error, so ASP.NET Core aborts the request instead of answering 500.
- Check `ct.IsCancellationRequested` or call `ct.ThrowIfCancellationRequested()` in a lambda when a long synchronous step should stop early.
- Use `Result.RetryAsync` and `CombineAsync` when you want the library to pass the token. See [concurrency and resilience](concurrency-and-resilience.md) and [streams](streams.md).

## Pitfalls

- Using `Select` with an asynchronous lambda gives a `Result<Task<T>>`, not a `Result<T>`. Use `SelectAwait`.
- A `Task<Option<T>>` that faults throws at `await`. Wrap it with `Result.TryAsync` if you want an error.
- Do not call `.Result` or `.Wait()` on the pipeline. Await it.

## See also

- [Concurrency and resilience](concurrency-and-resilience.md)
- [Streams](streams.md)
- [Option operations](../option/operations.md)

---

Back to: [Async](README.md) · Next: [Concurrency and resilience](concurrency-and-resilience.md)
