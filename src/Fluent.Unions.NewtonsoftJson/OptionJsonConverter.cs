using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson;

/// <summary>
/// Serializes an <see cref="Option{T}"/> as its value, or as <c>null</c> when it holds none.
/// </summary>
public sealed class OptionJsonConverter : JsonConverter
{
    private static readonly ConcurrentDictionary<Type, Accessor> Accessors = new();

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        var optionType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return optionType.IsGenericType && optionType.GetGenericTypeDefinition() == typeof(Option<>);
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var optionType = Nullable.GetUnderlyingType(objectType) ?? objectType;

        if (reader.TokenType == JsonToken.Null)
            return optionType == objectType ? GetAccessor(optionType).None() : null;

        return GetAccessor(optionType).Read(reader, serializer);
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        GetAccessor(value.GetType()).Write(writer, (IUnion)value, serializer);
    }

    private static Accessor GetAccessor(Type optionType) =>
        Accessors.GetOrAdd(optionType, static type =>
            (Accessor)Activator.CreateInstance(typeof(Accessor<>).MakeGenericType(type.GetGenericArguments()[0]))!);

    private abstract class Accessor
    {
        public abstract object None();

        public abstract object Read(JsonReader reader, JsonSerializer serializer);

        public abstract void Write(JsonWriter writer, IUnion option, JsonSerializer serializer);
    }

    private sealed class Accessor<T> : Accessor
    {
        public override object None() => new Option<T>(default(None));

        public override object Read(JsonReader reader, JsonSerializer serializer) =>
            serializer.Deserialize(reader, typeof(T)) is T payload ? new Option<T>(payload) : new Option<T>(default(None));

        public override void Write(JsonWriter writer, IUnion option, JsonSerializer serializer)
        {
            if (option.Value is T payload)
                serializer.Serialize(writer, payload, typeof(T));
            else
                writer.WriteNull();
        }
    }
}
