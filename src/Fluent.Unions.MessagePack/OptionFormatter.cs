using MessagePack;
using MessagePack.Formatters;

namespace Fluent.Unions.MessagePack;

/// <summary>
/// Writes an <see cref="Option{T}"/> as its value, or as nil when it holds none. Reading nil gives <see cref="Option.None"/>.
/// </summary>
public sealed class OptionFormatter<T> : IMessagePackFormatter<Option<T>>
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, Option<T> value, MessagePackSerializerOptions options)
    {
        if (value.TryGetValue(out var payload))
            options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, payload, options);
        else
            writer.WriteNil();
    }

    /// <inheritdoc />
    public Option<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return Option.None;

        var payload = options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
        return payload is null ? Option.None : Option.Some(payload);
    }
}
