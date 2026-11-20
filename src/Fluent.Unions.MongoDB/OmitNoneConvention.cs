using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Fluent.Unions.MongoDB;

/// <summary>
/// Leaves out the element of an <see cref="Option{T}"/> member that holds no value, instead of writing BSON null.
/// </summary>
public sealed class OmitNoneConvention : ConventionBase, IMemberMapConvention
{
    /// <inheritdoc />
    public void Apply(BsonMemberMap memberMap)
    {
        var type = memberMap.MemberType;

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Option<>))
            return;

        var hasValue = (Func<object, bool>)typeof(OmitNoneConvention)
            .GetMethod(nameof(CreateHasValue), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type.GetGenericArguments()[0])
            .Invoke(null, [memberMap])!;

        memberMap.SetShouldSerializeMethod(hasValue);
    }

    private static Func<object, bool> CreateHasValue<T>(BsonMemberMap memberMap)
    {
        var getter = memberMap.Getter;
        return instance => ((Option<T>)getter(instance)).HasValue;
    }
}
