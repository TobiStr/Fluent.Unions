using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

public static partial class ResultTaskCore
{
    /// <summary>
    /// Chains an asynchronous operation and combines both values. Enables async query syntax.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TOutResult> SelectMany<TSelf, T, TError, TMidResult, TMid, TOutResult, TOut>(
        Task<TSelf> task,
        Func<T, Task<TMidResult>> selector,
        Func<T, TMid, TOut> resultSelector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TMidResult : struct, IFluentResult<TMidResult, TMid, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector, resultSelector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, Task<TMidResult>> selector, Func<T, TMid, TOut> resultSelector)
        {
            var result = await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false);
            if (!result.TryGetValue(out var value))
                return TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

            TMidResult middle;
            try
            {
                middle = await Flow.Settle<TMidResult, TError>(selector(value)).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Flow.Thrown<TOutResult, TError>(exception);
            }

            if (!middle.TryGetValue(out var mid))
                return TOutResult.FromError(Flow.ErrorOf<TMidResult, TMid, TError>(middle));

            try
            {
                return TOutResult.FromValue(resultSelector(value, mid));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Flow.Thrown<TOutResult, TError>(exception);
            }
        }
    }

    /// <inheritdoc cref="ResultQueryCore.Zip{TFirst, T1, TSecond, T2, TError, TOutResult}(TFirst, TSecond)"/>
    public static Task<TOutResult> Zip<TFirst, T1, TSecond, T2, TError, TOutResult>(Task<TFirst> first, TSecond second)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1 First, T2 Second), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(first);

        return Run(first, second);

        static async Task<TOutResult> Run(Task<TFirst> first, TSecond second) =>
            ResultQueryCore.Zip<TFirst, T1, TSecond, T2, TError, TOutResult>(
                await Flow.Settle<TFirst, TError>(first).ConfigureAwait(false),
                second);
    }

    /// <inheritdoc cref="ResultQueryCore.Zip{TFirst, T1, TSecond, T2, TError, TOutResult, TOut}(TFirst, TSecond, Func{T1, T2, TOut})"/>
    public static Task<TOutResult> Zip<TFirst, T1, TSecond, T2, TError, TOutResult, TOut>(
        Task<TFirst> first,
        TSecond second,
        Func<T1, T2, TOut> resultSelector)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(resultSelector);
        ArgumentNullException.ThrowIfNull(first);

        return Run(first, second, resultSelector);

        static async Task<TOutResult> Run(Task<TFirst> first, TSecond second, Func<T1, T2, TOut> resultSelector) =>
            ResultQueryCore.Zip<TFirst, T1, TSecond, T2, TError, TOutResult, TOut>(
                await Flow.Settle<TFirst, TError>(first).ConfigureAwait(false),
                second,
                resultSelector);
    }

    /// <summary>
    /// Captures the value for later steps of the chain. <paramref name="value"/> is assigned at once
    /// and filled when the task completes with a value, before any later step runs.
    /// </summary>
    public static Task<TSelf> Out<TSelf, T, TError>(Task<TSelf> task, out Captured<T> value)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        var captured = new Captured<T>();
        value = captured;
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, captured);

        static async Task<TSelf> Run(Task<TSelf> task, Captured<T> captured)
        {
            var result = await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false);
            if (result.TryGetValue(out var payload))
                captured.Set(payload);

            return result;
        }
    }

    /// <summary>
    /// Awaits the result and throws its error if it was not successful, preserving the stack trace.
    /// </summary>
    public static async Task<TSelf> Check<TSelf, T, TError>(Task<TSelf> task)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        ResultCore.Check<TSelf, T, TError>(await task.ConfigureAwait(false));

    /// <summary>
    /// Awaits the result and throws its error if it was not successful, preserving the stack trace.
    /// </summary>
    public static async Task<TSelf> Check<TSelf, TError>(Task<TSelf> task)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull =>
        ResultCore.Check<TSelf, TError>(await task.ConfigureAwait(false));
}
