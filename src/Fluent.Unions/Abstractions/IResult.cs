using System.Diagnostics.CodeAnalysis;

namespace Fluent.Unions.Abstractions;

/// <summary>
/// Any result type, read without knowing its type arguments.
/// </summary>
public interface IErrorResult
{
    /// <summary>
    /// The value, the error, or <c>null</c> when the result is empty.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Indicates whether the result holds a value, or is successful for a result without a value.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// The error, or <c>null</c> when the result is successful. An empty result holds the error for an uninitialized result.
    /// </summary>
    object? Error { get; }
}

/// <summary>
/// A result type whose errors are restricted to <typeparamref name="TError"/>.
/// </summary>
public interface IErrorResult<TSelf, TError> : IErrorResult
    where TSelf : struct, IErrorResult<TSelf, TError>
    where TError : notnull
{
    object? IErrorResult.Error => TryGetError(out var error) ? error : null;

    /// <summary>
    /// Gets the error, if the result holds one. An empty result holds the error for an uninitialized result.
    /// </summary>
    bool TryGetError([MaybeNullWhen(false)] out TError error);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    static abstract TSelf FromError(TError error);

    /// <summary>
    /// Converts an unexpected exception thrown inside an operator into an error. Must not return <c>null</c> or throw.
    /// </summary>
    static abstract TError FromException(Exception exception);
}

/// <summary>
/// An error type that can represent an unexpected exception, so that chains of a <c>[FluentResult]</c> type do not throw.
/// </summary>
public interface IFromException<TSelf> where TSelf : notnull
{
    /// <summary>
    /// Wraps an exception thrown inside an operator. Must not return <c>null</c> or throw.
    /// </summary>
    static abstract TSelf FromException(Exception exception);
}

/// <summary>
/// A result type that holds a <typeparamref name="T"/> or a <typeparamref name="TError"/>.
/// Declare it with <c>[FluentResult]</c> instead of implementing it.
/// </summary>
public interface IResult<TSelf, T, TError> : IErrorResult<TSelf, TError>
    where TSelf : struct, IResult<TSelf, T, TError>
    where TError : notnull
{
    /// <summary>
    /// Creates a successful result. A <c>null</c> value gives an empty result.
    /// </summary>
    static abstract TSelf FromValue(T value);
}
