using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Serialization;

namespace Fluent.Unions;

/// <summary>
/// The success case of a <see cref="Result"/>.
/// </summary>
public readonly record struct Ok;

/// <summary>
/// The outcome of an operation without a value: <see cref="Ok"/> or an <see cref="Exception"/>.
/// A <c>default</c> result counts as an error.
/// </summary>
[JsonConverter(typeof(ResultJsonConverter))]
public union Result(Ok, Exception)
{
    /// <summary>
    /// Indicates whether the result is <see cref="Ok"/>.
    /// </summary>
    public bool IsSuccess => Value is Ok;

    /// <summary>
    /// Indicates whether the result holds an error.
    /// </summary>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result is <see cref="Ok"/>.</exception>
    public Exception GetError() => TryGetError(out var error)
        ? error
        : throw new InvalidOperationException("The Result is successful and holds no error.");

    /// <summary>
    /// Gets the error, if the result holds one.
    /// </summary>
    public bool TryGetError([NotNullWhen(true)] out Exception? error)
    {
        error = Case as Exception;
        return error is not null;
    }

    internal object Case => Value ?? ResultErrors.Uninitialized(typeof(Result));

    /// <summary>
    /// A successful result.
    /// </summary>
    public static Result Ok => new Ok();

    /// <summary>
    /// A failed result.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <c>null</c>.</exception>
    public static Result Error(Exception error) => error ?? throw new ArgumentNullException(nameof(error));

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

        return Capture.Run(() => Result<T>.Of(body()));
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

        return Capture.RunAsync(async () => Result<T>.Of(await body().ConfigureAwait(false)));
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
    /// and a faulted task counts as an error.
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
            results => results.FirstOrDefault(result => result.IsError, Ok));
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

                if (!result.TryGetError(out var error) || attempt == maxAttempts || !(shouldRetry?.Invoke(error) ?? true))
                    return result;
            }
        });
    }

    /// <summary>
    /// Runs <paramref name="operation"/> until it does not fail, at most <paramref name="maxAttempts"/> times, waiting
    /// <paramref name="delay"/> between attempts, and returns the last result. Thrown exceptions count as failures.
    /// Cancellation throws <see cref="OperationCanceledException"/>.
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

                if (!result.TryGetError(out var error) || attempt == maxAttempts || !(shouldRetry?.Invoke(error) ?? true))
                    return result;

                cancellationToken.ThrowIfCancellationRequested();

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        });
    }

    /// <summary>Combines two results into a tuple, or collects every error like <c>CombineAll()</c> on a sequence.</summary>
    public static Result<(T1, T2)> CombineAll<T1, T2>(Result<T1> first, Result<T2> second)
    {
        if (CollectErrors(first.Case, second.Case) is { } error)
            return error;

        return (first.GetValue(), second.GetValue());
    }

    /// <summary>Combines three results into a tuple, or collects every error.</summary>
    public static Result<(T1, T2, T3)> CombineAll<T1, T2, T3>(Result<T1> first, Result<T2> second, Result<T3> third)
    {
        if (CollectErrors(first.Case, second.Case, third.Case) is { } error)
            return error;

        return (first.GetValue(), second.GetValue(), third.GetValue());
    }

    /// <summary>Combines four results into a tuple, or collects every error.</summary>
    public static Result<(T1, T2, T3, T4)> CombineAll<T1, T2, T3, T4>(
        Result<T1> first,
        Result<T2> second,
        Result<T3> third,
        Result<T4> fourth)
    {
        if (CollectErrors(first.Case, second.Case, third.Case, fourth.Case) is { } error)
            return error;

        return (first.GetValue(), second.GetValue(), third.GetValue(), fourth.GetValue());
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

    private static Exception? CollectErrors(params object[] values)
    {
        var errors = values.OfType<Exception>().ToList();

        return errors.Count == 0 ? null : ValidationErrors.Combine(errors);
    }
}

/// <summary>
/// The outcome of an operation: a <typeparamref name="T"/> or an <see cref="Exception"/>.
/// A <c>default</c> result, or one created from <c>null</c>, counts as an error.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
[JsonConverter(typeof(ResultJsonConverter))]
public union Result<T>(T, Exception) : IResult<Result<T>, T, Exception>
{
    static Result<T> IResult<Result<T>, T, Exception>.FromValue(T value) => Of(value);

    static Result<T> IErrorResult<Result<T>, Exception>.FromError(Exception error) => error;

    static Exception IErrorResult<Result<T>, Exception>.FromException(Exception exception) => exception;

    /// <summary>
    /// Indicates whether the result holds a value.
    /// </summary>
    public bool IsSuccess => Value is not (null or Exception);

    /// <summary>
    /// Indicates whether the result holds an error.
    /// </summary>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result holds an error, which becomes the inner exception.</exception>
    public T GetValue() => Case switch
    {
        Exception error => throw new InvalidOperationException(
            $"{ResultErrors.Name(typeof(Result<T>))} holds an error, not a value: {error.Message}", error),
        var value => (T)value
    };

    /// <summary>
    /// Gets the value, if the result holds one.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (IsSuccess)
        {
            value = (T)Value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result holds a value.</exception>
    public Exception GetError() => TryGetError(out var error)
        ? error
        : throw new InvalidOperationException($"{ResultErrors.Name(typeof(Result<T>))} holds a value, not an error.");

    /// <summary>
    /// Gets the error, if the result holds one.
    /// </summary>
    public bool TryGetError([NotNullWhen(true)] out Exception? error)
    {
        error = Case as Exception;
        return error is not null;
    }

    internal object Case => Value ?? ResultErrors.Uninitialized(typeof(Result<T>));

    internal static Result<T> Of(T? value) =>
        value is null ? new Result<T>(ResultErrors.Uninitialized(typeof(Result<T>))) : new Result<T>(value);

    /// <summary>
    /// A successful result.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static Result<T> Ok(T value) => new(value ?? throw new ArgumentNullException(nameof(value)));

    /// <summary>
    /// A failed result.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <c>null</c>.</exception>
    public static Result<T> Error(Exception error) => error ?? throw new ArgumentNullException(nameof(error));

    /// <summary>
    /// Splits the result Go style: <c>var (value, error) = result;</c>. Exactly one of them is set.
    /// </summary>
    public void Deconstruct(out T? value, out Exception? error)
    {
        TryGetValue(out value);
        TryGetError(out error);
    }

    /// <summary>
    /// Drops the value and keeps success or the error.
    /// </summary>
    public static implicit operator Result(Result<T> result) => result.Case switch
    {
        Exception error => error,
        _ => Result.Ok
    };

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? "";
}
