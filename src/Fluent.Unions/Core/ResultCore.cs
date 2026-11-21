using System.ComponentModel;
using System.Runtime.ExceptionServices;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Operators that leave a result chain.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultCore
{
    /// <summary>
    /// Leaves the result with one callback for the value and one for the error.
    /// </summary>
    public static TOut Match<TSelf, T, TError, TOut>(TSelf result, Func<T, TOut> onSuccess, Func<TError, TOut> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.TryGetValue(out var value) ? onSuccess(value) : onError(Flow.ErrorOf<TSelf, T, TError>(result));
    }

    /// <summary>
    /// Leaves the result with one callback for success and one for the error.
    /// </summary>
    public static TOut Match<TSelf, TError, TOut>(TSelf result, Func<TOut> onSuccess, Func<TError, TOut> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.TryGetError(out var error) ? onError(error) : onSuccess();
    }

    /// <summary>
    /// Returns the result when it is successful, otherwise throws its error with the original stack trace.
    /// An error that is not an exception is thrown inside a <see cref="ResultErrorException"/>.
    /// </summary>
    public static TSelf Check<TSelf, TError>(TSelf result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        if (result.TryGetError(out var error))
            Throw(error);

        return result;
    }

    /// <summary>
    /// Returns the result when it holds a value, otherwise throws its error with the original stack trace.
    /// An error that is not an exception is thrown inside a <see cref="ResultErrorException"/>.
    /// </summary>
    public static TSelf Check<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (result.TryGetError(out var error))
            Throw(error);

        return result;
    }

    private static void Throw<TError>(TError error) where TError : notnull
    {
        if (Flow.AsException(error) is { } exception)
            ExceptionDispatchInfo.Throw(exception);

        throw new ResultErrorException(error);
    }
}
