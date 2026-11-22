using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Rx-style operators that run only when a result holds an error. Values pass through.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultErrorCore
{
    /// <summary>
    /// Handles the error. Return a value to recover, or an error to replace it.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError>(TSelf result, Func<TError, TSelf> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (result.TryGetValue(out _))
            return result;

        return Handle<TSelf, TError, TError>(Flow.ErrorOf<TSelf, T, TError>(result), handler);
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> only, also as a case of an error union. Other errors pass through.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError, TException>(TSelf result, Func<TException, TSelf> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (result.TryGetValue(out _) || !Flow.TryGetCase<TError, TException>(Flow.ErrorOf<TSelf, T, TError>(result), out var match))
            return result;

        return Handle<TSelf, TError, TException>(match, handler);
    }

    /// <summary>
    /// Replaces the error with <paramref name="fallback"/>.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError>(TSelf result, TSelf fallback)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        result.TryGetValue(out _) ? result : fallback;

    /// <summary>
    /// Handles the error. Return success to recover, or an error to replace it.
    /// </summary>
    public static TSelf Catch<TSelf, TError>(TSelf result, Func<TError, TSelf> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.TryGetError(out var error) ? Handle<TSelf, TError, TError>(error, handler) : result;
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> only, also as a case of an error union. Other errors pass through.
    /// </summary>
    [OperatorName("Catch")]
    public static TSelf CatchCase<TSelf, TError, TException>(TSelf result, Func<TException, TSelf> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.TryGetError(out var error) && Flow.TryGetCase<TError, TException>(error, out var match)
            ? Handle<TSelf, TError, TException>(match, handler)
            : result;
    }

    /// <summary>
    /// Replaces the error with <paramref name="fallback"/>.
    /// </summary>
    public static TSelf Catch<TSelf, TError>(TSelf result, TSelf fallback)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull =>
        result.TryGetError(out _) ? fallback : result;

    private static TSelf Handle<TSelf, TError, TCase>(TCase error, Func<TCase, TSelf> handler)
        where TSelf : struct, IErrorResult<TSelf, TError>
        where TError : notnull
    {
        try
        {
            return handler(error);
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }
}
