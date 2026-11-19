using System.Runtime.ExceptionServices;

namespace Fluent.Unions.Extensions;

/// <summary>
/// Operators that leave a result chain.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Leaves the result with a value: one callback for each state, so none of them is forgotten.
    /// </summary>
    public static TOut Match<T, TOut>(
        this Result<T> result,
        Func<T, TOut> onNext,
        Func<Exception, TOut> onError,
        Func<TOut> onEmpty)
    {
        ArgumentNullException.ThrowIfNull(onNext);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(onEmpty);

        return result.Value switch
        {
            Exception error => onError(error),
            T value => onNext(value),
            _ => onEmpty()
        };
    }

    /// <summary>
    /// Leaves the result with a value: one callback for each state, so none of them is forgotten.
    /// </summary>
    public static TOut Match<TOut>(
        this Result result,
        Func<TOut> onNext,
        Func<Exception, TOut> onError,
        Func<TOut> onEmpty)
    {
        ArgumentNullException.ThrowIfNull(onNext);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(onEmpty);

        return result.Value switch
        {
            Exception error => onError(error),
            Ok => onNext(),
            _ => onEmpty()
        };
    }

    /// <summary>
    /// Checks if the <see cref="Result"/> was not successful and throws the underlying exception, if available.
    /// While re-throwing, it preserves the StackTrace. Returns the Result if successful, so that it can be quickly checked.
    /// </summary>
    /// <param name="result"></param>
    public static Result Check(this Result result)
    {
        if (!result.Success)
            ExceptionDispatchInfo.Capture(result.GetError()).Throw();

        return result;
    }

    /// <summary>
    /// Checks if the <see cref="Result{T}"/> was not successful and throws the underlying exception, if available.
    /// While re-throwing, it preserves the StackTrace. Returns the Result if successful, so that it can be quickly checked.
    /// </summary>
    /// <param name="result"></param>
    public static Result<T> Check<T>(this Result<T> result)
    {
        if (!result.Success)
            ExceptionDispatchInfo.Capture(result.GetError()).Throw();

        return result;
    }
}
