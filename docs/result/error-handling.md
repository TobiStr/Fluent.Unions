# Error handling

This page covers how errors flow through a chain, how to handle them, and the few places where an exception still escapes.

```csharp
using Fluent.Unions;
```

## Chains never throw

An exception thrown inside a lambda that you pass to an operator becomes the error of the result, as in Rx. The rest of the chain is skipped.

```csharp
Result<int> broken = GetCustomer(id).Select(c => int.Parse(c.Name)); // error: FormatException

Result<Order> throwing = GetCustomer(id)
    .Chain(GetCart)
    .Chain(Unreliable); // Unreliable throws a TimeoutException: error: TimeoutException
```

You never need a `try`/`catch` around a chain. Only [`Check()`](#check) throws, on purpose.

### What is captured

| Thrown | Becomes |
| --- | --- |
| Any exception inside a lambda, including `OutOfMemoryException` | the error |
| A faulted task passed into a chain, such as `task.Select(...)` | the error |
| `OperationCanceledException` | nothing: it propagates. Only `RetryAsync` treats one as a failure while its own token is not cancelled. |

Every operator that takes a lambda follows these rules. With your own result type, the error is what `FromException` makes of the exception, unless the exception already is your error type. Your type can also capture fewer exceptions, so that a bug such as a `NullReferenceException` escapes instead of becoming an ordinary error. See [Capture policy](../errors/custom-error-types.md#capture-policy). The default `Result<T>` captures every exception.

## Catch

`Catch` runs only for an error. Ok results pass through. Because the union converts both values and exceptions implicitly, one operator does two jobs: recover with a value, or translate the error into another exception.

Recover with a value:

```csharp
Result<Customer> recovered = GetCustomer(id)
    .Catch(e => new Customer(Guid.Empty, "Guest", ""));
```

Translate the error, keeping the original as the inner exception:

```csharp
Result<Customer> translated = GetCustomer(id)
    .Catch(e => new DomainException("Lookup failed.", e));
```

Handle one exception type only. Other errors pass through. The type goes on the lambda parameter:

```csharp
Result<Customer> onlyMissing = GetCustomer(id)
    .Catch((KeyNotFoundException e) => new Customer(Guid.Empty, "Guest", ""));
```

Replace any error with a fixed result:

```csharp
Result<Customer> fallback = GetCustomer(id).Catch(Guest); // Guest is a Result<Customer>
```

The handler may return another result, so a fallback can itself fail:

```csharp
Result<Customer> fallbackChain = GetCustomer(id).Catch(_ => GetCustomer(Guid.NewGuid()));
```

On a `Result`, return `Result.Ok` to recover:

```csharp
Result saved = Save(known).Catch(e => Result.Ok);
```

An exception thrown inside the handler becomes the new error. For asynchronous handlers use `CatchAwait`.

## OnErrorDo

Runs an action with the error and returns the same result. Use it to log and keep the error moving. When the callback may throw, prefer [`TapError`](#do-or-tap), which keeps the original error.

```csharp
Result<Customer> logged = GetCustomer(id)
    .OnErrorDo(e => log.LogError(e, "Customer {Id}", id));
```

## Do with two callbacks

`Do(onSuccess, onError)` handles both sides in one step:

```csharp
Result<Customer> both = GetCustomer(id).Do(
    c => log.LogInformation("ok"),
    e => log.LogWarning("fail"));
```

`OnErrorDo` and `Do(onSuccess, onError)` count as handling the error for analyzer FLU0001. A chain ending in `Do(onSuccess)` does not.

## When a callback throws

`Do`, `OnErrorDo` and `Finally` treat their callback as part of the chain. When it throws, its exception becomes the result, and the last failure wins:

| Result before | Callback throws | Result after |
| --- | --- | --- |
| ok | in `Do` or `Finally` | error: the callback's exception |
| error | in `OnErrorDo`, `Do(onSuccess, onError)` or `Finally` | error: the callback's exception. The original error is gone. |

A chain is not a transaction. A step that already wrote to a database stays written when a later step fails, and nothing is rolled back. Use a transaction or a compensating step for writes that must succeed together.

## Do or Tap

Use `Do` for a side effect that must succeed, such as publishing an event the caller relies on. Use `Tap` for an observer, such as logging or metrics, whose failure must not change the business outcome:

```csharp
Result<Order> placed = PlaceOrder(cart)
    .Tap(order => metrics.OrderPlaced(order.Total))
    .TapError(e => log.LogWarning(e, "Order failed"));
```

`Tap`, `TapError` and `TapAwait` return the same result and never capture. When the observer throws, the exception propagates with its original stack trace, so a logging failure shows up as a logging failure instead of turning a saved order into an error. `TapError` keeps the original error. Wrap the observer in its own `try`/`catch` if its failure should be ignored.

## Check

`Check()` throws the stored exception when the result is an error, and returns the result when it is ok. It rethrows with `ExceptionDispatchInfo`, so the stack trace points at the original throw, with the layers that rethrew appended.

```csharp
try
{
    Customer c = GetCustomer(id).Check().GetValue();
}
catch (KeyNotFoundException ex)
{
    log.LogError(ex, "{Trace}", ex.StackTrace);
}
```

Use it at a boundary where an exception is the right signal, such as a handler that must throw.

## Try and TryAsync

The way into a chain from code that throws. A thrown exception becomes the error, except `OperationCanceledException`, which propagates. A `null` value gives an error, as in every operator.

```csharp
Result<int> quantity = Result.Try(() => int.Parse(input));
Result<Payment> tried = await Result.TryAsync(async () => (await ChargeAsync(known)).GetValue());
```

| Body | `Try` returns | `TryAsync` body |
| --- | --- | --- |
| returns a `T` | `Result<T>` | `Task<T>` |
| returns a `Result<T>` | that `Result<T>`, not nested | `Task<Result<T>>` |
| returns nothing | `Result` (ok) | `Task` |
| returns a `Result` | that `Result` | `Task<Result>` |

`TryAsync` also catches an exception that the body throws before it returns its task.

## Retry and RetryAsync

`Result.Retry` runs an operation again while it fails, up to `maxAttempts` times, and returns the last result. A thrown exception counts as a failure. `shouldRetry` limits which errors are retried. Without it, every error is.

```csharp
Result<Payment> retried = Result.Retry(
    () => Charge(known),
    maxAttempts: 3,
    shouldRetry: e => e is TimeoutException);
```

`RetryAsync` passes a `CancellationToken` to the operation and can wait between attempts:

```csharp
Result<Payment> retriedAsync = await Result.RetryAsync(
    token => ChargeFlaky(known, token),
    maxAttempts: 3,
    delay: TimeSpan.FromSeconds(1),
    shouldRetry: e => e is TimeoutException,
    cancellationToken: ct);
```

An error that `shouldRetry` rejects ends the loop after one attempt. Cancellation stops retrying and throws `OperationCanceledException`. A `maxAttempts` below 1 throws `ArgumentOutOfRangeException`. Only retry operations that are safe to repeat, such as an idempotent payment call. See [Async](../async/README.md) for timeouts and cancellation.

## What still throws

| Call | Throws |
| --- | --- |
| Any operator with a `null` lambda | `ArgumentNullException`, immediately |
| `Check()` | the stored exception |
| `GetValue()` | `InvalidOperationException` for an error, with the error as `InnerException` |
| `GetError()` | `InvalidOperationException` when the result holds no exception |
| `Match` callbacks | whatever your callback throws |
| `GetValueOrDefault(() => ...)` factory | whatever your factory throws |
| `Result<T>.Ok(null)` | `ArgumentNullException` |
| `switch` expression over a `default` result | `SwitchExpressionException`, see [The default result](core-concepts.md#the-default-result) |
| Any operator whose lambda is cancelled | `OperationCanceledException` |
| `Tap`, `TapError` and `TapAwait` callbacks | whatever your callback throws |
| An exception your result type does not capture | that exception, see [Capture policy](../errors/custom-error-types.md#capture-policy) |
| `FromException` that returns `null` | `InvalidOperationException`, with the original exception as `InnerException` |
| `FromException` that throws | `InvalidOperationException`, with an `AggregateException` of the original exception and the failure of `FromException` |

Operators that leave the chain, `Match` and the `GetValueOrDefault` factory, do not catch because there is no result left to hold the error. `Tap` does not catch on purpose.

## Pitfalls

- `Catch` with an exception type needs the type on the lambda parameter: `(KeyNotFoundException e) => ...`.
- Translating to a general `Exception` hides the cause for callers that match on type. Keep the original as the inner exception.
- Logging in `Do` turns a successful result into an error when the logger throws. Log with `Tap` instead.

## See also

- [Operations](operations.md)
- [Collections](collections.md)
- [Async](../async/README.md)

---

Back to: [Result](README.md) · Next: [Collections](collections.md)
