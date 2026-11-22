# OpenAPI

The package `Fluent.Unions.OpenApi` (namespace `Fluent.Unions.OpenApi`) describes `Option<T>` as a nullable `T` in the documents that `Microsoft.AspNetCore.OpenApi` generates. That is the shape [`OptionJsonConverter`](../serialization/README.md) writes: the value, or `null` for none.

## In this section

This folder has three pages: this one for Microsoft.AspNetCore.OpenApi, [Swashbuckle](swashbuckle.md) and [NSwag](nswag.md). The topics of this page are below.

- [Quick example](#quick-example): register the transformer and see the schema.
- [What you get](#what-you-get): the schema for each kind of `T`.
- [When to use](#when-to-use): where it helps.
- [Rules](#rules): what is changed and what is not.

## Quick example

```csharp
using Fluent.Unions.OpenApi;

builder.Services.AddOpenApi(options => options.AddFluentUnions());

app.MapOpenApi();
app.MapGet("/products/{id}", (int id) => GetProduct(id));

public record Product(string Name, Option<string> Nickname, Option<int> Stock);
// "nickname": type ["null", "string"], "stock": type ["null", "integer", "string"] with format int32
```

Without `AddFluentUnions()` the schema of an option is an object without a type, so clients and generators see an untyped value.

## What you get

| Property type | Schema |
| --- | --- |
| `Option<string>` | `string` or `null` |
| `Option<int>` | the schema ASP.NET Core gives `int`, with `null` added |
| `Option<Address>` | the properties of `Address`, with `null` added |
| `Option<Node>` inside `Node` | an object or `null`; the nested option points to that same schema through a `$ref` |
| `Option<Color>` for an enum written as strings, `Option<object>` | `oneOf` of null and the schema of `T`, because that schema has no `type` |
| any other type | unchanged |

`Option<int>` keeps the `integer` or `string` type and the pattern that ASP.NET Core writes for numbers, because it reads numbers from strings by default.

## When to use

- You expose `Option<T>` in a request or response body and publish an OpenAPI document with `Microsoft.AspNetCore.OpenApi`.
- You generate clients from that document and want an option to become a nullable type.

## Rules

- **Nullable, not optional.** An option property stays in `required`, because the serializer always writes it, as `null` for none.
- **Only `Option<T>` is changed.** An option used as a route or query parameter is not described yet. The responses of an endpoint that returns a result come from [`WithResultMapping`](../aspnetcore/endpoint-filter.md#openapi) in Fluent.Unions.AspNetCore.
- **Microsoft.AspNetCore.OpenApi on this page.** For Swashbuckle see [Swashbuckle](swashbuckle.md), for NSwag see [NSwag](nswag.md). Those two leave an option property out of `required`, as they do for every struct property.
- **Package version.** It builds on `Microsoft.AspNetCore.OpenApi` 11.0.0, which uses the `Microsoft.OpenApi` 3.x model.

---

Back to index: [Docs index](../README.md)
