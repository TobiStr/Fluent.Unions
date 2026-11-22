# Analyzer

The `Fluent.Unions` package includes analyzers that catch the mistakes the compiler lets through: a result that is dropped, an exception that escapes a result, a union that cannot tell its cases apart. They are added to your project with the package and need no setup.

## In this section

This folder has one page, this one. Each rule has its own section below, and the help link of every diagnostic points to it.

## Rules

| Rule | Severity | Reports | Code fix |
| --- | --- | --- | --- |
| [FLU0001](#flu0001) | Warning | A result that is not used | |
| [FLU0002](#flu0002) | Warning | An async lambda passed to a synchronous operator | Use the `Await` or `Async` operator |
| [FLU0003](#flu0003) | Warning | An async lambda that becomes `async void` | Use `DoAwait` |
| [FLU0004](#flu0004) | Error | A type argument that overlaps another case, such as `Result<Exception>` | |
| [FLU0005](#flu0005) | Error | A nullable type argument, such as `Result<string?>` | |
| [FLU0006](#flu0006) | Error | `Select` that returns a nested result | Use `Chain` |
| [FLU0007](#flu0007) | Warning | A result created with `default` | |
| [FLU0008](#flu0008) | Warning | A result that is serialized to JSON | |
| [FLU0009](#flu0009) | Warning | `FromException` or `FromError` that throws or returns `null` | |
| [FLU0010](#flu0010) | Info | `GetValue()` or `GetError()` without a check | |
| [FLU0011](#flu0011) | Info | A cast of `Value` | |
| [FLU0012](#flu0012) | Info | A `throw` in a method that returns a result | Return the exception |
| [FLU0013](#flu0013) | Info | `ToOption()` that drops an error | |
| [FLU0101](#flu0101) | Error | A `[FluentResult]` declaration that is not supported, with the reason | |
| [FLU0102](#flu0102) | Error | A `[FluentResult]` error type that cannot represent an unexpected exception | |
| [FLU0103](#flu0103) | Info | A case of an error union that gets no conversion, with the reason | |

The errors mark a type or a call that is wrong every time it appears, so they fail the build. The warnings point at bugs. The info rules point at code that works, but is easy to get wrong. They show as suggestions in the IDE and do not appear in the build output.

## Configuring the severity

Change a rule like any other in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.FLU0001.severity = error
dotnet_diagnostic.FLU0010.severity = warning
dotnet_diagnostic.FLU0012.severity = none
```

Use `error`, `warning`, `suggestion`, `silent` or `none`. To change a rule for one folder only, put the setting in a section of `.editorconfig` that matches those files.

## Suppressing in one place

Suppress a rule for one statement:

```csharp
#pragma warning disable FLU0001
Save(order);
#pragma warning restore FLU0001
```

Or for a whole member with `[SuppressMessage("Usage", "FLU0001")]`. Generated code is never analyzed.

<a id="flu0001"></a>

## FLU0001: Result is not used

A call returns a result, and the result is dropped, like Rust's `#[must_use]`. A dropped result hides its error.

```csharp
Save(order);                  // warning FLU0001: The result of 'Save' is not used
_ = Save(order);              // fine: discarded on purpose
Save(order).Check();          // fine: Check throws the error
var result = Save(order);     // fine: you hold the result
```

A call is reported when it stands alone as a statement and returns `Result`, `Result<T>`, your own result type declared with `[FluentResult]`, or a `Task` or `ValueTask` of one of them.

```csharp
maybe?.Save(order);                            // conditional access
await SaveAsync(order);                        // an awaited Task<Result>
Load(order.Id).Do(o => Console.WriteLine(o));  // a chain that ends in Do(onSuccess)
Load(order.Id).Finally(() => { });             // a chain that ends in Finally
```

- **A chain is judged by its last call.** The error is still dropped after `Do(v => ...)`, `Finally(...)` and `Catch(...)`.
- **An unawaited task is reported in a method that is not `async`.** Inside an `async` method the compiler already warns with CS4014.

Not reported:

- Assignments, returns, arguments and explicit discards with `_ =`.
- Calls that end in `Check()`, `OnErrorDo(...)` or `Do(onSuccess, onError)`. These are recognized by name and by the library they come from, so a method of your own called `Check` does not count. The operators that the generator writes for your own result type count as library operators, so `OnErrorDo(...)` and `Do(onSuccess, onError)` on it are handled the same way.
- Options. A missing value is not an error, so `Option<T>` is never reported.

Prefer an explicit discard when ignoring a result is on purpose, such as a best-effort log write. It documents the decision and needs no suppression.

<a id="flu0002"></a>

## FLU0002: Async lambda passed to a synchronous operator

`Try`, `Select`, `Chain` and `SelectMany` call the lambda and keep what it returns. An async lambda returns a task, so the result holds the task, not its outcome, and an exception inside the task is never caught:

```csharp
var order = Result.Try(async () => await LoadOrderAsync(id));
// warning FLU0002: 'Try' does not await the async lambda, so its exception escapes the result; use 'TryAsync'
// order is a Result<Task<Order>>: always a success, even when LoadOrderAsync fails

var price = order.Select(async o => await PriceAsync(o));
// warning FLU0002: use 'SelectAwait'
```

The code fix switches to the operator that awaits: `Try` to `TryAsync`, `Select` to `SelectAwait`, `Chain` to `ChainAwait`, `SelectMany` to `ChainAwait`. The result then becomes a `Task<Result<T>>`, so you may need to `await` it.

Operators that take a `Func<Task>` already, such as `TryAsync`, `SelectAwait` and `DoAwait`, are not reported. Neither is `Select` on `Loadable<T>`, which has no awaiting counterpart, nor a lambda stored in a variable first.

<a id="flu0003"></a>

## FLU0003: Async lambda becomes async void

`Do`, `OnErrorDo` and the other side-effect operators take an `Action`. An async lambda passed as an `Action` is `async void`: nobody awaits it, the chain goes on before it finishes, and an exception from it can crash the process.

```csharp
result.Do(async o => await SaveAsync(o));
// warning FLU0003: The async lambda passed to 'Do' is async void; use 'DoAwait'
```

The code fix switches `Do` to `DoAwait`, which awaits the lambda. When an operator has no awaiting counterpart, await the work outside the chain instead.

<a id="flu0004"></a>

## FLU0004: Type argument overlaps another case

A union decides its case by the runtime type of the value it holds. When a type argument overlaps another case, a value of that type is read as the other case:

```csharp
Result<Exception> last = new TimeoutException();
// error FLU0004: 'Result<Exception>' cannot tell its cases apart, because the type argument 'Exception' overlaps the case 'Exception'
// last.IsSuccess is false: the "value" is read as the error

Result<object> any = Load();       // error: object overlaps Exception
Option<object> maybe = Find();     // error: object overlaps None
Outcome<object> r;                 // error: object overlaps CheckoutError
```

The overlapping cases are: `Exception` for `Result<T>`, the error type, or one of its union cases, for a declared result type, `None` for `Option<T>`, and `Exception`, `NotLoaded` and `Loading` for `Loadable<T>`. A type overlaps when it is the same type, a base type, a derived type, or an interface of it.

To carry an exception as a value, wrap it in a record of your own, such as `record FailedAttempt(Exception Exception)`. For errors as data, give the error its own type.

Only types written in source are checked. A type inferred with `var` is not.

<a id="flu0005"></a>

## FLU0005: Nullable type argument

A `null` value does not become a case of its own. A result holds an error instead, and an option holds `None`. A nullable type argument promises something the type cannot hold:

```csharp
Result<string?> name = customer.Nickname;
// error FLU0005: 'Result<string?>' cannot hold null; use a non-nullable type argument, or Option<T> for a missing value
// with a null nickname, name is an error
```

Use `Result<Option<string>>` when a successful outcome may have no value, and `Option<int>` instead of `Option<int?>`.

<a id="flu0006"></a>

## FLU0006: Select returns a nested result

A selector that returns a result nests it inside the outer one. The inner error is then hidden behind an outer success:

```csharp
Result<Result<Customer>> customer = LoadOrder(id).Select(o => FindCustomer(o.CustomerId));
// error FLU0006: 'Select' gives a nested 'Result<Result<Customer>>'; use 'Chain'
```

The code fix switches `Select` to `Chain`, or `SelectAwait` to `ChainAwait`, which flattens the result. The same applies to `Option<Option<T>>`.

`Result<Option<T>>` and `Option<Result<T>>` are not reported. They are different shapes on purpose, see [Transpose](../option/conversions.md#transpose).

<a id="flu0007"></a>

## FLU0007: Result created with default

A `default` result holds neither a value nor an error, so it counts as an error that says it was not initialized:

```csharp
Result<Order> order = default;
// warning FLU0007: A default 'Result<Order>' is an error; create it with a value or an error

return new Result<int>();          // warning
```

Create a result from a value or an exception instead. `Option<T>` and `Loadable<T>` are not reported, because their `default` is a valid `None` and not loaded.

The rule sees `default` and `new()` of a result type only. It does not report an element of a new array, a field that is never assigned, `FirstOrDefault()` on a sequence of results, or `default(T)` in generic code. See [The default result](../result/core-concepts.md#the-default-result) for how such a result behaves.

To fail the build on it, raise the rule to an error:

```ini
[*.cs]
dotnet_diagnostic.FLU0007.severity = error
```

<a id="flu0008"></a>

## FLU0008: Result is serialized

Results refuse to serialize, because an error would expose exception details such as the stack trace. Serializing one throws `NotSupportedException` at runtime. The rule reports it at compile time in two places.

`JsonSerializer` calls, including a result inside a property, a list or a dictionary of the serialized type:

```csharp
JsonSerializer.Serialize(new OrderResponse(LoadOrder(id)));
// warning FLU0008: 'Result<Order>' throws NotSupportedException when it is serialized; convert it first, for example with Match or GetValueOrDefault

public record OrderResponse(Result<Order> Order);
```

Minimal API endpoints that return a result without the result mapping:

```csharp
app.MapGet("/orders/{id}", (int id) => LoadOrder(id));
// warning FLU0008: add .WithResultMapping() from Fluent.Unions.AspNetCore, or return ToHttpResult()

app.MapGet("/orders/{id}", (int id) => LoadOrder(id)).WithResultMapping();  // fine
app.MapGet("/orders/{id}", (int id) => LoadOrder(id).ToHttpResult());       // fine
```

`WithResultMapping()` on a route group counts when the group is a local variable in the same method. An endpoint mapped on a group that comes from a parameter or a field is not reported, because the analyzer cannot see whether that group is mapped.

<a id="flu0009"></a>

## FLU0009: Error factory throws or returns null

`FromException` and `FromError` turn failures into results. When they throw or return `null`, the failure escapes the result:

```csharp
public union CheckoutError(CartEmpty, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) =>
        throw new NotSupportedException();
    // warning FLU0009: 'FromException' must not throw
}
```

Map every exception to a case, such as `Unexpected`. See [Custom error types](../errors/custom-error-types.md#the-three-static-members) for the contract.

<a id="flu0010"></a>

## FLU0010: Result is read without a check

`GetValue()` throws on an error, and `GetError()` throws on a value. Read them after a check, or with an operation that handles both cases:

```csharp
var order = LoadOrder(id).GetValue();
// info FLU0010: 'GetValue' throws when the result is an error; check it first, or use TryGetValue, Match or switch

var result = LoadOrder(id);
if (result.IsSuccess)
    Ship(result.GetValue());       // fine: checked

if (result.IsError)
    return;
Ship(result.GetValue());           // fine: the error case returned earlier
```

The rule recognizes `IsSuccess`, `IsError`, `TryGetValue` and `TryGetError` in `if` conditions, conditional expressions, `&&`, `||` and `!`, and an earlier `if` that returns, throws, breaks or continues. A check it does not understand, such as a `switch`, still reports. It is info-level because `GetValue()` is sometimes the right call where a failure is a bug. Prefer `TryGetValue`, `Match` or a `switch`.

<a id="flu0011"></a>

## FLU0011: Value is cast

`Value` holds whichever case the union has. A cast to one case throws `InvalidCastException` on every other case:

```csharp
var order = (Order)result.Value;
// info FLU0011: The cast of 'result.Value' throws when it holds another case; use a pattern or TryGetValue

if (result.Value is Order order) { }   // fine
if (result.TryGetValue(out var o)) { } // fine
```

<a id="flu0012"></a>

## FLU0012: Throw in a method that returns a result

A method that returns a result promises to report failures in it. A thrown exception bypasses that promise, and the caller's chain never sees it:

```csharp
Result<Order> Ship(Order order)
{
    if (order.IsShipped)
        throw new OrderShippedException(order.Id);
    // info FLU0012: Return the exception instead of throwing it; 'Ship' returns a result
    ...
}
```

The code fix replaces `throw` with `return`, because an exception converts to a result. This also works in `async` methods that return a `Task<Result<T>>`.

The throw is only reported when the thrown type converts to the result. A result type declared with `[FluentResult<DomainException>]` holds only a `DomainException`, so `throw new InvalidOperationException()` in a method that returns it is not reported: `return new InvalidOperationException();` would not compile.

Not reported:

- Throw expressions, such as `value ?? throw new ArgumentNullException(...)`, and rethrows with `throw;`.
- Guard and programming errors: `ArgumentException` and its subclasses, `ObjectDisposedException`, `NotImplementedException`, `NotSupportedException`, `UnreachableException`, and `OperationCanceledException`, which should cancel the caller instead of becoming an error.

<a id="flu0013"></a>

## FLU0013: ToOption drops the error

`ToOption()` on a result turns an error into `None` without a trace:

```csharp
Option<Customer> customer = LoadCustomer(id).ToOption();
// info FLU0013: ToOption drops the error of the result; handle or log it first

Option<Customer> logged = LoadCustomer(id)
    .OnErrorDo(e => logger.LogWarning(e, "Could not load {Id}", id))
    .ToOption();                   // fine: the error is logged
```

A chain that contains `OnErrorDo`, `Do(onSuccess, onError)` or `Catch` before `ToOption()` is not reported.

## Your own Result type

Every rule above treats a type declared with `[FluentResult<TError>]` like the library's `Result<T>`. `FLU0004` also reports a value type that is the error type or one of its union cases, such as `Outcome<CartEmpty>`, because a value of that type would become an error.

The generator adds three rules:

<a id="flu0101"></a>

### FLU0101: Unsupported declaration

The message names the reason. The type must be:

- a `struct` declared `readonly` and `partial` (the `readonly` may be on any part), not a record struct, a ref struct or a `file` type;
- declared directly in a namespace, not inside another type;
- generic with at most one type parameter, which has no constraints and does not allow ref structs, or a value-less companion next to a generic one;
- named differently from the members the generator adds, such as `Ok` or `Error`;
- no less accessible than its error type: a public result needs a public error type.

```csharp
[FluentResult<DomainException>]
public partial struct Result<T>;   // error FLU0101: 'Result' is not a supported result declaration: it must be 'readonly'
```

A struct without a type parameter is the value-less companion. It needs a generic struct with the same name, namespace and error type next to it, and both must be declared with `[FluentResult]`. Without a declared companion, the generator writes one.

<a id="flu0102"></a>

### FLU0102: No exception mapping

The error type must implement `IFromException<TError>` for itself, or the struct must declare a `public` or `internal` `static TError FromException(Exception exception)`. A method with another parameter or return type, a private method and an `IFromException<Other>` of another type do not count. Without a mapping, an exception thrown inside a step would have nowhere to go.

<a id="flu0103"></a>

### FLU0103: Case without a conversion

A case of the error union gets no conversion to the result when C# does not allow one: an interface, `object` or `System.ValueType`, or a type that is less accessible than the result, because conversion operators are public. The message names the reason. Return the case through `Error(...)` instead.

---

Back to index: [Docs index](../README.md)
