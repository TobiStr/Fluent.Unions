# Patterns

This page shows how options replace `null` in everyday code, and when to choose an option, a result or a nullable type.

```csharp
using Fluent.Unions;
```

## Replacing null returns

A method that returns `Customer?` relies on every caller remembering the check. Returning `Option<Customer>` makes the missing case part of the type.

```csharp
// Before
static Customer? FindCustomer(Guid id) => customers.GetValueOrDefault(id);

// After
static Option<Customer> FindCustomer(Guid id) => customers.GetValueOrNone(id);
```

Callers then choose how to handle it:

```csharp
string greeting = FindCustomer(id).Match(c => $"Hello {c.Name}", () => "Hello guest");
```

At the boundary to code that expects `null`, convert back with `GetValueOrDefault()`.

## Lookups with fallbacks

`Or` tries sources in order and runs each alternative only when the earlier ones found nothing. A common case is a cache in front of a database:

```csharp
static Option<Customer> FindCustomer(Guid id) =>
    cache.Find(id)
        .Or(() => database.Find(id))
        .Do(customer => cache.Store(customer));
```

Because `Do(onSome)` runs only with a value, a miss in both sources does not store anything. This also stores a customer that was already in the cache. To store only after a database hit, put the `Do` inside the alternative:

```csharp
static Option<Customer> FindCustomer(Guid id) =>
    cache.Find(id).Or(() => database.Find(id).Do(cache.Store));
```

Add a last resort with `DefaultIfEmpty`:

```csharp
Customer customer = cache.Find(id)
    .Or(() => database.Find(id))
    .GetValueOrDefault(() => Customer.Guest);
```

## Optional configuration values

`Option.TryParse` and `GetValueOrNone` turn loose configuration into typed options without exceptions.

```csharp
IReadOnlyDictionary<string, string> settings = LoadSettings();

int pageSize = settings.GetValueOrNone("PageSize")
    .Chain(text => Option.TryParse<int>(text))
    .Where(size => size > 0)
    .GetValueOrDefault(20);
```

A missing key, an unparsable value and a non-positive value all give the default of `20`. Add `OnNoneDo` to log the fallback if you need to know.

## Optional DTO fields with JSON

Use `Option<T>` for fields that may be `null` or absent in the payload. Reading `null` gives `None`, and writing `None` gives `null`.

```csharp
public record CustomerDto(string Name, Option<string> Nickname, Option<int> Age);

var dto = JsonSerializer.Deserialize<CustomerDto>("""{"Name":"Ada","Nickname":null,"Age":36}""")!;

string display = dto.Nickname.GetValueOrDefault(dto.Name); // "Ada"
```

A property that is missing from the JSON stays a `default` option, which also holds no value. If you must tell "absent" from "null", an option cannot, because both read as no value. See [serialization](../serialization/README.md) for the details.

## Combining several optional inputs

`Zip` combines two options. Query syntax scales to more, and keeps every earlier value in scope.

```csharp
Option<(Customer First, Product Second)> pair = FindCustomer(customerId).Zip(FindProduct(sku));

Option<Order> order =
    from customer in FindCustomer(customerId)
    from product in FindProduct(sku)
    from address in FindAddress(customer.AddressId)
    select new Order(customer, product, address);
```

The result is `None` when any input is missing. To report which one is missing, use results instead, with `ToResult` for each lookup:

```csharp
Result<Order> order =
    from customer in FindCustomer(customerId).ToResult(() => new KeyNotFoundException("Unknown customer."))
    from product in FindProduct(sku).ToResult(() => new KeyNotFoundException("Unknown product."))
    select new Order(customer, product);
```

## Option, Result or nullable

| Use | When |
| --- | --- |
| `Option<T>` | Missing is a normal outcome and needs no explanation: a lookup, an optional setting, an optional field. |
| `Result<T>` | The operation can fail and the caller should know why: I/O, parsing user input, a rule violation. |
| `T?` | A private detail or an API boundary that expects `null`, such as EF Core properties or other libraries. |

Rules of thumb:

- If the caller would ask "why is it missing?", use a result.
- If every caller treats missing the same way, an option is enough.
- Options are a poor fit for `T?` fields of entities that EF Core maps. Keep a nullable backing field and expose the option as a property. [Entity Framework Core](../entity-framework-core/README.md) shows the mapping.
- The unused-result analyzer does not report ignored options, because a missing value is not an error.

## Pitfalls

- `Or` and `DefaultIfEmpty` take factories, not values, for the lazy case. `DefaultIfEmpty(value)` evaluates `value` immediately.
- `Do` after `Or` runs for values from either source. Place it where you mean it.

## See also

- [Operations](operations.md)
- [Conversions](conversions.md)
- [Validation](../validation/README.md) for reporting several problems at once

---

Back to: [Option](README.md) · Next: [Async](../async/README.md)
