# NSwag

The package `Fluent.Unions.NSwag` (namespace `Fluent.Unions.NSwag`) describes `Option<T>` as a nullable `T` in the documents that `NSwag` generates. That is the shape [`OptionJsonConverter`](../serialization/README.md) writes: the value, or `null` for none. For `Microsoft.AspNetCore.OpenApi` see the [OpenAPI page](README.md).

## In this section

- [Quick example](#quick-example): register the schema processor and see the schema.
- [What you get](#what-you-get): the schema for each kind of `T`.
- [Rules](#rules): what is changed and what is not.

## Quick example

```csharp
using Fluent.Unions.NSwag;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(settings => settings.AddFluentUnions());

app.UseOpenApi();
app.MapGet("/products/{id}", (int id) => GetProduct(id));

public record Product(string Name, Option<string> Nickname, Option<int> Stock);
// "nickname": type string, nullable; "stock": type integer, format int32, nullable
```

`AddFluentUnions()` adds one `ISchemaProcessor` to the schema settings of the document. Without it an option is an object with the properties `value` and `hasValue`, which is not what is written as JSON.

## What you get

| Property type | Schema |
| --- | --- |
| `Option<string>`, `Option<int>` | the schema NSwag gives `T`, `nullable: true` |
| `Option<List<string>>`, `Option<Address[]>` | the array schema, nullable |
| `Option<Dictionary<string, int>>` | the dictionary schema, nullable |
| `Option<Address>`, `Option<Color>` | `oneOf` of null and a `$ref` to the schema of `T` |
| `Option<Option<int>>` | `oneOf` of null and a `$ref` to the schema of `Option<int>` |
| `Node` with an `Option<Node>` inside | the nested option points back to `Node` through a `$ref` |
| any other type | unchanged |

The null branch is `nullable: true` with `enum: [null]`, because an OpenAPI 3.0 schema has no `null` type. The processor works with the System.Text.Json and the Newtonsoft.Json schema settings, as long as the document is an OpenAPI 3 document.

## Rules

- **Nullable and not required.** NSwag does not mark an option property as required, and the processor does not add it to `required`.
- **No Swagger 2.** A Swagger 2 document (`AddSwaggerDocument`) cannot describe a nullable reference. The processor throws a `NotSupportedException` when it meets an `Option<T>` there, instead of writing a schema that loses the null.
- **Only `Option<T>` is changed.** An option used as a route or query parameter is not described yet. The responses of an endpoint that returns a result come from [`WithResultMapping`](../aspnetcore/endpoint-filter.md#openapi) in Fluent.Unions.AspNetCore.
- **Why a processor.** NJsonSchema maps a single closed type through an `ITypeMapper`, so it cannot match every `Option<T>`. A schema processor sees each one.
- **Package version.** It builds on `NSwag.Generation` 14.8.0 and NJsonSchema 11.6.2 (MIT).

---

Back to index: [Docs index](../README.md)
