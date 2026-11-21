using System.ComponentModel;
using System.Diagnostics;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Conversions between results and options, loadables and activities.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultConversionCore
{
    /// <summary>
    /// Keeps the value of a successful result. An error gives an option without a value.
    /// </summary>
    public static Option<T> ToOption<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        result.TryGetValue(out var value) ? Option<T>.Of(value) : Option<T>.NoValue;

    /// <summary>
    /// Swaps an optional result into a result of an option: no value gives ok with <see cref="None"/>,
    /// and an error stays an error.
    /// </summary>
    public static TOutResult Transpose<TSelf, T, TError, TOutResult>(Option<TSelf> option)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, Option<T>, TError>
        where TError : notnull
    {
        if (!option.TryGetValue(out var inner))
            return TOutResult.FromValue(Option<T>.NoValue);

        return inner.TryGetValue(out var value)
            ? TOutResult.FromValue(Option<T>.Of(value))
            : TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(inner));
    }

    /// <summary>
    /// Swaps a result of an option into an optional result: ok with <see cref="None"/> gives no value,
    /// and an error is kept inside the option. Reverses the other <c>Transpose</c>.
    /// </summary>
    public static Option<TOutResult> Transpose<TSelf, T, TError, TOutResult>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, Option<T>, TError>
        where TOutResult : struct, IFluentResult<TOutResult, T, TError>
        where TError : notnull
    {
        if (!result.TryGetValue(out var option))
            return new Option<TOutResult>(TOutResult.FromError(Flow.ErrorOf<TSelf, Option<T>, TError>(result)));

        return option.TryGetValue(out var value) ? new Option<TOutResult>(TOutResult.FromValue(value)) : Option<TOutResult>.NoValue;
    }

    /// <summary>
    /// Turns a finished result into a loaded or failed state. An error that is not an exception is wrapped in a
    /// <see cref="ResultErrorException"/>.
    /// </summary>
    public static Loadable<T> ToLoadable<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (result.TryGetValue(out var value))
            return new Loadable<T>(value);

        var error = Flow.ErrorOf<TSelf, T, TError>(result);
        return new Loadable<T>(Flow.AsException(error) ?? new ResultErrorException(error));
    }

    /// <summary>
    /// Awaits the result and turns it into a loaded or failed state. A faulted task counts as failed.
    /// </summary>
    public static Task<Loadable<T>> ToLoadable<TSelf, T, TError>(Task<TSelf> task)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Run(task);

        static async Task<Loadable<T>> Run(Task<TSelf> task) =>
            ToLoadable<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false));
    }

    /// <summary>
    /// Sets the status from the result and records the error. An error that is not an exception is wrapped in a
    /// <see cref="ResultErrorException"/>.
    /// </summary>
    public static Activity? RecordResult<TSelf, T, TError>(Activity? activity, TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        ActivityExtensions.Record(activity, result.TryGetValue(out _) ? null : ErrorAsException(Flow.ErrorOf<TSelf, T, TError>(result)));

    /// <summary>
    /// Sets the status from the result and records the error. An error that is not an exception is wrapped in a
    /// <see cref="ResultErrorException"/>.
    /// </summary>
    [OperatorName("RecordResult")]
    public static Activity? RecordUnitResult<TSelf, TError>(Activity? activity, TSelf result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull =>
        ActivityExtensions.Record(activity, result.TryGetError(out var error) ? ErrorAsException(error) : null);

    /// <summary>
    /// Gives an error from a <see cref="TimeoutException"/> when the task does not finish within <paramref name="timeout"/>.
    /// The operation itself keeps running; pass it a cancellation token to stop it.
    /// </summary>
    public static Task<TSelf> Timeout<TSelf, T, TError>(Task<TSelf> task, TimeSpan timeout)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Flow.Settle<TSelf, TError>(task.WaitAsync(timeout)).AsTask();
    }

    /// <inheritdoc cref="Timeout{TSelf, T, TError}(Task{TSelf}, TimeSpan)"/>
    [OperatorName("Timeout")]
    public static Task<TSelf> TimeoutUnit<TSelf, TError>(Task<TSelf> task, TimeSpan timeout)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Flow.Settle<TSelf, TError>(task.WaitAsync(timeout)).AsTask();
    }

    private static Exception ErrorAsException<TError>(TError error) where TError : notnull =>
        Flow.AsException(error) ?? new ResultErrorException(error);
}
