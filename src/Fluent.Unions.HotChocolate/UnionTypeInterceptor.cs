using Fluent.Unions.Abstractions;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace Fluent.Unions.HotChocolate;

internal sealed class UnionTypeInterceptor : TypeInterceptor
{
    private const string MiddlewareKey = "Fluent.Unions.HotChocolate";

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration objectType)
        {
            return;
        }

        foreach (var field in objectType.Fields)
        {
            Rewrite(discoveryContext.TypeInspector, field);
        }
    }

    private static void Rewrite(ITypeInspector inspector, ObjectFieldConfiguration field)
    {
        if (field.Type is not ExtendedTypeReference reference || Classify(reference.Type) is not { } kind)
        {
            return;
        }

        var valueType = inspector.ChangeNullability(reference.Type.TypeArguments![0], kind.IsNullable);
        field.Type = reference.WithType(valueType);
        field.MiddlewareConfigurations.Add(new FieldMiddlewareConfiguration(kind.Middleware, false, MiddlewareKey));
    }

    private static Kind? Classify(IExtendedType type)
    {
        if (!type.IsGeneric)
        {
            return null;
        }

        if (ResultTypes.ValueType(type.Type) is not null)
        {
            return new Kind(false, UnionFieldMiddleware.UnwrapResult);
        }

        return type.Type.GetGenericTypeDefinition() == typeof(Option<>) ? new Kind(true, UnionFieldMiddleware.UnwrapOption) : null;
    }

    private readonly record struct Kind(bool IsNullable, FieldMiddleware Middleware);
}
