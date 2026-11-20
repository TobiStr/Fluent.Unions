![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.MessagePack

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.MessagePack.svg)](https://www.nuget.org/packages/Fluent.Unions.MessagePack) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.MessagePack.svg)](https://www.nuget.org/packages/Fluent.Unions.MessagePack) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/messagepack/)

A [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) (MIT) formatter and resolver for `Option<T>`. An option is written as its value, or as nil when it holds none. Results refuse to serialize, like in the System.Text.Json support of Fluent.Unions.

## Installation

```
dotnet add package Fluent.Unions.MessagePack
```

## Usage

```csharp
using MessagePack;
using Fluent.Unions;
using Fluent.Unions.MessagePack;

var options = MessagePackSerializerOptions.Standard.WithUnions();

byte[] bytes = MessagePackSerializer.Serialize<Option<int>>(7, options);                 // 7
byte[] none = MessagePackSerializer.Serialize<Option<int>>(Option.None, options);        // nil
Option<int> back = MessagePackSerializer.Deserialize<Option<int>>(bytes, options);       // 7
```

`WithUnions()` puts `UnionsResolver` in front of the resolver the options already use.

## Source-generated formatters

The MessagePack source generator cannot generate code for a member of type `Option<T>`: it treats the union as a type to generate a formatter for, and the generated resolver does not compile (CS0426). Choose one of these on the containing type:

```csharp
[MessagePackObject]
public class Customer
{
    [Key(0)]
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

The first names the formatter per member and keeps the source generator. The second falls back to the reflection-based resolver, which finds `UnionsResolver` through the options.

## Documentation

- [MessagePack documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/messagepack/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)

## Rules

- **Nil is `None`.** Nil reads as `None`, and a formatter that returns `null` also gives `None`. `null` never becomes a value.
- **Results throw.** `Result`, `Result<T>` and `Result<T, TError>` throw `NotSupportedException`, wrapped by MessagePack in a `MessagePackSerializationException`, when writing and when reading. An error would expose exception details such as the stack trace. Convert the result first.
- **Reflection.** `UnionsResolver` creates formatters with `MakeGenericType`. AOT and trimming are not tested.
- **Your own unions** are not covered by this package.
