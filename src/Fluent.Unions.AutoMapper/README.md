![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.AutoMapper

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.AutoMapper.svg)](https://www.nuget.org/packages/Fluent.Unions.AutoMapper) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.AutoMapper.svg)](https://www.nuget.org/packages/Fluent.Unions.AutoMapper) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/mapping/)

Maps `Option<T>` properties with AutoMapper: `T?` to `Option<T>`, `Option<T>` to `T?`, and `Option<TSource>` to `Option<TDestination>`. It builds on Fluent.Unions.

> **AutoMapper licensing.** This package depends on AutoMapper 17, which Lucky Penny Software dual licenses: the Reciprocal Public License 1.5 (RPL-1.5) or a commercial agreement. Read the [AutoMapper terms](https://luckypennysoftware.com/license) before you use it. AutoMapper may ask for a license key and may log a warning without one.

## Installation

```
dotnet add package Fluent.Unions.AutoMapper
```

## Usage

```csharp
using AutoMapper;
using Fluent.Unions.AutoMapper;

var configuration = new MapperConfiguration(cfg =>
{
    cfg.AddFluentUnions();
    cfg.CreateMap<Address, AddressDto>();
    cfg.CreateMap<Person, PersonDto>().ReverseMap();   // string? Nickname <-> Option<string> Nickname
});

configuration.AssertConfigurationIsValid();
```

`None` and `null` map to each other, and the element is mapped with the maps you create. `Option<int>` to `int` gives `0` for `None`. `AssertConfigurationIsValid` reports a missing element map, such as `Address` to `AddressDto`.

## Documentation

- [Mapping documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/mapping/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
