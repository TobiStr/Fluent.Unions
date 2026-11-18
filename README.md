# result-v2

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

- **Chains do not throw.** An exception thrown inside a lambda becomes the result's error, as in Rx. Only `Check()` throws on purpose. A `null` lambda argument still throws `ArgumentNullException`.
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
| `OnErrorDo(e => ...)` | Runs an action with the error and returns the same result. |
| `Finally(() => ...)` | Runs an action in every state. |

**Leaving the chain**

| Operator | What it does |
| --- | --- |
| `GetValueOrDefault()` | Returns the value, or `default` for an error or empty result. |
| `GetValueOrDefault(fallback)` | Returns the value, or `fallback`. |
| `DefaultIfEmpty(fallback)` | Turns an empty result into an ok one. It stays a result. |

**Sequences** (`IEnumerable<Result<T>>`)

| Operator | What it does |
| --- | --- |
| `Combine()` | Returns `Result<IReadOnlyList<T>>` with all values. The first error wins, and empty results are skipped. |
| `Values()` | Returns the values of the ok results. |
| `Errors()` | Returns the exceptions of the error results. |

`Combine()` is deliberately not called `ToList()`: overload resolution would prefer it over `Enumerable.ToList()` and silently change what existing code returns.

### Query syntax

`Select`, `SelectMany` and `Where` follow the LINQ query pattern, so query expressions work:

```csharp
Result<int> sum =
    from a in Parse("20")
    from b in Parse("22")
    where a < b
    select a + b;
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
- Not investigated: JSON serialization, AOT and trimming, hand-written class unions.

## Build and test

```
dotnet build ResultType.slnx
dotnet test ResultType.slnx
```

Tests use NUnit. Project layout: `src/ResultType` is the library, `test/ResultType.Tests` holds the tests, and `UseCases.cs` there contains runnable sample flows.
