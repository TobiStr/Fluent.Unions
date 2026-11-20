# Custom error types

This page shows how to declare your own result type whose errors are limited to a base exception, and how the library's operators work on it.

## Declaring the type

Declare a union with a value case and an error case, and implement `IResult<TSelf, T, TError>` from `Fluent.Unions.Abstractions`:

```csharp
using Fluent.Unions.Abstractions;

namespace Shop.Domain;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner);

public class NotFoundException(string message) : DomainException(message);

public class UnexpectedDomainException(Exception inner) : DomainException(inner.Message, inner);

public union Result<T>(T, DomainException) : IResult<Result<T>, T, DomainException>
{
    public static Result<T> FromValue(T value) => value;
    public static Result<T> FromError(DomainException error) => error;
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new UnexpectedDomainException(exception);
}
```

Your methods now return `Result<T>` from `Shop.Domain`. Only a `DomainException` can be an error, and the compiler enforces it:

```csharp
using Shop.Domain;

static Result<Customer> GetCustomer(Guid id) =>
    id == Guid.Empty ? new NotFoundException($"No customer {id}.") : new Customer(id, "Ada");
```

### The three static members

| Member | Contract |
| --- | --- |
| `FromValue(value)` | Creates the ok result. Operators that produce a value, such as `Select`, call it. |
| `FromError(error)` | Creates the failed result. |
| `FromException(exception)` | Turns an exception thrown inside an operator into your error type. |

`FromException` is the member with a trap. An operator keeps an exception that already is a `DomainException` as it is. Every other exception goes through the hook. The hook:

- **must not return `null`.** The operator then throws `InvalidOperationException` with the original exception as `InnerException`.
- **must not throw.** The exception from the hook escapes the operator, so the chain throws, which defeats the point.

Wrapping the original exception, as `UnexpectedDomainException` does, keeps the cause and its stack trace.

## Operators

`using Fluent.Unions;` brings in operators that work on any `IResult` implementation and return your type.

| Operator | Notes |
| --- | --- |
| `Chain(next)`, `ChainAwait(next)` | `next` must return a result with the same error type. |
| `Catch(e => ...)`, `Catch((NotFoundException e) => ...)`, `Catch(fallback)` | The typed form needs the exception type on the lambda parameter. |
| `CatchAwait(e => ...)` | `e` is your error type and the handler returns a `Task` of your result. |
| `Do`, `DoAwait`, `OnErrorDo`, `Finally`, `FinallyAwait` | Same behavior as on the library's `Result<T>`. |
| `TryGetError(out error)` | Gets your error type, if the result holds one. A `default` result holds the error `FromException` makes of an `InvalidOperationException`. |
| `Out(out var value)` | Captures the value for later steps. Filled only when the result holds a value. |
| `Ensure(predicate, e => ...)`, `Ensure(predicate, message)` | The first form returns your error type. The message form makes a `ValidationError` and passes it through `FromException`. |
| `Validate(combine, rules...)` | Runs every rule and collects all errors. `combine` turns the list of errors into one error of your type. |
| `GetValueOrDefault()`, `GetValueOrDefault(fallback)` | The value, or `default` or the fallback for an error. |
| `Check()` | Rethrows the error with its stack trace. An error that is not an exception is thrown inside a `ResultErrorException`. |
| `Match(onSuccess, onError)` | `onError` receives your error type. |
| `ToOption()`, `ToLoadable()` | An error gives no value or a failed state. A failed `ToLoadable` wraps an error that is not an exception in `ResultErrorException`. |
| `Unwrap()` | Flattens a result of a result with the same error type. |
| `activity.RecordResult(result)` | Sets the status and records the error on an `Activity`. |
| `Select`, `SelectAwait`, `MapError`, `Zip`, `BindAppend`, `WithValue`, `Transpose` | Change the result type, so they need explicit type arguments. See below. |

### Chain

`Chain` infers your result type from what `next` returns. A method group works as is:

```csharp
Result<Order> order = GetCustomer(id).Chain(GetCart).Chain(PlaceOrder);
```

A lambda that returns a plain value has no result type to infer. Give it a return type or cast the value:

```csharp
Result<int> length = GetCustomer(id).Chain(Result<int> (c) => c.Name.Length);
Result<int> sameLength = GetCustomer(id).Chain(c => (Result<int>)c.Name.Length);
```

An error passes through without calling `next`. An exception thrown in `next` becomes an error through `FromException`.

### Catch

One operator recovers, translates and falls back:

```csharp
var missing = GetCustomer(Guid.Empty);

Result<Customer> recovered = missing.Catch(e => new Customer(Guid.Empty, "Guest"));
Result<Customer> translated = missing.Catch(e => new DomainException("Lookup failed", e));
Result<Customer> typed = missing.Catch((NotFoundException e) => new Customer(Guid.Empty, "Guest"));
```

The typed form handles only that type. Other errors pass through unchanged.

## Writing the wrappers

C# cannot express "the same kind of result with another type argument", so `Select` and `MapError` cannot infer your type. They exist with explicit type arguments. Wrap them once, one line each:

```csharp
public static class ResultExtensions
{
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector) =>
        result.Select<Result<T>, T, DomainException, Result<TOut>, TOut>(selector);

    public static Result<T> ToDomain<T>(this Fluent.Unions.Result<T> result) =>
        result.MapError<Fluent.Unions.Result<T>, T, Exception, Result<T>, DomainException>(Result<T>.FromException);

    public static async Task<Result<TOut>> Chain<T, TOut>(this Task<Result<T>> task, Func<T, Result<TOut>> next) =>
        (await task).Chain(next);
}
```

What each wrapper gives you:

- **`Select`** projects the value. It also enables query syntax with a single `from`. A projection that returns `null` gives an error.

  ```csharp
  var length = GetCustomer(id).Select(c => c.Name.Length);                // 3
  var query = from c in GetCustomer(id) select c.Name.ToUpperInvariant(); // "ADA"
  ```

- **`ToDomain`** turns a library result into yours. It is how you start from code that throws. A non-domain exception becomes `UnexpectedDomainException` through `FromException`. A mapping that returns `null` gives an error.

  ```csharp
  var fromThrowing = Fluent.Unions.Result.Try(() => LoadFromDatabase(id)).ToDomain();
  ```

- **`SelectAwait`, `Zip`, `BindAppend`, `WithValue` and `Transpose`** change the result type as well, so they also need explicit type arguments. Wrap them the same way, or spell the type arguments out at the call site:

  ```csharp
  Result<int> score = await GetCustomer(id)
      .SelectAwait<Result<Customer>, Customer, DomainException, Result<int>, int>(LoadScoreAsync);

  Result<(Customer, Cart)> both = GetCustomer(id)
      .BindAppend<Result<Customer>, Customer, DomainException, Result<Cart>, Cart, Result<(Customer, Cart)>>(GetCart);
  ```

  The type arguments are the receiver type, its value type, your error type, then the types of the other result and of the result you get back.

### Operators that keep your type

`Ensure`, `Validate`, `Out`, `DoAwait`, `CatchAwait`, `FinallyAwait`, `WhereAwait`, `ToOption`, `ToLoadable`, `Unwrap` and `RecordResult` infer everything from the receiver and need no wrapper:

```csharp
Result<Customer> checkedCustomer = GetCustomer(id)
    .Ensure(c => c.Name.Length > 0, "A customer needs a name.")
    .Ensure(c => c.Active, c => new DomainException($"{c.Name} is inactive."))
    .Validate(
        errors => new DomainException(string.Join("; ", errors.Select(e => e.Message))),
        CheckAddress,
        CheckEmail);
```

`Validate` runs every rule, so the caller decides how errors merge. A rule can return any result type that has your error type, such as `Result<Address>`. A rule that throws counts as a failure. Give a rule a typed return, for example through a method group, because the compiler infers the rule's result type from it.

- **The `Task` wrapper** keeps an asynchronous pipeline one expression:

  ```csharp
  Result<Order> order = await GetCustomerAsync(id).Chain(GetCart).Chain(PlaceOrder);
  ```

Without the wrapper, await first: `(await GetCustomerAsync(id)).Chain(GetCart)`. `ChainAwait` takes an asynchronous function and is called on the result, not on the task:

```csharp
Result<Cart> cart = await GetCustomer(id).ChainAwait(LoadCartAsync);
```

## Extension packages

The extension packages also accept a custom type that implements `IResult<TSelf, T, TError>`, including `Result<T, TError>`. The concrete overloads for `Result<T>` keep binding first.

| Package | What works on your type | Details |
| --- | --- | --- |
| `Fluent.Unions.AwesomeAssertions` | `Should()` with `BeOk`, `BeOkWithValue`, `BeOkMatching`, `BeError`, and `BeError<TCase>` | [AwesomeAssertions](../awesome-assertions/README.md#custom-result-types) |
| `Fluent.Unions.FluentValidation` | `Validate(validator)` and `Validate(validator, map)`, with async forms | [FluentValidation](../fluent-validation/README.md#custom-result-types) |
| `Fluent.Unions.AspNetCore` | `ToHttpResult(onError)`, and `WithResultMapping()` for handlers that return your type | [HTTP results](../aspnetcore/http-results.md#custom-result-types), [Endpoint filter](../aspnetcore/endpoint-filter.md#custom-result-types) |
| `Fluent.Unions.Polly` | `HandleError`, `ExecuteResultAsync` and `ToResult`, with explicit type arguments | [Polly](../polly/README.md#custom-result-types) |

## Namespaces

The operators live in `Fluent.Unions`, next to the library's own `Result<T>`. Code inside your own namespace finds your `Result<T>` first, so `using Fluent.Unions;` is safe there:

```csharp
using Fluent.Unions;

namespace Shop.Domain;

static Result<int> F() => 1; // Shop.Domain.Result<int>
```

In another namespace that imports both, the compiler stops with CS0104 (ambiguous reference):

```csharp
using Fluent.Unions;
using Shop.Domain;

namespace Shop.Api;

static Result<int> F() => 1; // error CS0104: 'Result<>' is an ambiguous reference
```

Give your type a distinct name, such as `DomainResult<T>`, to avoid this altogether. Inside your own namespace, refer to the library's type by its full name, `Fluent.Unions.Result<T>`, as `ToDomain` does.

## Allocations

The operators take your result through the interface, which boxes it. The repo README puts this at 24 bytes per operator call in a Release build, on top of what the operator allocates anyway. The library's own `Result<T>` is unaffected and still binds to its concrete operators.

## Limitations

These are not provided for custom types:

- `Combine`, `CombineAll`, `Partition` and `Try`.
- The factory overload of `GetValueOrDefault`.
- Query syntax with several `from` clauses, so `SelectMany` and `SelectManyAwait`. `Chain` and `ChainAwait` cover that form.
- `DiscardValue`, the `Ensure*` rule catalogue, and the remaining `...Await` operators such as `EnsureAwait`.
- `Task` overloads of every operator, unless you write the wrapper above.

## Pitfalls

- A `FromException` that returns `null` or throws breaks the rule that chains do not throw. Test it with an exception that is not yours.
- `Result<T>` in your namespace and in `Fluent.Unions` collide as soon as a file imports both.
- A `Chain` lambda that returns a plain value does not compile without a cast or a declared return type.

## See also

- [Errors as data](errors-as-data.md): when the error is not an exception at all.
- [Error mapping](../aspnetcore/error-mapping.md): map a `DomainException` hierarchy to HTTP status codes.
- [Endpoint filter](../aspnetcore/endpoint-filter.md#custom-result-types): return your result type straight from a handler.
- `test/Fluent.Unions.Tests/CustomErrors` in the repo: a complete custom type with tests for every operator.

---

Back to: [Errors](README.md) · Next: [Errors as data](errors-as-data.md)
