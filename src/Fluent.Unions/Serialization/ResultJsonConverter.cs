using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluent.Unions.Serialization;

/// <summary>
/// Refuses to serialize <see cref="Result"/> and <see cref="Result{T}"/>, because an error would expose exception details
/// such as the stack trace. Applied to both types by default. Convert a result before serializing it.
/// </summary>
public sealed class ResultJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(Result)
        || typeToConvert.IsGenericType
        && (typeToConvert.GetGenericTypeDefinition() == typeof(Result<>) || typeToConvert.GetGenericTypeDefinition() == typeof(Result<,>));

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Rejecting<>).MakeGenericType(typeToConvert))!;

    private sealed class Rejecting<TResult> : JsonConverter<TResult>
    {
        public override TResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw NotSupported();

        public override void Write(Utf8JsonWriter writer, TResult value, JsonSerializerOptions options) =>
            throw NotSupported();

        private static NotSupportedException NotSupported() => new(
            $"{typeof(TResult)} is not serialized, because an error would expose exception details. " +
            "Convert it first, for example with Match, GetValueOrDefault or ToOption.");
    }
}
