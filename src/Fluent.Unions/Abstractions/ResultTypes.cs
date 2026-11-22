using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Fluent.Unions.Abstractions;

/// <summary>
/// Recognizes result types at runtime: the library's own and every type declared with <c>[FluentResult]</c> or implementing <see cref="IErrorResult{TSelf, TError}"/>.
/// </summary>
public static class ResultTypes
{
    private const DynamicallyAccessedMemberTypes Contracts = DynamicallyAccessedMemberTypes.Interfaces;

    private static readonly ConcurrentDictionary<Type, Type?> ErrorTypes = new();
    private static readonly ConcurrentDictionary<Type, Type?> ValueTypes = new();

    /// <summary>
    /// Indicates whether <paramref name="type"/> is a result type.
    /// </summary>
    public static bool IsResult([DynamicallyAccessedMembers(Contracts)] Type type) => ErrorType(type) is not null;

    /// <summary>
    /// The error type of a result type, or <c>null</c> when <paramref name="type"/> is not a result type.
    /// </summary>
    public static Type? ErrorType([DynamicallyAccessedMembers(Contracts)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return ErrorTypes.TryGetValue(type, out var error)
            ? error
            : ErrorTypes.GetOrAdd(type, type.IsValueType ? Contract(type, typeof(IErrorResult<,>))?.GetGenericArguments()[1] : null);
    }

    /// <summary>
    /// The value type of a result type, or <c>null</c> for a result type without a value or a type that is not a result type.
    /// </summary>
    public static Type? ValueType([DynamicallyAccessedMembers(Contracts)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return ValueTypes.TryGetValue(type, out var value)
            ? value
            : ValueTypes.GetOrAdd(type, type.IsValueType ? Contract(type, typeof(IResult<,,>))?.GetGenericArguments()[1] : null);
    }

    private static Type? Contract([DynamicallyAccessedMembers(Contracts)] Type type, Type definition) =>
        type.GetInterfaces().FirstOrDefault(contract => contract.IsGenericType
            && contract.GetGenericTypeDefinition() == definition
            && contract.GetGenericArguments()[0] == type);
}
