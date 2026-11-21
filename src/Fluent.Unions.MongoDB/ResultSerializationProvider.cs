using Fluent.Unions.Abstractions;
using MongoDB.Bson.Serialization;

namespace Fluent.Unions.MongoDB;

internal sealed class ResultSerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer? GetSerializer(Type type) =>
        ResultTypes.IsResult(type) ? (IBsonSerializer)Activator.CreateInstance(typeof(ResultSerializer<>).MakeGenericType(type))! : null;
}
