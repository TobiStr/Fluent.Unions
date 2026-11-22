# Concurrency and resilience

This page covers running several results in parallel, collecting all errors, retrying, and timeouts.

```csharp
using Fluent.Unions;
```

## Result.WhenAll

`Result.WhenAll` awaits all tasks and combines their results like `Combine()`. It takes an `IEnumerable<Task<Result<T>>>` and returns the values in sequence order. There is a non-generic version for `Task<Result>` that returns `Ok` or the first error.

```csharp
IEnumerable<Task<Result<Product>>> loads = skus.Select(LoadProductAsync);

Result<IReadOnlyList<Product>> products = await Result.WhenAll(loads);
```

```csharp
Result saved = await Result.WhenAll(items.Select(SaveItemAsync)); // Ok, or the first error
```

Rules:

- **All tasks run to completion.** `WhenAll` waits for every task, even after one failed. It does not cancel the others.
- **The first error by order wins**, not the first to fail in time. With the tasks `[A, B, C]`, an error in `B` beats an error in `C` even if `C` failed earlier.
- **A faulted task counts as an error.** It does not make `WhenAll` throw. A cancelled task does: its `OperationCanceledException` propagates.

```csharp
var tasks = new[]
{
    Task.FromResult<Result<int>>(1),
    Task.FromException<Result<int>>(new IOException("disk")),
    Task.FromResult<Result<int>>(new TimeoutException("slow"))
};

Result<IReadOnlyList<int>> result = await Result.WhenAll(tasks);
// error: disk
```

## Collecting all errors

`Result.WhenAll` reports only one error. To report every error, await with `Task.WhenAll` and use `CombineAll`:

```csharp
Result<Product>[] results = await Task.WhenAll(skus.Select(LoadProductAsync));

Result<IReadOnlyList<Product>> products = results.CombineAll();
// ok with all products, or every error: ValidationErrors or AggregateException
```

`Task.WhenAll` throws when a task is faulted, so every task must return a result instead of throwing. Wrap sources that throw with `Result.TryAsync`:

```csharp
Result<Product>[] results = await Task.WhenAll(
    skus.Select(sku => Result.TryAsync(() => LoadProductRawAsync(sku))));
```

For the split view, use `Partition()` on the results. See [validation](../validation/README.md#partition-for-reporting).

## Result.Retry and Result.RetryAsync

Both run an operation again while it fails, as Rx's `Retry` does. They return the last result.

| Parameter | Meaning |
| --- | --- |
| `maxAttempts` | Total attempts, including the first. Must be at least 1, otherwise `ArgumentOutOfRangeException`. |
| `delay` | `RetryAsync` only. Wait between attempts. Default: none. |
| `shouldRetry` | Optional. Called with the error. Return `false` to stop retrying and return that error. |
| `cancellationToken` | `RetryAsync` only. Passed to the operation, and stops retrying. |

```csharp
Result<Rate> rate = Result.Retry(() => LoadRate(currency), maxAttempts: 3);
```

```csharp
Result<Payment> payment = await Result.RetryAsync(
    ct => ChargeAsync(order, ct),
    maxAttempts: 4,
    delay: TimeSpan.FromSeconds(1),
    shouldRetry: error => error is HttpRequestException or TimeoutException,
    cancellationToken: cancellationToken);
```

Details:

- An exception thrown by the operation counts as a failure, like an error result. `Retry` lets an `OperationCanceledException` propagate. `RetryAsync` retries one that its token did not cause, such as an `HttpClient` timeout, like any other failure.
- `shouldRetry` is only called for errors, and not after the last attempt. An error it rejects is returned at once.
- The `delay` is the same for every attempt. There is no backoff or jitter. Write your own loop, or use [Polly](../polly/README.md), when you need one.
- **Cancellation stops retrying.** When your token is cancelled, `RetryAsync` throws `OperationCanceledException` instead of waiting or retrying again.
- Retry only operations that are safe to repeat. A payment call needs an idempotency key.

## Timeout

`Timeout` gives an error holding a `TimeoutException` when a `Task<Result<T>>` or `Task<Result>` takes too long.

```csharp
Result<Customer> customer = await LoadCustomerAsync(id).Timeout(TimeSpan.FromSeconds(5));
// error: TimeoutException, when the load takes longer than 5 seconds
```

**The operation keeps running.** `Timeout` stops waiting. It does not cancel anything. Cancel a token after the timeout so the work stops too:

```csharp
using var cts = new CancellationTokenSource();

Result<Customer> customer = await LoadCustomerAsync(id, cts.Token)
    .Timeout(TimeSpan.FromSeconds(5))
    .Finally(cts.Cancel);
```

The error stays the `TimeoutException`. Do not give the token the same deadline as `Timeout`: when the token fires first, the load throws `OperationCanceledException`, which propagates instead of becoming an error.

With ASP.NET Core, the `TimeoutException` maps to HTTP 504. See [ASP.NET Core](../aspnetcore/README.md).

## Pitfalls

- `Result.WhenAll` reports one error. Use `Task.WhenAll` with `CombineAll` for all of them.
- `Task.WhenAll` throws when a task faults. Make sure each task returns a result.
- Starting the tasks is your job. `WhenAll(skus.Select(LoadProductAsync))` starts them while it enumerates. Materialize a lazy sequence once, with `ToList()`, if you enumerate it twice.
- `Timeout` does not cancel the work.

## See also

- [Task pipelines](task-pipelines.md)
- [Validation](../validation/README.md)
- [ASP.NET Core](../aspnetcore/README.md)

---

Back to: [Async](README.md) · Next: [Streams](streams.md)
