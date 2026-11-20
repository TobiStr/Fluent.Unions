# Conversions

This page covers how options convert to and from nullable types, results and `Loadable<T>`.

```csharp
using Fluent.Unions;
```

## ToOption

`ToOption()` turns a value that may be missing into an option. There are four overloads.

| From | Missing case |
| --- | --- |
| nullable reference (`T?` where `T : class`) | `null` gives `None` |
| nullable struct (`T?` where `T : struct`) | `null` gives `None` |
| `Result<T>` | an error gives `None` |
| `Loadable<T>` | anything but a loaded value gives `None` |

```csharp
string? nickname = customer.Nickname;
int? age = customer.Age;

Option<string> maybeNickname = nickname.ToOption(); // None when null
Option<int> maybeAge = age.ToOption();              // None when null
Option<Customer> loaded = LoadCustomer(id).ToOption(); // LoadCustomer returns Result<Customer>
```

Converting a result drops the error. Log it first with `OnErrorDo` when it matters:

```csharp
Option<Customer> customer = LoadCustomer(id)
    .OnErrorDo(e => logger.LogWarning(e, "Could not load {Id}", id))
    .ToOption();
```

For a `Loadable<T>`, `loadable.ToOption()` gives the loaded value, and `None` while it is not loaded, loading or failed.

## ToNullable

Converts an option of a struct to a nullable struct. `None` gives `null`.

```csharp
Option<int> quantity = 3;

int? nullable = quantity.ToNullable(); // 3
```

It exists only for value types. For references, `GetValueOrDefault()` returns `null` for `None`.

## ToResult

Turns `None` into the error from a factory. The factory runs only when there is no value.

```csharp
Result<Customer> customer = FindCustomer(id)
    .ToResult(() => new KeyNotFoundException($"No customer {id}."));
```

Use it at the boundary where "not found" becomes a failure the caller must see. If the factory throws, that exception becomes the error. If it returns `null`, the error is an `InvalidOperationException`. `Task<Option<T>>` has a `ToResult` overload too.

```csharp
Result<Customer> customer = await FindCustomerAsync(id)
    .ToResult(() => new KeyNotFoundException($"No customer {id}."));
```

Nullable references and nullable value types convert directly, without going through an option first:

```csharp
Customer? found = repository.Find(id);
Result<Customer> customer = found.ToResult(() => new KeyNotFoundException($"No customer {id}."));

int? stock = inventory.GetStock(sku);
Result<int> available = stock.ToResult(() => new KeyNotFoundException($"No stock for {sku}."));
```

## Transpose

`Transpose()` swaps the nesting of an optional value that may be invalid, like Rust's `transpose`. It exists in both directions.

`Option<Result<T>>` to `Result<Option<T>>`:

| Input | Output |
| --- | --- |
| `None` | ok with `None` |
| ok with a value | ok with the value |
| error | error |

`Result<Option<T>>` to `Option<Result<T>>`:

| Input | Output |
| --- | --- |
| ok with `None` | `None` |
| ok with a value | the ok result |
| error | the error result |

Transposing twice gives back the original state.

```csharp
static Option<Result<int>> ParseOptional(string? text) =>
    text.ToOption().Select(t => Result.Try(() => int.Parse(t)));

Result<Option<int>> parsed = ParseOptional(input).Transpose();
// input null:   ok with None
// input "42":   ok with 42
// input "abc":  error: The input string 'abc' was not in a correct format.
```

This is the shape of an optional form field that must be valid when present. A missing field is fine, an invalid one is an error.

## JSON

Options serialize as their value or `null` without registration. Results refuse to serialize. See [serialization](../serialization/README.md).

## Pitfalls

- `Result<T>.ToOption()` drops the error without a trace. Log or handle the error first.
- `ToNullable()` does not exist for reference types.
- `ToResult` needs a factory, not an exception instance, so the exception and its stack trace are created only when needed.

## See also

- [Core concepts](core-concepts.md)
- [Patterns](patterns.md)
- [Serialization](../serialization/README.md)

---

Back to: [Option](README.md) · Next: [Patterns](patterns.md)
