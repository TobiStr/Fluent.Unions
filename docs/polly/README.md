# Polly

The package `Fluent.Unions.Polly` (namespace `Fluent.Unions.Polly`) connects `Result<T>` to [Polly](https://www.pollydocs.org) resilience strategies. It depends on `Polly.Core` and adds no retry logic of its own.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): retry a call that returns a result.
- [What you get back](#what-you-get-back): the result for each outcome.
- [With your own result type](#with-your-own-result-type): the same members for your own result type.
- [When to use](#when-to-use): where it helps, and when the core operators are enough.
- [Rules](#rules): what is handled and what is not.

## Quick example

```csharp
using Polly;
using Polly.Retry;
using Fluent.Unions.Polly;

var pipeline = new ResiliencePipelineBuilder<Result<Payment>>()
    .AddRetry(new RetryStrategyOptions<Result<Payment>>
    {
        ShouldHandle = new PredicateBuilder<Result<Payment>>()
            .HandleError(error => error is HttpRequestException),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
    })
    .Build();

Result<Payment> payment = await pipeline.ExecuteResultAsync(ct => ChargeAsync(order, ct), cancellationToken);
// an Ok payment after the last attempt, or the last HttpRequestException as the error
```

`ChargeAsync` returns `ValueTask<Result<Payment>>`. Retry only operations that are safe to repeat, and give a payment call an idempotency key.

## What you get back

| Outcome of the pipeline | Result |
| --- | --- |
| the operation returns an Ok result | that result |
| the operation returns an error result | that result, after the strategies ran |
| the operation or a strategy throws | error holding the exception |
| the token is cancelled | `OperationCanceledException` is thrown |

| Member | On |
| --- | --- |
| `HandleError()` | `PredicateBuilder<Result<T>>` |
| `HandleError(predicate)` | `PredicateBuilder<Result<T>>` |
| `ExecuteResultAsync(operation, cancellationToken)` | `ResiliencePipeline<Result<T>>` and `ResiliencePipeline` |
| `ToResult()` | `Outcome<Result<T>>` |

`ToResult` converts the outcome that Polly passes to callbacks such as `OnRetry`: an exception becomes an error, and an outcome without a value is empty.

## With your own result type

A result type declared with `[FluentResult<TError>]` (see [your own result type](../errors/custom-error-types.md)) and `Result<T, TError>` work with the same members. `HandleError<TResult, TError>()` and `ExecuteResultAsync<TResult, TError>(...)` take two type arguments: the result type and its error type. Polly's builders carry only the result type, so the compiler cannot infer `TError`. Write both at the call, for example `HandleError<Result<int>, DomainException>()`.

```csharp
var pipeline = new ResiliencePipelineBuilder<CallResult<Payment>>()
    .AddRetry(new RetryStrategyOptions<CallResult<Payment>>
    {
        ShouldHandle = new PredicateBuilder<CallResult<Payment>>()
            .HandleError<CallResult<Payment>, CallError>(error => error.Value is Transient),
        MaxRetryAttempts = 3,
    })
    .Build();

CallResult<Payment> payment = await pipeline
    .ExecuteResultAsync<CallResult<Payment>, CallError>(ct => ChargeAsync(order, ct), cancellationToken);
// an Ok payment, or the last error. A thrown exception becomes the error that FromException makes of it.
```

| Member | On |
| --- | --- |
| `HandleError<TResult, TError>()` | `PredicateBuilder<TResult>` |
| `HandleError<TResult, TError>(predicate)` | `PredicateBuilder<TResult>`, with a `Func<TError, bool>` |
| `ExecuteResultAsync<TResult, TError>(operation, cancellationToken)` | `ResiliencePipeline<TResult>` and `ResiliencePipeline` |
| `ToResult<TResult, TError>()` | `Outcome<TResult>` |

`TResult` must be a struct that implements `IErrorResult<TResult, TError>`, which `IResult<TSelf, T, TError>` extends. Empty is never handled. An exception that escapes the pipeline, including `OperationCanceledException`, becomes `TResult.FromError(TResult.FromException(exception))`. The overloads for `Result<T>` stay in use for `Result<T>` and need no type arguments.

## When to use

- You already use Polly strategies such as circuit breaker, timeout, hedging or backoff, and your operations return `Result<T>`.
- You want a retry to react to the error inside a result instead of only to a thrown exception.
- For a fixed delay and a plain attempt limit, `Result.Retry` and `Result.RetryAsync` need no package. See [Concurrency and resilience](../async/concurrency-and-resilience.md).

## Rules

- **Only the result state is read.** `HandleError` looks at the error in the result. Thrown exceptions stay with Polly's `Handle<TException>()`, which you can add to the same builder.
- **Cancellation is not an error.** A cancelled token throws `OperationCanceledException`, as everywhere in .NET.
- **The predicate is a lambda.** `HandleError(error => error is TimeoutException)` takes a `Func<Exception, bool>`. For a custom type it takes a `Func<TError, bool>`.
- **Exceptions are captured.** `ExecuteResultAsync` does not throw for an operation or strategy failure, except for cancellation. Use `ToResult` on an `Outcome` for the same rule in your own callbacks.
- **No sync form yet.** Only the async `ExecuteResultAsync` exists.

---

Back to index: [Docs index](../README.md)
