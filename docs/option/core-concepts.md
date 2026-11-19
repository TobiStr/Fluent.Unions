# Core concepts

This page covers what an `Option<T>` is, how to create one, its members, and how to match it.

## An option is a union of T and None

`Option<T>` is a C# 15 union of `T` and `None`. It holds a value or `None`, never both.

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

Option<int> quantity = 3;                   // value
Option<string> nickname = Option.None;      // None
```

A `default` option also holds no value. That is the state of an uninitialized field or a missing JSON property. Everything in the library treats it like `None`.

## Creating options

| Source | Code | Result for a missing value |
| --- | --- | --- |
| A value | `Option<int> o = 3;` or `Option.Some(3)` | |
| No value | `Option<int> o = Option.None;` | `None` |
| A nullable reference | `name.ToOption()` | `null` gives `None` |
| A nullable value type | `age.ToOption()` | `null` gives `None` |
| A string | `Option.TryParse<int>("42")` | a failed parse gives `None` |

```csharp
Option<Customer> customer = new Customer(id, "Ada"); // implicit
Option<Customer> same = Option.Some(new Customer(id, "Ada"));
```

`ToOption` is for values that may be `null`:

```csharp
string? nickname = customer.Nickname;
Option<string> maybeNickname = nickname.ToOption(); // None when nickname is null

int? age = customer.Age;
Option<int> maybeAge = age.ToOption();              // None when age is null
```

`Option.TryParse<T>` works for any `IParsable<T>`, and for a `ReadOnlySpan<char>` with any `ISpanParsable<T>`. It takes an optional `IFormatProvider`.

```csharp
Option<int> quantity = Option.TryParse<int>("42"); // 42
Option<int> invalid = Option.TryParse<int>("abc"); // None
```

`Option.Some(value)` throws `ArgumentNullException` for `null`. The implicit conversion does not throw: converting a `null` reference gives a `default` option, which holds no value.

```csharp
string? missing = null;
Option<string> viaImplicit = missing!;          // default option, HasValue is false
Option<string> viaSome = Option.Some(missing!); // throws ArgumentNullException
```

## Members

| Member | Behavior |
| --- | --- |
| `HasValue` | `true` when the option holds a value. |
| `TryGetValue(out value)` | Returns `true` and the value, or `false` with `default`. |
| `GetValue()` | The value. Throws `InvalidOperationException` when there is none. |
| `ToString()` | The value's `ToString()`, or `""` when there is no value. |

```csharp
Option<int> quantity = 3;

if (quantity.TryGetValue(out var count))
    Console.WriteLine($"{count} items");

int safe = quantity.GetValue(); // 3
string text = quantity.ToString(); // "3"
```

Prefer `TryGetValue`, `Match` or `GetValueOrDefault` over `GetValue()`. `GetValue()` is the one call that turns a missing value into an exception.

## Matching an option

Both cases are visible in a `switch`, so the compiler can check them:

```csharp
static string Describe(Option<Customer> option) => option switch
{
    Customer customer => customer.Name,
    None or null => "nobody"
};
```

Write `None or null`, or `_`, never only `None`. Every option the library returns holds a real `None`, but a `default` option holds nothing, so a lone `None` arm does not match it. The compiler does not warn about that, and the `switch` throws `SwitchExpressionException` at runtime.

```csharp
Option<Customer> uninitialized = default;

Describe(uninitialized); // "nobody", because of `None or null`
```

`Match` avoids the problem. It calls `onNone` for both `None` and a `default` option:

```csharp
string name = option.Match(customer => customer.Name, () => "nobody");
```

## Zero, false and empty strings are values

Only `null` and `None` mean "no value". An option holding `0`, `false` or `""` has a value.

```csharp
Option<int> zero = 0;
Option<string> empty = "";

zero.HasValue;  // true
empty.HasValue; // true
```

`Where` and `DefaultIfEmpty` follow the same rule. If `0` should mean "none" in your domain, say so with `Where(n => n != 0)`.

## Pitfalls

- A bare `None` arm in a `switch` misses `default` options and throws at runtime.
- An implicit `null` gives a `default` option, not an error. Prefer `ToOption()`, which gives a real `None`.
- Options never hold `null`. `Select(x => (string?)null)` gives `None`.

## See also

- [Operations](operations.md)
- [Conversions](conversions.md)
- [Serialization](../serialization/README.md)

---

Back to: [Option](README.md) · Next: [Operations](operations.md)
