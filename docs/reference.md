# Reference

Every public type and operator, grouped by what you do with it. Each row links to the page that explains it with examples.

```csharp
using Fluent.Unions;               // Result, Result<T>, Option<T>, Loadable<T>, Result<T, TError>, Captured<T>
using Fluent.Unions.Extensions;    // all operators
using Fluent.Unions.Abstractions;  // IResult, IErrorResult, IFromException (custom error types)
using Fluent.Unions.Serialization; // OptionJsonConverter, ResultJsonConverter, UnionJsonConverter
using Fluent.Unions.AspNetCore;    // ToHttpResult, WithResultMapping, ResultHttpOptions (Fluent.Unions.AspNetCore)
```

## Types

| Type | Cases | Docs |
| --- | --- | --- |
| `Result` | `Ok` or `Exception`; `default` is empty | [Result core concepts](result/core-concepts.md) |
| `Result<T>` | `T` or `Exception`; `default` is empty | [Result core concepts](result/core-concepts.md) |
| `Result<T, TError>` | `T` or `TError`, where `TError` implements `IFromException<TError>` | [Errors as data](errors/errors-as-data.md) |
| `Option<T>` | `T` or `None`; `default` holds no value either | [Option core concepts](option/core-concepts.md) |
| `Loadable<T>` | `NotLoaded`, `Loading`, `T` or `Exception` | [Loadable](unions/loadable.md) |
| `Captured<T>` | Holder filled by `Out(out var value)` | [Result operations](result/operations.md) |
| `Ok`, `None`, `NotLoaded`, `Loading` | Marker structs used as cases | |
| `ResultErrorException` | Thrown by `Check()` for errors that are not exceptions | [Errors as data](errors/errors-as-data.md) |

## Creating

| Member | Result | Docs |
| --- | --- | --- |
| implicit conversion from `T`, `Exception`, `Ok`, `None` | the matching case | [Result](result/core-concepts.md), [Option](option/core-concepts.md) |
| `Result.Ok`, `Result<T>.Ok(value)`, `Result.Error(e)`, `Result<T>.Error(e)` | explicit creation | [Result core concepts](result/core-concepts.md) |
| `Result.Try(...)`, `Result.TryAsync(...)` | runs code that may throw | [Error handling](result/error-handling.md) |
| `Option.None`, `Option.Some(value)` | explicit creation | [Option core concepts](option/core-concepts.md) |
| `value.ToOption()` | from a nullable reference or nullable value type | [Option conversions](option/conversions.md) |
| `Option.TryParse<T>(text)` | parses any `IParsable<T>` / `ISpanParsable<T>` | [Option core concepts](option/core-concepts.md) |
| `Loadable.NotLoaded`, `Loadable.Loading` | waiting states | [Loadable](unions/loadable.md) |

## Members

| Member | On | Docs |
| --- | --- | --- |
| `Success`, `Message`, `GetOk()`, `GetError()` | `Result`, `Result<T>`, `Result<T, TError>` (no `Message`) | [Result core concepts](result/core-concepts.md) |
| `HasValue`, `TryGetValue(out value)`, `GetValue()` | `Option<T>` | [Option core concepts](option/core-concepts.md) |
| `IsNotLoaded`, `IsLoading`, `HasValue`, `IsFaulted`, `TryGetValue` | `Loadable<T>` | [Loadable](unions/loadable.md) |
| `var (value, error) = result` | `Result<T>` | [Result core concepts](result/core-concepts.md) |
| `Value` | every union, from .NET's `IUnion` | [Your own unions](unions/your-own-unions.md) |

## Operators on the value

| Operator | Result | Option | Task overloads | Docs |
| --- | --- | --- | --- | --- |
| `Select` | yes | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Chain` / `SelectMany` | yes | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Where` | `Result<T>` | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Zip` | `Result<T>` | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Out(out var value)` | `Result<T>` | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Unwrap` | nested results | nested options | | [Result](result/operations.md), [Option](option/operations.md) |
| `Or(() => ...)` | | yes | yes | [Option operations](option/operations.md) |

## Operators on the error

| Operator | On | Docs |
| --- | --- | --- |
| `Catch(e => ...)`, `Catch((IOException e) => ...)`, `Catch(fallback)` | results, `Task` of results, custom types | [Error handling](result/error-handling.md) |
| `OnErrorDo(e => ...)`, `Do(v => ..., e => ...)` | results, `Task` of results, custom types | [Error handling](result/error-handling.md) |
| `MapError(e => ...)` | `Result<T, TError>`, custom types (explicit type arguments) | [Errors as data](errors/errors-as-data.md) |
| `Validate(rules...)` | `Result<T>`, `Task<Result<T>>` | [Validation](validation/README.md) |

## Side effects

| Operator | Docs |
| --- | --- |
| `Do(v => ...)`, `Finally(() => ...)` | [Result operations](result/operations.md) |
| `OnNoneDo(() => ...)`, `Do(v => ..., () => ...)` | [Option operations](option/operations.md) |

## Leaving the chain

| Operator | On | Docs |
| --- | --- | --- |
| `Match(...)` | results (three callbacks), options (two), `Loadable<T>` (four), custom types | [Result](result/operations.md), [Option](option/operations.md) |
| `GetValueOrDefault()`, `GetValueOrDefault(fallback)`, `GetValueOrDefault(() => ...)` | results, options | [Result](result/operations.md), [Option](option/operations.md) |
| `DefaultIfEmpty(fallback)`, `DefaultIfEmpty(() => ...)` | results, options | [Result](result/operations.md), [Option](option/operations.md) |
| `Check()` | results, custom types | [Error handling](result/error-handling.md) |
| `ToNullable()` | `Option<T>` of a struct | [Option conversions](option/conversions.md) |

## Conversions

| Operator | From → to | Docs |
| --- | --- | --- |
| `ToOption()` | `Result<T>` → `Option<T>`, `Loadable<T>` → `Option<T>` | [Option conversions](option/conversions.md) |
| `ToResult(() => error)` | `Option<T>` → `Result<T>` | [Option conversions](option/conversions.md) |
| `Transpose()` | `Option<Result<T>>` ↔ `Result<Option<T>>` | [Option conversions](option/conversions.md) |
| `ToLoadable()` | `Result<T>` / `Task<Result<T>>` → `Loadable<T>` | [Loadable](unions/loadable.md) |
| implicit | `Result<T>` → `Result` | [Result core concepts](result/core-concepts.md) |

## Sequences

| Operator | On | Docs |
| --- | --- | --- |
| `Combine()`, `CombineAll()`, `Partition()`, `Values()`, `Errors()` | `IEnumerable<Result<T>>` | [Result collections](result/collections.md) |
| `Combine()`, `Values()` | `IEnumerable<Option<T>>` | [Option collections](option/collections.md) |
| `FirstOrNone()`, `LastOrNone()`, `SingleOrNone()`, `ElementAtOrNone(i)` | `IEnumerable<T>` | [Option collections](option/collections.md) |
| `GetValueOrNone(key)` | `IReadOnlyDictionary<TKey, TValue>` | [Option collections](option/collections.md) |

## Async

| Member | Docs |
| --- | --- |
| `Task` overloads of the operators, `SelectAwait`, `ChainAwait`, `SelectManyAwait`, `WhereAwait`, `DoAwait`, `CatchAwait`, `FinallyAwait` | [Task pipelines](async/task-pipelines.md) |
| `Result.WhenAll(tasks)`, `Result.Retry(...)`, `Result.RetryAsync(...)`, `task.Timeout(duration)` | [Concurrency and resilience](async/concurrency-and-resilience.md) |
| `Values()`, `Errors()`, `CombineAsync()` on `IAsyncEnumerable` | [Streams](async/streams.md) |

## Your own unions and custom error types

| Member | Docs |
| --- | --- |
| `union.Is<TCase>()`, `union.As<TCase>()` | [Your own unions](unions/your-own-unions.md) |
| `IResult<TSelf, T, TError>`, `IErrorResult<TSelf, TError>` | [Custom error types](errors/custom-error-types.md) |
| `IFromException<TSelf>`, `Result<T, TError>` | [Errors as data](errors/errors-as-data.md) |

## Serialization

| Type | Docs |
| --- | --- |
| `OptionJsonConverter` (applied to `Option<T>`), `ResultJsonConverter` (refuses results), `UnionJsonConverter` | [Serialization](serialization/README.md) |

## ASP.NET Core (`Fluent.Unions.AspNetCore`)

| Member | Docs |
| --- | --- |
| `ToHttpResult()` on results, options, `Result<T, TError>` and their `Task` versions | [HTTP results](aspnetcore/http-results.md) |
| `WithResultMapping()`, `ResultEndpointFilter` | [Endpoint filter](aspnetcore/endpoint-filter.md) |
| `ResultHttpOptions.Map<TException>(statusCode)` | [Error mapping](aspnetcore/error-mapping.md) |

## Analyzer

| Rule | Docs |
| --- | --- |
| `FLU0001`: the result of a call is not used | [Analyzer](analyzer/README.md) |

---

Back to index: [Docs index](README.md)
