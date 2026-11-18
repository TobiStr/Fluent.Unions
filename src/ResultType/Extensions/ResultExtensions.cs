using System.Runtime.ExceptionServices;

namespace ResultType.Extensions;

public static class ResultExtensions
{
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
