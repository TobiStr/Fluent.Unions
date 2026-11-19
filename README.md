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

## A real-world example: checkout

A shop places an order. Like most real code, it works with code it does not control: a repository that returns `null`, and a payment SDK that throws. The request can fail in six ways: invalid input, unknown customer, unknown product, out of stock, payment declined, or success.

### The traditional way

Null checks, early returns, a `try`/`catch`, and an `out` parameter that carries the error as a string:

```csharp
public static Receipt? PlaceOrder(Shop shop, CheckoutRequest request, out string? error)
{
    if (string.IsNullOrWhiteSpace(request.Sku))
    {
        error = "Sku must not be empty or white space.";
        return null;
    }

    if (request.Quantity is < 1 or > 10)
    {
        error = "Quantity must be between 1 and 10.";
        return null;
    }

    var customer = shop.FindCustomer(request.CustomerId);
    if (customer is null)
    {
        error = $"No customer {request.CustomerId}.";
        return null;
    }

    var product = shop.FindProduct(request.Sku);
    if (product is null)
    {
        error = $"No product {request.Sku}.";
        return null;
    }

    if (product.Stock < request.Quantity)
    {
        error = $"Only {product.Stock} left of {product.Sku}.";
        return null;
    }

    var total = product.Price * request.Quantity;
    string paymentId;
    try
    {
        paymentId = shop.Charge(customer, total);
    }
    catch (PaymentDeclinedException exception)
    {
        error = exception.Message;
        return null;
    }

    var receipt = new Receipt(customer.Name, product.Sku, total, paymentId);
    shop.Save(receipt);
    error = null;
    return receipt;
}
```

The caller has to remember the null check, and can only tell the failures apart by inspecting strings:

```csharp
var receipt = PlaceOrder(shop, request, out var error);
if (receipt is not null) return 201;
if (error!.StartsWith("No ")) return 404;
if (error.StartsWith("Only ")) return 409;
if (error.Contains("declined")) return 402;
return 400;
```

### With Fluent.Unions

The same flow as one chain. Each step runs only if the previous one succeeded:

```csharp
public static Result<Receipt> PlaceOrder(Shop shop, CheckoutRequest request) =>
    Result<CheckoutRequest>.Ok(request)
        .Validate(
            r => r.Sku.EnsureNotWhiteSpace(),
            r => r.Quantity.EnsureInRange(1, 10))
        .Chain(r => FindCustomer(shop, r.CustomerId)).Out(out var customer)
        .Chain(_ => FindProduct(shop, request.Sku))
        .Ensure(p => p.Stock >= request.Quantity, p => new InvalidOperationException($"Only {p.Stock} left of {p.Sku}."))
        .Select(p => (Product: p, Total: p.Price * request.Quantity))
        .Select(x => new Receipt(customer.Value.Name, x.Product.Sku, x.Total, shop.Charge(customer, x.Total)))
        .Do(shop.Save);

private static Result<Customer> FindCustomer(Shop shop, Guid id) =>
    shop.FindCustomer(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));

private static Result<Product> FindProduct(Shop shop, string sku) =>
    shop.FindProduct(sku).ToResult(() => new KeyNotFoundException($"No product {sku}."));
```

The caller matches on the error's type, and the compiler checks that every case is handled:

```csharp
int status = PlaceOrder(shop, request) switch
{
    Receipt => 201,
    AggregateException => 400,          // every validation error at once
    KeyNotFoundException => 404,
    InvalidOperationException => 409,
    PaymentDeclinedException => 402,
    Exception => 500
};
```

That is fine for one call. With many commands, don't repeat it in each one. Commands like `PlaceOrder` only return a result, and turning it into an HTTP response happens once, at the boundary:

```csharp
public record HttpError(int StatusCode, string Message);

public record HttpResponse(int StatusCode, object? Body);

public static class HttpResponseExtensions
{
    public static HttpResponse ToHttpResponse<T>(this Result<T> result, int successStatusCode = 200) => result.Match(
        value => new HttpResponse(successStatusCode, value),
        error => ToHttpResponse(error.ToHttpError()),
        () => new HttpResponse(204, null));

    public static HttpError ToHttpError(this Exception error) => error switch
    {
        AggregateException validation => new(400, string.Join(" ", validation.InnerExceptions.Select(e => e.Message))),
        KeyNotFoundException => new(404, error.Message),
        InvalidOperationException => new(409, error.Message),
        PaymentDeclinedException => new(402, error.Message),
        _ => new(500, "An unexpected error occurred.")
    };

    private static HttpResponse ToHttpResponse(HttpError error) => new(error.StatusCode, error);
}
```

Each endpoint is then one line:

```csharp
HttpResponse response = PlaceOrder(shop, request).ToHttpResponse(201);
// 201 with the receipt, or e.g. 400 "Sku must not be empty or white space. Quantity must be between 1 and 10."
```

In ASP.NET Core, [Fluent.Unions.AspNetCore](docs/aspnetcore/README.md) provides this boundary ready-made: `ToHttpResult()` answers with ProblemDetails, and the status codes are configured once with `ResultHttpOptions`.

### What the union gives you

- **The signature is honest.** `Result<Receipt>` says that the call can fail. `Receipt?` with an `out string?` hides it, and nothing makes the caller check.
- **Every case is handled.** A `switch` over the result has to cover both the receipt and the error, or the compiler reports the missing case (CS8509). `Match` takes one callback per state, so none can be forgotten there either.
- **Errors keep their type, and the mapping lives in one place.** `ToHttpError` maps `KeyNotFoundException` and `PaymentDeclinedException` to status codes, not strings. Rewording a message cannot change a status code, and unknown errors answer 500 without leaking their details.
- **Throwing code joins the chain.** `shop.Charge` throws on a declined card. Inside `Select` that exception becomes the error, so no `try`/`catch` is needed and the order is not saved.
- **No `null` leaks.** `ToResult(...)` turns the repository's `null` into a named error once, at the boundary.
- **Validation reports everything.** The traditional version stops at the first problem. `Validate` collects all of them in one `AggregateException`.
- **The happy path reads top to bottom.** Errors skip the remaining steps instead of breaking the flow with early returns.

Both versions, including the `Shop` stub, are in [`CheckoutSample.cs`](test/Fluent.Unions.Tests/Samples/CheckoutSample.cs). Its tests check that they return the same status code for all six cases, and that the error mapping hides unexpected errors.

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
