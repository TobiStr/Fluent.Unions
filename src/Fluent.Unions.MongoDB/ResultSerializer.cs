using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Fluent.Unions.MongoDB;

/// <summary>
/// Refuses to serialize a result, because an error would expose exception details such as the stack trace. Convert a result before serializing it.
/// </summary>
/// <typeparam name="TResult">The result type: <see cref="Result"/>, <see cref="Result{T}"/> or your own result type.</typeparam>
public sealed class ResultSerializer<TResult> : SerializerBase<TResult>
{
    /// <inheritdoc />
    public override TResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        throw NotSupported();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TResult value) =>
        throw NotSupported();

    private static NotSupportedException NotSupported() => new(
        $"{typeof(TResult)} is not serialized, because an error would expose exception details. " +
        "Convert it first, for example with Match, GetValueOrDefault or ToOption.");
}
