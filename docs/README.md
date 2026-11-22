# Fluent.Unions documentation

Fluent.Unions brings `Result`, `Result<T>`, `Option<T>` and `Loadable<T>` to C# 15, built on the `union` keyword. Fluent operators are named after LINQ and Rx.NET. An analyzer and an ASP.NET Core package help you use results safely from the first method to the HTTP response.

## Your error type, your Result

Use the default `Result<T>`, whose error is any `Exception`, or declare your own. One struct is enough, and the source generator writes the rest:

```csharp
[FluentResult<DomainException>]
public readonly partial struct Result<T>;
```

| The generator writes | What it means for you |
| --- | --- |
| The union cases, constructors and implicit conversions | Return a value or a `DomainException` and it becomes a result |
| The value-less `Result` next to it | `Result.Ok`, `Result.Try`, `Result.From`, `Result.WhenAll` for your error type |
| Every operator and every `Task` and `...Await` overload | `Select`, `Chain`, `Ensure`, `Catch`, `Tap`, `MapError`, `Match`, query syntax, ... |
| One-line forwarders to the library | Your type behaves exactly like the built-in one, never boxes its value, and works in native AOT |

Make the error type a union and the compiler checks that every case is handled. `Capture` and `Propagate` on the attribute choose which exceptions become errors, so a bug does not pass as an expected failure. See [Your own Result type](errors/custom-error-types.md).

## Start here

- [Getting started](getting-started/README.md): what the library is, installation, and a quick start.
- [Your own Result type](errors/custom-error-types.md): the declaration, what the generator writes, the capture policy, and how to choose the shape of your errors.
- [Reference](reference.md): every type and operator on one page, with links to the detailed docs.

## Concepts and operators

| Folder | What it covers |
| --- | --- |
| [Result](result/README.md) | `Result` and `Result<T>`: the two states, creating results, operators, error handling, collections, patterns. |
| [Option](option/README.md) | `Option<T>`: values that may be missing, operators, collections, conversions, patterns. |
| [Async](async/README.md) | `Task` pipelines, the `...Await` operators, `WhenAll`, retries, timeouts, async streams. |
| [Validation](validation/README.md) | Running every rule and collecting every error with `Validate` and `CombineAll`, plus the rule catalogue. |
| [Errors](errors/README.md) | Your own Result type with `[FluentResult<TError>]`, and a closed set of errors as a union. |
| [Unions](unions/README.md) | Helpers for your own unions, `Loadable<T>` for UI state, and modeling with unions. |
| [Serialization](serialization/README.md) | `System.Text.Json` support for options, results and your own unions. |

## Integrations and tooling

| Folder | What it covers |
| --- | --- |
| [ASP.NET Core](aspnetcore/README.md) | The `Fluent.Unions.AspNetCore` package: HTTP responses, ProblemDetails, the endpoint filter, error mapping. |
| [AwesomeAssertions](awesome-assertions/README.md) | The `Fluent.Unions.AwesomeAssertions` package: assert on a `Result`, `Result<T>` or `Option<T>` and on the exception inside an error. |
| [Entity Framework Core](entity-framework-core/README.md) | Storing options in EF Core with nullable backing fields. |
| [FluentValidation](fluent-validation/README.md) | The `Fluent.Unions.FluentValidation` package: run a validator and get every failure as a `Result<T>`. |
| [OpenAPI](openapi/README.md) | The `Fluent.Unions.OpenApi` package: describe `Option<T>` as a nullable `T` in the generated OpenAPI document. |
| [Polly](polly/README.md) | The `Fluent.Unions.Polly` package: handle a `Result<T>` error in a Polly strategy and run a pipeline that returns a result. |
| [Dapper](dapper/README.md) | The `Fluent.Unions.Dapper` package: map `Option<T>` to and from database `NULL` with Dapper type handlers. |
| [Refit](refit/README.md) | The `Fluent.Unions.Refit` package: convert Refit API responses to results, with a 404 as `None`. |
| [FluentAssertions](fluent-assertions/README.md) | The `Fluent.Unions.FluentAssertions` package: the same assertions as the AwesomeAssertions package, for FluentAssertions 8 (commercial license for commercial use). |
| [Serilog](serilog/README.md) | The `Fluent.Unions.Serilog` package: log results, options and loadables as structured Serilog values. |
| [Newtonsoft.Json](newtonsoft-json/README.md) | The `Fluent.Unions.NewtonsoftJson` package: option, union and result serialization for Newtonsoft.Json. |
| [Mediator and MediatR](mediator/README.md) | The `Fluent.Unions.Mediator` and `Fluent.Unions.MediatR` packages: pipeline behaviors that return a failed result instead of throwing. |
| [MongoDB](mongodb/README.md) | The `Fluent.Unions.MongoDB` package: store `Option<T>` as a value or BSON null and refuse to serialize results. |
| [Swashbuckle](openapi/swashbuckle.md) | The `Fluent.Unions.Swashbuckle` package: describe `Option<T>` as a nullable `T` in Swashbuckle documents. |
| [NSwag](openapi/nswag.md) | The `Fluent.Unions.NSwag` package: describe `Option<T>` as a nullable `T` in NSwag documents. |
| [Testing](testing/README.md) | The `Fluent.Unions.Shouldly`, `.TUnit`, `.NUnit` and `.Xunit` packages: assert on a `Result`, `Result<T>` or `Option<T>` in each test library. |
| [Mapping](mapping/README.md) | The `Fluent.Unions.Mapster` and `Fluent.Unions.AutoMapper` packages: map `Option<T>` to and from `T?` and between option types. |
| [HotChocolate](hotchocolate/README.md) | The `Fluent.Unions.HotChocolate` package: resolvers return `Result<T>` and `Option<T>`, errors become GraphQL errors or mutation error unions. |
| [MessagePack](messagepack/README.md) | The `Fluent.Unions.MessagePack` package: formatter and resolver for `Option<T>`. |
| [Orleans](orleans/README.md) | The `Fluent.Unions.Orleans` package: `Option<T>` in grain calls and state. |
| [gRPC](grpc/README.md) | The `Fluent.Unions.Grpc` package: calls as `Result<T>`, errors as status codes. |
| [Verify](verify/README.md) | The `Fluent.Unions.Verify` package: readable Verify snapshots of results, options and loadables. |
| [Tracing](tracing/README.md) | `RecordResult` copies a result onto a `System.Diagnostics.Activity`: status and exception event. |
| [Analyzer](analyzer/README.md) | Rules for ignored results, async lambdas that escape a result, overlapping cases and more. |

## Conventions in these docs

- **Example domain:** an online shop (customers, carts, orders, payments).
- **Results in comments:** a trailing comment shows the result of an expression: `// 3`, `// None`, `// error: Not found`.
- **Compiled examples:** every C# example was compiled against the library when it was written.

---

Next: [Getting started](getting-started/README.md)
