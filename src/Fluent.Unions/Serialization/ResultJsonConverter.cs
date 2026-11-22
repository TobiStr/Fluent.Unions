using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluent.Unions.Serialization;

/// <summary>
/// Refuses to serialize every result type, such as <see cref="Result"/> and <see cref="Result{T}"/>, because an error would expose exception details
/// such as the stack trace. Applied to both types by default. Convert a result before serializing it.
/// </summary>
public sealed class ResultJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "System.Text.Json asks only about types it serializes, and a result type that is serialized keeps its interfaces.")]
    public override bool CanConvert(Type typeToConvert) => Abstractions.ResultTypes.IsResult(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
            throw NotSupported(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(typeof(Rejecting<>).MakeGenericType(typeToConvert))!;
    }

    private static NotSupportedException NotSupported(Type resultType) => new(
        $"{resultType} is not serialized, because an error would expose exception details. " +
        "Convert it first, for example with Match, GetValueOrDefault or ToOption.");

    private sealed class Rejecting<TResult> : JsonConverter<TResult>
    {
        public override TResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw NotSupported(typeof(TResult));

        public override void Write(Utf8JsonWriter writer, TResult value, JsonSerializerOptions options) =>
            throw NotSupported(typeof(TResult));
    }
}
