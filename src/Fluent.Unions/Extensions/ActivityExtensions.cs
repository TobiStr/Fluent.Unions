using System.Diagnostics;

namespace Fluent.Unions;

/// <summary>
/// Records the outcome of a result on an <see cref="Activity"/>.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets the status from the result and records the exception of an error.
    /// </summary>
    public static Activity? RecordResult(this Activity? activity, Result result) =>
        global::Fluent.Unions.Core.ResultConversionCore.RecordUnitResult<Result, Exception>(activity, result);

    /// <inheritdoc cref="RecordResult(Activity?, Result)"/>
    public static Activity? RecordResult<T>(this Activity? activity, Result<T> result) =>
        global::Fluent.Unions.Core.ResultConversionCore.RecordResult<Result<T>, T, Exception>(activity, result);

    internal static Activity? Record(Activity? activity, Exception? error)
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
        else
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }

        return activity;
    }
}
