![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.NSwag

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.NSwag.svg)](https://www.nuget.org/packages/Fluent.Unions.NSwag) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.NSwag.svg)](https://www.nuget.org/packages/Fluent.Unions.NSwag) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/openapi/nswag/)

Describes `Option<T>` as a nullable `T` in the documents that NSwag generates. It builds on Fluent.Unions and matches the JSON shape of an option: the value, or `null` for none.

## Installation

```
dotnet add package Fluent.Unions.NSwag
```

## Usage

Register the schema processor on the document settings. Without it, an option is described as an object with `value` and `hasValue`.

```csharp
using Fluent.Unions.NSwag;

builder.Services.AddOpenApiDocument(settings => settings.AddFluentUnions());
```

Options in a request or response body then become nullable types, so generated clients see them.

```csharp
public record Product(string Name, Option<string> Nickname, Option<int> Stock, Option<Address> Shipping);
// "nickname": string, nullable; "stock": integer int32, nullable; "shipping": null or Address
```

A plain `T` (primitives, arrays, lists, dictionaries) gets `nullable: true`. A `T` that NSwag describes through a reference (records, enums, nested options) becomes a `oneOf` of null and that reference. An option property is not required.

Swagger 2 documents (`AddSwaggerDocument`) are not supported: Swagger 2 cannot express this, so the processor throws a `NotSupportedException` instead of writing a wrong schema.

## Documentation

- [NSwag documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/openapi/nswag.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
