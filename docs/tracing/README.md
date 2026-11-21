# Tracing

`RecordResult` (namespace `Fluent.Unions`, in the core package) copies the outcome of a result onto a `System.Diagnostics.Activity`, so a span shows whether an operation worked and why it failed. It needs no extra package.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): record a result on the current span.
- [What gets recorded](#what-gets-recorded): status and exception event for each state.
- [When to use](#when-to-use): where it helps.

## Quick example

```csharp
using System.Diagnostics;

var source = new ActivitySource("Shop.Checkout");

using var activity = source.StartActivity("PlaceOrder");

Result<Order> order = await orders.PlaceOrderAsync(cartId);
activity.RecordResult(order);

// order failed with "Payment declined":
// activity.Status            -> Error
// activity.StatusDescription -> "Payment declined"
// activity.Events            -> one "exception" event
```

`StartActivity` returns `null` when nobody listens. `RecordResult` accepts a `null` activity and does nothing, so no check is needed. It returns the activity, so a call can end a chain.

## What gets recorded

| Input | Status | Description | Exception event |
| --- | --- | --- | --- |
| value or `Result.Ok` | `Ok` | | no |
| error | `Error` | the exception message | yes, through `AddException` |
| Your own result type, error that is an exception | `Error` | the exception message | yes |
| Your own result type, error that is not an exception | `Error` | the message of a `ResultErrorException` holding the error | yes, as `ResultErrorException` |
| `default` result | `Error` | the message of the `InvalidOperationException` it holds | yes |
| `null` activity | nothing | | no |

`RecordResult` has overloads for `Result` and `Result<T>`. Your own [result type](../errors/custom-error-types.md) gets a generated `RecordResult` operator, so `activity.RecordResult(result)` works the same. An error that is not an exception is recorded as a `ResultErrorException`.

## When to use

- Your operations return results and you want the span status to follow without a `try`/`catch`.
- You want failed spans to carry the exception type and message in tracing tools.

Back to index: [Docs index](../README.md)
