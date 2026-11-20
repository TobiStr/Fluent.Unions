![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Dapper

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Dapper.svg)](https://www.nuget.org/packages/Fluent.Unions.Dapper) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Dapper.svg)](https://www.nuget.org/packages/Fluent.Unions.Dapper) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/dapper/)

Maps `Option<T>` to and from database `NULL` with Dapper type handlers: no value is `NULL`, a value is the value. It builds on Fluent.Unions and Dapper (Apache-2.0).

## Installation

```
dotnet add package Fluent.Unions.Dapper
```

## Usage

Register a handler once at startup for every `T` you use in an `Option<T>`, or register the common database types at once.

```csharp
using Fluent.Unions.Dapper;

OptionHandlers.Register<int>();
OptionHandlers.Register<OrderStatus>();
OptionHandlers.RegisterCommon(); // bool, byte, short, int, long, float, double, decimal, string, Guid, DateTime, DateTimeOffset, TimeSpan, byte[]
```

Use an option as a parameter and as a column.

```csharp
connection.Execute("UPDATE person SET age = @age WHERE id = @id", new { id, age = Option.Some(30) });
// age = Option.None writes NULL

public class PersonRow
{
    public int Id { get; set; }
    public Option<string> Nickname { get; set; }
    public Option<int> Age { get; set; }
}

IEnumerable<PersonRow> people = connection.Query<PersonRow>("SELECT id, nickname, age FROM person");
```

Dapper cannot fill the constructor of a positional record from an `Option<T>` column. Use properties with `set` or `init`.

Dapper.AOT is not supported. It generates its own mapping code and does not call these handlers.

## Documentation

- [Dapper documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/dapper/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
