using System.Diagnostics;
using Fluent.Unions.Abstractions;

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

    /// <summary>
    /// Sets the status from the result and records the error. An error that is not an exception is wrapped in a <see cref="ResultErrorException"/>.
    /// </summary>
    public static Activity? RecordResult<T, TError>(this Activity? activity, Result<T, TError> result)
        where TError : notnull, IFromException<TError> =>
        Record(activity, result.TryGetError(out var error) ? error as Exception ?? new ResultErrorException(error) : null);

    /// <summary>
    /// Sets the status from a custom result and records the error. An error that is not an exception is wrapped in a <see cref="ResultErrorException"/>.
    /// </summary>
    public static Activity? RecordResult<TSelf, T, TError>(this Activity? activity, IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return Record(activity, result.TryGetError(out var error) ? error as Exception ?? new ResultErrorException(error) : null);
    }

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
