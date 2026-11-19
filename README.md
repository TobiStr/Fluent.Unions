# Fluent.Unions

> The Result pattern has shaped how I write .NET code for over four years. It started with [ResultType](https://github.com/TobiStr/ResultType) and continued with [PartialResult](https://github.com/TobiStr/PartialResult). Fluent.Unions is the next step: with C# 15 unions, the compiler now enforces what those libraries could only encourage.

`Result`, `Result<T>` and `Option<T>` for .NET, built on the C# 15 `union` keyword. A method returns either its value or an error, and the compiler checks that callers handle both. Fluent operators are named after LINQ and Rx.NET, so a whole flow reads as one expression.

## Installation

```
dotnet add package Fluent.Unions
dotnet add package Fluent.Unions.AspNetCore   # optional: HTTP responses for ASP.NET Core
dotnet add package Fluent.Unions.FluentValidation   # optional: FluentValidation validators as results
dotnet add package Fluent.Unions.Polly   # optional: Polly strategies that handle results
dotnet add package Fluent.Unions.OpenApi   # optional: Option<T> as nullable T in OpenAPI documents
dotnet add package Fluent.Unions.AwesomeAssertions   # optional: AwesomeAssertions for results and options
```

Requires the .NET 11 SDK. C# 15 is the default language version for `net11.0`, so no `LangVersion` setting is needed.

## At a glance

Return the value or the exception directly, and match both cases:

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

static Result<Customer> GetCustomer(Guid id) =>
    customers.TryGetValue(id, out var customer) ? customer : new KeyNotFoundException($"No customer {id}.");

string greeting = GetCustomer(id) switch
{
    Customer customer => $"Hello {customer.Name}",
    Exception error => error.Message
};
```

Chain steps fluently. The first error skips the rest, and exceptions thrown inside become the error:

```csharp
Result<Order> order = GetCustomer(id)
    .Chain(GetCart)
    .Where(cart => cart.Items.Count > 0)
    .Chain(PlaceOrder)
    .OnErrorDo(error => logger.LogWarning(error, "Checkout failed"));
```

Model missing values with `Option<T>`, and turn throwing code into results with `Try`:

```csharp
Option<Customer> cached = cache.GetValueOrNone(id).Or(() => FindInDatabase(id));
Result<int> quantity = Result.Try(() => int.Parse(input));
```

Go asynchronous without breaking the chain, and answer HTTP requests in one line:

```csharp
app.MapGet("/orders/{id}", (Guid id) => LoadOrderAsync(id)
    .ChainAwait(order => EnrichAsync(order))
    .ToHttpResult());   // 200, 404, or ProblemDetails with a mapped status code
```

## Features

- **Compiler-checked handling:**
  - **Matching:** `switch` over a result or option warns when a case is missing.
  - **Analyzer:** `FLU0001` warns when a result is ignored.
- **LINQ and Rx operators:**
  - **On the value:** `Select`, `Chain`, `Where`, `Zip`, `Ensure`, `BindAppend`.
  - **On the error and side effects:** `Catch`, `Do`, `OnErrorDo`, `Finally`.
  - **Leaving the chain:** `Match`, `GetValueOrDefault`, `DefaultIfEmpty`.
  - **Query syntax** works too.
- **`Option<T>`** for missing values: `TryParse`, `FirstOrNone`, `GetValueOrNone`, `Or`, and conversions to and from results.
- **Async everywhere:** `Task` overloads, `...Await` operators, `Result.WhenAll`, `RetryAsync`, `Timeout`, async streams.
- **Validation** that collects every error: `Validate`, `CombineAll`, and a catalogue of ready-made rules such as `EnsureNotEmpty` and `EnsureInRange`.
- **Custom error types:** restrict errors to your own base exception, or model errors as data with `Result<T, TError>` and a union of error records.
- **More unions:** `Loadable<T>` for UI state, plus `Is`/`As` and a JSON converter for your own unions.
- **Integrations:**
  - **ASP.NET Core:** HTTP responses with ProblemDetails.
  - **System.Text.Json:** support for options; results refuse to serialize, so exception details cannot leak.
  - **EF Core:** a documented mapping pattern.
  - **FluentValidation:** run a validator and get every failure as a `Result<T>`.
  - **Polly:** handle a result error in a resilience strategy.
  - **OpenAPI:** `Option<T>` is published as a nullable `T`.
  - **AwesomeAssertions:** `Should().BeOk()`, `BeNone()` and assertions on the error.
  - **OpenTelemetry:** `Activity.RecordResult` sets the status and records the exception.

## Documentation

The [docs](docs/README.md) explain every concern with several examples. The [reference](docs/reference.md) lists every type and operator on one page.

| Area | Page | What you learn |
| --- | --- | --- |
| Getting started | [Introduction](docs/getting-started/README.md) | What the library is and which package to use |
| | [Installation](docs/getting-started/installation.md) | Requirements, packages, namespaces, the analyzer, CS8509 as an error |
| | [Quick start](docs/getting-started/quick-start.md) | A 10-minute walkthrough from `return` to an HTTP endpoint |
| | [Limitations](docs/getting-started/limitations.md) | What the library cannot do |
| Result | [Overview](docs/result/README.md) | `Result` and `Result<T>` in short |
| | [Core concepts](docs/result/core-concepts.md) | The three states, creating results, members, matching, deconstruction, `Check()` |
| | [Operations](docs/result/operations.md) | `Select`, `Chain`, `Where`, `Zip`, `Ensure`, `CombineAll`, `BindAppend`, `Do`, `Out`, `Finally`, `Unwrap`, `Match` and more |
| | [Error handling](docs/result/error-handling.md) | `Catch`, `OnErrorDo`, `Try`, `Retry`, and what still throws |
| | [Collections](docs/result/collections.md) | `Combine`, `CombineAll`, `Partition`, `Values`, `Errors` |
| | [Patterns](docs/result/patterns.md) | Pipelines, query syntax, using earlier values, early returns, service layers |
| Option | [Overview](docs/option/README.md) | `Option<T>` in short |
| | [Core concepts](docs/option/core-concepts.md) | Creating options, members, matching, why `default` means no value |
| | [Operations](docs/option/operations.md) | `Select`, `Chain`, `Where`, `Or`, `OnNoneDo`, `Match` and more |
| | [Collections](docs/option/collections.md) | `FirstOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone`, `Combine` |
| | [Conversions](docs/option/conversions.md) | `ToOption`, `ToNullable`, `ToResult`, `Transpose` |
| | [Patterns](docs/option/patterns.md) | Replacing `null`, fallbacks, optional settings and DTO fields |
| Async | [Overview](docs/async/README.md) | How `Task` pipelines behave |
| | [Task pipelines](docs/async/task-pipelines.md) | `Task` overloads, `...Await` operators, async query syntax |
| | [Concurrency and resilience](docs/async/concurrency-and-resilience.md) | `WhenAll`, `Retry`, `RetryAsync`, `Timeout` |
| | [Streams](docs/async/streams.md) | `IAsyncEnumerable` of results and options |
| Validation | [Validation](docs/validation/README.md) | `Validate`, `CombineAll`, the rule catalogue, and validation errors over HTTP |
| Errors | [Overview](docs/errors/README.md) | Choosing between exceptions, custom types and errors as data |
| | [Custom error types](docs/errors/custom-error-types.md) | Your own result type with `IResult` |
| | [Errors as data](docs/errors/errors-as-data.md) | `Result<T, TError>` with a union of error records |
| Unions | [Overview](docs/unions/README.md) | Helpers for unions in short |
| | [Your own unions](docs/unions/your-own-unions.md) | `Is`, `As`, and the JSON converter for your unions |
| | [Loadable](docs/unions/loadable.md) | `Loadable<T>` for asynchronously loaded UI state |
| | [Modeling](docs/unions/modeling.md) | State machines and event sourcing with unions |
| Serialization | [Serialization](docs/serialization/README.md) | `System.Text.Json` for options, results and your unions |
| ASP.NET Core | [Overview](docs/aspnetcore/README.md) | The `Fluent.Unions.AspNetCore` package in short |
| | [HTTP results](docs/aspnetcore/http-results.md) | `ToHttpResult` for results, options and errors as data |
| | [Endpoint filter](docs/aspnetcore/endpoint-filter.md) | Returning results straight from handlers with `WithResultMapping` |
| | [Error mapping](docs/aspnetcore/error-mapping.md) | Status codes, ProblemDetails, and what is exposed |
| Entity Framework Core | [Entity Framework Core](docs/entity-framework-core/README.md) | Storing options with nullable backing fields |
| FluentValidation | [FluentValidation](docs/fluent-validation/README.md) | `ValidateToResult` and `Validate(validator)` with every failure collected |
| Polly | [Polly](docs/polly/README.md) | `HandleError`, `ExecuteResultAsync` and `ToResult` for resilience pipelines |
| OpenAPI | [OpenAPI](docs/openapi/README.md) | `AddFluentUnions()` describes `Option<T>` as a nullable `T` |
| AwesomeAssertions | [AwesomeAssertions](docs/awesome-assertions/README.md) | Assertions for `Result`, `Result<T>`, `Option<T>` and the error inside |
| Tracing | [Tracing](docs/tracing/README.md) | `Activity.RecordResult` for status and exception events |
| Analyzer | [Analyzer](docs/analyzer/README.md) | Warning `FLU0001` for ignored results, and how to configure it |

## Build and test

```
dotnet build Fluent.Unions.slnx
dotnet test Fluent.Unions.slnx
```

| Folder | Contents |
| --- | --- |
| `src/Fluent.Unions` | The `Fluent.Unions` package |
| `src/Fluent.Unions.Analyzers` | The analyzer packed into `Fluent.Unions` |
| `src/Fluent.Unions.AspNetCore` | The `Fluent.Unions.AspNetCore` package |
| `src/Fluent.Unions.FluentValidation` | The `Fluent.Unions.FluentValidation` package |
| `src/Fluent.Unions.Polly` | The `Fluent.Unions.Polly` package |
| `src/Fluent.Unions.OpenApi` | The `Fluent.Unions.OpenApi` package |
| `src/Fluent.Unions.AwesomeAssertions` | The `Fluent.Unions.AwesomeAssertions` package |
| `test/` | The NUnit tests for each project. `test/Fluent.Unions.Tests/Samples` holds runnable modeling samples |
| `docs/` | The documentation |

Shared package settings live in `src/Directory.Build.props`. The license is in `LICENSE`.
