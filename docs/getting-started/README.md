# Getting started

Fluent.Unions is a small .NET library for return values that can fail or be missing. It is built on the C# 15 `union` keyword, so the compiler checks that callers handle every case.

The types:

| Type | Holds | Use it for |
| --- | --- | --- |
| `Result` | `Ok` or an `Exception` | An operation with no return value that can fail. |
| `Result<T>` | a `T` or an `Exception` | An operation that returns a value or fails. |
| `Option<T>` | a `T` or `None` | A value that may be missing, where missing is normal. |
| `Loadable<T>` | not loaded, loading, a `T` or an `Exception` | UI state for data that loads asynchronously. |
| `Result<T, TError>` | a `T` or an error you define | Errors as data: a union of plain records instead of exceptions. |

Operators are named after LINQ and Rx.NET: `Select`, `Where`, `SelectMany`, `Catch`, `Do`, `Finally`, `Match`. The same names work on `Task<Result<T>>`, so an asynchronous pipeline stays one expression.

Two packages:

- `Fluent.Unions` has the types, the operators and an analyzer that warns when you ignore a result.
- `Fluent.Unions.AspNetCore` turns results and options into HTTP responses.

## In this section

- [Installation](installation.md): requirements, packages, namespaces, the analyzer and CS8509.
- [Quick start](quick-start.md): a 10-minute walkthrough from `return` to an ASP.NET Core endpoint.
- [Limitations](limitations.md): what the library cannot do.
- [Result](../result/README.md): the detailed guide, the page to read after the quick start.

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

Result<Order> order = GetCustomer(id)
    .Chain(GetCart)
    .Chain(PlaceOrder);
```

Each step runs only if the previous one succeeded. The first error ends the chain and becomes the result.

## When to use

- A method can fail in ways the caller should look at, and you do not want to rely on callers remembering a `try`/`catch`.
- You want "not found" without `null` checks. Use `Option<T>`.
- You want a pipeline of steps where each can fail, without nested `if` statements.

## Rules

- A `Result` is a struct. `default(Result<T>)` is a third state, empty, that is neither ok nor an error.
- Operators on results never throw for failures inside your lambdas. They turn the exception into the error. Only `Check()` throws on purpose.
- Operators on options do not catch exceptions, because an option has no error state.

---

Back to index: [Docs index](../README.md) · Next: [Installation](installation.md)
