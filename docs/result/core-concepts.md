# Core concepts

This page explains what a result is, how to create one, and how to read it.

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;
```

## Result and Result&lt;T&gt;

`Result<T>` holds a `T` or an `Exception`. Use it when the operation returns something.

`Result` holds `Ok` or an `Exception`. Use it when the operation returns nothing, such as a save.

Both are structs. There is no `null` result, and `Result r = null;` does not compile.

## The three states

| State | Meaning | Produced by |
| --- | --- | --- |
| ok | holds a value (or `Ok`) | returning a value, `Result.Ok`, `Result<T>.Ok(value)`, a successful `Try` |
| error | holds an exception | returning an exception, `Error(...)`, a throwing lambda inside an operator, a throwing `Try` body |
| empty | holds nothing | `default`, a `null` value, `Where` with a failing predicate |

Empty is easy to miss. A `null` payload cannot be told apart from `default`, so it gives an empty result with `Success == false`:

```csharp
string? nickname = null;
Result<string> nullPayload = nickname!; // empty
Result<Order> missing = default;        // empty
```

`Where` turns a value that fails the predicate into an empty result, the closest thing to LINQ's "no element".

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

`Result.Ok` is a property. `Result<T>.Ok(value)` is a method and throws `ArgumentNullException` for `null`, where the implicit conversion gives an empty result.

`Try` and `TryAsync` run code that may throw:

```csharp
Result<int> number = Result.Try(() => int.Parse("42"));
Result written = Result.Try(() => File.WriteAllText("a.txt", "x"));
```

See [Error handling](error-handling.md#try-and-tryasync) for the body types they accept.

## Members

| Member | Behavior |
| --- | --- |
| `Success` | `true` only when the result is ok. `false` for error and empty. |
| `Message` | The exception message, or `""` for ok and empty. |
| `GetOk()` | The value of a `Result<T>`. Throws `InvalidOperationException` for an error or an empty result. |
| `GetError()` | The exception. Throws `InvalidOperationException` when the result holds none. |
| `ToString()` | The value's `ToString()`, the exception's `ToString()`, or `""` when empty. |

`ToString()` on an error gives the full exception text with type name and stack trace, not just `Message`. Use `Message` for display.

```csharp
Result<Order> placed = PlaceOrder(cart);

if (placed.Success)
{
    Order o = placed.GetOk();
}
else if (placed.Message != "")
{
    Exception error = placed.GetError();
}
```

The `else if` guards against the empty state, where `GetError()` would throw. Prefer `switch` or `Match`, which cannot forget a state.

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

The success arm of `Result` is the `Ok` struct. An empty result matches neither arm and the switch throws at runtime. Use [`Match`](operations.md#match), which has a callback for the empty state, when an empty result is possible.

Leaving out an arm is warning CS8509. To make it an error, add `<WarningsAsErrors>CS8509</WarningsAsErrors>` to the project file. See [Installation](../getting-started/installation.md).

## Result&lt;T&gt; to Result

`Result<T>` converts implicitly to `Result`. The conversion keeps ok, error and empty, and drops the value.

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
if (customer is null) return default; // the result was empty
```

Check `error` first. On an error the value is `default`, so `0` for an `int`. An empty result gives `default` and `null`. Deconstruction exists on `Result<T>` only, not on `Result`.

## Check()

`Check()` returns the result unchanged when it is ok. Otherwise it throws the underlying exception. It rethrows with `ExceptionDispatchInfo`, so the original stack trace is kept and each rethrowing layer is appended.

```csharp
Result<Order> checkedOrder = placed.Check();
Result checkedSave = Save(order).Check();
```

`Check()` on an empty result throws `InvalidOperationException` ("Error property for Result&lt;T&gt; not set."). It also exists on `Task<Result<T>>` and `Task<Result>`.

## Pitfalls

- `GetOk()` throws for error and empty. Use it only after checking `Success`.
- `Result<Exception>` compiles, but converting an exception to it fails with CS0457, because both cases take an `Exception`. Wrap the payload in another type.
- Equality is not payload-based. Two results wrapping different class instances are not equal.

## See also

- [Operations](operations.md)
- [Error handling](error-handling.md)
- [Option](../option/README.md)

---

Back to: [Result](README.md) · Next: [Operations](operations.md)
