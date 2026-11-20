using MongoDB.Bson.Serialization;

namespace Fluent.Unions.MongoDB;

internal sealed class ResultSerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer? GetSerializer(Type type) =>
        IsResult(type) ? (IBsonSerializer)Activator.CreateInstance(typeof(ResultSerializer<>).MakeGenericType(type))! : null;

    private static bool IsResult(Type type) =>
        type == typeof(Result)
        || type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(Result<>) || type.GetGenericTypeDefinition() == typeof(Result<,>));
}
