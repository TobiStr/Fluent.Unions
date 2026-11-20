# MongoDB

The package `Fluent.Unions.MongoDB` (namespace `Fluent.Unions.MongoDB`) stores `Option<T>` as its value or BSON null, and refuses to serialize results. It depends on `MongoDB.Bson` (license Apache-2.0) and works with the official MongoDB .NET driver.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register once and use options in documents.
- [Options](#options): what is written and read for `Option<T>`.
- [None and missing elements](#none-and-missing-elements): BSON null versus a missing element, and how to omit `None`.
- [Results refuse to serialize](#results-refuse-to-serialize): why they throw.
- [Rules](#rules): when to register and what is not supported.

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

FluentUnionsBson.Register();

public class Customer
{
    public string Name { get; set; } = "";

    public Option<string> Nickname { get; set; }
}

new Customer { Name = "Ada", Nickname = "Countess" }.ToBsonDocument(); // { "Name" : "Ada", "Nickname" : "Countess" }
new Customer { Name = "Ada", Nickname = Option.None }.ToBsonDocument(); // { "Name" : "Ada", "Nickname" : null }
```

`Register()` calls `BsonSerializer.RegisterGenericSerializerDefinition(typeof(Option<>), typeof(OptionSerializer<>))` and registers a serialization provider for the result types. Call it once at startup, before the first class map or serializer lookup. Repeated calls do nothing.

## Options

An option is written as its value, or as BSON null when it holds none. Reading BSON null gives `None`. The value is written by the serializer registered for `T`, so enums, nested classes and lists follow your existing class maps and conventions. This works for class-mapped properties, positional records and lists.

No attribute is needed on the properties. The driver finds `OptionSerializer<T>` through the registration, so it does not try to map the members of the union itself.

To use a specific serializer for the value, create `OptionSerializer<T>` with it:

```csharp
new OptionSerializer<Level>(new EnumSerializer<Level>(BsonType.String))
```

## None and missing elements

| Value in the document | Reads as |
| --- | --- |
| a value | the value |
| BSON null | `None` |
| missing element | `default`, which holds no value |

A `default` option has `HasValue` equal to `false`, so a missing element and BSON null both read as "no value".

By default, `None` and `default` are both written as BSON null. `BsonIgnoreIfNull` never leaves out an option, because it is a struct, and `BsonIgnoreIfDefault` leaves out only `default`, not `None`. To omit the element instead, add `OmitNoneConvention`:

```csharp
using MongoDB.Bson.Serialization.Conventions;

ConventionRegistry.Register(
    "OmitNone",
    new ConventionPack { new OmitNoneConvention() },
    _ => true);
```

With the convention, every `Option<T>` member without a value is left out of the document, and reads back as no value. Register the convention before the first class map is created, like `Register()`. Restrict it with the filter to the types you want.

## Results refuse to serialize

`Result`, `Result<T>` and `Result<T, TError>` throw `NotSupportedException` when written and when read. Without that, an error would expose its message, stack trace and target site in the stored document. When a result is a member of a class, the driver wraps the exception in a `BsonSerializationException` (writing) or a `FormatException` (reading). The `NotSupportedException` is its inner exception.

Convert a result before it reaches the serializer, for example with `Match`, `GetValueOrDefault()` or `ToOption()`. See [Serialization](../serialization/README.md#results-refuse-to-serialize).

## When to use

- Documents with optional fields that must round-trip with the MongoDB .NET driver.
- Not for results. Convert them first.

## Rules

- **Register early.** The driver caches serializers and class maps. A type whose class map was built before `Register()` keeps the old serializer for its options.
- **Both registrations are global.** `BsonSerializer` has no scope, so the serializers apply to the whole process.
- **`Option<T>` is not queried specially.** LINQ and filter builders see the member as the serializer writes it: the value, or null for `None`.
- **Nested options flatten.** `Option<Option<T>>` loses the difference between its levels, as with `System.Text.Json`.
- **Reflection.** The registrations and the convention use reflection. AOT and trimming have not been tested.

---

Back to index: [Docs index](../README.md)
