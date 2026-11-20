# MessagePack

The package `Fluent.Unions.MessagePack` (namespace `Fluent.Unions.MessagePack`) adds a formatter and resolver for `Option<T>` to [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp). It depends on `MessagePack` 3 (MIT). Results are deliberately not serialized.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register the resolver and round-trip an option.
- [Options](#options): what is written and read.
- [Source generator](#source-generator): the limit with `[MessagePackObject]` types and the two ways around it.
- [Results refuse to serialize](#results-refuse-to-serialize): what is thrown.
- [Rules](#rules): limits and pitfalls.

## Quick example

```csharp
using MessagePack;
using Fluent.Unions;
using Fluent.Unions.MessagePack;

var options = MessagePackSerializerOptions.Standard.WithUnions();

byte[] bytes = MessagePackSerializer.Serialize<Option<int>>(7, options);
Option<int> back = MessagePackSerializer.Deserialize<Option<int>>(bytes, options); // 7

MessagePackSerializer.Serialize<Option<int>>(Option.None, options); // a single nil byte
```

`WithUnions()` returns options that ask `UnionsResolver` first and then the resolver the options already had. Without it, MessagePack does not know `Option<T>`.

## Options

An option is written as its value, or as nil when it holds none. Reading nil gives `None`. This works at the root, in arrays and in dictionaries. A `default` option holds no value, so it is written as nil.

| Option | Written as |
| --- | --- |
| `Option.Some(7)` | `7` |
| `Option.None`, `default` | nil |

The value is written with the formatter of `T` from the same resolver, so `T` can be any type the options can serialize. A formatter that reads `null` gives `None`. `Option<Option<T>>` loses the difference between its levels, like in [System.Text.Json](../serialization/README.md).

## Source generator

The MessagePack source generator (part of the `MessagePack` package) cannot handle a member of type `Option<T>`. It treats the union as a type to generate a formatter for, and the generated `GeneratedMessagePackResolver` fails to compile with error CS0426. This also happens for an `Option<T>[]` member. The compiler error appears in the generated file, not in your code.

Choose one of these on the type that has the member:

```csharp
[MessagePackObject]
public class Customer
{
    [Key(0)]
    public string Name { get; set; } = "";

    [Key(1)]
    [MessagePackFormatter(typeof(OptionFormatter<string>))]
    public Option<string> Nickname { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class Scoreboard
{
    [Key(0)]
    public Option<int>[] Scores { get; set; } = [];
}
```

- **`[MessagePackFormatter(typeof(OptionFormatter<T>))]`** names the formatter for one member. The type keeps its generated formatter.
- **`SuppressSourceGeneration = true`** makes the type use the reflection-based resolver at runtime, which finds `UnionsResolver` through the options. Use it when a member is an array or collection of options.

Both work with `WithUnions()`. The MessagePack analyzer reports nothing for either form, so keep the attribute in place.

## Results refuse to serialize

`Result`, `Result<T>` and `Result<T, TError>` throw when written and when read. An error would be written with its message and stack trace, which leaks internals. MessagePack wraps the exception:

```csharp
Result<int> error = new InvalidOperationException("Secret");

MessagePackSerializer.Serialize(error, options);
// MessagePackSerializationException, InnerException is NotSupportedException, the text does not contain "Secret"
```

Convert a result before serializing it, for example with `Match`, `GetValueOrDefault` or `ToOption`. See [Serialization](../serialization/README.md#results-refuse-to-serialize) for the same rule in System.Text.Json.

## Rules

- `UnionsResolver` needs to be in the options. `WithUnions()` is the shortest way, or add `UnionsResolver.Instance` to a `CompositeResolver` yourself.
- `Option<T>` in a `[MessagePackObject]` type needs one of the two source generator workarounds above.
- `UnionsResolver` creates formatters through reflection (`MakeGenericType`). AOT and trimming have not been investigated or tested.
- Your own C# 15 unions are not covered. MessagePack's own `[Union]` attribute is for class hierarchies, and has not been tried with `union` types.

## See also

- [Serialization](../serialization/README.md)
- [Option](../option/README.md)

---

Back to index: [Docs index](../README.md)
