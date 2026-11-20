using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.Internal;
using AutoMapper.Internal.Mappers;

namespace Fluent.Unions.AutoMapper;

internal sealed class OptionMapper : IObjectMapper
{
    private static readonly MethodInfo SomeMethod = typeof(OptionMapper).GetMethod(nameof(Some), BindingFlags.Static | BindingFlags.NonPublic)!;

    public bool IsMatch(TypePair context) => IsOption(context.SourceType) || IsOption(context.DestinationType);

    public Expression MapExpression(
        IGlobalConfiguration configuration,
        ProfileMap profileMap,
        MemberMap memberMap,
        Expression sourceExpression,
        Expression destExpression)
    {
        var source = sourceExpression.Type;
        var destination = destExpression.Type;
        var sourceElement = ElementType(source);
        var destinationElement = ElementType(destination);
        var variable = Expression.Variable(source, "source");
        var assign = Expression.Assign(variable, sourceExpression);

        var hasValue = IsOption(source)
            ? (Expression)Expression.Property(variable, nameof(Option<int>.HasValue))
            : HasValue(variable);
        var value = IsOption(source)
            ? Expression.Call(variable, nameof(Option<int>.GetValue), null)
            : Value(variable);

        var mapped = ExpressionBuilder.MapExpression(
            configuration, profileMap, new TypePair(sourceElement, destinationElement), value, memberMap);

        var present = IsOption(destination)
            ? (Expression)Expression.Call(SomeMethod.MakeGenericMethod(destinationElement), mapped)
            : Expression.Convert(mapped, destination);

        return Expression.Block(
            [variable],
            assign,
            Expression.Condition(hasValue, present, Expression.Default(destination)));
    }

    public TypePair? GetAssociatedTypes(TypePair initialTypes) =>
        new TypePair(ElementType(initialTypes.SourceType), ElementType(initialTypes.DestinationType));

    internal static bool IsOption(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Option<>);

    private static Type ElementType(Type type) =>
        IsOption(type) ? type.GenericTypeArguments[0] : Nullable.GetUnderlyingType(type) ?? type;

    private static Expression HasValue(Expression source) =>
        Nullable.GetUnderlyingType(source.Type) is not null ? Expression.Property(source, "HasValue")
        : source.Type.IsValueType ? Expression.Constant(true)
        : Expression.NotEqual(source, Expression.Constant(null, source.Type));

    private static Expression Value(Expression source) =>
        Nullable.GetUnderlyingType(source.Type) is not null ? Expression.Property(source, "Value") : source;

    private static Option<T> Some<T>(T value) => value is null ? default : new Option<T>(value);
}
