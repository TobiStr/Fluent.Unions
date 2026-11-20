# Entity Framework Core

EF Core cannot map an `Option<T>` property to a nullable column with a value converter. This page explains why and shows the pattern that works: a nullable backing field mapped to the column, with the option as the public property. There is no Fluent.Unions package for EF Core, and none is needed.

## In this section

This folder has one page, this one.

## Why a value converter fails

EF decides whether a column is nullable from the CLR type of the property. `Option<T>` is a struct, so it is non-nullable to EF, and EF refuses to make such a property optional. A converter such as this one builds:

```csharp
b.Entity<Customer>().Property(c => c.Nickname)
    .HasConversion(o => o.GetValueOrDefault(), s => s.ToOption());
```

The column is `NOT NULL`, and saving `None` fails:

```
DbUpdateException: SQLite Error 19: 'NOT NULL constraint failed: Customers.Nickname'.
```

Marking the property optional does not help. `IsRequired(false)` throws when the model is built:

```
InvalidOperationException: The property 'Customer.Nickname' cannot be marked as nullable/optional because
the type of the property is 'Option<string>' which is not a nullable type. ...
```

The messages are shortened.

## The backing-field pattern

Keep a nullable field that EF maps to the column, and expose the option as a property that EF ignores. The same shape works for reference types and value types:

| Type | Backing field | Getter | Setter |
| --- | --- | --- | --- |
| reference type, such as `string` | `string?` | `field.ToOption()` | `value.GetValueOrDefault()` |
| value type, such as `int` | `int?` | `field.ToOption()` | `value.ToNullable()` |

`ToOption()` turns a `null` into `None` for both kinds of field. `GetValueOrDefault()` on a `None` gives `null` for a reference type. `ToNullable()` is for options of structs, and gives `null` for `None`.

## A full example

```csharp
using Microsoft.EntityFrameworkCore;
using Fluent.Unions;

public class Customer
{
    private string? nickname;
    private int? age;

    public int Id { get; set; }
    public required string Name { get; set; }

    public Option<string> Nickname { get => nickname.ToOption(); set => nickname = value.GetValueOrDefault(); }
    public Option<int> Age { get => age.ToOption(); set => age = value.ToNullable(); }
}

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Ignore(c => c.Nickname).Ignore(c => c.Age);
            customer.Property<string?>("nickname").HasColumnName("Nickname");
            customer.Property<int?>("age").HasColumnName("Age");
        });
    }
}
```

What each part does:

- `Ignore` keeps EF away from the option properties.
- `Property<string?>("nickname")` maps the private field of that name as a field-only property.
- `HasColumnName` gives the column the public name.

The columns are nullable, and `None` is stored as `NULL`:

```csharp
db.Customers.Add(new Customer { Name = "Ada", Nickname = "Countess", Age = 36 });
db.Customers.Add(new Customer { Name = "Alan" });
await db.SaveChangesAsync();

var alan = await db.Customers.SingleAsync(c => c.Name == "Alan");
alan.Nickname.HasValue; // false
alan.Age.HasValue;      // false
```

## Querying

EF cannot translate the option property. Filter on the field name with `EF.Property`:

```csharp
var withNickname = await db.Customers
    .Where(c => EF.Property<string?>(c, "nickname") != null)
    .ToListAsync();

var over30 = await db.Customers
    .Where(c => EF.Property<int?>(c, "age") > 30)
    .ToListAsync();
```

Once the entities are loaded, use the option as usual: `ada.Nickname.GetValue()`, `ada.Age.Select(a => a + 1)`.

## When to use

- You want `Option<T>` in the domain model, and the database already stores `NULL` for "no value".
- You do not want `null` to leak out of the entity's public surface.

## Rules

- **Do not use `HasConversion` for `Option<T>`.** It gives a `NOT NULL` column, and `IsRequired(false)` throws.
- **Ignore the option property and map the field.** If you forget `Ignore`, EF throws `InvalidOperationException` ("union types are not supported") when it first uses the model.
- **The field name is a string.** `Property<string?>("nickname")` and `EF.Property<string?>(c, "nickname")` must match the field name, and a rename is not caught by the compiler. Put the name in a constant if you use it in several places.
- **Query on the field, not on the property.** `Where(c => c.Nickname.HasValue)` fails with "could not be translated".
- **Compare the field with `null`, not with `None`.** The field holds the raw value.
- **Verified with SQLite.** The pattern was run against EF Core 11 with the SQLite provider. Other providers are expected to behave the same, because the mapping is of plain nullable columns, but they were not run.

---

Back to index: [Docs index](../README.md) · Next: [Analyzer](../analyzer/README.md)
