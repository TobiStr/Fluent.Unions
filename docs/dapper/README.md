# Dapper

The package `Fluent.Unions.Dapper` (namespace `Fluent.Unions.Dapper`) maps `Option<T>` to and from database `NULL` with [Dapper](https://github.com/DapperLib/Dapper) type handlers. It depends on `Dapper` (Apache-2.0).

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): write and read an option.
- [Registering handlers](#registering-handlers): one type, or the common types.
- [What is mapped](#what-is-mapped): value and `NULL` in both directions.
- [Rules](#rules): limits of Dapper and of the mapping.

## Quick example

```csharp
using Dapper;
using Fluent.Unions.Dapper;

FluentUnionsDapper.Register();

public class PersonRow
{
    public int Id { get; set; }
    public Option<string> Nickname { get; set; }
    public Option<int> Age { get; set; }
}

connection.Execute(
    "INSERT INTO person (id, nickname, age) VALUES (@id, @nickname, @age)",
    new { id = 1, nickname = Option.Some("Ann"), age = (Option<int>)Option.None });

PersonRow person = connection.QuerySingle<PersonRow>("SELECT id, nickname, age FROM person WHERE id = @id", new { id = 1 });
// person.Nickname holds "Ann", person.Age holds no value
```

## Registering handlers

Dapper keeps one handler per closed type, so each `Option<T>` is registered on its own. Do it once at startup, before the first query.

| Member | Registers |
| --- | --- |
| `FluentUnionsDapper.Register<T>()` | a handler for `Option<T>` |
| `FluentUnionsDapper.Register()` | `Option<T>` for bool, byte, short, int, long, float, double, decimal, string, Guid, DateTime, DateTimeOffset, TimeSpan and byte[] |

`OptionTypeHandler<T>` is the handler itself, for `SqlMapper.AddTypeHandler` if you prefer to register it yourself. Registering a type twice is harmless. An enum is registered with `Register<MyEnum>()`; it is written as its underlying integer.

## What is mapped

| Direction | Option | Database |
| --- | --- | --- |
| parameter | `Option.Some(x)` | the value `x` |
| parameter | `(Option<int>)Option.None` or `default` | `NULL` |
| column | `NULL` | `Option.None` |
| column | a value | `Option.Some(value)` |

The parameter keeps its database type for the common types, so a `NULL` option still has a type. For other types the provider infers it from the value.

In an anonymous parameter object, cast `Option.None` to the option type, as in the quick example. A bare `Option.None` has the type `None`, which has no handler.

A column value that is not a `T` is converted: numbers with `Convert`, text with the type's converter (for example text to `Guid`, `DateTime` or `DateTimeOffset` on SQLite), bytes to `Guid`, a `DateTime` to `DateTimeOffset`, and enums from their name (case-insensitive, like plain Dapper) or from a number. A value that cannot be converted throws.

## Rules

- **Register before use.** An unregistered `Option<T>` is not a Dapper type. The error appears when the query runs, not at startup.
- **Use properties, not a positional constructor.** Dapper picks a constructor by the column types and cannot match an `Option<T>` parameter. Properties with `set` or `init`, tuples and single-column queries work.
- **No `null` value.** A `NULL` column is `Option.None`. An empty string is a value.
- **Dapper.AOT is not supported.** It generates the mapping at build time and does not call `SqlMapper` type handlers.
- **Tested on SQLite.** The tests run against an in-memory SQLite database with `Microsoft.Data.Sqlite` (MIT). Other ADO.NET providers receive the same `DbType` and value.

---

Back to index: [Docs index](../README.md)
