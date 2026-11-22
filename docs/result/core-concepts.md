# Core concepts

This page explains what a result is, how to create one, and how to read it.

```csharp
using Fluent.Unions;
```

## Result and Result&lt;T&gt;

`Result<T>` holds a `T` or an `Exception`. Use it when the operation returns something.

`Result` holds `Ok` or an `Exception`. Use it when the operation returns nothing, such as a save.

Both are structs. There is no `null` result, and `Result r = null;` does not compile.

## Two states

A result is ok or an error, nothing else.

| State | Meaning | Produced by |
| --- | --- | --- |
| ok | holds a value (or `Ok`) | returning a value, `Result.Ok`, `Result<T>.Ok(value)`, a successful `Try` |
| error | holds an exception | returning an exception, `Error(...)`, a throwing lambda inside an operator, a throwing `Try` body |

A `default` result, and a result created from a `null` value, count as an error for the library: an `InvalidOperationException` that says the result holds neither a value nor an error. Use `Option<T>` for a value that may be missing.

```csharp
string? nickname = null;
Result<string> fromNull = nickname!;      // error
Result<Order> missing = default;          // error
Result<string> nick = Result.From(user).Select(u => u.Nickname); // error when Nickname is null
```

## The default result

A `default` result is an error for every library member, but native C# pattern matching sees no case at all. The compiler checks a `switch` against the two case types only, so it cannot know about the empty state.

| Consumed through | Behavior |
| --- | --- |
| `IsError`, `IsSuccess` | `true`, `false` |
| `TryGetError`, `GetError()` | The "holds neither a value nor an error" exception. Each call creates a new instance. |
| `TryGetValue`, `GetValue()` | `false`; `GetValue()` throws with that exception as `InnerException` |
| Every operator, `Match`, `Deconstruct`, `Check()` | Treat it as that error |
| `ToString()` | The message of that exception |
| `switch` expression | Throws `SwitchExpressionException`, unless it has a `null` arm |
| `switch` statement | Matches no case and runs no branch |
| `is T`, `is Exception` | `false` |
| `is null`, `Value` | `true`, `null` |
| `Equals`, `GetHashCode()` | Equal to every other `default` result and to a result created from `null` |
| JSON | Refused like every result, see [Serialization](../serialization/README.md) |

Your own result types behave the same. Their error is what `FromException` makes of that exception, see [Custom error types](../errors/custom-error-types.md).

A `default` result comes from code that forgot to create one:

- `default` or `new()` of a result type: analyzer warning [FLU0007](../analyzer/README.md#flu0007) reports it.
- An element of a new array, or a field or property that was never assigned.
- `FirstOrDefault()` and similar on a sequence of results, and `default(T)` in generic code.

The analyzer cannot see the last two groups. Where a result may come from code you do not control, read it with `Match` or the operators, or add a `null` arm to the `switch`:

```csharp
static string Describe(Result<Order> result) => result switch
{
    Order order => $"Order {order.Id}",
    Exception error => error.Message,
    null => "Not initialized"
};
```

## Creating results

Return the value or the exception. The union converts both implicitly.

```csharp
Result<Order> a = order;
Result<Order> b = new InvalidOperationException("Out of stock.");
```

The explicit forms are useful where there is no target type:

```csharp
Result<Order> c = Result<Order>.Ok(order);
Result<Order> d = Result<Order>.Error(new InvalidOperationException("Out of stock."));
Result ok = Result.Ok;
Result failed = Result.Error(new TimeoutException());
```

`Result.Ok` is a property. `Result<T>.Ok(value)` is a method and throws `ArgumentNullException` for `null`, where the implicit conversion gives an error.

`Result.From(value)` does the same as `Result<T>.Ok(value)`, including the `ArgumentNullException` for `null`, but infers `T`. Use it to start a chain from a plain value:

```csharp
Result<Order> placed = Result.From(order)
    .Ensure(o => o.Lines.Count > 0, "An order needs at least one line.")
    .Chain(Save);
```

`Try` and `TryAsync` run code that may throw:

```csharp
Result<int> number = Result.Try(() => int.Parse("42"));
Result written = Result.Try(() => File.WriteAllText("a.txt", "x"));
```

See [Error handling](error-handling.md#try-and-tryasync) for the body types they accept.

## Members

| Member | Behavior |
| --- | --- |
| `IsSuccess` | `true` when the result is ok. |
| `IsError` | `true` when the result holds an error. |
| `TryGetValue(out value)` | Gets the value of a `Result<T>`, if there is one. |
| `TryGetError(out error)` | Gets the exception, if there is one. |
| `GetValue()` | The value of a `Result<T>`. Throws `InvalidOperationException` for an error, with the error as `InnerException`. |
| `GetError()` | The exception. Throws `InvalidOperationException` when the result is ok. |
| `ToString()` | The value's `ToString()`, `Ok` for a result without a value, or `TypeName: Message` of the exception. |

`ToString()` on an error gives the short type name and the message, such as `InvalidOperationException: No order 7.`. It leaves out the stack trace and the inner exceptions, so it is safe to log.

```csharp
Result<Order> placed = PlaceOrder(cart);

if (placed.TryGetValue(out var order))
{
    Ship(order);
}
else if (placed.TryGetError(out var error))
{
    logger.LogWarning(error, "Order failed");
}
```

Prefer `switch` or `Match` when both cases produce a value.

## Pattern matching

Both cases appear as arms, so the compiler checks them.

```csharp
static string Describe(Result<Order> result) => result switch
{
    Order order => $"Order {order.Id}: {order.Total}",
    Exception error => error.Message
};

static string DescribeValidation(Result result) => result switch
{
    Ok => "valid",
    Exception error => error.Message
};
```

The success arm of `Result` is the `Ok` struct. A `default` result is not covered by the arms and makes the switch throw at runtime; see [The default result](#the-default-result). [`Match`](operations.md#match) treats it as an error instead.

In ASP.NET Core code that imports `Microsoft.AspNetCore.Http.HttpResults`, `Ok` is ambiguous with the HTTP result of the same name (CS0104). Add `using Ok = Fluent.Unions.Ok;` to that file.

Leaving out an arm is warning CS8509. To make it an error, add `<WarningsAsErrors>CS8509</WarningsAsErrors>` to the project file. See [Installation](../getting-started/installation.md).

## Result&lt;T&gt; to Result

`Result<T>` converts implicitly to `Result`. The conversion keeps ok or the error, and drops the value.

```csharp
Result<Order> typed = placed;
Result plain = typed;
```

`Result<T>` is not a subclass of `Result`, so there is no upcast and no conversion the other way.

## Go-style deconstruction

In statement code, a `Result<T>` deconstructs into a value and an error:

```csharp
var (customer, error) = GetCustomer(id);
if (error is not null) return error;
```

Exactly one of the two is set. Check `error` first: on an error the value is `default`, so `0` for an `int`. Deconstruction exists on `Result<T>` only, not on `Result`.

## Check()

`Check()` returns the result unchanged when it is ok. Otherwise it throws the error. It rethrows with `ExceptionDispatchInfo`, so the original stack trace is kept and each rethrowing layer is appended.

```csharp
Result<Order> checkedOrder = placed.Check();
Result checkedSave = Save(order).Check();
```

It also exists on `Task<Result<T>>` and `Task<Result>`.

## Pitfalls

- `GetValue()` throws for an error. Prefer `TryGetValue`, `switch` or `Match`.
- `Result<Exception>` compiles, but converting an exception to it fails with CS0457, because both cases take an `Exception`. Analyzer error [FLU0004](../analyzer/README.md#flu0004) reports the type. Wrap the payload in another type.
- Results have no `==` operator. `Equals` compares the payload with the payload's own `Equals`: two results holding equal records are equal, and two results holding different instances of a class without value equality are not. Two error results are equal only when they hold the same exception instance.

## See also

- [Operations](operations.md)
- [Error handling](error-handling.md)
- [Option](../option/README.md)

---

Back to: [Result](README.md) · Next: [Operations](operations.md)
