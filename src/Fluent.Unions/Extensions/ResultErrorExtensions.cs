namespace Fluent.Unions.Extensions;

/// <summary>
/// Rx-style operators that run only when a result holds an exception. Values and empty results pass through.
/// </summary>
public static class ResultErrorExtensions
{
    /// <summary>
    /// Handles the error. Return a value to recover, or an exception to replace the error.
    /// </summary>
    public static Result<T> Catch<T>(this Result<T> result, Func<Exception, Result<T>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is Exception error ? Capture.Run(() => handler(error)) : result;
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> only. Other errors pass through.
    /// </summary>
    public static Result<T> Catch<T, TException>(this Result<T> result, Func<TException, Result<T>> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TException error ? Capture.Run(() => handler(error)) : result;
    }

    /// <summary>
    /// Replaces the error with <paramref name="fallback"/>.
    /// </summary>
    public static Result<T> Catch<T>(this Result<T> result, Result<T> fallback) =>
        result.Value is Exception ? fallback : result;

    /// <summary>
    /// Handles the error. Return <see cref="Result.Ok"/> to recover, or an exception to replace the error.
    /// </summary>
    public static Result Catch(this Result result, Func<Exception, Result> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is Exception error ? Capture.Run(() => handler(error)) : result;
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> only. Other errors pass through.
    /// </summary>
    public static Result Catch<TException>(this Result result, Func<TException, Result> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TException error ? Capture.Run(() => handler(error)) : result;
    }

    /// <summary>
    /// Replaces the error with <paramref name="fallback"/>.
    /// </summary>
    public static Result Catch(this Result result, Result fallback) =>
        result.Value is Exception ? fallback : result;
}
