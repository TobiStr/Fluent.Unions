# Serialization

`System.Text.Json` works with options and your own unions. Results, including your own result types, are deliberately not serialized. This page covers what is written and read for each, and the limits.

## In this section

This folder has one page. The topics are below.

- [Options](#options): written as the value or `null`.
- [Results](#results-refuse-to-serialize): why they throw, and how to convert first.
- [Your own unions](#your-own-unions): `UnionJsonConverter` with `$type` and `$value`.
- [AOT and trimming](#aot-and-trimming)

## Quick example

```csharp
using System.Text.Json;
using Fluent.Unions;

public record CustomerDto(string Name, Option<string> Nickname);

JsonSerializer.Serialize(new CustomerDto("Ada", Option.None)); // {"Name":"Ada","Nickname":null}
JsonSerializer.Serialize(new CustomerDto("Ada", "Countess"));  // {"Name":"Ada","Nickname":"Countess"}
```

## When to use

- Optional fields in request and response DTOs: `Option<T>`.
- Unions of your own, such as events or commands, that must round-trip: `UnionJsonConverter`.
- Never for `Result<T>`. Convert to a DTO first.

## Rules

- `Option<T>` needs no registration. The converter is applied by attribute.
- A missing property reads as a `default` option, which holds no value.
- `WhenWritingNull` does not omit `None`. `WhenWritingDefault` omits every option without a value, because `None` equals `default`.
- `Option<Option<T>>` is not a round-trip type: it loses the difference between its levels.
- `Result` and `Result<T>` throw `NotSupportedException` in both directions.
- The factories use reflection. Trimmed and native AOT apps register the generic converters, see [AOT and trimming](#aot-and-trimming).

## Options

An option is written as its value, or as `null` when it holds none. Reading `null` gives `None`. This works at the root, in properties, in arrays and in dictionaries.

```csharp
Option<int>[] options = [1, Option.None, 3];

JsonSerializer.Serialize(options);                          // [1,null,3]
JsonSerializer.Deserialize<Option<int>[]>("[1,null,3]");    // 1, None, 3
```

The converter is applied by an attribute on `Option<T>`, so nothing needs to be added to `JsonSerializerOptions`.

### Missing properties

A property that is absent from the JSON is not read by the converter, so it stays `default`. A `default` option holds no value, so `HasValue` is `false`. The difference from `None` is internal. The library treats both the same.

```csharp
var dto = JsonSerializer.Deserialize<CustomerDto>("""{"Name":"Ada"}""")!;

dto.Nickname.HasValue; // false
```

Absent and `null` therefore read as "no value". An option cannot tell them apart in a useful way.

### Ignore conditions

An option is a struct, so `null` checks do not apply to it.

| Setting | `None` | `default` |
| --- | --- | --- |
| none | `"Nickname":null` | `"Nickname":null` |
| `WhenWritingNull` | `"Nickname":null` | `"Nickname":null` |
| `WhenWritingDefault` | property omitted | property omitted |

```csharp
var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

JsonSerializer.Serialize(new CustomerDto("Ada", Option.None), options); // {"Name":"Ada"}
JsonSerializer.Serialize(new CustomerDto("Ada", default), options);     // {"Name":"Ada"}
```

Do not rely on `WhenWritingNull` to omit options. If you must omit absent fields, map to a DTO with nullable properties.

### Nested options

`Option<Option<T>>` writes `null` for both `None` and `Some(None)`, so reading gives `None` either way. It is not supported as a round-trip type in JSON, MessagePack or MongoDB; only Orleans keeps both levels. Flatten with `Unwrap()` before you serialize, or use a DTO with an explicit flag.

### Source-generated contexts

With a source-generated `JsonSerializerContext`, register `T` as well as `Option<T>`. The converter serializes the value through the options, so the context must know `T`.

```csharp
[JsonSerializable(typeof(CustomerDto))]
[JsonSerializable(typeof(string))]
public partial class AppJsonContext : JsonSerializerContext;
```

## Results refuse to serialize

`JsonSerializer` throws `NotSupportedException` for `Result` and `Result<T>`, when writing and when reading. `ResultJsonConverter` refuses every result type, so the same holds for your own [result type](../errors/custom-error-types.md) declared with `[FluentResult<TError>]`, like the default type. Integration authors can recognize result types with `Fluent.Unions.Abstractions.ResultTypes` (`IsResult(Type)`, `ErrorType(Type)`, `ValueType(Type)`). Without that, an error would be written with its message, stack trace and target site, which leaks internals to clients.

```csharp
Result<int> error = new InvalidOperationException("Secret");

JsonSerializer.Serialize(error); // throws NotSupportedException, the message does not contain "Secret"
```

Convert a result to a DTO first. Choose what the client sees:

```csharp
record OrderResponse(Order? Order, string? Error);

OrderResponse response = LoadOrder(id).Match(
    order => new OrderResponse(order, null),
    error => new OrderResponse(null, "The order could not be loaded."),
    () => new OrderResponse(null, "Not found."));
```

Other ways out are `GetValueOrDefault()` and `ToOption()`. In ASP.NET Core, use `ToHttpResult()`, which writes the value or ProblemDetails. See [ASP.NET Core](../aspnetcore/README.md).

## Your own unions

By default, `System.Text.Json` writes only the value of a union, which cannot be read back when several cases look alike. `UnionJsonConverter` also writes the case name:

```csharp
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;

public record Circle(double Radius);
public record Square(double Side);

[JsonConverter(typeof(UnionJsonConverter))]
public union Shape(Circle, Square);

JsonSerializer.Serialize<Shape>(new Circle(2));
// {"$type":"Circle","$value":{"Radius":2}}

Shape shape = JsonSerializer.Deserialize<Shape>("""{"$type":"Square","$value":{"Side":3}}""");
```

Behavior:

| Payload | Result |
| --- | --- |
| `{"$type":...,"$value":...}`, properties in any order | the case. Other properties are ignored. |
| `null` | an empty union, which is also how an empty union is written |
| a missing, `null` or unknown `$type` | `JsonException` |
| a missing `$value` | `JsonException` |
| a `$value` that gives an empty union, such as `null` for a class case | `JsonException`. A case whose own converter reads `null`, such as `Option<T>`, still reads it. |
| a number, string or array instead of an object | `JsonException` |

The converter handles union structs only.

### Stable case names

By default a case is named after its type, such as `Circle` or ``List`1``. Renaming the type changes the name, so stored payloads stop reading, and two cases with the same short name, such as `List<int>` and `List<string>` or `Orders.Item` and `Billing.Item`, cannot be told apart. A union with such a clash throws `InvalidOperationException` when the converter is created, and the message names both types.

Give a case a name that does not depend on its type with `[UnionCaseName]` on the union. `Aliases` are names that are still read:

```csharp
[JsonConverter(typeof(UnionJsonConverter))]
[UnionCaseName(typeof(Circle), "circle", Aliases = ["Circle"])]
[UnionCaseName(typeof(List<int>), "numbers")]
[UnionCaseName(typeof(List<string>), "words")]
public union Drawing(Circle, List<int>, List<string>);
```

To rename a case type safely, add `[UnionCaseName(typeof(Round), "Circle")]` before you rename `Circle` to `Round`: the payloads keep the old name. To move to a new name, write it as the `Name` and keep the old one in `Aliases` until every stored payload is rewritten. Cases without the attribute keep the name of their type, so adding the attribute to one case never changes the others.

Every name and alias must be unique within the union. A clash, an empty name, a type that is not a case, or two attributes for one case throw `InvalidOperationException` when the converter is created. The Newtonsoft.Json converter reads the same attribute.

The constants `UnionJsonConverter.TypeProperty` and `UnionJsonConverter.ValueProperty` hold the two property names. See [your own unions](../unions/README.md) for `Is` and `As`.

## AOT and trimming

Register `OptionJsonConverter<T>` for each value type of an option and apply `UnionJsonConverter<Shape>` to each union, instead of relying on the factories. Both write the same JSON. A source-generated context must also register every value type and union case type. In a native AOT app the factories throw a `NotSupportedException` that names the converter to register. See [AOT and trimming](../getting-started/aot-and-trimming.md) for the tested combinations.

## Pitfalls

- `JsonIgnoreCondition.WhenWritingNull` does not omit `None`.
- A source-generated context without `T` registered fails at runtime for `Option<T>`.
- Serializing a `Result<T>`, or your own result type, throws. Convert it before the serializer sees it.
- Two union cases with the same type name cannot be told apart without `[UnionCaseName]`.
- Renaming a case type changes its name in JSON. Pin the old name with `[UnionCaseName]` first.

## See also

- [Option conversions](../option/conversions.md)
- [Your own unions](../unions/README.md)
- [ASP.NET Core](../aspnetcore/README.md)

---

Back to index: [Docs index](../README.md) · Next: [Errors](../errors/README.md)
