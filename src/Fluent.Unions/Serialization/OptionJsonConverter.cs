using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Fluent.Unions.Serialization;

/// <summary>
/// Serializes an <see cref="Option{T}"/> as its value, or as <c>null</c> when it holds none. Applied to <see cref="Option{T}"/> by default.
/// In a native AOT app, register <see cref="OptionJsonConverter{T}"/> for each value type instead.
/// </summary>
public sealed class OptionJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);

    /// <inheritdoc />
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification = "Without dynamic code a failure becomes a NotSupportedException that names OptionJsonConverter<T>.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        try
        {
            return (JsonConverter)Activator.CreateInstance(typeof(OptionJsonConverter<>).MakeGenericType(valueType))!;
        }
        catch (Exception exception) when (!RuntimeFeature.IsDynamicCodeSupported && exception is not OutOfMemoryException)
        {
            throw new NotSupportedException(
                $"Option<{valueType.Name}> needs OptionJsonConverter<{valueType.Name}> in JsonSerializerOptions.Converters in a native AOT app.",
                exception);
        }
    }
}

/// <summary>
/// Serializes an <see cref="Option{T}"/> of one value type as its value, or as <c>null</c> when it holds none.
/// Register it in <see cref="JsonSerializerOptions.Converters"/> in a native AOT app.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class OptionJsonConverter<T> : JsonConverter<Option<T>>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null
            ? Option<T>.NoValue
            : Option<T>.Of(JsonSerializer.Deserialize(ref reader, TypeInfo(options)));

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
    {
        if (value.TryGetValue(out var payload))
            JsonSerializer.Serialize(writer, payload, TypeInfo(options));
        else
            writer.WriteNullValue();
    }

    private static JsonTypeInfo<T> TypeInfo(JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
