![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Mapster

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Mapster.svg)](https://www.nuget.org/packages/Fluent.Unions.Mapster) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Mapster.svg)](https://www.nuget.org/packages/Fluent.Unions.Mapster) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/mapping/)

Maps `Option<T>` properties with Mapster: `T?` to `Option<T>`, `Option<T>` to `T?`, and `Option<TSource>` to `Option<TDestination>`. It builds on Fluent.Unions.

## Installation

```
dotnet add package Fluent.Unions.Mapster
```

## Usage

Register the rules once, on `TypeAdapterConfig.GlobalSettings` or on your own config.

```csharp
using Mapster;
using Fluent.Unions.Mapster;

TypeAdapterConfig.GlobalSettings.AddFluentUnions();

var dto = person.Adapt<PersonDto>();   // string? Nickname -> Option<string> Nickname
var back = dto.Adapt<Person>();        // None -> null
```

`None` and `null` map to each other, and the element is mapped with your configured rules. `Option<int>` to `int` gives `0` for `None`.

Mapster.Tool code generation is not supported for `Option<T>`: the rules use the runtime configuration, which the generator cannot turn into source.

## Documentation

- [Mapping documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/mapping/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
