using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Factories that become static members of the result type without a value.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultStaticCore
{
    /// <summary>
    /// Starts a successful result from a value, with the type inferred.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static TSelf From<TSelf, T, TError>(T value)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return TSelf.FromValue(value);
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns its value, or the thrown exception as an error.
    /// </summary>
    public static TSelf Try<TSelf, T, TError>(Func<T> body)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            return TSelf.FromValue(body());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static TSelf Try<TSelf, T, TError>(Func<TSelf> body)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            return body();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns success, or the thrown exception as an error.
    /// </summary>
    public static TSelf Try<TSelf, TError>(Action body)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            body();
            return TSelf.Success;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static TSelf Try<TSelf, TError>(Func<TSelf> body)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            return body();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns its value, or the thrown exception as an error.
    /// </summary>
    public static Task<TSelf> TryAsync<TSelf, T, TError>(Func<Task<T>> body)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        return Run(body);

        static async Task<TSelf> Run(Func<Task<T>> body)
        {
            try
            {
                return TSelf.FromValue(await body().ConfigureAwait(false));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Flow.Thrown<TSelf, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static Task<TSelf> TryAsync<TSelf, T, TError>(Func<Task<TSelf>> body)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        return Invoke<TSelf, TError>(body);
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns success, or the thrown exception as an error.
    /// </summary>
    public static Task<TSelf> TryAsync<TSelf, TError>(Func<Task> body)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        return Run(body);

        static async Task<TSelf> Run(Func<Task> body)
        {
            try
            {
                await body().ConfigureAwait(false);
                return TSelf.Success;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Flow.Thrown<TSelf, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static Task<TSelf> TryAsync<TSelf, TError>(Func<Task<TSelf>> body)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(body);

        return Invoke<TSelf, TError>(body);
    }

    /// <summary>
    /// Awaits all tasks and combines their values like <c>Combine()</c>: the first error in sequence order wins,
    /// and a faulted task counts as an error.
    /// </summary>
    public static Task<TOutResult> WhenAll<TSelf, T, TError, TOutResult>(IEnumerable<Task<TSelf>> tasks)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, IReadOnlyList<T>, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return Run(Task.WhenAll(tasks.Select(Settle<TSelf, TError>)));

        static async Task<TOutResult> Run(Task<TSelf[]> all) =>
            ResultCollectionCore.Combine<TSelf, T, TError, TOutResult>(await all.ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits all tasks and returns success, or the first error in sequence order. A faulted task counts as an error.
    /// </summary>
    public static Task<TSelf> WhenAll<TSelf, TError>(IEnumerable<Task<TSelf>> tasks)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return Run(Task.WhenAll(tasks.Select(Settle<TSelf, TError>)));

        static async Task<TSelf> Run(Task<TSelf[]> all)
        {
            foreach (var result in await all.ConfigureAwait(false))
            {
                if (result.TryGetError(out _))
                    return result;
            }

            return TSelf.Success;
        }
    }

    /// <summary>
    /// Runs <paramref name="operation"/> until it does not fail, at most <paramref name="maxAttempts"/> times, and returns the last result.
    /// Thrown exceptions count as failures. <paramref name="shouldRetry"/> can limit which errors are retried.
    /// </summary>
    public static TSelf Retry<TSelf, T, TError>(Func<TSelf> operation, int maxAttempts, Func<TError, bool>? shouldRetry = null)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        try
        {
            for (var attempt = 1; ; attempt++)
            {
                TSelf result;
                try
                {
                    result = operation();
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    result = Flow.Thrown<TSelf, TError>(exception);
                }

                if (!result.TryGetError(out var error) || attempt == maxAttempts || !(shouldRetry?.Invoke(error) ?? true))
                    return result;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="operation"/> until it does not fail, at most <paramref name="maxAttempts"/> times, waiting
    /// <paramref name="delay"/> between attempts, and returns the last result. Thrown exceptions count as failures.
    /// Cancellation throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public static Task<TSelf> RetryAsync<TSelf, T, TError>(
        Func<CancellationToken, Task<TSelf>> operation,
        int maxAttempts,
        TimeSpan delay = default,
        Func<TError, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        return Run(operation, maxAttempts, delay, shouldRetry, cancellationToken);

        static async Task<TSelf> Run(
            Func<CancellationToken, Task<TSelf>> operation,
            int maxAttempts,
            TimeSpan delay,
            Func<TError, bool>? shouldRetry,
            CancellationToken cancellationToken)
        {
            try
            {
                for (var attempt = 1; ; attempt++)
                {
                    TSelf result;
                    try
                    {
                        result = await operation(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        result = Flow.Thrown<TSelf, TError>(exception);
                    }

                    if (!result.TryGetError(out var error) || attempt == maxAttempts || !(shouldRetry?.Invoke(error) ?? true))
                        return result;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Flow.Thrown<TSelf, TError>(exception);
            }
        }
    }

    /// <summary>Combines two results into a tuple, or collects every error like <c>CombineAll()</c> on a sequence.</summary>
    public static TOutResult CombineAll<TFirst, T1, TSecond, T2, TError, TOutResult>(TFirst first, TSecond second)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2), TError>
        where TError : notnull
    {
        List<TError>? errors = null;
        var hasFirst = first.TryGetValue(out var a);
        if (!hasFirst)
            (errors ??= []).Add(Flow.ErrorOf<TFirst, T1, TError>(first));
        var hasSecond = second.TryGetValue(out var b);
        if (!hasSecond)
            (errors ??= []).Add(Flow.ErrorOf<TSecond, T2, TError>(second));

        return errors is null
            ? TOutResult.FromValue((a!, b!))
            : TOutResult.FromError(Flow.Combine<TOutResult, TError>(errors));
    }

    /// <summary>Combines three results into a tuple, or collects every error.</summary>
    public static TOutResult CombineAll<TFirst, T1, TSecond, T2, TThird, T3, TError, TOutResult>(TFirst first, TSecond second, TThird third)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TThird : struct, IFluentResult<TThird, T3, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3), TError>
        where TError : notnull
    {
        List<TError>? errors = null;
        if (!first.TryGetValue(out var a))
            (errors ??= []).Add(Flow.ErrorOf<TFirst, T1, TError>(first));
        if (!second.TryGetValue(out var b))
            (errors ??= []).Add(Flow.ErrorOf<TSecond, T2, TError>(second));
        if (!third.TryGetValue(out var c))
            (errors ??= []).Add(Flow.ErrorOf<TThird, T3, TError>(third));

        return errors is null
            ? TOutResult.FromValue((a!, b!, c!))
            : TOutResult.FromError(Flow.Combine<TOutResult, TError>(errors));
    }

    /// <summary>Combines four results into a tuple, or collects every error.</summary>
    public static TOutResult CombineAll<TFirst, T1, TSecond, T2, TThird, T3, TFourth, T4, TError, TOutResult>(
        TFirst first,
        TSecond second,
        TThird third,
        TFourth fourth)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TThird : struct, IFluentResult<TThird, T3, TError>
        where TFourth : struct, IFluentResult<TFourth, T4, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3, T4), TError>
        where TError : notnull
    {
        List<TError>? errors = null;
        if (!first.TryGetValue(out var a))
            (errors ??= []).Add(Flow.ErrorOf<TFirst, T1, TError>(first));
        if (!second.TryGetValue(out var b))
            (errors ??= []).Add(Flow.ErrorOf<TSecond, T2, TError>(second));
        if (!third.TryGetValue(out var c))
            (errors ??= []).Add(Flow.ErrorOf<TThird, T3, TError>(third));
        if (!fourth.TryGetValue(out var d))
            (errors ??= []).Add(Flow.ErrorOf<TFourth, T4, TError>(fourth));

        return errors is null
            ? TOutResult.FromValue((a!, b!, c!, d!))
            : TOutResult.FromError(Flow.Combine<TOutResult, TError>(errors));
    }

    /// <summary>
    /// Awaits both tasks and combines them like <c>CombineAll</c>. A faulted task counts as an error.
    /// </summary>
    public static Task<TOutResult> CombineAllAsync<TFirst, T1, TSecond, T2, TError, TOutResult>(Task<TFirst> first, Task<TSecond> second)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2), TError>
        where TError : notnull
    {
        return Run(Settle<TFirst, TError>(first), Settle<TSecond, TError>(second));

        static async Task<TOutResult> Run(Task<TFirst> first, Task<TSecond> second)
        {
            await Task.WhenAll(first, second).ConfigureAwait(false);

            return CombineAll<TFirst, T1, TSecond, T2, TError, TOutResult>(
                await first.ConfigureAwait(false),
                await second.ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Awaits all three tasks and combines them like <c>CombineAll</c>. A faulted task counts as an error.
    /// </summary>
    public static Task<TOutResult> CombineAllAsync<TFirst, T1, TSecond, T2, TThird, T3, TError, TOutResult>(
        Task<TFirst> first,
        Task<TSecond> second,
        Task<TThird> third)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TThird : struct, IFluentResult<TThird, T3, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3), TError>
        where TError : notnull
    {
        return Run(Settle<TFirst, TError>(first), Settle<TSecond, TError>(second), Settle<TThird, TError>(third));

        static async Task<TOutResult> Run(Task<TFirst> first, Task<TSecond> second, Task<TThird> third)
        {
            await Task.WhenAll(first, second, third).ConfigureAwait(false);

            return CombineAll<TFirst, T1, TSecond, T2, TThird, T3, TError, TOutResult>(
                await first.ConfigureAwait(false),
                await second.ConfigureAwait(false),
                await third.ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Awaits all four tasks and combines them like <c>CombineAll</c>. A faulted task counts as an error.
    /// </summary>
    public static Task<TOutResult> CombineAllAsync<TFirst, T1, TSecond, T2, TThird, T3, TFourth, T4, TError, TOutResult>(
        Task<TFirst> first,
        Task<TSecond> second,
        Task<TThird> third,
        Task<TFourth> fourth)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TThird : struct, IFluentResult<TThird, T3, TError>
        where TFourth : struct, IFluentResult<TFourth, T4, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3, T4), TError>
        where TError : notnull
    {
        return Run(
            Settle<TFirst, TError>(first),
            Settle<TSecond, TError>(second),
            Settle<TThird, TError>(third),
            Settle<TFourth, TError>(fourth));

        static async Task<TOutResult> Run(Task<TFirst> first, Task<TSecond> second, Task<TThird> third, Task<TFourth> fourth)
        {
            await Task.WhenAll(first, second, third, fourth).ConfigureAwait(false);

            return CombineAll<TFirst, T1, TSecond, T2, TThird, T3, TFourth, T4, TError, TOutResult>(
                await first.ConfigureAwait(false),
                await second.ConfigureAwait(false),
                await third.ConfigureAwait(false),
                await fourth.ConfigureAwait(false));
        }
    }

    private static Task<TResult> Settle<TResult, TError>(Task<TResult> task)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Flow.Settle<TResult, TError>(task).AsTask();
    }

    private static async Task<TResult> Invoke<TResult, TError>(Func<Task<TResult>> body)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TResult, TError>(exception);
        }
    }
}
