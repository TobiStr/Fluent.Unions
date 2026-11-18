using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResultType.Serialization;

/// <summary>
/// Serializes an <see cref="Option{T}"/> as its value, or as <c>null</c> when it holds none. Applied to <see cref="Option{T}"/> by default.
/// </summary>
public sealed class OptionJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class Converter<T> : JsonConverter<Option<T>>
    {
        public override bool HandleNull => true;

        public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? Option<T>.NoValue
                : Option<T>.Of(JsonSerializer.Deserialize<T>(ref reader, options));

        public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
        {
            if (value.TryGetValue(out var payload))
                JsonSerializer.Serialize(writer, payload, options);
            else
                writer.WriteNullValue();
        }
    }
}
