![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Orleans

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Orleans.svg)](https://www.nuget.org/packages/Fluent.Unions.Orleans) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Orleans.svg)](https://www.nuget.org/packages/Fluent.Unions.Orleans) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/orleans/)

[Orleans](https://github.com/dotnet/orleans) (MIT) serialization for `Option<T>`, so an option can be a grain argument, a grain result, or part of grain state. A surrogate converter carries the option; results refuse to serialize.

## Installation

```
dotnet add package Fluent.Unions.Orleans
```

## Usage

Register it with the Orleans serializer, in the silo and in every client.

```csharp
using Fluent.Unions.Orleans;

builder.Services.AddSerializer(serializer => serializer.AddFluentUnions());
```

Then use `Option<T>` like any other type.

```csharp
public interface IUserGrain : IGrainWithStringKey
{
    Task<Option<string>> GetNickname();
    Task SetNickname(Option<string> nickname);
}

[GenerateSerializer]
public sealed record Profile([property: Id(0)] string Name, [property: Id(1)] Option<string> Nickname);
```

## Rules

- **Wire format.** An option is stored as `OptionSurrogate<T>`: `HasValue` (field 0) and `Value` (field 1). `None` and `default` are the same on the wire.
- **Results throw.** `Result`, `Result<T>` and your own result types declared with [`[FluentResult<TError>]`](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md) throw `NotSupportedException` when serialized. Inside a silo, a local call also refuses to copy `Result` and `Result<T>`, but copies your own result type. An error would carry the exception details. Return an `Option<T>` or a DTO from a grain, or let the grain throw: Orleans carries exceptions by itself.
- **Code generation.** The Orleans code generator only sees the surrogate struct, so it does not depend on how C# 15 unions are compiled. AOT and trimming are not tested.

## Documentation

- [Orleans documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/orleans/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
