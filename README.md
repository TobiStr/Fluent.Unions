# Result.Net with Union Types

A small Result type for .NET, rebuilt on the C# 15 `union` keyword. A method returns either its value or an `Exception`, and the compiler checks that callers handle both.

Use it when you want to mildly enforce that callers look at the outcome of an operation, and when you want error handling that does not depend on `null` checks.

## Requirements

- .NET 11 SDK
- C# 15 (the default language version for `net11.0`, no `LangVersion` setting needed)

The library targets `net11.0` only. Unions do not parse at C# 14 or lower, so there is no multi-targeting.

## Quick start

Reference `src/ResultType/ResultType.csproj`, or copy `Result.cs` and the `Extensions` folder into your project. There is no NuGet package.

Return the value or the exception directly. The union provides the implicit conversions.

```csharp
using ResultType;
using ResultType.Extensions;

static Result<Person> GetPerson(Guid id)
{
    try
    {
        return database.GetPersonById(id);
    }
    catch (Exception ex)
    {
        return ex;
    }
}

static Result Save(Person person)
{
    try
    {
        return Result.Ok;
    }
    catch (Exception ex)
    {
        return ex;
    }
}
```

`Result` carries either `Ok` or an `Exception`. `Result<T>` carries either a `T` or an `Exception`.

Members:

| Member | Behavior |
| --- | --- |
| `Success` | `true` only when the result holds `Ok` / a `T`. |
| `Message` | The exception message, or `""` when not failed. |
| `GetOk()` (`Result<T>`) | The payload. Throws `InvalidOperationException` on failure or when no payload is set. |
| `GetError()` | The exception. Throws `InvalidOperationException` when the result is not a failure. |
| `Result.Ok` | A successful `Result`. A property. |
| `Result<T>.Ok(payload)` | A successful `Result<T>`. A method. Throws `ArgumentNullException` for `null`. |
| `Error(exception)` | A failed result. |
| `Result.Try(...)` / `Result.TryAsync(...)` | Runs code that may throw and returns a result. See below. |

## Pattern matching

Matching is the main way to consume a result. Both cases are visible, so the compiler can check you handled them.

```csharp
static string Describe(Result<Person> result) => result switch
{
    Person person => $"{person.Name} ({person.Age})",
    Exception error => error.Message
};

static string DescribeSave(Result result) => result switch
{
    Ok => "saved",
    Exception error => error.Message
};
```

Omitting an arm produces warning CS8509 ("not exhaustive"). It is only a warning by default. To make it break the build, add this to your project file:

```xml
<PropertyGroup>
  <WarningsAsErrors>CS8509</WarningsAsErrors>
</PropertyGroup>
```

The test project does this.

## Check()

`Check()` returns the result unchanged when it succeeded. Otherwise it throws the underlying exception.

It rethrows with `ExceptionDispatchInfo`, so the stack trace of the original throw is preserved, and each rethrowing layer is appended. `StackTraceTests` in `test/ResultType.Tests` shows this.

```csharp
static Result Copy(Guid id)
{
    try
    {
        var person = GetPerson(id).Check();
        return Save(person.GetOk()).Check();
    }
    catch (Exception ex)
    {
        return ex;
    }
}
```

Calling `Check()` on a `default` result throws `InvalidOperationException` (see below).

## Try and TryAsync

`Result.Try` runs code that may throw and turns the outcome into a result. It is the opposite of `Check()`, and the usual way into a chain from code that throws:

```csharp
Result<int> number = Result.Try(() => int.Parse(input));
Result saved = Result.Try(() => File.WriteAllText(path, text));
Result<Person> person = await Result.TryAsync(() => LoadPersonAsync(id));

Result<Order> order = Result.Try(() => LoadCustomer(id))
    .Chain(GetCart)
    .Chain(PlaceOrder);
```

| Body | `Try` returns | `TryAsync` body |
| --- | --- | --- |
| returns a `T` | `Result<T>` | `Task<T>` |
| returns a `Result<T>` | that `Result<T>`, not nested | `Task<Result<T>>` |
| returns nothing | `Result` (`Ok`) | `Task` |
| returns a `Result` | that `Result` | `Task<Result>` |

A thrown exception becomes the error, and a `null` value gives an empty result, the same as inside any operator. `TryAsync` also catches an exception the body throws before it returns its task.

## Converting Result&lt;T&gt; to Result

`Result<T>` is no longer a subclass of `Result`. An implicit conversion keeps the success state and the exception, and drops the payload.

```csharp
Result<Person> typed = GetPerson(id);
Result plain = typed;
```

## Fluent chaining

`using ResultType.Extensions;` adds operators named after LINQ and Rx.NET, so a whole flow reads as one expression:

```csharp
Result<string> label = GetPerson(id)
    .Where(person => person.Age >= 18)
    .Do(person => logger.LogInformation("Found {Name}", person.Name))
    .Select(person => person.Name.ToUpperInvariant())
    .Catch((KeyNotFoundException _) => "UNKNOWN");
```

### Three states

A result is **ok** (holds a value or `Ok`), **error** (holds an exception) or **empty** (`default`, holds nothing). Each operator runs in some states and passes the result through untouched in the others:

| Operators | ok | error | empty |
| --- | --- | --- | --- |
| `Select`, `Chain`, `SelectMany`, `Where`, `Zip`, `Do(onNext)` | runs | passes through | passes through |
| `Catch`, `Do(onNext, onError)`, `OnErrorDo` | passes through | runs | passes through |
| `Finally` | runs | runs | runs |
| `DefaultIfEmpty` | passes through | passes through | becomes ok |

Rules that hold for every operator:

- **Chains do not throw.** An exception thrown inside a lambda becomes the result's error, as in Rx. Only `Check()` throws on purpose. A `null` lambda argument still throws `ArgumentNullException`. Operators that leave the chain, `GetValueOrDefault(() => ...)` and `Match`, let exceptions from their callbacks propagate.
- **`null` means empty.** A selector that returns `null` gives an empty result, the same as an implicit `null` payload.
- **`Where` filters into empty.** A value that fails the predicate leaves the result empty, the closest thing to LINQ's "no element". Follow it with `DefaultIfEmpty(fallback)` to supply a value instead.

### Operators

**On the value**

| Operator | What it does |
| --- | --- |
| `Select(v => ...)` | Projects the value. On `Result` it takes `() => ...` and produces a `Result<T>`. |
| `Chain(v => ...)` | Chains a call that returns `Result<T>` or `Result`. |
| `SelectMany(v => ...)` | The LINQ name for `Chain`. It exists so query syntax works. |
| `Where(v => ...)` | Keeps the value when the predicate holds, otherwise the result becomes empty. |
| `Zip(other)` / `Zip(other, (a, b) => ...)` | Combines two results. The first error wins, then an empty side makes the result empty. |

`Chain` links result-returning functions (bind or flatMap in other libraries). Pass a method group, and the next call runs only if the previous one succeeded:

```csharp
Result<Order> order = GetCustomer(id)
    .Chain(GetCart)
    .Chain(PlaceOrder);
```

**On the error**

| Operator | What it does |
| --- | --- |
| `Catch(e => ...)` | Handles the error. Return a value to recover, or an exception to replace the error. |
| `Catch((IOException e) => ...)` | Handles only that exception type. Other errors pass through. |
| `Catch(fallback)` | Replaces any error with another result. |

Because the union converts both cases implicitly, recovering and translating an error are the same operator:

```csharp
result.Catch(e => "fallback");                             // recover with a value
result.Catch(e => new DomainException("Lookup failed", e)); // translate the error
```

**Side effects**

| Operator | What it does |
| --- | --- |
| `Do(v => ...)` | Runs an action with the value and returns the same result. |
| `Do(v => ..., e => ...)` | Runs one action for the value or another for the error. |
| `Out(out var value)` | Captures the value for later steps of the chain. See "Using values from earlier steps". |
| `OnErrorDo(e => ...)` | Runs an action with the error and returns the same result. |
| `Finally(() => ...)` | Runs an action in every state. |

**Leaving the chain**

| Operator | What it does |
| --- | --- |
| `GetValueOrDefault()` | Returns the value, or `default` for an error or empty result. |
| `GetValueOrDefault(fallback)` | Returns the value, or `fallback`. |
| `GetValueOrDefault(() => ...)` | Returns the value, or the factory's result. The factory runs only when needed. |
| `DefaultIfEmpty(fallback)` / `DefaultIfEmpty(() => ...)` | Turns an empty result into an ok one. It stays a result, and a throwing factory gives an error. |
| `Match(v => ..., e => ..., () => ...)` | Returns a value with one callback per state: ok, error and empty. |
| `Unwrap()` | Flattens `Result<Result<T>>`, or `Result<Result>` from `Select(x => Save(x))`. Like `Task.Unwrap()`. |

`Match` ends a pipeline, including an asynchronous one, without a `switch`:

```csharp
IResult response = await LoadPersonAsync(id).Match(
    person => Results.Ok(person),
    error => Results.Problem(error.Message),
    () => Results.NotFound());
```

It takes a callback for the empty state too, so a value filtered out by `Where` cannot slip through as an exception.

**Sequences** (`IEnumerable<Result<T>>`)

| Operator | What it does |
| --- | --- |
| `Combine()` | Returns `Result<IReadOnlyList<T>>` with all values. The first error wins, and empty results are skipped. |
| `CombineAll()` | Like `Combine()`, but collects every error in an `AggregateException`. |
| `Partition()` | Returns `(Values, Errors)` in one pass. Empty results are skipped. |
| `Values()` | Returns the values of the ok results. |
| `Errors()` | Returns the exceptions of the error results. |

`Combine()` is deliberately not called `ToList()`: overload resolution would prefer it over `Enumerable.ToList()` and silently change what existing code returns.

### Validation

`Combine` stops at the first error. To report every problem at once, as input validation usually should, use `Validate` for one value or `CombineAll` for many:

```csharp
Result<Order> order = ParseOrder(dto).Validate(
    o => o.Quantity > 0 ? Result.Ok : new ValidationException("Quantity must be positive."),
    o => o.Email.Contains('@') ? Result.Ok : new ValidationException("Email is invalid."));
```

Every rule runs. If any fail, the error is an `AggregateException` holding all failures, even when only one rule failed, so you always catch the same type: `Catch((AggregateException e) => ...)`. A rule that throws or returns an empty result counts as a failure, nested `AggregateException`s are flattened, and errors and empty results pass through without running the rules. `Validate` works on `Task<Result<T>>` too.

### Query syntax

`Select`, `SelectMany` and `Where` follow the LINQ query pattern, so query expressions work:

```csharp
Result<int> sum =
    from a in Parse("20")
    from b in Parse("22")
    where a < b
    select a + b;
```

### Using values from earlier steps

A fluent chain hands each step only the previous value. When a later step needs an earlier one, take it out of the chain with `Out(out var ...)`:

```csharp
Result<string> order = GetCustomer(id).Out(out var customer)
    .Chain(GetCart).Out(out var cart)
    .Chain(_ => Submit(customer, cart))
    .Select(confirmation => $"{confirmation} for {customer.Value.Name}");
```

`customer` is a `Captured<Customer>`, which fills when the chain reaches that step with a value. Later steps only run after a success, so inside the chain it always holds a value.

- **As an argument**, it converts implicitly to the value type: `Submit(customer, cart)`.
- **For members**, use `.Value`: `customer.Value.Name`. Interfaces and `object` need `.Value` as an argument too, because C# has no implicit conversion to them.
- **Outside the chain**, check `HasValue` first. After a failed or empty chain, `.Value` throws `InvalidOperationException` instead of handing you `null`.

It works the same in asynchronous chains, without awaiting each step first. The holder is assigned immediately and filled when the task completes, before any later step runs:

```csharp
Result<string> order = await GetCustomerAsync(id).Out(out var customer)
    .ChainAwait(GetCartAsync).Out(out var cart)
    .ChainAwait(_ => SubmitAsync(customer, cart));
```

In statement code, a result also deconstructs Go style, which allows early returns:

```csharp
var (customer, error) = GetCustomer(id);
if (error is not null) return error;
if (customer is null) return default; // the result was empty
```

On an error, the value is `default`, so `0` for an `int`. Always check `error` first.

`Out` exists on `Result<T>`, `Option<T>` and their `Task` versions. For purely sequential steps, query syntax keeps every earlier value in scope without a holder, also across tasks:

```csharp
Result<string> label =
    from customer in GetCustomer(id)
    from cart in GetCart(customer)
    let total = cart.Items * 10
    from confirmation in Submit(customer, cart)
    select $"{confirmation} for {customer.Name}, total {total}";
```

### Async

Every operator also works on `Task<Result<T>>` and `Task<Result>`, so a pipeline stays one expression after the first asynchronous call. A faulted task becomes an error result.

```csharp
Result<string> name = await GetPersonAsync(id)
    .Where(person => person.Age >= 18)
    .Select(person => person.Name)
    .Catch(e => "unknown");
```

For asynchronous lambdas, use the `...Await` variants, named after System.Linq.Async: `SelectAwait`, `ChainAwait`, `SelectManyAwait`, `WhereAwait`, `DoAwait`, `CatchAwait` and `FinallyAwait`.

```csharp
Result<Address> address = await GetPersonAsync(id)
    .ChainAwait(person => GetAddressAsync(person.AddressId))
    .DoAwait(address => cache.StoreAsync(address));
```

Query syntax works across tasks too:

```csharp
Result<string> label = await (
    from person in GetPersonAsync(id)
    from address in GetAddressAsync(person.AddressId)
    select $"{person.Name}, {address.City}");
```

`Check()` has a `Task` overload as well: `await GetPersonAsync(id).Check()`.

The operators take no `CancellationToken`, because they do no I/O themselves. Capture the token in your lambdas. A cancelled operation becomes an error result holding the `OperationCanceledException`.

More helpers for asynchronous code:

| Helper | What it does |
| --- | --- |
| `await Result.WhenAll(tasks)` | Like `Task.WhenAll`, then `Combine()`: all values, or the first error in sequence order. A faulted task counts as an error. Also for `Task<Result>`. |
| `Result.RetryAsync(ct => CallAsync(ct), maxAttempts: 3, delay: TimeSpan.FromSeconds(1))` | Runs the operation again while it fails, as Rx's `Retry` does. `shouldRetry` limits which errors are retried, and cancellation stops retrying. `Result.Retry` is the synchronous version. |
| `task.Timeout(TimeSpan.FromSeconds(5))` | Gives an error holding a `TimeoutException` when the task takes longer. The operation keeps running, so also pass it a cancellation token. |
| `stream.Values()` / `stream.Errors()` | Streams the values or errors of an `IAsyncEnumerable<Result<T>>`. `Values()` works on `IAsyncEnumerable<Option<T>>` too. |
| `await stream.CombineAsync()` | Collects an `IAsyncEnumerable<Result<T>>` like `Combine()`. An exception thrown by the stream becomes the error. |

For all errors instead of the first one, use `(await Task.WhenAll(tasks)).CombineAll()`. A faulted task throws there, though, as `Task.WhenAll` always does.

## Option

`Option<T>` is for a value that may be missing, where the absence is normal and not an error: a lookup that finds nothing, an optional setting. It holds a `T` or `None`.

```csharp
static Option<Person> FindPerson(Guid id) => people.TryGetValue(id, out var person) ? person : Option.None;

string? nickname = person.Nickname;
Option<string> maybeNickname = nickname.ToOption();
Option<int> firstAdult = ages.FirstOrNone(age => age >= 18);
```

| Member | Behavior |
| --- | --- |
| `HasValue` | `true` when the option holds a value. `0`, `false` and empty strings are values. |
| `TryGetValue(out value)` | Returns `true` and the value, or `false`. |
| `GetValue()` | The value. Throws `InvalidOperationException` when there is none. |
| `Option.None` | No value. Converts to any `Option<T>`. |
| `Option.Some(value)` | A value. Throws `ArgumentNullException` for `null`. |
| `value.ToOption()` | From a nullable reference or a nullable value type. `null` gives `None`. |
| `option.ToNullable()` | Back to a nullable value type, for `Option<int>` and other structs. |
| `Option.TryParse<int>("42")` | Parses any `IParsable<T>` (or `ISpanParsable<T>` from a span), `None` when parsing fails. |
| `dictionary.GetValueOrNone(key)` | Looks up an `IReadOnlyDictionary<TKey, TValue>`, `None` for a missing key. |
| `source.SingleOrNone()` / `source.ElementAtOrNone(i)` | Like `SingleOrDefault` and `ElementAtOrDefault`. `SingleOrNone` still throws for several elements, as LINQ does. |

### Matching an option

```csharp
static string Describe(Option<Person> option) => option switch
{
    Person person => person.Name,
    None or null => "nobody"
};
```

Leaving out the second arm is error CS8509 when you treat it as an error, as the test project does. Write `None or null`, or `_`, rather than just `None`. Every option the library returns holds a real `None`. A `default` option, such as an uninitialized field, holds nothing, though, and a `None` arm alone does not match it. The compiler does not warn about that, and the switch throws at runtime.

### Operators on options

The operators mirror the ones on results, named after LINQ and Rx.NET:

| Operator | What it does |
| --- | --- |
| `Select(v => ...)` | Projects the value. A `null` projection gives `None`. |
| `Chain(v => ...)` / `SelectMany(v => ...)` | Chains a call that returns an option. |
| `Where(v => ...)` | Keeps the value when the predicate holds, otherwise gives `None`. |
| `Zip(other)` / `Zip(other, (a, b) => ...)` | Combines two options. Holds a value only when both do. |
| `Do(v => ...)` / `Do(v => ..., () => ...)` | Runs an action with the value, or another one for `None`. |
| `OnNoneDo(() => ...)` | Runs an action when there is no value. |
| `Out(out var value)` | Captures the value for later steps of the chain, as on results. |
| `Finally(() => ...)` | Runs an action in both cases. |
| `GetValueOrDefault()` / `GetValueOrDefault(fallback)` / `GetValueOrDefault(() => ...)` | Leaves the option with the value, `default`, `fallback` or the factory's result. |
| `DefaultIfEmpty(fallback)` / `DefaultIfEmpty(() => ...)` | Turns `None` into an option with the fallback. The factory runs only when needed. |
| `Or(() => ...)` | Uses another option when there is no value: `cache.Find(id).Or(() => db.Find(id))`. |
| `Match(v => ..., () => ...)` | Returns a value with one callback for the value and one for `None`. |
| `Unwrap()` | Flattens `Option<Option<T>>`. |
| `options.Values()` | The values of all options that hold one. |
| `options.Combine()` | All values as `Option<IReadOnlyList<T>>`, or `None` if any option holds none. |
| `source.FirstOrNone()` / `source.LastOrNone()` | Like `FirstOrDefault` and `LastOrDefault`, with an optional predicate. |

Query syntax works, and so do `Task<Option<T>>` overloads and the `SelectAwait`, `ChainAwait`, `SelectManyAwait`, `WhereAwait` and `DoAwait` variants:

```csharp
Option<string> label = await (
    from person in FindPersonAsync(id)
    from address in FindAddressAsync(person.AddressId)
    where address.Country == "DE"
    select $"{person.Name}, {address.City}");
```

Unlike the result operators, the option operators do not catch exceptions, because an option has no error state. An exception thrown in a lambda, or a faulted task, propagates as usual.

### Between options and results

```csharp
Result<Person> person = FindPerson(id).ToResult(() => new KeyNotFoundException($"No person {id}."));
Option<Person> maybe = LoadPerson(id).ToOption();
```

`ToResult` turns `None` into the error from the factory. `ToOption` keeps the value of a successful result and drops errors and empty results.

`Transpose()` swaps the nesting of an optional value that may be invalid, like Rust's `transpose`:

| From | To |
| --- | --- |
| `Option<Result<T>>`: `None` / ok / error / empty | `Result<Option<T>>`: ok with `None` / ok with the value / error / empty |
| `Result<Option<T>>`: ok with `None` / ok with the value / error / empty | `Option<Result<T>>`: `None` / the ok result / the error result / the empty result |

Transposing twice gives back the original state.

### JSON

`System.Text.Json` serializes options without registration: an option writes its value, or `null` when it holds none, and reading `null` gives `None`.

```csharp
record Person(string Name, Option<string> Nickname);

JsonSerializer.Serialize(new Person("Ada", Option.None)); // {"Name":"Ada","Nickname":null}
```

- A missing property reads as a `default` option, which holds no value as well.
- `JsonIgnoreCondition.WhenWritingNull` does not omit `None`, because an option is a struct. `WhenWritingDefault` omits only uninitialized options.
- `Option<Option<T>>` writes `null` for both levels, so the difference is lost.
- With a source-generated `JsonSerializerContext`, register `T` as well, because the converter serializes the value through the options.

Results are deliberately not serialized: `JsonSerializer` throws `NotSupportedException` for `Result` and `Result<T>`. Without that, an error would be written with its message, stack trace and target site. Convert a result first, for example with `Match`, `GetValueOrDefault` or `ToOption`.

## Custom error types

The library's results accept any `Exception`. To restrict errors to your own base class, for example a `DomainException` that you later map to HTTP status codes, declare your own union and implement `IResult<TSelf, T, TError>` from `ResultType.Abstractions`:

```csharp
using ResultType.Abstractions;

namespace MyApp;

public union Result<T>(T, DomainException) : IResult<Result<T>, T, DomainException>
{
    public static Result<T> FromValue(T value) => value;
    public static Result<T> FromError(DomainException error) => error;
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new UnexpectedDomainException(exception);
}
```

Your methods return your own `Result<T>`. You keep `return value;`, `return error;`, exhaustive `switch` matching and `Task<Result<T>>`. Only another `DomainException` can be an error, and the compiler enforces it.

`FromException` is the hook for exceptions thrown inside operators. An exception that already is a `DomainException` is kept as is, and anything else goes through the hook, so chains still do not throw. The hook must not return `null` (the operator throws `InvalidOperationException`) and must not throw (that exception escapes the chain).

### Operators on custom types

`using ResultType.Extensions;` brings in operators that work on any implementation and return your type:

| Operator | Notes |
| --- | --- |
| `Chain(next)` / `ChainAwait(next)` | `next` must return a result with the same error type. A lambda needs its return type: `Result<int> (name) => name.Length` or a cast. |
| `Catch(e => ...)`, `Catch((NotFoundException e) => ...)`, `Catch(fallback)` | The typed form needs the exception type on the lambda parameter. |
| `Do`, `OnErrorDo`, `Finally`, `Where`, `DefaultIfEmpty` | Same behavior as on the library's `Result<T>`. |
| `GetValueOrDefault()`, `GetValueOrDefault(fallback)`, `Check()` | `Check()` on an empty result throws `InvalidOperationException`. |

`Select` and `MapError` cannot infer your result type, because C# has no way to express "the same kind of result with another type argument". They exist with explicit type arguments, so you wrap them once in one line each:

```csharp
public static class ResultExtensions
{
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector) =>
        result.Select<Result<T>, T, DomainException, Result<TOut>, TOut>(selector);

    public static Result<T> ToDomain<T>(this ResultType.Result<T> result) =>
        result.MapError<ResultType.Result<T>, T, Exception, Result<T>, DomainException>(Result<T>.FromException);
}
```

`Select` also enables query syntax with a single `from`. `ToDomain` turns a library result into yours, which is how you start from code that throws: `ResultType.Result.Try(() => Load(id)).ToDomain()`. A mapping that returns `null` gives an error, not an empty result.

Things to know:

- **Namespaces:** call sites only need `using ResultType.Extensions;`. Importing `ResultType` as well makes `Result<T>` ambiguous (CS0104) wherever your namespace is also imported.
- **Tasks:** operators on `Task<YourResult<T>>` are not provided, because C# cannot infer them. Await first, `(await LoadAsync(id)).Chain(...)`, or write a one-line wrapper:

  ```csharp
  public static async Task<Result<TOut>> Chain<T, TOut>(this Task<Result<T>> task, Func<T, Result<TOut>> next) =>
      (await task).Chain(next);
  ```

- **`Match` works too:** `Match(v => ..., e => ..., () => ...)`, with `e` typed as your error type.
- **Not provided:** `Zip`, `Combine`, `CombineAll`, `Partition`, `Validate`, `Unwrap`, `Transpose`, the factory overloads of `GetValueOrDefault` and `DefaultIfEmpty`, `Try`, query syntax with several `from` clauses, and the other `...Await` operators.
- **Allocations:** the operators take your result through the interface, which boxes it. That is 24 bytes per operator call in a Release build, on top of what the operator allocates anyway. The library's own `Result<T>` is unaffected and still binds to its concrete operators.

## Unused results analyzer

The package includes an analyzer, like Rust's `#[must_use]`. Calling a method that returns a result and ignoring it gives warning `FLU0001`:

```csharp
Save(person);      // warning FLU0001: The result of 'Save' is not used
_ = Save(person);  // fine: discarded on purpose
Save(person).Check();
```

It covers `Result`, `Result<T>`, your own types that implement `IResult`, and their `Task` and `ValueTask` versions, including `await SaveAsync();`. It does not report:

- assignments, returns and arguments, and explicit discards with `_ =`;
- calls ending in `Check()`, `OnErrorDo(...)` or `Do(onNext, onError)`, which already handle the error;
- an unawaited task inside an `async` method, where the compiler already warns with CS4014;
- options, because a missing value is not an error.

A chain that ends in `Do(v => ...)` or `Finally(...)` is reported, because its error is still dropped. Change the severity in `.editorconfig` like any other rule: `dotnet_diagnostic.FLU0001.severity = error`.

## Differences from v1

- Both types are structs. There is no inheritance and no `null` result. `Result r = null;` does not compile.
- `Result<T>` to `Result` is a conversion, not an upcast.
- The success case of `Result` is the `Ok` struct. Use it in `switch` arms. `Result.Ok` is still the static property that creates one.
- The `where T : class` constraint is gone. `Result<int>` works.
- `default(Result)` and `default(Result<T>)` hold nothing. `Success` is `false`, `Message` is `""`, `GetOk()` throws "Payload ... not set." and `GetError()` throws "Error property ... not set.".
- A `null` payload converted implicitly gives `Success == false`, because it cannot be told apart from `default`. In v1 it gave `Success == true`. `Result<T>.Ok(null)` still throws `ArgumentNullException`.
- `Check()` uses `ExceptionDispatchInfo` instead of the serialization-based stack trace hack.
- `ToString()` on `Result<T>` returns the payload's `ToString()` (or the exception's), or `""` when nothing is set.
- No per-file copyright headers. The license is in `LICENSE`.

## Limitations

- `Result<Exception>` declares fine, but implicit conversion from an exception fails with CS0457 (ambiguous user-defined conversions), because both cases take an `Exception`. Wrap the payload in another type.
- No `null` literal for a result. Use `default` only if you want the empty state.
- Equality is not payload-based. Two results wrapping distinct class instances are not equal.
- Not investigated: AOT and trimming (the JSON converters use reflection), hand-written class unions.

## Build and test

```
dotnet build ResultType.slnx
dotnet test ResultType.slnx
```

Tests use NUnit. Project layout: `src/ResultType` is the library, `test/ResultType.Tests` holds the tests, and `UseCases.cs` there contains runnable sample flows. `CustomErrors/` there contains a complete custom error type as a user would write it. `src/ResultType.Analyzers` is the analyzer packed with the library, tested in `test/ResultType.Analyzers.Tests`.
