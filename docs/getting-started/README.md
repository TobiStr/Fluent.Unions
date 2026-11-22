# Getting started

Fluent.Unions is a small .NET library for return values that can fail or be missing. It is built on the C# 15 `union` keyword, so the compiler checks that callers handle every case.

The types:

| Type | Holds | Use it for |
| --- | --- | --- |
| `Result` | `Ok` or an `Exception` | An operation with no return value that can fail. |
| `Result<T>` | a `T` or an `Exception` | An operation that returns a value or fails. |
| `Option<T>` | a `T` or `None` | A value that may be missing, where missing is normal. |
| `Loadable<T>` | not loaded, loading, a `T` or an `Exception` | UI state for data that loads asynchronously. |
| Your own `Result<T>` | a `T` or your error type | Errors restricted to your own exception type, or a closed set of cases checked by the compiler. Declared with `[FluentResult<TError>]`. |

Operators are named after LINQ and Rx.NET: `Select`, `SelectMany`, `Catch`, `Do`, `Finally`, `Match`, plus `Chain` and `Ensure`. The same names work on `Task<Result<T>>`, so an asynchronous pipeline stays one expression.

Two packages:

- `Fluent.Unions` has the types, the operators and analyzers that warn when you ignore a result or misuse a union.
- `Fluent.Unions.AspNetCore` turns results and options into HTTP responses.

## Choose your Result

Start with the default `Result<T>` from `using Fluent.Unions;`. When you want errors of your own type, declare it once in your root namespace and keep writing `Result<T>`:

```csharp
[FluentResult<DomainException>]
public readonly partial struct Result<T>;
```

Every operator below works on both. See [Your own Result type](../errors/custom-error-types.md).

## In this section

- [Installation](installation.md): requirements, packages, namespaces, the analyzer and CS8509.
- [Quick start](quick-start.md): a 10-minute walkthrough from `return` to an ASP.NET Core endpoint.
- [Limitations](limitations.md): what the library cannot do.
- [AOT and trimming](aot-and-trimming.md): what works in native AOT and trimmed apps, and what is tested.
- [Result](../result/README.md): the detailed guide, the page to read after the quick start.

## Quick example

```csharp
using Fluent.Unions;

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

- A `Result` is a struct. `default(Result<T>)` is not a third state for the library: it counts as an error. A native `switch` sees no case for it; see [The default result](../result/core-concepts.md#the-default-result).
- Operators on results never throw for failures inside your lambdas. They turn the exception into the error. Only `Check()` throws on purpose.
- Operators on options do not catch exceptions, because an option has no error state.

---

Back to index: [Docs index](../README.md) · Next: [Installation](installation.md)
