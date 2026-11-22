# AOT and trimming

Which parts of Fluent.Unions work in a native AOT app and in a trimmed app, and which claims are tested.

The core package is marked `IsAotCompatible`, so the trim and AOT analyzers check it on every build. CI publishes [`test/Fluent.Unions.Aot`](https://github.com/TobiStr/Fluent.Unions/tree/main/test/Fluent.Unions.Aot) for `linux-x64` twice, as native AOT and as a trimmed self-contained app, runs it and fails on a non-zero exit code. Trim and AOT warnings fail the publish.

## Compatibility

| Area | Native AOT | Trimmed | Status |
| --- | --- | --- | --- |
| `Result`, `Result<T>`, `Option<T>` and every operator, including `Task` pipelines | yes | yes | tested |
| Your own result types with `[FluentResult]`, `Capture`, `MapError` and `Tap` | yes | yes | tested |
| `ResultTypes.IsResult`, `ErrorType`, `ValueType` | yes | yes | tested |
| `Option<T>` in System.Text.Json with `OptionJsonConverter<T>` registered | yes | yes | tested |
| `Option<T>` in System.Text.Json without registration | reference types only; a value type throws `NotSupportedException` that names `OptionJsonConverter<T>` | yes | tested |
| Your own unions with `[JsonConverter(typeof(UnionJsonConverter<Shape>))]` | yes | yes, with one call, see below | tested |
| Your own unions with the `UnionJsonConverter` factory | throws `NotSupportedException` that names `UnionJsonConverter<T>` | not supported | |
| Serializing a result | throws `NotSupportedException` | throws `NotSupportedException` | tested |
| `ValidateAnnotations()` | no | no | annotated with `RequiresUnreferencedCode` |
| Newtonsoft.Json, MessagePack and MongoDB registrations | no | no | annotated with `RequiresUnreferencedCode` and `RequiresDynamicCode` |
| MediatR and Mediator behaviors, AutoMapper, Mapster | no | no | built on reflection |
| ASP.NET Core, OpenAPI, Swashbuckle, NSwag, HotChocolate, Grpc, Orleans, Polly, Refit, Serilog, Dapper, FluentValidation | | | not verified |
| Assertion and snapshot packages | | | not relevant: they run in test projects |

## System.Text.Json

Use a source-generated context, register `OptionJsonConverter<T>` for each value type of an option, and register every value type and union case type as well, because the converters read them through the context:

```csharp
[JsonSourceGenerationOptions(Converters = [typeof(OptionJsonConverter<int>), typeof(OptionJsonConverter<string>)])]
[JsonSerializable(typeof(CustomerDto))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Circle))]
[JsonSerializable(typeof(Square))]
public partial class AppJsonContext : JsonSerializerContext;
```

Apply the union converter for the concrete union instead of the factory:

```csharp
[JsonConverter(typeof(UnionJsonConverter<Shape>))]
public union Shape(Circle, Square);
```

Both write the same JSON as the factories, so a trimmed or AOT app reads what a regular app wrote.

## Trimmed apps and union cases

In a trimmed app that is not native AOT, the trimmer removes the constructor of a union case that the app never creates itself, and reading such a case then fails with `'Square' is not a case of Shape`. Native AOT keeps them. Call `UnionCaseNames.For` once with each union, for example at startup:

```csharp
UnionCaseNames.For(typeof(Shape));
```

The call tells the trimmer to keep the constructors of `Shape`.

---

Back to: [Getting started](README.md) · See also: [Serialization](../serialization/README.md), [Limitations](limitations.md)
