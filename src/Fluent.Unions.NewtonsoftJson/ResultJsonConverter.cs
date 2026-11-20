using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson;

/// <summary>
/// Refuses to serialize <see cref="Result"/> and <see cref="Result{T}"/>, because an error would expose exception details
/// such as the stack trace. Convert a result before serializing it.
/// </summary>
public sealed class ResultJsonConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        var resultType = Nullable.GetUnderlyingType(objectType) ?? objectType;

        return resultType == typeof(Result)
            || resultType.IsGenericType
            && (resultType.GetGenericTypeDefinition() == typeof(Result<>) || resultType.GetGenericTypeDefinition() == typeof(Result<,>));
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
        throw NotSupported(Nullable.GetUnderlyingType(objectType) ?? objectType);

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        throw NotSupported(value!.GetType());

    private static NotSupportedException NotSupported(Type resultType) => new(
        $"{resultType} is not serialized, because an error would expose exception details. " +
        "Convert it first, for example with Match, GetValueOrDefault or ToOption.");
}
