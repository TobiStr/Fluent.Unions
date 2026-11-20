![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Swashbuckle

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Swashbuckle.svg)](https://www.nuget.org/packages/Fluent.Unions.Swashbuckle) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Swashbuckle.svg)](https://www.nuget.org/packages/Fluent.Unions.Swashbuckle) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/openapi/swashbuckle/)

Describes `Option<T>` as a nullable `T` in the documents that Swashbuckle.AspNetCore generates. It builds on Fluent.Unions and matches the JSON shape of an option: the value, or `null` for none.

## Installation

```
dotnet add package Fluent.Unions.Swashbuckle
```

## Usage

Register the schema filter. Without it, an option is not described as a nullable type.

```csharp
using Fluent.Unions.Swashbuckle;

builder.Services.AddSwaggerGen(options => options.AddFluentUnions());
```

Options in a request or response body then become nullable types, so generated clients see them.

```csharp
public record Product(string Name, Option<string> Nickname, Option<int> Stock, Option<Address> Shipping);
// "nickname": string, nullable; "stock": integer int32, nullable; "shipping": null or Address
```

A plain `T` (primitives, arrays, lists) gets `nullable` in an OpenAPI 3.0 document and `null` in the type in a 3.1 document. A `T` that Swashbuckle describes through a reference (records, enums, nested options) becomes a `oneOf` of null and that reference. An option property is not required, as for any value type in Swashbuckle.

## Documentation

- [Swashbuckle documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/openapi/swashbuckle.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
