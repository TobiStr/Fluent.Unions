# Your own Result type

This page shows how to declare a `Result<T>` whose error is your own type, what the generator writes for it, and how it behaves.

## Declaring the type

Give your error type a way to represent an unexpected exception, then mark a `readonly partial struct`:

```csharp
using Fluent.Unions;
using Fluent.Unions.Abstractions;

namespace Shop;

public class DomainException(string message, Exception? inner = null)
    : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) => exception switch
    {
        DomainException domain => domain,
        ValidationError or ValidationErrors => new InvalidInputException(exception),
        _ => new UnexpectedDomainException(exception)
    };
}

public sealed class NotFoundException(string message) : DomainException(message);

public sealed class InvalidInputException(Exception validation) : DomainException("The input is invalid.", validation);

public sealed class UnexpectedDomainException(Exception inner) : DomainException("An unexpected error occurred.", inner);

[FluentResult<DomainException>]
public readonly partial struct Result<T>;
```

Your methods now return `Result<T>` from `Shop`. Only a `DomainException` can be an error:

```csharp
static Result<Customer> GetCustomer(Guid id) =>
    customers.TryGetValue(id, out var customer) ? customer : new NotFoundException($"No customer {id}.");
```

If the error type cannot implement `IFromException<TError>`, declare the mapping on the struct instead. It must be a `public` or `internal` static method with a single `Exception` parameter that returns the error type:

```csharp
[FluentResult<PaymentException>]
public readonly partial struct PaymentResult<T>
{
    public static PaymentException FromException(Exception exception) => new PaymentException("Unexpected failure.", exception);
}
```

## What the generator writes

| Part | Members |
| --- | --- |
| The union | Constructors for the value and the error, implicit conversions, `Value`, `IsSuccess`, `IsError`, `TryGetValue`, `TryGetError`, `GetValue`, `GetError`, `Deconstruct`, `Ok(value)`, `Error(error)` |
| `Result` without a value | The same members with `Ok` as the success case, plus `Result.From`, `Try`, `TryAsync`, `WhenAll`, `Retry`, `RetryAsync`, `CombineAll` and `CombineAllAsync` |
| Operators | Every operator of the default type: `Select`, `Chain`, `Ensure`, `Validate`, `Zip`, `ChainAppend`, `Do`, `OnErrorDo`, `Tap`, `TapError`, `TapAwait`, `Finally`, `Out`, `Catch`, `MapError`, `Match`, `GetValueOrDefault`, `Check`, `Combine`, `CombineAll`, `Partition`, `ToOption`, `ToLoadable`, `Transpose`, `Unwrap`, `WithValue`, `DiscardValue`, query syntax, every `Task` overload and every `...Await` operator |

Type inference works as with the default type, so no operator needs explicit type arguments:

```csharp
Result<string> label = await GetCustomerAsync(id)
    .Select(customer => customer.Name)
    .Ensure(name => name.Length > 0, "A customer needs a name.")
    .ChainAwait(LoadLabelAsync);
```

The operators are one-line forwarders to the library. Behaviour lives in one place, so your type and the default type cannot drift apart.

### Options

| Property | Default | Effect |
| --- | --- | --- |
| `CaseConversions` | `true` | When the error type is a union, each case converts straight to the result. See [Errors as data](errors-as-data.md). |
| `Capture` | `ExceptionCapture.All` | Which exceptions thrown inside an operator become an error. See [Capture policy](#capture-policy). |
| `Propagate` | none | Exception types that operators never capture. See [Capture policy](#capture-policy). |

### The value-less companion

The generator writes `Result` without a value next to `Result<T>`. To add members to it, declare it yourself with the same attribute and the same error type:

```csharp
[FluentResult<DomainException>]
public readonly partial struct Result<T>;

[FluentResult<DomainException>]
public readonly partial struct Result
{
    public static Result NotImplemented => Error(new DomainException("Not implemented."));
}
```

A `[FluentResult]` struct without a type parameter needs the generic one next to it, with the same name, namespace and error type. Without it, `FLU0101` is reported.

### Names of the generated classes

The operators of `Result<T>` are extension methods in static classes named `{TypeName}{Group}Operators`, such as `ResultOperators`, `ResultQueryOperators` and `ResultTaskOperators`. They sit in the namespace of your type. A class of your own with another name never collides with them.

### What ToString returns

| Case | Text |
| --- | --- |
| A value | `value.ToString()`, or an empty string for a value that prints `null` |
| An error that is an exception | `TypeName: Message`, without stack trace and without inner exceptions |
| An error that is not an exception | `error.ToString()` |
| `Ok` of the result without a value | `Ok` |
| A `default` result | The text of the error that `FromException` makes of the "holds neither a value nor an error" exception |

The text is safe to log or to return in a response. An error union with an exception case prints that exception.

## Unexpected exceptions

- An exception thrown inside a step that already is a `DomainException` is kept as it is.
- Any other exception goes through `FromException`. That includes the `ValidationError` from `Ensure(predicate, "message")` and the `ValidationErrors` from `Validate`, so `FromException` is the place to map them to your own validation type.
- An `OperationCanceledException` is never turned into an error. It propagates.
- With a [capture policy](#capture-policy), exceptions that the policy excludes propagate as well.
- A `default` result counts as an error: `FromException` receives an `InvalidOperationException` that says the result is empty. Native pattern matching sees no case, as with the default type; see [The default result](../result/core-concepts.md#the-default-result).

`FromException` must not throw and must not return `null`. Test it with an exception that is not yours. When it returns `null`, the operator throws an `InvalidOperationException` with the original exception as `InnerException`. When it throws, the operator throws an `InvalidOperationException` whose `InnerException` is an `AggregateException` of the original exception and the failure of `FromException`. Analyzer [FLU0009](../analyzer/README.md#flu0009) reports a `FromException` that throws or returns `null`.

## Capture policy

By default every exception thrown inside an operator becomes an error. That keeps a chain from throwing, but a bug such as a `NullReferenceException` then looks like any other failure. Two options on the attribute let such exceptions escape instead:

```csharp
[FluentResult<DomainException>(Capture = ExceptionCapture.Declared)]
public readonly partial struct Result<T>;

[FluentResult<DomainException>(Propagate = [typeof(NullReferenceException), typeof(ArgumentException)])]
public readonly partial struct Result<T>;
```

| Setting | Captured | Propagates |
| --- | --- | --- |
| default | every exception | `OperationCanceledException` |
| `Capture = ExceptionCapture.Declared` | the error type and its subtypes, and each case of an error union that is an exception | everything else |
| `Propagate = [...]` | every other exception | the listed types and their subtypes |

The two combine: `Declared` with `Propagate` captures declared exceptions that are not listed. An error union whose cases are all data, such as records, declares no exception, so `Declared` captures nothing and every thrown exception escapes.

An exception that is not captured is never caught. It propagates from the operator with its original stack trace, and an asynchronous operator returns a faulted task. Errors that the library creates itself, such as the `ValidationError` from `Ensure(predicate, "message")` or the error of a `default` result, are not thrown and still go through `FromException`.

Use `Declared` when your error type lists every failure you expect, so that anything else is a bug that should crash loudly. Use `Propagate` when you capture almost everything but want certain programming errors to escape.

The setting applies to every operator, to `Try`, `TryAsync`, `Retry` and `RetryAsync`, and to the MediatR, Mediator, Polly and FluentValidation integrations. A declared companion without a value must use the same settings as `Result<T>`, otherwise `FLU0101` is reported. `Declared` on a type whose error type is `Exception` captures everything anyway and gives warning [FLU0104](../analyzer/README.md#flu0104). A type in `Propagate` that is not an exception gives error [FLU0105](../analyzer/README.md#flu0105).

Give `UnexpectedDomainException` a fixed message and keep the original as `InnerException`. Responses and logs often show the message of the error, and the message of an unexpected exception can hold connection strings or other internals. The ASP.NET Core integration also recognizes a `ValidationError` or `ValidationErrors` as the inner exception, as in `InvalidInputException`, and still writes a validation problem.

## Namespaces

Declare the type in your root namespace, such as `Shop`. C# looks for a type in the enclosing namespaces before it looks at `using` directives, so code in `Shop` and in every namespace below it uses your `Result<T>`, even with `using Fluent.Unions;`.

A file in another root namespace that imports both `Shop` and `Fluent.Unions` sees two `Result<T>` types and reports CS0104. Write `Shop.Result<T>` there, or keep `Fluent.Unions` out of that file.

## Performance

Generated types store the value and the error in fields instead of an `object`, so value types are not boxed. The operators take the struct directly and catch exceptions without closures:

| Operator call | Hand-written type on `IResult` (0.3.0) | Generated type |
| --- | ---: | ---: |
| Create a `Result<int>` | 24 B | 0 B |
| `Chain` | 56 B | 0 B |
| `Select` on an `int` | 80 B | 0 B |
| `Ensure` | 152 B | 0 B |
| `Match` | 24 B | 0 B |

The default `Result<T>` uses the same layout and operators. See [`bench/results`](https://github.com/TobiStr/Fluent.Unions/tree/main/bench/results) in the repo.

## Diagnostics

| Rule | Meaning |
| --- | --- |
| `FLU0101` | The declaration is not supported. The message names the reason: not a `readonly partial struct`, a record struct, a ref struct, a `file` type, a nested type, more than one type parameter, a constrained type parameter, an error type that is less accessible than the result, a name that the generator uses for a member, or a mismatch between `Result<T>` and its value-less companion. |
| `FLU0102` | The error type cannot represent an unexpected exception: implement `IFromException<TError>` for the error type itself, or declare an accessible `public static TError FromException(Exception exception)` on the struct. |
| `FLU0103` | A case of the error union gets no conversion to the result. The message names the reason: an interface, `object` or `ValueType`, or a case that is less accessible than the result. |
| `FLU0004` | `Result<X>` where `X` is the error type or one of its cases: a value of `X` would become an error. |

The other analyzers, such as `FLU0001` for an ignored result and `FLU0010` for an unchecked `GetValue`, treat your type like the default one.

## Integration packages

The integration packages accept your type wherever they accept the default one: ASP.NET Core maps it to HTTP responses, the assertion packages assert on it, Polly and FluentValidation work with it, Serilog and Verify write it, and every serializer refuses it like the default type. See each package's page for details.

## Migrating from 0.3.0

0.3.0 had two ways to get your own error type. Both are gone, and `[FluentResult<TError>]` replaces them. Do not implement `IResult<TSelf, T, TError>` by hand: declared types implement it, and the integration packages accept any type that does.

From a hand-written `union` that implemented `IResult<TSelf, T, TError>`, with wrapper methods for `Select` and the `Task` overloads:

1. Implement `IFromException<TError>` on the error type, or keep `FromException` on the struct.
2. Replace `public union Result<T>(T, DomainException) : IResult<...> { ... }` with `[FluentResult<DomainException>] public readonly partial struct Result<T>;`.
3. Delete the wrapper class. Every operator is generated now.

From `Result<T, TError>`:

1. Declare a type for the error: `[FluentResult<CheckoutError>] public readonly partial struct Outcome<T>;`.
2. Replace `Result<Order, CheckoutError>` with `Outcome<Order>`. The `(CheckoutError)` casts on errors are no longer needed.
3. `MapError` now names the target type: `outcome.MapError<ClientResult<Order>, ClientError>(...)`, see [Errors as data](errors-as-data.md#translating-errors-between-layers).
4. Replace `Catch<TCase>` on a record case with the general `Catch`. The typed `Catch<TCase>` takes exception cases only.

The generated type does not box and has the `Task` overloads.

## Pitfalls

- A `FromException` that returns `null` or throws breaks the rule that chains do not throw.
- Mixing your `Result<T>` and `Fluent.Unions.Result<T>` in one file outside your namespace needs the namespace written out.
- `Result<DomainException>` is reported by `FLU0004`: a `DomainException` value could not be told apart from an error.

## See also

- [Errors as data](errors-as-data.md): a union as the error type, checked by an exhaustive `switch`.
- [Error mapping](../aspnetcore/error-mapping.md): map your error types to HTTP status codes.
- `test/Fluent.Unions.Tests/OwnResult` in the repo: a generated type with tests for every operator family.

---

Back to: [Errors](README.md) · Next: [Errors as data](errors-as-data.md)
