using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Fluent.Unions.MongoDB;

/// <summary>
/// Serializes an <see cref="Option{T}"/> as its value, or as BSON null when it holds none. Reading BSON null gives <c>None</c>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class OptionSerializer<T> : SerializerBase<Option<T>>
{
    private readonly IBsonSerializer<T> _valueSerializer;

    /// <summary>
    /// Creates a serializer that uses the registered serializer of <typeparamref name="T"/>.
    /// </summary>
    public OptionSerializer()
        : this(BsonSerializer.LookupSerializer<T>())
    {
    }

    /// <summary>
    /// Creates a serializer that uses <paramref name="valueSerializer"/> for the value.
    /// </summary>
    public OptionSerializer(IBsonSerializer<T> valueSerializer) =>
        _valueSerializer = valueSerializer ?? throw new ArgumentNullException(nameof(valueSerializer));

    /// <inheritdoc />
    public override Option<T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;

        if (reader.GetCurrentBsonType() == BsonType.Null)
        {
            reader.ReadNull();
            return Option.None;
        }

        var value = _valueSerializer.Deserialize(context);
        return value is null ? Option.None : Option.Some(value);
    }

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Option<T> value)
    {
        if (value.TryGetValue(out var payload))
            _valueSerializer.Serialize(context, payload);
        else
            context.Writer.WriteNull();
    }
}
