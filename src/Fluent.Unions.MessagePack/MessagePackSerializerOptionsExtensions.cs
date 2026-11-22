using System.Diagnostics.CodeAnalysis;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Fluent.Unions.MessagePack;

/// <summary>
/// Adds the Fluent.Unions formatters to <see cref="MessagePackSerializerOptions"/>.
/// </summary>
public static class MessagePackSerializerOptionsExtensions
{
    /// <summary>
    /// Returns options that resolve <see cref="UnionsResolver"/> first, then the resolver the options already use. Options that already have it are returned unchanged.
    /// </summary>
    [RequiresUnreferencedCode("Creates formatters through reflection. Not supported in trimmed or native AOT apps; see the AOT and trimming page of the docs.")]
    [RequiresDynamicCode("Creates formatters through reflection. Not supported in trimmed or native AOT apps; see the AOT and trimming page of the docs.")]
    public static MessagePackSerializerOptions WithFluentUnions(this MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Resolver is UnionsFirstResolver
            ? options
            : options.WithResolver(new UnionsFirstResolver(CompositeResolver.Create(UnionsResolver.Instance, options.Resolver)));
    }

    private sealed class UnionsFirstResolver(IFormatterResolver resolver) : IFormatterResolver
    {
        public IMessagePackFormatter<T>? GetFormatter<T>() => resolver.GetFormatter<T>();
    }
}
