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

A result can never be "empty". A `default` result, and a result created from a `null` value, count as an error: an `InvalidOperationException` that says the result holds neither a value nor an error. Use `Option<T>` for a value that may be missing.

```csharp
string? nickname = null;
Result<string> fromNull = nickname!;      // error
Result<Order> missing = default;          // error
Result<string> nick = Result.From(user).Select(u => u.Nickname); // error when Nickname is null
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

`Result.From(value)` does the same as `Result<T>.Ok(value)`, but infers `T`. Use it to start a chain from a plain value:

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

`ToString()` on an error gives the short type name and the message, such as `InvalidOperationException: No order 7.`. It leaves out the stack trace and the inner exceptions, so it is safe to log. A `default` result prints the message of the exception that says it is empty.

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

The success arm of `Result` is the `Ok` struct. A `default` result is not covered by the arms and makes the switch throw at runtime; it only comes from forgetting to initialize a result. [`Match`](operations.md#match) treats it as an error instead.

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
- Equality is not payload-based. Two results wrapping different class instances are not equal.

## See also

- [Operations](operations.md)
- [Error handling](error-handling.md)
- [Option](../option/README.md)

---

Back to: [Result](README.md) · Next: [Operations](operations.md)
