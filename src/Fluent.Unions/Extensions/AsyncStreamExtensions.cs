using System.Runtime.CompilerServices;

namespace Fluent.Unions;

/// <summary>
/// Operators for asynchronous streams of results and options, and timeouts for result tasks.
/// </summary>
public static class AsyncStreamExtensions
{
    /// <summary>
    /// Streams the values of the successful results.
    /// </summary>
    public static IAsyncEnumerable<T> Values<T>(this IAsyncEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Iterate(results);

        static async IAsyncEnumerable<T> Iterate(
            IAsyncEnumerable<Result<T>> results,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (result.Case is not Exception && result.Case is T value)
                    yield return value;
            }
        }
    }

    /// <summary>
    /// Streams the errors of the failed results.
    /// </summary>
    public static IAsyncEnumerable<Exception> Errors<T>(this IAsyncEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Iterate(results);

        static async IAsyncEnumerable<Exception> Iterate(
            IAsyncEnumerable<Result<T>> results,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (result.Case is Exception error)
                    yield return error;
            }
        }
    }

    /// <summary>
    /// Collects all values of the stream into one result, like <c>Combine()</c>. Stops at the first error.
    /// An exception thrown by the stream becomes the error.
    /// </summary>
    public static Task<Result<IReadOnlyList<T>>> CombineAsync<T>(
        this IAsyncEnumerable<Result<T>> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Capture.RunAsync(async () =>
        {
            var values = new List<T>();

            await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                switch (result.Case)
                {
                    case Exception error:
                        return error;
                    case T value:
                        values.Add(value);
                        break;
                }
            }

            return Result<IReadOnlyList<T>>.Of(values);
        });
    }

    /// <summary>
    /// Streams the values of the options that hold one.
    /// </summary>
    public static IAsyncEnumerable<T> Values<T>(this IAsyncEnumerable<Option<T>> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Iterate(options);

        static async IAsyncEnumerable<T> Iterate(
            IAsyncEnumerable<Option<T>> options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var option in options.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (option.TryGetValue(out var value))
                    yield return value;
            }
        }
    }

    /// <summary>
    /// Gives an error holding a <see cref="TimeoutException"/> when the task does not finish within <paramref name="timeout"/>.
    /// The operation itself keeps running; pass it a cancellation token to stop it.
    /// </summary>
    public static Task<Result<T>> Timeout<T>(this Task<Result<T>> task, TimeSpan timeout) =>
        global::Fluent.Unions.Core.ResultConversionCore.Timeout<Result<T>, T, Exception>(task, timeout);

    /// <inheritdoc cref="Timeout{T}(Task{Result{T}}, TimeSpan)"/>
    public static Task<Result> Timeout(this Task<Result> task, TimeSpan timeout) =>
        global::Fluent.Unions.Core.ResultConversionCore.TimeoutUnit<Result, Exception>(task, timeout);
}
