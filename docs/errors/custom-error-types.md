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
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new UnexpectedDomainException(exception);
}

public sealed class NotFoundException(string message) : DomainException(message);

public sealed class UnexpectedDomainException(Exception inner) : DomainException(inner.Message, inner);

[FluentResult<DomainException>]
public readonly partial struct Result<T>;
```

Your methods now return `Result<T>` from `Shop`. Only a `DomainException` can be an error:

```csharp
static Result<Customer> GetCustomer(Guid id) =>
    customers.TryGetValue(id, out var customer) ? customer : new NotFoundException($"No customer {id}.");
```

If the error type cannot implement `IFromException<TError>`, declare the mapping on the struct instead:

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
| Operators | Every operator of the default type: `Select`, `Chain`, `Ensure`, `Validate`, `Zip`, `BindAppend`, `Do`, `OnErrorDo`, `Finally`, `Out`, `Catch`, `Match`, `GetValueOrDefault`, `Check`, `Combine`, `CombineAll`, `Partition`, `ToOption`, `ToLoadable`, `Transpose`, `Unwrap`, `WithValue`, `DiscardValue`, query syntax, every `Task` overload and every `...Await` operator |

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
| `Companion` | `true` | Generates `Result` without a value next to `Result<T>`. Add members to it with `public readonly partial struct Result { ... }`. Set it to `false` only when you declare it yourself with `[FluentResult<DomainException>] public readonly partial struct Result;`. |
| `CaseConversions` | `true` | When the error type is a union, each case converts straight to the result. See [Errors as data](errors-as-data.md). |

## Unexpected exceptions

- An exception thrown inside a step that already is a `DomainException` is kept as it is.
- Any other exception goes through `FromException`. That includes the `ValidationError` from `Ensure(predicate, "message")` and the `ValidationErrors` from `Validate`, so `FromException` is the place to map them to your own validation type.
- An `OperationCanceledException` is never turned into an error. It propagates.
- A `default` result counts as an error: `FromException` receives an `InvalidOperationException` that says the result is empty.

`FromException` must not throw and must not return `null`. Test it with an exception that is not yours.

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
| `FLU0101` | The declaration is not a top-level `readonly partial struct` with at most one type parameter and no constraints. |
| `FLU0102` | The error type cannot represent an unexpected exception: implement `IFromException<TError>` or declare `FromException` on the struct. |
| `FLU0103` | A case of the error union is an interface and gets no conversion to the result. |
| `FLU0004` | `Result<X>` where `X` is the error type or one of its cases: a value of `X` would become an error. |

The other analyzers, such as `FLU0001` for an ignored result and `FLU0010` for an unchecked `GetValue`, treat your type like the default one.

## Integration packages

The integration packages accept your type wherever they accept the default one: ASP.NET Core maps it to HTTP responses, the assertion packages assert on it, Polly and FluentValidation work with it, Serilog and Verify write it, and every serializer refuses it like the default type. See each package's page for details.

## Migrating from a hand-written type

In 0.3.0 a custom type was a `union` that implemented `IResult<TSelf, T, TError>` by hand, with wrapper methods for `Select` and the `Task` overloads. To move to the generated type:

1. Implement `IFromException<TError>` on the error type, or keep `FromException` on the struct.
2. Replace `public union Result<T>(T, DomainException) : IResult<...> { ... }` with `[FluentResult<DomainException>] public readonly partial struct Result<T>;`.
3. Delete the wrapper class. Every operator is generated now.

A hand-written type keeps working: the operators in `ResultInterfaceExtensions` still accept any `IResult<TSelf, T, TError>`, with boxing and without the `Task` overloads.

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
