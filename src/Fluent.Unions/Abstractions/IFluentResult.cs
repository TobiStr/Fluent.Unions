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

    /// <summary>
    /// Gets the error, if the result holds one. An empty result holds the error for an uninitialized result.
    /// </summary>
    bool TryGetError([MaybeNullWhen(false)] out TError error);
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

    /// <summary>
    /// Indicates whether the result is successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the error, if the result holds one. An empty result holds the error for an uninitialized result.
    /// </summary>
    bool TryGetError([MaybeNullWhen(false)] out TError error);
}
