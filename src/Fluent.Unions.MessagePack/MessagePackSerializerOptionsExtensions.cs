using MessagePack;
using MessagePack.Resolvers;

namespace Fluent.Unions.MessagePack;

/// <summary>
/// Adds the Fluent.Unions formatters to <see cref="MessagePackSerializerOptions"/>.
/// </summary>
public static class MessagePackSerializerOptionsExtensions
{
    /// <summary>
    /// Returns options that resolve <see cref="UnionsResolver"/> first, then the resolver the options already use.
    /// </summary>
    public static MessagePackSerializerOptions WithUnions(this MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.WithResolver(CompositeResolver.Create(UnionsResolver.Instance, options.Resolver));
    }
}
