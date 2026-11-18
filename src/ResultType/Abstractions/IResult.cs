namespace ResultType.Abstractions;

/// <summary>
/// A result type whose errors are restricted to <typeparamref name="TError"/>.
/// </summary>
public interface IErrorResult<TSelf, TError>
    where TSelf : struct, IErrorResult<TSelf, TError>
    where TError : notnull
{
    /// <summary>
    /// The value, the error, or <c>null</c> when the result is empty.
    /// </summary>
    object? Value { get; }

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
/// An error type that can represent an unexpected exception, so that <see cref="Result{T, TError}"/> chains do not throw.
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
/// Implement it on your own union to use the operators in <c>ResultInterfaceExtensions</c>.
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
