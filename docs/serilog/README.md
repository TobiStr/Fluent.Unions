# Serilog

The package `Fluent.Unions.Serilog` (namespace `Fluent.Unions.Serilog`) logs results, options and loadables as structured values with [Serilog](https://serilog.net). It depends on `Serilog` (Apache-2.0) and adds one destructuring policy.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register the policy and log a result.
- [Log shape](#log-shape): what each type becomes.
- [Exceptions and stack traces](#exceptions-and-stack-traces): the safe default and the exception slot.
- [With your own result type](#with-your-own-result-type): what is logged for a declared result type.
- [When to use](#when-to-use)
- [Rules](#rules): what is and is not destructured.

## Quick example

```csharp
using Serilog;
using Fluent.Unions.Serilog;

Log.Logger = new LoggerConfiguration()
    .Destructure.WithFluentUnions()
    .WriteTo.Console()
    .CreateLogger();

Result<Order> order = LoadOrder(id);
Log.Information("Loaded {@Order}", order);
// Loaded { Id: 42, Total: 9.5 }                                         for a value
// Loaded { Error: { Type: "System.TimeoutException", Message: "Slow" } } for an error
```

Without the policy, Serilog logs a union as an opaque object through `ToString()` or reflection. With it, the log event holds real properties that a structured sink can query.

## Log shape

The shape matches the JSON of [options](../serialization/README.md#options): a value is the value, `None` is `null`.

| Type and state | Logged as |
| --- | --- |
| `Option<T>` with a value | the value, destructured |
| `Option<T>` `None` or `default` | `null` |
| `Result<T>` or your own result type with a value | the value, destructured |
| `Result<T>` or your own result type with an exception | `{ Error: { Type, Message } }` |
| Your own result type with an error that is not an exception | `{ Error: <TError, destructured> }` |
| `Result` that is Ok | `"Ok"` |
| `Result` with an exception | `{ Error: { Type, Message } }` |
| `Loadable<T>` not loaded or `default` | `"NotLoaded"` |
| `Loadable<T>` loading | `"Loading"` |
| `Loadable<T>` with a value | the value, destructured |
| `Loadable<T>` faulted | `{ Error: { Type, Message } }` |

`Type` is the full name of the exception type and `Message` is its message. A `default` result is an error, so it logs the uninitialized error. Values nest: an option property of a destructured object, or an option in an array, logs as its value or `null`.

An error that is not an exception, such as a record of an error union, is destructured like any other object, so its properties are logged: `{ Error: { Id: "42" } }`.

## Exceptions and stack traces

The policy logs only the type and message of an error. It never logs the stack trace, the target site or inner exceptions. To attach the full exception, pass it to the log call. Serilog puts it in the exception slot of the log event, where sinks render it:

```csharp
if (order.TryGetError(out var error))
{
    Log.Error(error, "Could not load {@Order}", order);
}
```

## With your own result type

`.Destructure.WithFluentUnions()` destructures any result type, including one declared with `[FluentResult<TError>]` (see [Your own Result type](../errors/custom-error-types.md)), like the default ones. A value-less success logs `"Ok"`.

## When to use

- You log results, options or loadables with `{@Name}` and want searchable properties instead of text.
- You want errors in logs without the stack trace in the message properties.

## Rules

- **Destructure with `@`.** `{Order}` renders with `ToString()`, which is the value, or `{ExceptionType}: {Message}` for an error, without the stack trace. Serilog applies destructuring policies only for `{@Order}`, so prefer `{@Order}` to get structured properties.
- **Only the five types and result types are handled.** Other values, including your own unions, are left to Serilog and other policies.
- **No `null` values.** An option or result never holds `null`. A `null` in the log means an option with no value.
- **An error and a value can look alike.** A value that is a structure with only an `Error` property is indistinguishable from an error in the log. Use `Match` and log the cases yourself when that matters.
- **Reflection for the generic types.** The policy reads the union through `IUnion` and the generic type definition. AOT and trimming have not been tested.
- **Call `WithFluentUnions` once.** It adds the policy to the logger configuration.

---

Back to index: [Docs index](../README.md)
