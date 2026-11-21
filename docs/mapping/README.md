# Mapping

The packages `Fluent.Unions.Mapster` and `Fluent.Unions.AutoMapper` (namespaces `Fluent.Unions.Mapster` and `Fluent.Unions.AutoMapper`) teach [Mapster](https://github.com/MapsterMapper/Mapster) and [AutoMapper](https://automapper.io) to map `Option<T>` properties. Install the one for the mapper you use.

> **AutoMapper licensing.** `Fluent.Unions.AutoMapper` depends on AutoMapper 17, which Lucky Penny Software dual licenses: the Reciprocal Public License 1.5 (RPL-1.5) or a commercial agreement. AutoMapper may ask for a license key and may log a warning without one. Read the [AutoMapper terms](https://luckypennysoftware.com/license) first. Mapster is MIT.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register the rules and map a DTO.
- [What is mapped](#what-is-mapped): each source and destination pair.
- [Mapster](#mapster): registration, element rules and code generation.
- [AutoMapper](#automapper): registration and configuration validation.
- [Not included](#not-included): why `Result<T>` has no mapping.
- [Rules](#rules): behavior and limits.

## Quick example

```csharp
public class Person  { public string? Nickname { get; set; } public int? Age { get; set; } }
public class PersonDto { public Option<string> Nickname { get; set; } public Option<int> Age { get; set; } }
```

Mapster:

```csharp
using Mapster;
using Fluent.Unions.Mapster;

TypeAdapterConfig.GlobalSettings.AddFluentUnions();

PersonDto dto = person.Adapt<PersonDto>();   // null -> None, "A" -> Some("A")
Person back = dto.Adapt<Person>();           // None -> null
```

AutoMapper:

```csharp
using AutoMapper;
using Fluent.Unions.AutoMapper;

var configuration = new MapperConfiguration(cfg =>
{
    cfg.AddFluentUnions();
    cfg.CreateMap<Person, PersonDto>().ReverseMap();
});
configuration.AssertConfigurationIsValid();

PersonDto dto = configuration.CreateMapper().Map<PersonDto>(person);
```

## What is mapped

| Source | Destination | Result |
| --- | --- | --- |
| `T?` (class or struct) | `Option<T>` | `null` gives `None`, else `Some` of the mapped value |
| `Option<T>` | `T?` (class or struct) | `None` gives `null`, else the mapped value |
| `Option<TSource>` | `Option<TDestination>` | `None` stays `None`, else the element mapped to `TDestination` |
| `Option<T>` | non-nullable struct | `None` gives `default`, else the mapped value |

The element is mapped with the configuration you already have, so `Option<Address>` to `Option<AddressDto>` uses your `Address` to `AddressDto` map, and `Option<int>` to `Option<long>` converts the number. Collections such as `List<string?>` to `List<Option<string>>` work in both directions. A mapped value that is `null` becomes `None`.

## Mapster

`AddFluentUnions()` extends a `TypeAdapterConfig` with one rule for every pair that involves `Option<T>`. It works on `TypeAdapterConfig.GlobalSettings` and on a config you own, and calling it again on the same config adds nothing. `config.Compile()` validates the setup.

- **Element rules apply.** A `NewConfig<Address, AddressDto>()` rule with custom members is used inside the option.
- **Code generation is not supported.** Mapster.Tool (`dotnet mapster`) renders expression trees to source. The rules call back into the runtime `TypeAdapterConfig`, which the generator cannot render, so do not generate mappers for types with `Option<T>` members. Runtime mapping is unaffected. ASSUMPTION: this is based on how the generator works, it was not run against these rules.

## AutoMapper

`AddFluentUnions()` registers one mapper at the front of AutoMapper's mapper list, so it takes part in every map without an explicit `ConvertUsing`. Calling it again adds nothing.

- **Validation sees the element.** `AssertConfigurationIsValid()` checks the element pair. A missing `Address` to `AddressDto` map is reported for the member, as for a plain property.
- **License key and logging.** Pass a logger factory to the `MapperConfiguration` constructor, or configure AutoMapper through your container, to control the license log output.

## Not included

There is no mapping from `Result<TSource>` to `Result<TDestination>`. It is not a property type in a DTO, and the one-line `result.Select(value => value.Adapt<Dto>())` (Mapster) or `result.Select(value => mapper.Map<Dto>(value))` (AutoMapper) already maps the value and keeps the error without a new rule.

## Rules

- **`None` and `null` are the same state.** There is no `Some(null)`. Mapping never produces a value from `null`.
- **Mapping onto an existing object replaces the option.** The old value is not merged.
- **Errors are exceptions.** A failing element mapping throws the mapper's exception. Nothing is captured into a result.

---

Back to index: [Docs index](../README.md)
