using MongoDB.Bson.Serialization;

namespace Fluent.Unions.MongoDB;

/// <summary>
/// Registers the BSON serializers for Fluent.Unions types.
/// </summary>
public static class FluentUnionsBson
{
    private static readonly object Gate = new();

    private static bool _registered;

    /// <summary>
    /// Registers the serializer for <c>Option&lt;T&gt;</c> and the refusing serializers for results. Call it once at startup, before the first serializer lookup. Repeated calls do nothing.
    /// </summary>
    public static void Register()
    {
        lock (Gate)
        {
            if (_registered)
                return;

            BsonSerializer.RegisterGenericSerializerDefinition(typeof(Option<>), typeof(OptionSerializer<>));
            BsonSerializer.RegisterSerializationProvider(new ResultSerializationProvider());
            _registered = true;
        }
    }
}
