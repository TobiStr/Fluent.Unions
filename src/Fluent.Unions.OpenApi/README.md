![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.OpenApi

Describes `Option<T>` as a nullable `T` in the documents that Microsoft.AspNetCore.OpenApi generates. It builds on Fluent.Unions and matches the JSON shape of an option: the value, or `null` for none.

## Installation

```
dotnet add package Fluent.Unions.OpenApi
```

## Usage

Register the schema transformer. Without it, an option is an object without a type.

```csharp
using Fluent.Unions.OpenApi;

builder.Services.AddOpenApi(options => options.AddFluentUnions());

app.MapOpenApi();
```

Options in a request or response body then become nullable types, so generated clients see them.

```csharp
public record Product(string Name, Option<string> Nickname, Option<int> Stock);
// "nickname": type ["null", "string"], "stock": type ["null", "integer", "string"] with format int32
```

An option property stays in `required`, because it is always written, as `null` for none. Swashbuckle and NSwag are not supported.

## Documentation

- [OpenAPI documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/openapi/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
