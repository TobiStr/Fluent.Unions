# Fluent.Unions documentation

Fluent.Unions brings `Result`, `Result<T>`, `Option<T>`, `Result<T, TError>` and `Loadable<T>` to C# 15, built on the `union` keyword. Fluent operators are named after LINQ and Rx.NET. An analyzer and an ASP.NET Core package help you use results safely from the first method to the HTTP response.

## Start here

- [Getting started](getting-started/README.md): what the library is, installation, and a quick start.
- [Reference](reference.md): every type and operator on one page, with links to the detailed docs.

## Concepts and operators

| Folder | What it covers |
| --- | --- |
| [Result](result/README.md) | `Result` and `Result<T>`: the three states, creating results, operators, error handling, collections, patterns. |
| [Option](option/README.md) | `Option<T>`: values that may be missing, operators, collections, conversions, patterns. |
| [Async](async/README.md) | `Task` pipelines, the `...Await` operators, `WhenAll`, retries, timeouts, async streams. |
| [Validation](validation/README.md) | Running every rule and collecting every error with `Validate` and `CombineAll`, plus the rule catalogue. |
| [Errors](errors/README.md) | Custom error types: your own result type, and errors as data with `Result<T, TError>`. |
| [Unions](unions/README.md) | Helpers for your own unions, `Loadable<T>` for UI state, and modeling with unions. |
| [Serialization](serialization/README.md) | `System.Text.Json` support for options, results and your own unions. |

## Integrations and tooling

| Folder | What it covers |
| --- | --- |
| [ASP.NET Core](aspnetcore/README.md) | The `Fluent.Unions.AspNetCore` package: HTTP responses, ProblemDetails, the endpoint filter, error mapping. |
| [Entity Framework Core](entity-framework-core/README.md) | Storing options in EF Core with nullable backing fields. |
| [Tracing](tracing/README.md) | `RecordResult` copies a result onto a `System.Diagnostics.Activity`: status and exception event. |
| [Analyzer](analyzer/README.md) | Warning `FLU0001` for results that are ignored. |

## Conventions in these docs

- **Example domain:** an online shop (customers, carts, orders, payments).
- **Results in comments:** a trailing comment shows the result of an expression: `// 3`, `// None`, `// error: Not found`.
- **Compiled examples:** every C# example was compiled against the library when it was written.

---

Next: [Getting started](getting-started/README.md)
