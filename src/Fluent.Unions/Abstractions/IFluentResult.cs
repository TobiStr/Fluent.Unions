using System.Diagnostics.CodeAnalysis;

namespace Fluent.Unions.Abstractions;

/// <summary>
/// A result type that the operators read without boxing. Declare it with <c>[FluentResult]</c> instead of implementing it.
/// </summary>
public interface IFluentResult<TSelf, T, TError> : IResult<TSelf, T, TError>
    where TSelf : struct, IFluentResult<TSelf, T, TError>
    where TError : notnull
{
    /// <summary>
    /// Gets the value, if the result holds one.
    /// </summary>
    bool TryGetValue([MaybeNullWhen(false)] out T value);
}

/// <summary>
/// A result type without a value that the operators read without boxing.
/// </summary>
public interface IFluentUnitResult<TSelf, TError> : IErrorResult<TSelf, TError>
    where TSelf : struct, IFluentUnitResult<TSelf, TError>
    where TError : notnull
{
    /// <summary>
    /// The successful result.
    /// </summary>
    static abstract TSelf Success { get; }
}
