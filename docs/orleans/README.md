# Orleans

The package `Fluent.Unions.Orleans` (namespace `Fluent.Unions.Orleans`) lets an `Option<T>` cross [Orleans](https://github.com/dotnet/orleans) grain calls and live in grain state. It depends on `Microsoft.Orleans.Serialization` 10 (MIT) and the Orleans code generator. Results are deliberately not serialized.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register it and use options in a grain.
- [Options](#options): the wire format and the surrogate.
- [Results refuse to serialize](#results-refuse-to-serialize): why, and what to do instead.
- [Code generation](#code-generation): what was checked with C# 15 unions.
- [Rules](#rules): limits and pitfalls.

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.Orleans;

// silo and client
builder.Services.AddSerializer(serializer => serializer.AddUnions());

public interface IUserGrain : IGrainWithStringKey
{
    Task<Option<string>> GetNickname();
    Task SetNickname(Option<string> nickname);
}

[GenerateSerializer]
public sealed record Profile([property: Id(0)] string Name, [property: Id(1)] Option<string> Nickname);
```

Call `AddUnions()` wherever the Orleans serializer is configured, in the silo and in every client. It registers the converter, and the codec that rejects results. Orleans can also find the converter on its own in a project that references the package, but only `AddUnions()` adds the rejecting codec. Without it, serializing a result still fails, with Orleans' own `CodecNotFoundException`.

## Options

`AddUnions()` registers `OptionConverter<T>`, an Orleans surrogate converter. An option is converted to `OptionSurrogate<T>` and back:

| Field | Id | Content |
| --- | --- | --- |
| `HasValue` | 0 | `true` when the option holds a value |
| `Value` | 1 | the value, serialized by the codec of `T` |

`None` and a `default` option both give `HasValue = false` and read back as `None`. `T` must be serializable by Orleans, for example a primitive or a `[GenerateSerializer]` type. Orleans copies arguments of calls inside a silo, and the converter takes part in the copy too.

The surrogate is stored in grain state, so keep its field ids as they are in your own data.

## Results refuse to serialize

`Result`, `Result<T>` and `Result<T, TError>` throw `NotSupportedException` when serialized, in a grain call and in grain state:

```csharp
Result<int> error = new InvalidOperationException("Secret");

serializer.SerializeToArray(error); // NotSupportedException, the text does not contain "Secret"
```

A grain interface that returns a `Task<Result<T>>` compiles. The call fails when its response is serialized.

The reason is the same as for [System.Text.Json](../serialization/README.md#results-refuse-to-serialize): an error is an exception, and writing it would copy its message and stack trace to the other side. A grain-to-grain call stays inside your cluster, but a grain interface is often also used by an Orleans client in another process, and state is persisted. One rule that holds everywhere is easier to review than a rule per call.

What to do instead:

- Return `Option<T>` when the only failure is "nothing there".
- Return a DTO, or convert with `Match`, before the grain returns.
- Let the grain throw. Orleans carries a grain's exception to the caller, and you can turn it back with `await Result.TryAsync(() => grain.DoAsync())` on the calling side.

## Code generation

The Orleans code generator never sees `Option<T>`: it generates the serializer of the surrogate struct, and `Option<T>` is only named in the converter. These were built and run with C# 15 unions:

- a `[GenerateSerializer]` record with an `Option<string>` property, round-tripped through `Serializer<T>`;
- `Option<T>` inside `List<T>` and `Dictionary<TKey, TValue>`;
- an `IGrainWithStringKey` interface with `Task<Option<string>>`, `Option<int>` and `Task<Result<int>>` members, which compiles with the code generator. This was a compile check only. No silo was started.

## Rules

- Register `AddUnions()` in the silo and in every client.
- `Option<T>` needs no attribute on your types.
- Results throw. Convert them before the grain returns.
- Your own C# 15 unions are not covered. AOT and trimming have not been investigated or tested.

## See also

- [Serialization](../serialization/README.md)
- [Option](../option/README.md)

---

Back to index: [Docs index](../README.md)
