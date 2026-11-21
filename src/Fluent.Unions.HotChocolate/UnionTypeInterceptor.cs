using Fluent.Unions.Abstractions;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace Fluent.Unions.HotChocolate;

internal sealed class UnionTypeInterceptor : TypeInterceptor
{
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
        if (field.Type is not ExtendedTypeReference reference)
        {
            return;
        }

        var type = reference.Type;
        var isNullable = false;
        var middlewares = new List<Kind>();

        while (Classify(type) is { } kind)
        {
            type = inspector.ChangeNullability(type.TypeArguments![0], kind.IsNullable);
            isNullable |= kind.IsNullable;
            middlewares.Insert(0, kind);
        }

        if (middlewares.Count == 0)
        {
            return;
        }

        field.Type = reference.WithType(isNullable ? inspector.ChangeNullability(type, true) : type);

        foreach (var middleware in middlewares)
        {
            field.MiddlewareConfigurations.Add(new FieldMiddlewareConfiguration(middleware.Middleware, false, middleware.Key));
        }
    }

    private static Kind? Classify(IExtendedType type)
    {
        if (!type.IsGeneric)
        {
            return null;
        }

        if (ResultTypes.ValueType(type.Type) is not null)
        {
            return new Kind(false, UnionFieldMiddleware.UnwrapResult, "Fluent.Unions.HotChocolate.Result");
        }

        return type.Type.GetGenericTypeDefinition() == typeof(Option<>) ? new Kind(true, UnionFieldMiddleware.UnwrapOption, "Fluent.Unions.HotChocolate.Option") : null;
    }

    private readonly record struct Kind(bool IsNullable, FieldMiddleware Middleware, string Key);
}
