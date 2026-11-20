# Swashbuckle

The package `Fluent.Unions.Swashbuckle` (namespace `Fluent.Unions.Swashbuckle`) describes `Option<T>` as a nullable `T` in the documents that `Swashbuckle.AspNetCore` generates. That is the shape [`OptionJsonConverter`](../serialization/README.md) writes: the value, or `null` for none. For `Microsoft.AspNetCore.OpenApi` see the [OpenAPI page](README.md).

## In this section

- [Quick example](#quick-example): register the schema filter and see the schema.
- [What you get](#what-you-get): the schema for each kind of `T`.
- [Rules](#rules): what is changed and what is not.

## Quick example

```csharp
using Fluent.Unions.Swashbuckle;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddFluentUnions());

app.UseSwagger();
app.MapGet("/products/{id}", (int id) => GetProduct(id));

public record Product(string Name, Option<string> Nickname, Option<int> Stock);
// "nickname": type string, nullable; "stock": type integer, format int32, nullable
```

`AddFluentUnions()` adds one `ISchemaFilter`. Without it an option is not described as a nullable type, so generated clients see an untyped value.

## What you get

| Property type | Schema |
| --- | --- |
| `Option<string>`, `Option<int>` | the schema Swashbuckle gives `T`, nullable |
| `Option<List<string>>`, `Option<Address[]>` | the array schema, nullable |
| `Option<Address>`, `Option<Color>` | `oneOf` of null and a `$ref` to the schema of `T` |
| `Option<Option<int>>` | `oneOf` of null and a `$ref` to the schema of `Option<int>` |
| `Node` with an `Option<Node>` inside | the nested option points back to `Node` through a `$ref` |
| any other type | unchanged |

Nullable is written the way the document version says: `nullable: true` in an OpenAPI 3.0 document, and `null` in the `type` array in a 3.1 document (`UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1)`). The null branch of a `oneOf` follows the same rule.

## Rules

- **Nullable and not required.** Swashbuckle does not mark a struct property as required, so an option property is not in `required`, and a client treats it as optional and nullable.
- **Only `Option<T>` is changed.** `Result` types, and an option used as a route or query parameter, are not described yet.
- **Swashbuckle only.** For `Microsoft.AspNetCore.OpenApi` use `Fluent.Unions.OpenApi`, for NSwag see the [NSwag page](nswag.md).
- **Package version.** It builds on `Swashbuckle.AspNetCore.SwaggerGen` 11.0.1 (MIT), which uses the `Microsoft.OpenApi` 3.x model.

---

Back to index: [Docs index](../README.md)
