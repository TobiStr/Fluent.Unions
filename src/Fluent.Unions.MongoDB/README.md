![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.MongoDB

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.MongoDB.svg)](https://www.nuget.org/packages/Fluent.Unions.MongoDB) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.MongoDB.svg)](https://www.nuget.org/packages/Fluent.Unions.MongoDB) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/mongodb/)

Stores `Option<T>` in MongoDB documents as its value or BSON null, with a BSON serializer for the official MongoDB .NET driver. It builds on Fluent.Unions and `MongoDB.Bson` (Apache-2.0).

## Installation

```
dotnet add package Fluent.Unions.MongoDB
```

## Usage

Register once at startup, before the first class map is created. No attribute is needed on properties.

```csharp
using Fluent.Unions;
using Fluent.Unions.MongoDB;
using MongoDB.Bson;

FluentUnionsBson.Register();

public class Customer
{
    public string Name { get; set; } = "";

    public Option<string> Nickname { get; set; }
}

new Customer { Name = "Ada", Nickname = Option.None }.ToBsonDocument(); // { "Name" : "Ada", "Nickname" : null }
```

`None` is written as BSON null. Reading BSON null or a missing element gives an option without a value. To leave out the element instead, register `OmitNoneConvention` in a `ConventionPack`.

`Result`, `Result<T>` and your own result types declared with [`[FluentResult<TError>]`](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md) are not serialized: the serializer throws `NotSupportedException`, because an error would expose exception details. Convert a result first.

The serializers use reflection and are not supported in trimmed or native AOT apps. `FluentUnionsBson.Register()` carries `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`. See [AOT and trimming](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/getting-started/aot-and-trimming.md).
>>>>


## Documentation

- [MongoDB documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/mongodb/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
