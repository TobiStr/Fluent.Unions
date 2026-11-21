using Fluent.Unions.Abstractions;
using MessagePack;
using MessagePack.Formatters;

namespace Fluent.Unions.MessagePack;

/// <summary>
/// Resolves a formatter for <see cref="Option{T}"/>, and a formatter that refuses to serialize any result type,
/// because an error would expose exception details.
/// </summary>
public sealed class UnionsResolver : IFormatterResolver
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static readonly UnionsResolver Instance = new();

    private UnionsResolver()
    {
    }

    /// <inheritdoc />
    public IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

    private static class Cache<T>
    {
        public static readonly IMessagePackFormatter<T>? Formatter = Create();

        private static IMessagePackFormatter<T>? Create()
        {
            var type = typeof(T);

            if (ResultTypes.IsResult(type))
                return new RejectingFormatter<T>();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Option<>))
                return (IMessagePackFormatter<T>)Activator.CreateInstance(
                    typeof(OptionFormatter<>).MakeGenericType(type.GetGenericArguments()[0]))!;

            return null;
        }
    }

    private sealed class RejectingFormatter<T> : IMessagePackFormatter<T>
    {
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options) =>
            throw NotSupported();

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) =>
            throw NotSupported();

        private static NotSupportedException NotSupported() => new(
            $"{typeof(T)} is not serialized, because an error would expose exception details. " +
            "Convert it first, for example with Match, GetValueOrDefault or ToOption.");
    }
}
