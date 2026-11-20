# Reference

Every public type and operator, grouped by what you do with it. Each row links to the page that explains it with examples.

```csharp
using Fluent.Unions;               // Result, Result<T>, Option<T>, Loadable<T>, Result<T, TError>, ValidationError(s), and all operators
using Fluent.Unions.Validation;    // Is and the EnsureXxx rules
using Fluent.Unions.Abstractions;  // IResult, IErrorResult, IFromException (custom error types)
using Fluent.Unions.Serialization; // OptionJsonConverter, ResultJsonConverter, UnionJsonConverter
using Fluent.Unions.AspNetCore;    // ToHttpResult, WithResultMapping, ResultHttpOptions (Fluent.Unions.AspNetCore)
```

## Types

| Type | Cases | Docs |
| --- | --- | --- |
| `Result` | `Ok` or `Exception`; `default` counts as an error | [Result core concepts](result/core-concepts.md) |
| `Result<T>` | `T` or `Exception`; `default` and `null` count as an error | [Result core concepts](result/core-concepts.md) |
| `Result<T, TError>` | `T` or `TError`, where `TError` implements `IFromException<TError>` | [Errors as data](errors/errors-as-data.md) |
| `Option<T>` | `T` or `None`; `default` holds no value either | [Option core concepts](option/core-concepts.md) |
| `Loadable<T>` | `NotLoaded`, `Loading`, `T` or `Exception` | [Loadable](unions/loadable.md) |
| `Captured<T>` | Holder filled by `Out(out var value)` | [Result operations](result/operations.md) |
| `Ok`, `None`, `NotLoaded`, `Loading` | Marker structs used as cases | |
| `ResultErrorException` | Thrown by `Check()` for errors that are not exceptions | [Errors as data](errors/errors-as-data.md) |
| `ValidationError`, `ValidationErrors` | A failed rule with `MemberName`, and every failed rule of a value | [Validation](validation/README.md#errors) |

## Creating

| Member | Result | Docs |
| --- | --- | --- |
| implicit conversion from `T`, `Exception`, `Ok`, `None` | the matching case | [Result](result/core-concepts.md), [Option](option/core-concepts.md) |
| `Result.Ok`, `Result<T>.Ok(value)`, `Result.From(value)`, `Result.Error(e)`, `Result<T>.Error(e)` | explicit creation | [Result core concepts](result/core-concepts.md) |
| `Result.Try(...)`, `Result.TryAsync(...)` | runs code that may throw | [Error handling](result/error-handling.md) |
| `Option.None`, `Option.Some(value)` | explicit creation | [Option core concepts](option/core-concepts.md) |
| `value.ToOption()` | from a nullable reference or nullable value type | [Option conversions](option/conversions.md) |
| `Option.TryParse<T>(text)` | parses any `IParsable<T>` / `ISpanParsable<T>` | [Option core concepts](option/core-concepts.md) |
| `Loadable.NotLoaded`, `Loadable.Loading` | waiting states | [Loadable](unions/loadable.md) |

## Members

| Member | On | Docs |
| --- | --- | --- |
| `IsSuccess`, `IsError`, `TryGetError(out error)`, `GetError()` | `Result`, `Result<T>`, `Result<T, TError>`, and `TryGetError` on custom result types | [Result core concepts](result/core-concepts.md) |
| `TryGetValue(out value)`, `GetValue()` | `Result<T>`, `Result<T, TError>` | [Result core concepts](result/core-concepts.md) |
| `HasValue`, `TryGetValue(out value)`, `GetValue()` | `Option<T>` | [Option core concepts](option/core-concepts.md) |
| `IsNotLoaded`, `IsLoading`, `IsLoaded`, `IsFaulted`, `TryGetValue` | `Loadable<T>` | [Loadable](unions/loadable.md) |
| `var (value, error) = result` | `Result<T>` | [Result core concepts](result/core-concepts.md) |
| `Value` | every union, from .NET's `IUnion` | [Your own unions](unions/your-own-unions.md) |

## Operators on the value

| Operator | Result | Option | Task overloads | Docs |
| --- | --- | --- | --- | --- |
| `Select` | yes | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Chain` / `SelectMany` | yes | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Where` | `Option<T>` | yes | yes | [Option](option/operations.md) |
| `Zip` | `Result<T>` | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Ensure(predicate, message)`, `Ensure(predicate, v => exception)`, `EnsureAwait(...)` | `Result<T>` | | yes | [Result](result/operations.md) |
| `Result.CombineAll(a, b[, c[, d]])`, `Result.CombineAllAsync(...)` | `Result<T>` | | tasks | [Result](result/operations.md) |
| `BindAppend(v => ...)`, `BindAppend((a, b) => ...)`, `BindAppendAwait(...)` | `Result<T>`, tuples up to three elements | | yes | [Result](result/operations.md) |
| `WithValue(value)`, `DiscardValue()` | `Result`, `Result<T>` | | yes | [Result](result/operations.md) |
| `Out(out var value)` | `Result<T>` | yes | yes | [Result](result/operations.md), [Option](option/operations.md) |
| `Unwrap` | nested results | nested options | | [Result](result/operations.md), [Option](option/operations.md) |
| `Or(() => ...)` | | yes | yes | [Option operations](option/operations.md) |

## Operators on the error

| Operator | On | Docs |
| --- | --- | --- |
| `Catch(e => ...)`, `Catch((IOException e) => ...)`, `Catch(fallback)` | results, `Task` of results, custom types | [Error handling](result/error-handling.md) |
| `OnErrorDo(e => ...)`, `Do(v => ..., e => ...)` | results, `Task` of results, custom types | [Error handling](result/error-handling.md) |
| `MapError(e => ...)` | `Result<T, TError>`, custom types (explicit type arguments) | [Errors as data](errors/errors-as-data.md) |
| `Validate(rules...)` | `Result<T>`, `Task<Result<T>>`, custom types (`Validate(combine, rules...)`) | [Validation](validation/README.md), [Custom error types](errors/custom-error-types.md) |
| `ValidateAnnotations()` | `Result<T>`, `Task<Result<T>>` | [Validation](validation/README.md#dataannotations) |

## Validation rules

Namespace `Fluent.Unions.Validation`.

| API | On | Docs |
| --- | --- | --- |
| `Is.NotEmpty`, `Is.Empty`, `Is.NotWhiteSpace`, `Is.WhiteSpace` and the format, character class and `Includes*` predicates (`Is.MatchesEmail`, `Is.Alpha`, `Is.IncludesDigit`, ...) | `string?`, used as method groups | [Rule catalogue](validation/README.md#rule-catalogue) |
| `Is.HasLength(n)`, `Is.LongerThan(n)`, `Is.ShorterThan(n)` and the `OrEqual` forms | factories for `string?` | [String rules](validation/README.md#string-rules) |
| `Is.Contains(text)`, `Is.StartsWith(text)`, `Is.EndsWith(text)` and the `Not` forms, with a `StringComparison` | factories for `string?` | [String rules](validation/README.md#string-rules) |
| `Is.Matches(regex)`, `Is.Matches(pattern, options)`, `Is.NotMatches(...)` | factories for `string?` | [String rules](validation/README.md#string-rules) |
| `EnsureNotEmpty()`, `EnsureEmpty()`, `EnsureNotWhiteSpace()`, `EnsureWhiteSpace()` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `EnsureMatchesEmail()`, `EnsureMatchesUrl()`, `EnsureMatchesGuid()`, `EnsureMatchesIpAddress()`, `EnsureMatchesPhoneNumber()`, `EnsureBase64()` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `EnsureAlpha()`, `EnsureAlphanumeric()`, `EnsureNumeric()` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `EnsureIncludesDigit()`, `EnsureIncludesLetter()`, `EnsureIncludesLetterOrDigit()`, `EnsureIncludesLower()`, `EnsureIncludesUpper()`, `EnsureIncludesPunctuation()`, `EnsureIncludesSpecial()`, `EnsureIncludesSymbol()`, `EnsureIncludesWhiteSpace()` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `EnsureHasLength(n)`, `EnsureLongerThan(n)`, `EnsureLongerThanOrEqual(n)`, `EnsureShorterThan(n)`, `EnsureShorterThanOrEqual(n)` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `EnsureContains(text)`, `EnsureNotContains(text)`, `EnsureStartsWith(text)`, `EnsureNotStartsWith(text)`, `EnsureEndsWith(text)`, `EnsureNotEndsWith(text)` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `EnsureMatches(regex)`, `EnsureMatches(pattern, options)`, `EnsureNotMatches(...)` | `string?`, `Result<string>` | [String rules](validation/README.md#string-rules) |
| `value.Ensure(predicate, message)`, `value.Ensure(predicate, v => exception)` | any `T`, returns `Result<T>` | [Rule catalogue](validation/README.md#rule-catalogue) |
| `Is.Positive`, `Is.Negative`, `Is.NonPositive`, `Is.NonNegative`, `Is.Zero`, `Is.NonZero` | `T : INumberBase<T>`, used as method groups | [Number rules](validation/README.md#number-rules) |
| `Is.Even`, `Is.Odd`, `Is.DivisibleBy(divisor)` | `T : IBinaryInteger<T>` | [Number rules](validation/README.md#number-rules) |
| `Is.GreaterThan(bound)`, `Is.GreaterThanOrEqual(bound)`, `Is.LessThan(bound)`, `Is.LessThanOrEqual(bound)`, `Is.InRange(min, max)` | factories for `T : IComparable<T>` | [Number rules](validation/README.md#number-rules) |
| `EnsurePositive()`, `EnsureNegative()`, `EnsureNonPositive()`, `EnsureNonNegative()`, `EnsureZero()`, `EnsureNonZero()` | `T : INumberBase<T>`, `Result<T>` | [Number rules](validation/README.md#number-rules) |
| `EnsureEven()`, `EnsureOdd()`, `EnsureDivisibleBy(divisor)` | `T : IBinaryInteger<T>`, `Result<T>` | [Number rules](validation/README.md#number-rules) |
| `EnsureGreaterThan(bound)`, `EnsureGreaterThanOrEqual(bound)`, `EnsureLessThan(bound)`, `EnsureLessThanOrEqual(bound)`, `EnsureInRange(min, max)` | `T : IComparable<T>`, `Result<T>` | [Number rules](validation/README.md#number-rules) |
| `Is.Null`, `Is.NotNull`, `Is.Some`, `Is.None` | any `T` and `Option<T>`, used as method groups | [General rules](validation/README.md#general-rules) |
| `Is.EqualTo(expected)`, `Is.NotEqualTo(other)`, `Is.In(...)`, `Is.NotIn(...)` | factories for any `T` | [General rules](validation/README.md#general-rules) |
| `EnsureEqualTo(expected)`, `EnsureNotEqualTo(other)`, `EnsureIn([...])`, `EnsureNotIn([...])` | any `T`, `Result<T>` | [General rules](validation/README.md#general-rules) |
| `EnsureNotNull()` | any `T`, value only | [General rules](validation/README.md#general-rules) |
| `EnsureSome()`, `EnsureNone()` | `Option<T>`, `Result<Option<T>>` | [General rules](validation/README.md#general-rules) |
| `Is.NotEmpty`, `Is.Empty`, `Is.Unique` | `IEnumerable<TItem>?`, used as method groups | [Collection rules](validation/README.md#collection-rules) |
| `Is.Unique(comparer)`, `Is.HasCount(n)`, `Is.HasMinCount(n)`, `Is.HasMaxCount(n)` | factories for collections | [Collection rules](validation/README.md#collection-rules) |
| `Is.ContainsItem(item)`, `Is.NotContainsItem(item)`, `Is.AllMatch(predicate)`, `Is.AnyMatch(predicate)`, `Is.NoneMatch(predicate)` | factories for `IEnumerable<TItem>?` | [Collection rules](validation/README.md#collection-rules) |
| `EnsureNotEmpty()`, `EnsureEmpty()`, `EnsureUnique()`, `EnsureUnique(comparer)`, `EnsureHasCount(n)`, `EnsureHasMinCount(n)`, `EnsureHasMaxCount(n)` | any collection, `Result<TCollection>` | [Collection rules](validation/README.md#collection-rules) |
| `EnsureContainsItem(item)`, `EnsureNotContainsItem(item)`, `EnsureAllMatch(predicate)`, `EnsureAnyMatch(predicate)`, `EnsureNoneMatch(predicate)` | any collection, `Result<TCollection>` | [Collection rules](validation/README.md#collection-rules) |
| `Is.InPast(timeProvider)`, `Is.InPastOrPresent(...)`, `Is.InFuture(...)`, `Is.InFutureOrPresent(...)`, also taking a `DateTimeOffset` or `DateTime` as `now` | factories for `DateTimeOffset` and `DateTime` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |
| `EnsureInPast(timeProvider)`, `EnsureInPastOrPresent(...)`, `EnsureInFuture(...)`, `EnsureInFutureOrPresent(...)`, also taking `now` | `DateTimeOffset`, `Result<DateTimeOffset>`; with a `DateTime` `now`: `DateTime`, `Result<DateTime>` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |
| `Is.Positive`, `Is.Negative`, `Is.NonPositive`, `Is.NonNegative`, `Is.Zero`, `Is.NonZero` and the matching `Ensure*` rules | `TimeSpan`, `Result<TimeSpan>` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |
| `Is.Digit`, `Is.Letter`, `Is.LetterOrDigit`, `Is.Lower`, `Is.Upper`, `Is.Punctuation`, `Is.Special`, `Is.Symbol`, `Is.WhiteSpace` and the matching `Ensure*` rules | `char`, `Result<char>` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |
| `Is.NotEmpty`, `Is.Empty` and `EnsureNotEmpty()`, `EnsureEmpty()` | `Guid`, `Result<Guid>` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |
| `Is.True`, `Is.False` and `EnsureTrue()`, `EnsureFalse()` | `bool`, `Result<bool>` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |
| `Is.Defined`, `EnsureDefined()` | `TEnum : struct, Enum`, `Result<TEnum>` | [Date, time and other value rules](validation/README.md#date-time-and-other-value-rules) |

## Side effects

| Operator | Docs |
| --- | --- |
| `Do(v => ...)`, `Finally(() => ...)` | [Result operations](result/operations.md) |
| `OnNoneDo(() => ...)`, `Do(v => ..., () => ...)` | [Option operations](option/operations.md) |

## Leaving the chain

| Operator | On | Docs |
| --- | --- | --- |
| `Match(...)` | results and options (two callbacks), `Loadable<T>` (four), custom types | [Result](result/operations.md), [Option](option/operations.md) |
| `GetValueOrDefault()`, `GetValueOrDefault(fallback)`, `GetValueOrDefault(() => ...)` | results, options | [Result](result/operations.md), [Option](option/operations.md) |
| `DefaultIfEmpty(fallback)`, `DefaultIfEmpty(() => ...)` | options | [Option](option/operations.md) |
| `Check()` | results, custom types | [Error handling](result/error-handling.md) |
| `ToNullable()` | `Option<T>` of a struct | [Option conversions](option/conversions.md) |

## Conversions

| Operator | From → to | Docs |
| --- | --- | --- |
| `ToOption()` | `Result<T>` → `Option<T>`, `Loadable<T>` → `Option<T>` | [Option conversions](option/conversions.md) |
| `ToResult(() => error)` | `Option<T>`, `T?` (nullable reference or value type) → `Result<T>` | [Option conversions](option/conversions.md) |
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
| `Ensure`, `Validate`, `Out`, `DoAwait`, `CatchAwait`, `FinallyAwait`, `WhereAwait`, `ToOption`, `ToLoadable`, `Unwrap`, `RecordResult` on `IResult` | [Custom error types](errors/custom-error-types.md) |
| `SelectAwait`, `Zip`, `BindAppend`, `WithValue`, `Transpose` on `IResult` (explicit type arguments) | [Custom error types](errors/custom-error-types.md) |
| `IFromException<TSelf>`, `Result<T, TError>` | [Errors as data](errors/errors-as-data.md) |

## FluentValidation

Package `Fluent.Unions.FluentValidation`, namespace `Fluent.Unions.FluentValidation`.

| Member | On | Docs |
| --- | --- | --- |
| `ValidateToResult(instance)`, `ValidateToResultAsync(instance, cancellationToken)` | `IValidator<T>` | [FluentValidation](fluent-validation/README.md) |
| `Validate(validator)`, `ValidateAsync(validator, cancellationToken)` | `Result<T>` | [FluentValidation](fluent-validation/README.md) |
| `Validate(validator)`, `Validate(validator, map)`, `ValidateAsync(validator, cancellationToken)`, `ValidateAsync(validator, map, cancellationToken)` | custom types | [FluentValidation](fluent-validation/README.md#custom-result-types) |
| `RuleForSome(expression)` | `AbstractValidator<T>`, `Option<T>` properties | [FluentValidation](fluent-validation/README.md#option-properties) |

## Polly

Package `Fluent.Unions.Polly`, namespace `Fluent.Unions.Polly`.

| Member | On | Docs |
| --- | --- | --- |
| `HandleError()`, `HandleError(predicate)` | `PredicateBuilder<Result<T>>` | [Polly](polly/README.md) |
| `ExecuteResultAsync(operation, cancellationToken)` | `ResiliencePipeline<Result<T>>`, `ResiliencePipeline` | [Polly](polly/README.md) |
| `ToResult()` | `Outcome<Result<T>>` | [Polly](polly/README.md) |
| `HandleError<TResult, TError>()`, `HandleError<TResult, TError>(predicate)` | `PredicateBuilder<TResult>`, custom types (explicit type arguments) | [Polly](polly/README.md#custom-result-types) |
| `ExecuteResultAsync<TResult, TError>(operation, cancellationToken)` | `ResiliencePipeline<TResult>`, `ResiliencePipeline`, custom types (explicit type arguments) | [Polly](polly/README.md#custom-result-types) |
| `ToResult<TResult, TError>()` | `Outcome<TResult>`, custom types (explicit type arguments) | [Polly](polly/README.md#custom-result-types) |

## OpenAPI

Package `Fluent.Unions.OpenApi`, namespace `Fluent.Unions.OpenApi`.

| Member | On | Docs |
| --- | --- | --- |
| `AddFluentUnions()` | `OpenApiOptions` | [OpenAPI](openapi/README.md) |
| `OptionSchemaTransformer` | `IOpenApiSchemaTransformer` for `Option<T>` | [OpenAPI](openapi/README.md) |

## AwesomeAssertions

Package `Fluent.Unions.AwesomeAssertions`, namespace `Fluent.Unions.AwesomeAssertions`.

| Member | On | Docs |
| --- | --- | --- |
| `Should()` | `Result`, `Result<T>`, `Option<T>`, custom types | [AwesomeAssertions](awesome-assertions/README.md) |
| `BeOk()`, `BeError()`, `BeError<TException>()`, `BeError<TException>(predicate)`, `BeAggregateError()` | `ResultAssertions`, `ResultAssertions<T>` | [AwesomeAssertions](awesome-assertions/README.md) |
| `BeOk()` (with `Which`), `BeOkWithValue(value)`, `BeOkMatching(predicate)`, `BeOkSatisfying(assertion)` | `ResultAssertions<T>` | [AwesomeAssertions](awesome-assertions/README.md) |
| `BeOk()` (with `Which`), `BeOkWithValue(value)`, `BeOkMatching(predicate)`, `BeError()` (with `Which`), `BeError<TCase>()`, `BeError<TCase>(predicate)` | `CustomResultAssertions<TSelf, T, TError>` | [AwesomeAssertions](awesome-assertions/README.md#custom-result-types) |
| `BeSome()` (with `Which`), `BeSomeWithValue(value)`, `NotBeSomeWithValue(value)`, `BeSomeMatching(predicate)`, `BeSomeSatisfying(assertion)`, `BeNone()` | `OptionAssertions<T>` | [AwesomeAssertions](awesome-assertions/README.md) |
| `WithMessage(pattern)`, `WithMessageContaining(text)` | `ErrorAssertions<TException>` | [AwesomeAssertions](awesome-assertions/README.md) |
| `WithInnerCount(count)`, `WithInner<TException>()`, `WithInnerMessage(pattern)` | `AggregateErrorAssertions` | [AwesomeAssertions](awesome-assertions/README.md) |

## Dapper

Package `Fluent.Unions.Dapper`, namespace `Fluent.Unions.Dapper`.

| Member | On | Docs |
| --- | --- | --- |
| `Register<T>()` | `OptionHandlers`, registers `Option<T>` | [Dapper](dapper/README.md#registering-handlers) |
| `RegisterCommon()` | `OptionHandlers`, registers `Option<T>` for the common database types | [Dapper](dapper/README.md#registering-handlers) |
| `OptionTypeHandler<T>` | `SqlMapper.TypeHandler<Option<T>>` | [Dapper](dapper/README.md#what-is-mapped) |

## Refit

Package `Fluent.Unions.Refit`, namespace `Fluent.Unions.Refit`.

| Member | On | Docs |
| --- | --- | --- |
| `ToResult()` | `IApiResponse`, `Task<IApiResponse>` | [Refit](refit/README.md) |
| `ToResult<T>()` | `IApiResponse<T>`, `Task<IApiResponse<T>>`, `Task<ApiResponse<T>>` | [Refit](refit/README.md) |
| `ToOptionalResult<T>()` | `IApiResponse<T>`, `Task<IApiResponse<T>>`, `Task<ApiResponse<T>>` | [Refit](refit/README.md) |

## FluentAssertions

Package `Fluent.Unions.FluentAssertions`, namespace `Fluent.Unions.FluentAssertions`.

| Member | On | Docs |
| --- | --- | --- |
| `Should()` | `Result`, `Result<T>`, `Option<T>`, custom types | [FluentAssertions](fluent-assertions/README.md) |
| `BeOk()`, `BeError()`, `BeError<TException>()`, `BeError<TException>(predicate)`, `BeAggregateError()` | `ResultAssertions`, `ResultAssertions<T>` | [FluentAssertions](fluent-assertions/README.md) |
| `BeOk()` (with `Which`), `BeOkWithValue(value)`, `BeOkMatching(predicate)`, `BeOkSatisfying(assertion)` | `ResultAssertions<T>` | [FluentAssertions](fluent-assertions/README.md) |
| `BeOk()` (with `Which`), `BeOkWithValue(value)`, `BeOkMatching(predicate)`, `BeError()` (with `Which`), `BeError<TCase>()`, `BeError<TCase>(predicate)` | `CustomResultAssertions<TSelf, T, TError>` | [FluentAssertions](fluent-assertions/README.md#custom-result-types) |
| `BeSome()` (with `Which`), `BeSomeWithValue(value)`, `NotBeSomeWithValue(value)`, `BeSomeMatching(predicate)`, `BeSomeSatisfying(assertion)`, `BeNone()` | `OptionAssertions<T>` | [FluentAssertions](fluent-assertions/README.md) |
| `WithMessage(pattern)`, `WithMessageContaining(text)` | `ErrorAssertions<TException>` | [FluentAssertions](fluent-assertions/README.md) |
| `WithInnerCount(count)`, `WithInner<TException>()`, `WithInnerMessage(pattern)` | `AggregateErrorAssertions` | [FluentAssertions](fluent-assertions/README.md) |

## Serilog

Package `Fluent.Unions.Serilog`, namespace `Fluent.Unions.Serilog`.

| Member | On | Docs |
| --- | --- | --- |
| `WithFluentUnions()` | `LoggerDestructuringConfiguration` (`.Destructure.WithFluentUnions()`) | [Serilog](serilog/README.md) |
| `UnionDestructuringPolicy` | `IDestructuringPolicy` for `Result`, `Result<T>`, `Result<T, TError>`, `Option<T>`, `Loadable<T>` | [Serilog](serilog/README.md#log-shape) |

## Newtonsoft.Json

Package `Fluent.Unions.NewtonsoftJson`, namespace `Fluent.Unions.NewtonsoftJson`.

| Member | On | Docs |
| --- | --- | --- |
| `AddFluentUnions()` | `JsonSerializerSettings` | [Newtonsoft.Json](newtonsoft-json/README.md) |
| `OptionJsonConverter` | `Option<T>` | [Newtonsoft.Json](newtonsoft-json/README.md#options) |
| `ResultJsonConverter` | `Result`, `Result<T>`, `Result<T, TError>` | [Newtonsoft.Json](newtonsoft-json/README.md#results-refuse-to-serialize) |
| `UnionJsonConverter` | your own unions | [Newtonsoft.Json](newtonsoft-json/README.md#your-own-unions) |

## Mediator

Package `Fluent.Unions.Mediator`, namespace `Fluent.Unions.Mediator`.

| Member | On | Docs |
| --- | --- | --- |
| `ExceptionToResultBehavior<TMessage, TResponse>` | pipeline behavior, `TResponse` implements `IErrorResult<TResponse, Exception>` | [Mediator and MediatR](mediator/README.md#the-behaviors) |
| `ValidationToResultBehavior<TMessage, TResponse>` | pipeline behavior, same constraint | [Mediator and MediatR](mediator/README.md#the-behaviors) |

## MediatR

Package `Fluent.Unions.MediatR`, namespace `Fluent.Unions.MediatR`.

| Member | On | Docs |
| --- | --- | --- |
| `ExceptionToResultBehavior<TRequest, TResponse>` | pipeline behavior, `TResponse` implements `IErrorResult<TResponse, Exception>` | [Mediator and MediatR](mediator/README.md#the-behaviors) |
| `ValidationToResultBehavior<TRequest, TResponse>` | pipeline behavior, same constraint | [Mediator and MediatR](mediator/README.md#the-behaviors) |

## MongoDB

Package `Fluent.Unions.MongoDB`, namespace `Fluent.Unions.MongoDB`.

| Member | On | Docs |
| --- | --- | --- |
| `Register()` | `FluentUnionsBson` | [MongoDB](mongodb/README.md) |
| `OptionSerializer<T>` | `Option<T>` | [MongoDB](mongodb/README.md#options) |
| `OmitNoneConvention` | `ConventionPack`, `Option<T>` members | [MongoDB](mongodb/README.md#none-and-missing-elements) |
| `ResultSerializer<TResult>` | `Result`, `Result<T>`, `Result<T, TError>` (refuses) | [MongoDB](mongodb/README.md#results-refuse-to-serialize) |

## Swashbuckle

Package `Fluent.Unions.Swashbuckle`, namespace `Fluent.Unions.Swashbuckle`.

| Member | On | Docs |
| --- | --- | --- |
| `AddFluentUnions()`, `OptionSchemaFilter` | `SwaggerGenOptions`, `ISchemaFilter` | [Swashbuckle](openapi/swashbuckle.md) |

## NSwag

Package `Fluent.Unions.NSwag`, namespace `Fluent.Unions.NSwag`.

| Member | On | Docs |
| --- | --- | --- |
| `AddFluentUnions()`, `OptionSchemaProcessor` | `OpenApiDocumentGeneratorSettings`, `ISchemaProcessor` | [NSwag](openapi/nswag.md) |

## Testing

Packages `Fluent.Unions.Shouldly`, `.TUnit`, `.NUnit` and `.Xunit`, namespaces of the same names.

| Member | On | Docs |
| --- | --- | --- |
| `ShouldBeOk()`, `ShouldBeError()`, `ShouldBeError<TException>()`, `ShouldBeSome()`, `ShouldBeNone()` | `Result`, `Result<T>`, `Option<T>` (Shouldly) | [Testing](testing/README.md#shouldly) |
| `IsOk()`, `IsError()`, `IsError<TException>()`, `IsSome()`, `IsNone()` | `Assert.That` of `Result`, `Result<T>`, `Option<T>` (TUnit) | [Testing](testing/README.md#tunit) |
| `IsResult.Ok`, `Error`, `OkWith`, `ErrorWith`, `ErrorOfType<TException>()`; `IsOption.Some`, `None`, `SomeWith` | `UnionConstraint` (NUnit) | [Testing](testing/README.md#nunit) |
| `ResultAssert.Ok`, `Error`, `Error<TException>`; `OptionAssert.Some`, `None` | `Result`, `Result<T>`, `Option<T>` (xUnit v3) | [Testing](testing/README.md#xunit) |

## Mapping

Packages `Fluent.Unions.Mapster` and `Fluent.Unions.AutoMapper`, namespaces `Fluent.Unions.Mapster` and `Fluent.Unions.AutoMapper`.

| Member | On | Docs |
| --- | --- | --- |
| `AddFluentUnions()` | `TypeAdapterConfig` (Mapster) | [Mapping](mapping/README.md#mapster) |
| `AddFluentUnions()` | `IMapperConfigurationExpression` (AutoMapper) | [Mapping](mapping/README.md#automapper) |

## HotChocolate

Package `Fluent.Unions.HotChocolate`, namespace `Fluent.Unions.HotChocolate`.

| Member | On | Docs |
| --- | --- | --- |
| `AddFluentUnions()` | `IRequestExecutorBuilder` | [HotChocolate](hotchocolate/README.md) |

## Tracing

| Member | On | Docs |
| --- | --- | --- |
| `RecordResult(result)` | `Activity?`, with `Result`, `Result<T>` or `Result<T, TError>` | [Tracing](tracing/README.md) |

## Serialization

| Type | Docs |
| --- | --- |
| `OptionJsonConverter` (applied to `Option<T>`), `ResultJsonConverter` (refuses results), `UnionJsonConverter` | [Serialization](serialization/README.md) |

## ASP.NET Core (`Fluent.Unions.AspNetCore`)

| Member | Docs |
| --- | --- |
| `ToHttpResult()` on results, options, `Result<T, TError>` and their `Task` versions | [HTTP results](aspnetcore/http-results.md) |
| `ToHttpResult(onError)` on custom result types | [HTTP results](aspnetcore/http-results.md#custom-result-types) |
| `WithResultMapping()`, `ResultEndpointFilter` | [Endpoint filter](aspnetcore/endpoint-filter.md) |
| `WithResultMapping()` on handlers that return custom result types | [Endpoint filter](aspnetcore/endpoint-filter.md#custom-result-types) |
| `ResultHttpOptions.Map<TException>(statusCode)` | [Error mapping](aspnetcore/error-mapping.md) |

## Analyzer

| Rule | Docs |
| --- | --- |
| `FLU0001`: the result of a call is not used | [Analyzer](analyzer/README.md#flu0001) |
| `FLU0002`: an async lambda passed to a synchronous operator | [Analyzer](analyzer/README.md#flu0002) |
| `FLU0003`: an async lambda that becomes `async void` | [Analyzer](analyzer/README.md#flu0003) |
| `FLU0004`: a type argument that overlaps another case | [Analyzer](analyzer/README.md#flu0004) |
| `FLU0005`: a nullable type argument | [Analyzer](analyzer/README.md#flu0005) |
| `FLU0006`: `Select` that returns a nested result | [Analyzer](analyzer/README.md#flu0006) |
| `FLU0007`: a result created with `default` | [Analyzer](analyzer/README.md#flu0007) |
| `FLU0008`: a result that is serialized to JSON | [Analyzer](analyzer/README.md#flu0008) |
| `FLU0009`: `FromException` or `FromError` that throws or returns `null` | [Analyzer](analyzer/README.md#flu0009) |
| `FLU0010`: `GetValue()` or `GetError()` without a check | [Analyzer](analyzer/README.md#flu0010) |
| `FLU0011`: a cast of `Value` | [Analyzer](analyzer/README.md#flu0011) |
| `FLU0012`: a `throw` in a method that returns a result | [Analyzer](analyzer/README.md#flu0012) |
| `FLU0013`: `ToOption()` that drops an error | [Analyzer](analyzer/README.md#flu0013) |

---

Back to index: [Docs index](README.md)
