# Analyzer

The `Fluent.Unions` package includes an analyzer, like Rust's `#[must_use]`. It reports warning `FLU0001` when a call returns a result and the result is dropped, because a dropped result hides its error. It is added to your project with the package, and needs no setup.

## In this section

This folder has one page, this one.

## Quick example

```csharp
Save(order);                  // warning FLU0001: The result of 'Save' is not used
_ = Save(order);              // fine: discarded on purpose
Save(order).Check();          // fine: Check throws the error
var result = Save(order);     // fine: you hold the result
```

`Save` returns a `Result`. The message names the method that was called.

## What is reported

A call is reported when it stands alone as a statement and returns one of these types:

- `Result` and `Result<T>`.
- `Result<T, TError>`, and your own result types that implement `IResult` or `IErrorResult` from `Fluent.Unions.Abstractions`.
- `Task` or `ValueTask` of any of them.

Each of these is a warning:

```csharp
Save(order);                                   // a discarded Result
maybe?.Save(order);                            // conditional access
await SaveAsync(order);                        // an awaited Task<Result>
Load(order.Id).Do(o => Console.WriteLine(o));  // a chain that ends in Do(onSuccess)
Load(order.Id).Finally(() => { });             // a chain that ends in Finally
```

Some details:

- **Awaited calls are reported.** `await SaveAsync(order);` finishes the task, and its result then goes nowhere.
- **An unawaited task is reported in a method that is not `async`.** `SaveAsync(order);` as a statement in a synchronous method is a warning.
- **A chain is judged by its last call.** The error is still dropped after `Do(v => ...)`, `Finally(...)` and `Catch(...)`, so these are reported when they end a statement.
- **Generated code is skipped.**

## What is not reported

```csharp
_ = Save(order);                                        // an explicit discard
var result = Save(order);                               // an assignment
Save(order).Check();                                    // Check throws the error
Load(order.Id).OnErrorDo(e => Log(e));                  // OnErrorDo handles the error
Load(order.Id).Do(o => Use(o), e => Log(e));            // Do with both callbacks handles it
await SaveAsync(order).Check();                         // Check on a task
SaveAsync(order);                                       // in an async method: the compiler warns, see below
```

- Assignments, returns, arguments and explicit discards with `_ =`.
- Calls that end in `Check()`, `OnErrorDo(...)` or `Do(onSuccess, onError)`. These are recognized by name and by the library assembly they come from, so a method of your own called `Check` does not count.
- An unawaited task inside an `async` method, such as `SaveAsync(order);`. The compiler already warns there with CS4014.
- Options. A missing value is not an error, so `Option<T>` is never reported.
- Methods that return anything else, such as an `int`.

## Configuring the severity

`FLU0001` is a warning by default. Change it like any other rule in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.FLU0001.severity = error
```

Use `warning` for the default, `suggestion` or `silent` to soften it, and `none` to turn it off. A severity of `error` makes the build fail on every discarded result, which fits a project that treats errors as values.

## Suppressing in one place

Prefer an explicit discard when ignoring a result is on purpose:

```csharp
_ = Save(order);
```

It documents the decision and needs no suppression. When that is not possible, suppress the rule for one statement:

```csharp
#pragma warning disable FLU0001
Save(order);
#pragma warning restore FLU0001
```

Or suppress it for a whole member with `[SuppressMessage("Usage", "FLU0001")]`. To turn the rule off for a file or a folder, set its severity to `none` in a section of `.editorconfig` that matches those files.

## Rules

- **Handle the error, not the warning.** Discarding a result with `_ =` is for results whose failure truly does not matter, such as a best-effort log write.
- **Fire-and-forget tasks need a decision.** In a synchronous method, `SaveAsync(order);` is reported. In an `async` method it is the compiler's CS4014. Either way, await it or discard it on purpose.
- **A chain that logs is still reported.** `Do(v => ...)` and `Finally(...)` run in some states and leave the error in place. End the chain with `OnErrorDo`, `Check()`, `Match` or an assignment.

---

Back to index: [Docs index](../README.md)
