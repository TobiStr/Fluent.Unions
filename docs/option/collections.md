# Collections

This page covers the operators between sequences, dictionaries and options. They replace `...OrDefault` calls that give `null` or `0` for "not found".

```csharp
using Fluent.Unions;
```

## FirstOrNone and LastOrNone

Like `FirstOrDefault` and `LastOrDefault`, but a missing element is `None`, not `default`. Both take an optional predicate.

```csharp
int[] ages = [12, 25, 41];

Option<int> first = ages.FirstOrNone();             // 12
Option<int> adult = ages.FirstOrNone(a => a >= 18); // 25
Option<int> last = ages.LastOrNone(a => a >= 18);   // 41
Option<int> none = ages.FirstOrNone(a => a > 100);  // None
```

The difference matters for value types: `FirstOrDefault` returns `0` for "not found", which is also a valid number.

## SingleOrNone

Returns the only element, or `None` for an empty sequence. **It throws `InvalidOperationException` when there are several**, like `SingleOrDefault`. It does not mean "at most one, else `None`". It also takes an optional predicate.

```csharp
Option<Customer> admin = customers.SingleOrNone(c => c.IsAdmin); // None, or the one admin
```

With two admins, the call throws. Use `FirstOrNone` when several matches are acceptable.

## ElementAtOrNone

The element at an index, or `None` when the index is negative or out of range.

```csharp
string[] names = ["a", "b"];

Option<string> second = names.ElementAtOrNone(1); // "b"
Option<string> third = names.ElementAtOrNone(2);  // None
```

## GetValueOrNone

Looks up an `IReadOnlyDictionary<TKey, TValue>`. A missing key is `None`.

```csharp
IReadOnlyDictionary<Guid, Customer> customers = LoadCustomers();

Option<Customer> customer = customers.GetValueOrNone(id);
```

## Values

Returns the values of all options that hold one, skipping the rest. The result is lazy, like LINQ.

```csharp
IEnumerable<Option<Customer>> lookups = ids.Select(FindCustomer);

IEnumerable<Customer> found = lookups.Values();
```

## Combine

Collects all values into one `Option<IReadOnlyList<T>>`. **If any option has no value, the whole result is `None`.** An empty sequence gives an option holding an empty list.

```csharp
Option<IReadOnlyList<Customer>> all = ids.Select(FindCustomer).Combine();
```

Use `Values()` when missing items may be skipped. Use `Combine()` when the whole set is only useful if every item exists, for example every product of an order:

```csharp
Option<IReadOnlyList<Product>> products = order.Skus.Select(FindProduct).Combine();
```

## Pitfalls

- `SingleOrNone` throws for several matches.
- `Combine()` on options gives `None` as soon as one item is missing. On `Result<T>` sequences, `Combine()` gives the first error instead.
- `Values()` and `Combine()` enumerate the sequence once per call. Materialize it first when enumerating twice is expensive.

## See also

- [Operations](operations.md)
- [Patterns](patterns.md)
- [Streams](../async/streams.md) for `IAsyncEnumerable<Option<T>>.Values()`

---

Back to: [Option](README.md) · Next: [Conversions](conversions.md)
