# Async

Every operator also works on `Task<Result<T>>`, `Task<Result>` and `Task<Option<T>>`, so a pipeline stays one expression after the first asynchronous call. This section covers those overloads, running tasks in parallel, retrying, and streaming.

## In this section

- [Task pipelines](task-pipelines.md): the `Task` overloads, the `...Await` variants for asynchronous lambdas, async query syntax, `Out`, `Match` and cancellation.
- [Concurrency and resilience](concurrency-and-resilience.md): `Result.WhenAll`, collecting all errors, `Retry`, `RetryAsync` and `Timeout`.
- [Streams](streams.md): `IAsyncEnumerable<Result<T>>` and `IAsyncEnumerable<Option<T>>`.

## Quick example

```csharp
using Fluent.Unions;

Result<string> label = await LoadOrderAsync(id)
    .Ensure(order => order.Items.Count > 0, "The order has no items.")
    .ChainAwait(order => LoadCustomerAsync(order.CustomerId))
    .Select(customer => customer.Name)
    .Catch((KeyNotFoundException _) => "unknown customer");
```

Without the overloads, each step would need its own `await` and its own state check.

## When to use

- You call asynchronous methods that return `Task<Result<T>>` or `Task<Option<T>>` and want to chain them without intermediate variables.
- You start a pipeline from code that throws, with `Result.TryAsync`.
- You run several results in parallel, retry a flaky call, or consume a stream of results.

## Rules

- A faulted task becomes an error result. For options, it stays faulted, because an option has no error state. `await` throws the original exception.
- A lambda that throws inside a result operator becomes the error. Inside an option operator, it propagates.
- The operators take no `CancellationToken`, because they do no I/O. Capture the token in your lambdas.
- Use the `...Await` variants when the lambda returns a `Task`. The plain operators take synchronous lambdas.
- There are no `Task` overloads for custom result types or for `Result<T, TError>`. Await first.

---

Back to index: [Docs index](../README.md) · Next: [Task pipelines](task-pipelines.md)
