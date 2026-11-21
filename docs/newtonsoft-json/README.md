# Newtonsoft.Json

The package `Fluent.Unions.NewtonsoftJson` (namespace `Fluent.Unions.NewtonsoftJson`) gives [Newtonsoft.Json](https://www.newtonsoft.com/json) the same behavior the core gives `System.Text.Json`: options are written as the value or `null`, your own unions round-trip, and results are not serialized. It depends on `Newtonsoft.Json` (MIT license). For `System.Text.Json`, see [Serialization](../serialization/README.md).

> **Warning: register `AddFluentUnions()` or a result leaks its exception.** Without it, Newtonsoft.Json writes a `Result` as a plain object, including the exception's class name, message and `StackTraceString`, for example `{"Value":{"ClassName":"System.InvalidOperationException","StackTraceString":"..."}}`. This happens wherever Newtonsoft.Json is the serializer and the converters are not registered, such as ASP.NET Core with `AddNewtonsoftJson()`. Register `AddFluentUnions()` on every `JsonSerializerSettings`, and never return a result from an endpoint, convert it first.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register once, then serialize.
- [Options](#options): value or `null`, in properties, collections and dictionaries.
- [Results](#results-refuse-to-serialize): why they throw.
- [Your own unions](#your-own-unions): `UnionJsonConverter` with `$type` and `$value`.
- [Rules](#rules) and [Pitfalls](#pitfalls)

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.NewtonsoftJson;
using Newtonsoft.Json;

public record CustomerDto(string Name, Option<string> Nickname);

var settings = new JsonSerializerSettings().AddFluentUnions();

JsonConvert.SerializeObject(new CustomerDto("Ada", Option.None), settings); // {"Name":"Ada","Nickname":null}
JsonConvert.SerializeObject(new CustomerDto("Ada", "Countess"), settings);  // {"Name":"Ada","Nickname":"Countess"}
```

`AddFluentUnions()` adds `OptionJsonConverter` and `ResultJsonConverter` to `settings.Converters` and returns the settings. Calling it again adds nothing. It works on any `JsonSerializerSettings`, such as `MvcNewtonsoftJsonOptions.SerializerSettings` in ASP.NET Core.

## When to use

- A project that serializes with Newtonsoft.Json and has `Option<T>` in its DTOs.
- Unions of your own that must round-trip: `UnionJsonConverter`.
- Never for `Result<T>`. Convert to a DTO first.

## Rules

- **Registration is required.** Newtonsoft.Json ignores the `System.Text.Json` attribute on `Option<T>`. Without `AddFluentUnions()`, an option is written as an object with `Value` and `HasValue`, not as its value, and a result is written with its full exception (see the warning at the top).
- A missing property reads as a `default` option, which holds no value.
- `DefaultValueHandling.Ignore` omits every option without a value, because `None` equals `default`. `NullValueHandling.Ignore` does not omit `None`.
- `Option<Option<T>>` loses the difference between its levels.
- `Result` and `Result<T>` throw `NotSupportedException` in both directions, with the same message as the core. Your own [result types](../errors/custom-error-types.md) are refused like the default type.
- A nullable `Option<T>?` reads `null` as `null`, not as `None`.

## Options

An option is written as its value, or as `null` when it holds none. Reading `null` gives `None`. This works at the root, in properties, in arrays, lists and dictionary values, and with nullable value types such as `Option<int?>`.

```csharp
Option<int>[] options = [1, Option.None, 3];

JsonConvert.SerializeObject(options, settings);                       // [1,null,3]
JsonConvert.DeserializeObject<Option<int>[]>("[1,null,3]", settings); // 1, None, 3
```

### Missing properties

A property that is absent from the JSON is not read by the converter, so it stays `default`. A `default` option holds no value, so `HasValue` is `false`, the same as for `None`.

```csharp
var dto = JsonConvert.DeserializeObject<CustomerDto>("""{"Name":"Ada"}""", settings)!;

dto.Nickname.HasValue; // false
```

### Ignore handling

An option is a struct, so `null` checks do not apply to it.

| Setting | `None` | `default` |
| --- | --- | --- |
| none | `"Nickname":null` | `"Nickname":null` |
| `NullValueHandling.Ignore` | `"Nickname":null` | `"Nickname":null` |
| `DefaultValueHandling.Ignore` | property omitted | property omitted |

### Nested options

`Option<Option<T>>` writes `null` for both `None` and `Some(None)`, so reading gives `None` either way. Flatten with `Unwrap()` before you serialize.

## Results refuse to serialize

`ResultJsonConverter` throws `NotSupportedException` for `Result` and `Result<T>`, when writing and when reading. With your own [result type](../errors/custom-error-types.md), it is refused like the default type. Without it, an error would be written with its message, stack trace and target site, which leaks internals to clients.

```csharp
Result<int> error = new InvalidOperationException("Secret");

JsonConvert.SerializeObject(error, settings); // throws NotSupportedException, the message does not contain "Secret"
```

Convert a result to a DTO first, for example with `Match`, `GetValueOrDefault()` or `ToOption()`. See [Serialization](../serialization/README.md#results-refuse-to-serialize).

## Your own unions

By default, Newtonsoft.Json writes a union as an object that cannot be read back. `UnionJsonConverter` writes the case name:

```csharp
using Fluent.Unions.NewtonsoftJson;
using Newtonsoft.Json;

public record Circle(double Radius);
public record Square(double Side);

[JsonConverter(typeof(UnionJsonConverter))]
public union Shape(Circle, Square);

JsonConvert.SerializeObject((Shape)new Circle(2));
// {"$type":"Circle","$value":{"Radius":2.0}}

Shape shape = JsonConvert.DeserializeObject<Shape>("""{"$type":"Square","$value":{"Side":3}}""");
```

Behavior:

- **Reading** accepts `$type` and `$value` in any order, and skips other properties.
- **An empty union** is written as `null` and read back as an empty union. A `Shape?` reads `null` as `null`.
- **A missing or unknown `$type`**, or a token that is not an object, throws `JsonSerializationException`.
- **Case names** are the case types' short names, so two cases must not share a name, even across namespaces. A union with duplicate case names throws `InvalidOperationException` on first use.
- **Value types only.** The converter handles union structs.
- **`$value` before `$type`.** The value is buffered. Dates stay as written, but a `decimal` beyond double precision can lose precision. The converter writes `$type` first.

Apply `UnionJsonConverter` with the attribute. You can also add it to `settings.Converters`: it never handles `Option<T>` or result types, so they keep their own converters in either registration order. The constants `UnionJsonConverter.TypeProperty` and `UnionJsonConverter.ValueProperty` hold the two property names. See [your own unions](../unions/README.md).

## AOT and trimming

Not supported. The converters use reflection (`MakeGenericType`, constructor lookup), as does Newtonsoft.Json. AOT and trimming have not been investigated or tested.

## Pitfalls

- Forgetting `AddFluentUnions()` writes options as objects and results with their exception, including the stack trace.
- `NullValueHandling.Ignore` does not omit `None`.
- Serializing a `Result<T>`, or your own result type, throws. Convert it first.
- Two union cases with the same type name cannot be told apart.
- The converter classes share their names with the core ones in `Fluent.Unions.Serialization`. Import only one of the two namespaces per file.

## See also

- [Serialization](../serialization/README.md) for `System.Text.Json`
- [Option conversions](../option/conversions.md)
- [Your own unions](../unions/README.md)

---

Back to index: [Docs index](../README.md)
