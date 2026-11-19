using System.Diagnostics;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Extensions;

/// <summary>
/// Records the outcome of a result on an <see cref="Activity"/>.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets the status from the result and records the exception of an error. An empty result is an error without an exception event.
    /// </summary>
    public static Activity? RecordResult(this Activity? activity, Result result) =>
        Record(activity, result.Success, result.Value as Exception);

    /// <inheritdoc cref="RecordResult(Activity?, Result)"/>
    public static Activity? RecordResult<T>(this Activity? activity, Result<T> result) =>
        Record(activity, result.Success, result.Value as Exception);

    /// <summary>
    /// Sets the status from the result and records the error. An error that is not an exception is wrapped in a <see cref="ResultErrorException"/>.
    /// </summary>
    public static Activity? RecordResult<T, TError>(this Activity? activity, Result<T, TError> result)
        where TError : notnull, IFromException<TError> =>
        Record(activity, result.Success, result.Value is TError error ? error as Exception ?? new ResultErrorException(error) : null);

    private static Activity? Record(Activity? activity, bool success, Exception? error)
    {
        if (activity is null)
        {
            return null;
        }

        if (error is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, error.Message);
            activity.AddException(error);
        }
        else if (success)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, "Result is empty");
        }

        return activity;
    }
}
