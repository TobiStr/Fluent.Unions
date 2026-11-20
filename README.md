<p align="center"><img src="assets/icon-1024.png" alt="Fluent.Unions" width="128" /></p>

# Fluent.Unions

<p align="center">
  <a href="https://www.nuget.org/packages/Fluent.Unions"><img src="https://img.shields.io/nuget/v/Fluent.Unions.svg" alt="NuGet" /></a>
  <a href="https://www.nuget.org/packages/Fluent.Unions"><img src="https://img.shields.io/nuget/dt/Fluent.Unions.svg" alt="Downloads" /></a>
  <a href="https://github.com/TobiStr/Fluent.Unions/actions/workflows/ci.yml"><img src="https://github.com/TobiStr/Fluent.Unions/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <a href="https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml"><img src="https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg" alt="Publish" /></a>
  <a href="https://tobistr.github.io/Fluent.Unions/"><img src="https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2" alt="Documentation" /></a>
</p>

> The Result pattern has shaped how I write .NET code for over 10 years. It started with the simple [ResultType](https://github.com/TobiStr/ResultType) and continued with [PartialResult](https://github.com/TobiStr/PartialResult). Fluent.Unions is the next big step: with C# 15 unions, the compiler now enforces what those libraries could only encourage.

`Result`, `Result<T>` and `Option<T>` for .NET, built on the C# 15 `union` keyword. A method returns either its value or an error, and the compiler checks that callers handle both. Fluent operators are named after LINQ and Rx.NET, so a whole flow reads as one expression.

## Installation

```
dotnet add package Fluent.Unions
dotnet add package Fluent.Unions.AspNetCore   # optional: HTTP responses for ASP.NET Core
dotnet add package Fluent.Unions.FluentValidation   # optional: FluentValidation validators as results
dotnet add package Fluent.Unions.Polly   # optional: Polly strategies that handle results
dotnet add package Fluent.Unions.OpenApi   # optional: Option<T> as nullable T in OpenAPI documents
dotnet add package Fluent.Unions.AwesomeAssertions   # optional: AwesomeAssertions for results and options
dotnet add package Fluent.Unions.Dapper   # optional: map Option<T> to and from database NULL with Dapper
dotnet add package Fluent.Unions.Refit   # optional: Refit API responses as Result, Result<T> and Result<Option<T>>
dotnet add package Fluent.Unions.FluentAssertions   # optional: FluentAssertions 8 for results and options (commercial license for commercial use)
dotnet add package Fluent.Unions.Serilog   # optional: structured logging of results, options and loadables
dotnet add package Fluent.Unions.NewtonsoftJson   # optional: Newtonsoft.Json converters for Option<T>, your own unions and results
```

Requires the .NET 11 SDK. C# 15 is the default language version for `net11.0`, so no `LangVersion` setting is needed.

`using Fluent.Unions;` brings in the types and every operator. The ready-made validation rules live in `Fluent.Unions.Validation`. In ASP.NET Core code that also imports `Microsoft.AspNetCore.Http.HttpResults`, the success case `Ok` has the same name as the HTTP result; add `using Ok = Fluent.Unions.Ok;` to the file.

## Common scenarios

**Return a value or an error, and handle both.** A result is always one of the two, and the compiler reports a `switch` that misses a case:

```csharp
Result<Customer> GetCustomer(Guid id) =>
    customers.TryGetValue(id, out var customer) ? customer : new KeyNotFoundException($"No customer {id}.");

string greeting = GetCustomer(id) switch
{
    Customer customer => $"Hello {customer.Name}",
    Exception error => error.Message
};
```

**Chain steps.** The first error skips the rest, and exceptions thrown inside a step become the error:

```csharp
Result<Order> order = GetCustomer(id)
    .Chain(GetCart)
    .Ensure(cart => cart.Items.Count > 0, "The cart is empty.")
    .Chain(PlaceOrder)
    .OnErrorDo(error => logger.LogWarning(error, "Checkout failed"));
```

**Wrap code you do not control.** Turn `null` and exceptions into results at the boundary:

```csharp
Result<Customer> customer = repository.Find(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));
Result<int> quantity = Result.Try(() => int.Parse(input));
```

**Validate input and get every error at once.** The ready-made rules in `Fluent.Unions.Validation` are part of the core package and name the property in their message:

```csharp
Result<SignUp> signUp = Result.From(form).Validate(
    f => f.Email.EnsureMatchesEmail(),          // "Email must be a valid email address."
    f => f.Age.EnsureInRange(18, 120),          // "Age must be between 18 and 120."
    f => f.Password.EnsureLongerThanOrEqual(12));
```

**Model missing values with `Option<T>`** instead of `null`:

```csharp
Option<Customer> cached = cache.GetValueOrNone(id).Or(() => FindInDatabase(id));
string name = cached.Select(c => c.Name).GetValueOrDefault("Guest");
```

**Stay fluent across `await`.** Every operator has a `Task` overload, and `...Await` operators take asynchronous steps:

```csharp
Result<decimal> total = await LoadCustomerAsync(id)
    .ChainAwait(customer => LoadCartAsync(customer.CartId))
    .Select(cart => cart.Total);
```

## A real-world example: checkout

A shop places an order. It calls code it does not control: a repository that returns `null` and a payment SDK that throws.

The traditional version needs null checks, early returns, a `try`/`catch`, and an `out` parameter that carries the error as a string:

```csharp
public static Receipt? PlaceOrder(Shop shop, CheckoutRequest request, out string? error)
{
    error = null;
    if (string.IsNullOrWhiteSpace(request.Sku)) { error = "Sku must not be empty or white space."; return null; }
    if (request.Quantity is < 1 or > 10) { error = "Quantity must be between 1 and 10."; return null; }

    var customer = shop.FindCustomer(request.CustomerId);
    if (customer is null) { error = $"No customer {request.CustomerId}."; return null; }

    var product = shop.FindProduct(request.Sku);
    if (product is null) { error = $"No product {request.Sku}."; return null; }
    if (product.Stock < request.Quantity) { error = $"Only {product.Stock} left of {product.Sku}."; return null; }

    var total = product.Price * request.Quantity;
    string paymentId;
    try { paymentId = shop.Charge(customer, total); }
    catch (PaymentDeclinedException exception) { error = exception.Message; return null; }

    var receipt = new Receipt(customer.Name, product.Sku, total, paymentId);
    shop.Save(receipt);
    return receipt;
}
```

With Fluent.Unions it is one chain, and each step runs only if the previous one succeeded:

```csharp
public static Result<Receipt> PlaceOrder(Shop shop, CheckoutRequest request) =>
    Result.From(request)
        .Validate(
            r => r.Sku.EnsureNotWhiteSpace(),
            r => r.Quantity.EnsureInRange(1, 10))
        .Chain(r => FindCustomer(shop, r.CustomerId)
            .Zip(FindProduct(shop, r.Sku), (customer, product) => new Order(customer, product, r.Quantity)))
        .Ensure(o => o.Product.Stock >= o.Quantity, o => new OutOfStockException($"Only {o.Product.Stock} left of {o.Product.Sku}."))
        .Chain(o => Result.Try(() => shop.Charge(o.Customer, o.Total))
            .Select(paymentId => new Receipt(o.Customer.Name, o.Product.Sku, o.Total, paymentId)))
        .Do(shop.Save);

private sealed record Order(Customer Customer, Product Product, int Quantity)
{
    public decimal Total => Product.Price * Quantity;
}

private static Result<Customer> FindCustomer(Shop shop, Guid id) =>
    shop.FindCustomer(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));

private static Result<Product> FindProduct(Shop shop, string sku) =>
    shop.FindProduct(sku).ToResult(() => new KeyNotFoundException($"No product {sku}."));
```

The caller matches on the error's type instead of parsing strings:

```csharp
int status = PlaceOrder(shop, request) switch
{
    Receipt => 201,
    ValidationErrors => 400,            // every validation error at once
    KeyNotFoundException => 404,
    OutOfStockException => 409,
    PaymentDeclinedException => 402,
    Exception => 500
};
```

What the union gives you:

- **An honest signature.** `Result<Receipt>` says that the call can fail; `Receipt?` with an `out string?` hides it.
- **Every case handled.** A `switch` that misses the error case is reported by the compiler (CS8509).
- **Typed errors.** Status codes come from exception types, not from message text.
- **No `try`/`catch`, no `null`.** `Result.Try` turns the declined card into an error and `ToResult` turns the repository's `null` into one, so the order is not saved. Unlike the traditional version, an unexpected exception from the payment SDK also becomes an error instead of escaping.
- **Every validation error at once**, each with the name of its property, instead of stopping at the first.

Both versions are tested side by side in [`CheckoutSample.cs`](https://github.com/TobiStr/Fluent.Unions/blob/main/test/Fluent.Unions.Tests/Samples/CheckoutSample.cs).

## Features

- **Compiler-checked handling:**
  - **Matching:** `switch` over a result or option warns when a case is missing.
  - **Analyzers:** warn when a result is ignored, an async lambda escapes a result, a union cannot tell its cases apart, a result is serialized, and more.
- **LINQ and Rx operators:**
  - **On the value:** `Select`, `Chain`, `Ensure`, `Zip`, `BindAppend`.
  - **On the error and side effects:** `Catch`, `Do`, `OnErrorDo`, `Finally`.
  - **Leaving the chain:** `Match`, `TryGetValue`, `GetValueOrDefault`.
  - **Query syntax** works too.
- **`Option<T>`** for missing values: `TryParse`, `FirstOrNone`, `GetValueOrNone`, `Or`, and conversions to and from results.
- **Async everywhere:** `Task` overloads, `...Await` operators, `Result.WhenAll`, `RetryAsync`, `Timeout`, async streams.
- **Validation** that collects every error as `ValidationErrors` with property names: `Validate`, `CombineAll`, and a catalogue of ready-made rules such as `EnsureNotEmpty` and `EnsureInRange`.
- **Custom error types:** restrict errors to your own base exception, or model errors as data with `Result<T, TError>` and a union of error records.
- **More unions:** `Loadable<T>` for UI state, plus `Is`/`As` and a JSON converter for your own unions.
- **Integrations:**
  - **ASP.NET Core:** HTTP responses with ProblemDetails, validation errors as ValidationProblemDetails, and OpenAPI response types for endpoints that return results.
  - **System.Text.Json:** support for options; results refuse to serialize, so exception details cannot leak.
  - **EF Core:** a documented mapping pattern.
  - **FluentValidation:** run a validator and get every failure as a `Result<T>`.
  - **Polly:** handle a result error in a resilience strategy.
  - **OpenAPI:** `Option<T>` is published as a nullable `T`.
  - **AwesomeAssertions:** `Should().BeOk()`, `BeNone()` and assertions on the error.
  - **Dapper:** `Option<T>` maps to and from database `NULL`.
  - **Refit:** API responses become results, with a 404 as `None` when you ask for it.
  - **FluentAssertions:** the same assertions for FluentAssertions 8.
  - **Serilog:** `.Destructure.WithFluentUnions()` logs results and options as structured values.
  - **Newtonsoft.Json:** the same option, union and result behavior as with System.Text.Json.
  - **OpenTelemetry:** `Activity.RecordResult` sets the status and records the exception.

## Documentation

The [docs](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/README.md) explain every concern with several examples. The [reference](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/reference.md) lists every type and operator on one page.

| Area                  | Page                                                                                                                      | What you learn                                                                                                              |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Getting started       | [Introduction](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/getting-started/README.md)                         | What the library is and which package to use                                                                                |
|                       | [Installation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/getting-started/installation.md)                   | Requirements, packages, namespaces, the analyzer, CS8509 as an error                                                        |
|                       | [Quick start](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/getting-started/quick-start.md)                     | A 10-minute walkthrough from `return` to an HTTP endpoint                                                                   |
|                       | [Limitations](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/getting-started/limitations.md)                     | What the library cannot do                                                                                                  |
| Result                | [Overview](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/result/README.md)                                      | `Result` and `Result<T>` in short                                                                                           |
|                       | [Core concepts](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/result/core-concepts.md)                          | The two states, creating results, members, matching, deconstruction, `Check()`                                              |
|                       | [Operations](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/result/operations.md)                                | `Select`, `Chain`, `Ensure`, `Zip`, `CombineAll`, `BindAppend`, `Do`, `Out`, `Finally`, `Unwrap`, `Match` and more         |
|                       | [Error handling](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/result/error-handling.md)                        | `Catch`, `OnErrorDo`, `Try`, `Retry`, and what still throws                                                                 |
|                       | [Collections](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/result/collections.md)                              | `Combine`, `CombineAll`, `Partition`, `Values`, `Errors`                                                                    |
|                       | [Patterns](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/result/patterns.md)                                    | Pipelines, query syntax, using earlier values, early returns, service layers                                                |
| Option                | [Overview](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/option/README.md)                                      | `Option<T>` in short                                                                                                        |
|                       | [Core concepts](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/option/core-concepts.md)                          | Creating options, members, matching, why `default` means no value                                                           |
|                       | [Operations](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/option/operations.md)                                | `Select`, `Chain`, `Where`, `Or`, `OnNoneDo`, `Match` and more                                                              |
|                       | [Collections](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/option/collections.md)                              | `FirstOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone`, `Combine`                                               |
|                       | [Conversions](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/option/conversions.md)                              | `ToOption`, `ToNullable`, `ToResult`, `Transpose`                                                                           |
|                       | [Patterns](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/option/patterns.md)                                    | Replacing `null`, fallbacks, optional settings and DTO fields                                                               |
| Async                 | [Overview](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/async/README.md)                                       | How `Task` pipelines behave                                                                                                 |
|                       | [Task pipelines](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/async/task-pipelines.md)                         | `Task` overloads, `...Await` operators, async query syntax                                                                  |
|                       | [Concurrency and resilience](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/async/concurrency-and-resilience.md) | `WhenAll`, `Retry`, `RetryAsync`, `Timeout`                                                                                 |
|                       | [Streams](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/async/streams.md)                                       | `IAsyncEnumerable` of results and options                                                                                   |
| Validation            | [Validation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/validation/README.md)                                | `Validate`, `CombineAll`, the rule catalogue, and validation errors over HTTP                                               |
| Errors                | [Overview](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/README.md)                                      | Choosing between exceptions, custom types and errors as data                                                                |
|                       | [Custom error types](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md)                | Your own result type with `IResult`                                                                                         |
|                       | [Errors as data](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/errors-as-data.md)                        | `Result<T, TError>` with a union of error records                                                                           |
| Unions                | [Overview](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/unions/README.md)                                      | Helpers for unions in short                                                                                                 |
|                       | [Your own unions](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/unions/your-own-unions.md)                      | `Is`, `As`, and the JSON converter for your unions                                                                          |
|                       | [Loadable](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/unions/loadable.md)                                    | `Loadable<T>` for asynchronously loaded UI state                                                                            |
|                       | [Modeling](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/unions/modeling.md)                                    | State machines and event sourcing with unions                                                                               |
| Serialization         | [Serialization](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/serialization/README.md)                          | `System.Text.Json` for options, results and your unions                                                                     |
| ASP.NET Core          | [Overview](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/aspnetcore/README.md)                                  | The `Fluent.Unions.AspNetCore` package in short                                                                             |
|                       | [HTTP results](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/aspnetcore/http-results.md)                        | `ToHttpResult` for results, options and errors as data                                                                      |
|                       | [Endpoint filter](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/aspnetcore/endpoint-filter.md)                  | Returning results straight from handlers with `WithResultMapping`                                                           |
|                       | [Error mapping](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/aspnetcore/error-mapping.md)                      | Status codes, ProblemDetails, and what is exposed                                                                           |
| Entity Framework Core | [Entity Framework Core](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/entity-framework-core/README.md)          | Storing options with nullable backing fields                                                                                |
| FluentValidation      | [FluentValidation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/fluent-validation/README.md)                   | `ValidateToResult` and `Validate(validator)` with every failure collected                                                   |
| Polly                 | [Polly](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/polly/README.md)                                          | `HandleError`, `ExecuteResultAsync` and `ToResult` for resilience pipelines                                                 |
| OpenAPI               | [OpenAPI](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/openapi/README.md)                                      | `AddFluentUnions()` describes `Option<T>` as a nullable `T`                                                                 |
| AwesomeAssertions     | [AwesomeAssertions](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/awesome-assertions/README.md)                 | Assertions for `Result`, `Result<T>`, `Option<T>` and the error inside                                                      |
| Dapper | [Dapper](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/dapper/README.md) | `OptionHandlers.Register<T>()` maps `Option<T>` to and from database `NULL` |
| Refit | [Refit](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/refit/README.md) | Refit API responses as `Result`, `Result<T>` and `Result<Option<T>>` |
| FluentAssertions | [FluentAssertions](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/fluent-assertions/README.md) | Assertions for `Result`, `Result<T>`, `Option<T>` and the error inside, for FluentAssertions 8 |
| Serilog | [Serilog](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/serilog/README.md) | `.Destructure.WithFluentUnions()` logs results, options and loadables as structured values |
| Newtonsoft.Json | [Newtonsoft.Json](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/newtonsoft-json/README.md) | Option, union and result serialization for Newtonsoft.Json |
| Tracing               | [Tracing](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/tracing/README.md)                                      | `Activity.RecordResult` for status and exception events                                                                     |
| Analyzer              | [Analyzer](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/analyzer/README.md)                                    | Rules `FLU0001` to `FLU0013`: ignored results, async lambdas, overlapping cases and more, with code fixes                     |

## Build and test

```
dotnet build Fluent.Unions.slnx
dotnet test Fluent.Unions.slnx
```

| Folder                                | Contents                                                                                             |
| ------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `src/Fluent.Unions`                   | The `Fluent.Unions` package                                                                          |
| `src/Fluent.Unions.Analyzers`         | The analyzers packed into `Fluent.Unions`                                                            |
| `src/Fluent.Unions.Analyzers.CodeFixes` | The code fixes for the analyzers, packed into `Fluent.Unions`                                      |
| `src/Fluent.Unions.AspNetCore`        | The `Fluent.Unions.AspNetCore` package                                                               |
| `src/Fluent.Unions.FluentValidation`  | The `Fluent.Unions.FluentValidation` package                                                         |
| `src/Fluent.Unions.Polly`             | The `Fluent.Unions.Polly` package                                                                    |
| `src/Fluent.Unions.OpenApi`           | The `Fluent.Unions.OpenApi` package                                                                  |
| `src/Fluent.Unions.AwesomeAssertions` | The `Fluent.Unions.AwesomeAssertions` package                                                        |
| `src/Fluent.Unions.Dapper` | The `Fluent.Unions.Dapper` package |
| `src/Fluent.Unions.Refit` | The `Fluent.Unions.Refit` package |
| `src/Fluent.Unions.FluentAssertions` | The `Fluent.Unions.FluentAssertions` package |
| `src/Fluent.Unions.Serilog` | The `Fluent.Unions.Serilog` package |
| `src/Fluent.Unions.NewtonsoftJson` | The `Fluent.Unions.NewtonsoftJson` package |
| `test/`                               | The NUnit tests for each project. `test/Fluent.Unions.Tests/Samples` holds runnable modeling samples |
| `docs/`                               | The documentation                                                                                    |
| `assets/`                             | The package icon (`icon.png`, 128 px) and a 1024 px master                                           |

Shared package settings live in `src/Directory.Build.props`. Each package's NuGet readme is the `README.md` in its project folder, so this file is shown on GitHub only. The license is in `LICENSE`.
