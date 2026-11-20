using System.Runtime.CompilerServices;
using NUnit.Framework.Constraints;

namespace Fluent.Unions.NUnit;

internal enum UnionKind
{
    Other,
    Ok,
    Error,
    Some,
    None,
}

internal readonly record struct UnionState(UnionKind Kind, object? Payload)
{
    public static UnionState Of(object? actual)
    {
        if (actual is not IUnion union)
        {
            return new UnionState(UnionKind.Other, actual);
        }

        var type = actual.GetType();
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var payload = union.Value;

        if (definition == typeof(Result) || definition == typeof(Result<>))
        {
            return payload is null or Exception ? new UnionState(UnionKind.Error, payload) : new UnionState(UnionKind.Ok, payload);
        }

        if (definition == typeof(Option<>))
        {
            return payload is null or None ? new UnionState(UnionKind.None, null) : new UnionState(UnionKind.Some, payload);
        }

        return new UnionState(UnionKind.Other, actual);
    }

    public void WriteTo(MessageWriter writer)
    {
        switch (this)
        {
            case { Kind: UnionKind.Ok, Payload: Ok }:
                writer.Write("ok");
                break;
            case { Kind: UnionKind.Ok }:
                writer.Write("ok with value ");
                writer.WriteValue(Payload);
                break;
            case { Kind: UnionKind.Error, Payload: Exception error }:
                writer.Write($"failed with {error.GetType().Name}: {error.Message}");
                break;
            case { Kind: UnionKind.Error }:
                writer.Write("a default result, which is an error");
                break;
            case { Kind: UnionKind.Some }:
                writer.Write("some with value ");
                writer.WriteValue(Payload);
                break;
            case { Kind: UnionKind.None }:
                writer.Write("none");
                break;
            default:
                writer.WriteValue(Payload);
                writer.Write(", which is not a result or an option");
                break;
        }
    }
}
