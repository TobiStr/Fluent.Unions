using System.Text.Json.Serialization;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Extensions;
using Fluent.Unions.Serialization;

namespace Fluent.Unions;

/// <summary>
/// Marks a successful <see cref="Result"/>.
/// </summary>
public readonly record struct Ok;

/// <summary>
/// Used to determine, whether a method executes successfully or not. Has implicit cast implemented:
/// Return <see cref="Exception"/> (or inherited type) to return a Result with Success set to 'false'.
/// </summary>
[JsonConverter(typeof(ResultJsonConverter))]
public union Result(Ok, Exception)
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success => Value is Ok;

    /// <summary>
    /// Gets the message of the Exception, if available.
    /// </summary>
    public string Message => (Value as Exception)?.Message ?? "";

    /// <summary>
    /// Gets the error payload of the failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Exception GetError() => Value as Exception
        ?? throw new InvalidOperationException("Error property for this Result not set.");

    /// <summary>
    /// Returns a positive Result.
    /// </summary>
    public static Result Ok => new Ok();

    /// <summary>
    /// Returns a negative Result with an Exception as payload.
    /// </summary>
    public static Result Error(Exception error) => error;

    /// <summary>
    /// Starts a successful result from a value, with the type inferred.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static Result<T> From<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Runs <paramref name="body"/> and returns its value, or the thrown exception as an error.
    /// </summary>
    public static Result<T> Try<T>(Func<T> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.Run(() => new Result<T>(body()));
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static Result<T> Try<T>(Func<Result<T>> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.Run(body);
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns <see cref="Ok"/>, or the thrown exception as an error.
    /// </summary>
    public static Result Try(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.Run(() =>
        {
            body();
            return Ok;
        });
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static Result Try(Func<Result> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.Run(body);
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns its value, or the thrown exception as an error.
    /// </summary>
    public static Task<Result<T>> TryAsync<T>(Func<Task<T>> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.RunAsync(async () => new Result<T>(await body().ConfigureAwait(false)));
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static Task<Result<T>> TryAsync<T>(Func<Task<Result<T>>> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.RunAsync(body);
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns <see cref="Ok"/>, or the thrown exception as an error.
    /// </summary>
    public static Task<Result> TryAsync(Func<Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.RunAsync(async () =>
        {
            await body().ConfigureAwait(false);
            return Ok;
        });
    }

    /// <summary>
    /// Awaits <paramref name="body"/> and returns its result, or the thrown exception as an error.
    /// </summary>
    public static Task<Result> TryAsync(Func<Task<Result>> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Capture.RunAsync(body);
    }

    /// <summary>
    /// Awaits all tasks and combines their values like <c>Combine()</c>: the first error in sequence order wins,
    /// a faulted task counts as an error, and empty results are skipped.
    /// </summary>
    public static Task<Result<IReadOnlyList<T>>> WhenAll<T>(IEnumerable<Task<Result<T>>> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return Capture.Then(Task.WhenAll(tasks.Select(Capture.Settle)), results => results.Combine());
    }

    /// <summary>
    /// Awaits all tasks and returns <see cref="Ok"/>, or the first error in sequence order. A faulted task counts as an error.
    /// </summary>
    public static Task<Result> WhenAll(IEnumerable<Task<Result>> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return Capture.Then(
            Task.WhenAll(tasks.Select(Capture.Settle)),
            results => results.FirstOrDefault(result => result.Value is Exception) is { Value: Exception error }
                ? Error(error)
                : Ok);
    }

    /// <summary>
    /// Runs <paramref name="operation"/> until it does not fail, at most <paramref name="maxAttempts"/> times, and returns the last result.
    /// Thrown exceptions count as failures. <paramref name="shouldRetry"/> can limit which errors are retried.
    /// </summary>
    public static Result<T> Retry<T>(Func<Result<T>> operation, int maxAttempts, Func<Exception, bool>? shouldRetry = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        return Capture.Run(() =>
        {
            for (var attempt = 1; ; attempt++)
            {
                var result = Capture.Run(operation);

                if (result.Value is not Exception error || attempt == maxAttempts || !(shouldRetry?.Invoke(error) ?? true))
                    return result;
            }
        });
    }

    /// <summary>
    /// Runs <paramref name="operation"/> until it does not fail, at most <paramref name="maxAttempts"/> times, waiting
    /// <paramref name="delay"/> between attempts, and returns the last result. Thrown exceptions count as failures.
    /// Cancellation stops retrying and gives an error holding the <see cref="OperationCanceledException"/>.
    /// </summary>
    public static Task<Result<T>> RetryAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        int maxAttempts,
        TimeSpan delay = default,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        return Capture.RunAsync(async () =>
        {
            for (var attempt = 1; ; attempt++)
            {
                var result = await Capture.RunAsync(() => operation(cancellationToken)).ConfigureAwait(false);

                if (result.Value is not Exception error
                    || attempt == maxAttempts
                    || cancellationToken.IsCancellationRequested
                    || !(shouldRetry?.Invoke(error) ?? true))
                    return result;

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        });
    }

    /// <summary>Combines two results into a tuple, or collects every error. An empty side makes it empty.</summary>
    public static Result<(T1, T2)> CombineAll<T1, T2>(Result<T1> first, Result<T2> second)
    {
        if (CollectErrors(first.Value, second.Value) is { } error)
            return error;

        return (first.Value, second.Value) is (T1 a, T2 b) ? new Result<(T1, T2)>((a, b)) : default;
    }

    /// <summary>Combines three results into a tuple, or collects every error. An empty side makes it empty.</summary>
    public static Result<(T1, T2, T3)> CombineAll<T1, T2, T3>(Result<T1> first, Result<T2> second, Result<T3> third)
    {
        if (CollectErrors(first.Value, second.Value, third.Value) is { } error)
            return error;

        return (first.Value, second.Value, third.Value) is (T1 a, T2 b, T3 c)
            ? new Result<(T1, T2, T3)>((a, b, c))
            : default;
    }

    /// <summary>Combines four results into a tuple, or collects every error. An empty side makes it empty.</summary>
    public static Result<(T1, T2, T3, T4)> CombineAll<T1, T2, T3, T4>(
        Result<T1> first,
        Result<T2> second,
        Result<T3> third,
        Result<T4> fourth)
    {
        if (CollectErrors(first.Value, second.Value, third.Value, fourth.Value) is { } error)
            return error;

        return (first.Value, second.Value, third.Value, fourth.Value) is (T1 a, T2 b, T3 c, T4 d)
            ? new Result<(T1, T2, T3, T4)>((a, b, c, d))
            : default;
    }

    /// <summary>
    /// Awaits both tasks and combines them like <see cref="CombineAll{T1, T2}"/>. A faulted task counts as an error.
    /// </summary>
    public static Task<Result<(T1, T2)>> CombineAllAsync<T1, T2>(Task<Result<T1>> first, Task<Result<T2>> second)
    {
        return Run(Capture.Settle(first), Capture.Settle(second));

        static async Task<Result<(T1, T2)>> Run(Task<Result<T1>> first, Task<Result<T2>> second)
        {
            await Task.WhenAll(first, second).ConfigureAwait(false);

            return CombineAll(await first.ConfigureAwait(false), await second.ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Awaits all three tasks and combines them like <see cref="CombineAll{T1, T2, T3}"/>. A faulted task counts as an error.
    /// </summary>
    public static Task<Result<(T1, T2, T3)>> CombineAllAsync<T1, T2, T3>(
        Task<Result<T1>> first,
        Task<Result<T2>> second,
        Task<Result<T3>> third)
    {
        return Run(Capture.Settle(first), Capture.Settle(second), Capture.Settle(third));

        static async Task<Result<(T1, T2, T3)>> Run(
            Task<Result<T1>> first,
            Task<Result<T2>> second,
            Task<Result<T3>> third)
        {
            await Task.WhenAll(first, second, third).ConfigureAwait(false);

            return CombineAll(
                await first.ConfigureAwait(false),
                await second.ConfigureAwait(false),
                await third.ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Awaits all four tasks and combines them like <see cref="CombineAll{T1, T2, T3, T4}"/>. A faulted task counts as an error.
    /// </summary>
    public static Task<Result<(T1, T2, T3, T4)>> CombineAllAsync<T1, T2, T3, T4>(
        Task<Result<T1>> first,
        Task<Result<T2>> second,
        Task<Result<T3>> third,
        Task<Result<T4>> fourth)
    {
        return Run(Capture.Settle(first), Capture.Settle(second), Capture.Settle(third), Capture.Settle(fourth));

        static async Task<Result<(T1, T2, T3, T4)>> Run(
            Task<Result<T1>> first,
            Task<Result<T2>> second,
            Task<Result<T3>> third,
            Task<Result<T4>> fourth)
        {
            await Task.WhenAll(first, second, third, fourth).ConfigureAwait(false);

            return CombineAll(
                await first.ConfigureAwait(false),
                await second.ConfigureAwait(false),
                await third.ConfigureAwait(false),
                await fourth.ConfigureAwait(false));
        }
    }

    private static AggregateException? CollectErrors(params object?[] values)
    {
        var errors = values.OfType<Exception>().ToList();

        return errors.Count == 0 ? null : new AggregateException(errors).Flatten();
    }
}

/// <summary>
/// Used to determine, whether a method executes successfully or not. Has implicit casts implemented:
/// Return <typeparamref name="T"/> to return a successful Result with this payload.
/// Return <see cref="Exception"/> (or inherited type) to return a Result with Success set to 'false'.
/// </summary>
/// <typeparam name="T">The type of the payload.</typeparam>
[JsonConverter(typeof(ResultJsonConverter))]
public union Result<T>(T, Exception) : IResult<Result<T>, T, Exception>
{
    static Result<T> IResult<Result<T>, T, Exception>.FromValue(T value) => new(value);

    static Result<T> IErrorResult<Result<T>, Exception>.FromError(Exception error) => error;

    static Exception IErrorResult<Result<T>, Exception>.FromException(Exception exception) => exception;

    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success => Value is T;

    /// <summary>
    /// Gets the message of the Exception, if available.
    /// </summary>
    public string Message => (Value as Exception)?.Message ?? "";

    /// <summary>
    /// Gets the underlying payload of the successful result.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T GetOk() => Value switch
    {
        T payload => payload,
        Exception => throw new InvalidOperationException($"Operation for Result<{typeof(T)}> was not successful."),
        _ => throw new InvalidOperationException($"Payload for Result<{typeof(T)}> was not set.")
    };

    /// <summary>
    /// Gets the error payload of the failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Exception GetError() => Value as Exception
        ?? throw new InvalidOperationException($"Error property for Result<{typeof(T)}> not set.");

    /// <summary>
    /// Returns a positive Result with an object as payload.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static Result<T> Ok(T payload) =>
        new(payload ?? throw new ArgumentNullException(nameof(payload)));

    /// <summary>
    /// Returns a negative Result with an Exception as payload.
    /// </summary>
    public static Result<T> Error(Exception error) => error;

    /// <summary>
    /// Splits the result Go style: <c>var (value, error) = result;</c>. An empty result gives <c>default</c> and <c>null</c>.
    /// </summary>
    public void Deconstruct(out T? value, out Exception? error)
    {
        switch (Value)
        {
            case Exception exception:
                value = default;
                error = exception;
                break;
            case T payload:
                value = payload;
                error = null;
                break;
            default:
                value = default;
                error = null;
                break;
        }
    }

    /// <summary>
    /// Implicit cast from Result&lt;T&gt; to <see cref="Result"/>, keeping Success and the error.
    /// </summary>
    public static implicit operator Result(Result<T> result) => result.Value switch
    {
        Exception error => error,
        T => Result.Ok,
        _ => default
    };

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? "";
}
