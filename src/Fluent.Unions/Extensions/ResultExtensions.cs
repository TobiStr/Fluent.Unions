using System.Runtime.ExceptionServices;

namespace Fluent.Unions;

/// <summary>
/// Operators that leave a result chain.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Leaves the result with one callback for the value and one for the error.
    /// </summary>
    public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<Exception, TOut> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.TryGetError(out var error) ? onError(error) : onSuccess(result.GetValue());
    }

    /// <summary>
    /// Leaves the result with one callback for success and one for the error.
    /// </summary>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Exception, TOut> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.TryGetError(out var error) ? onError(error) : onSuccess();
    }

    /// <summary>
    /// Returns the result when it is successful, otherwise throws its error with the original stack trace.
    /// </summary>
    public static Result Check(this Result result)
    {
        if (result.TryGetError(out var error))
            ExceptionDispatchInfo.Throw(error);

        return result;
    }

    /// <summary>
    /// Returns the result when it holds a value, otherwise throws its error with the original stack trace.
    /// </summary>
    public static Result<T> Check<T>(this Result<T> result)
    {
        if (result.TryGetError(out var error))
            ExceptionDispatchInfo.Throw(error);

        return result;
    }
}
