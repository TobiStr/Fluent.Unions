using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Operators for sequences of results.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultCollectionCore
{
    /// <summary>
    /// Collects all values into one result. The first error wins.
    /// </summary>
    public static TOutResult Combine<TSelf, T, TError, TOutResult>(IEnumerable<TSelf> results)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, IReadOnlyList<T>, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(results);

        try
        {
            var values = new List<T>();
            foreach (var result in results)
            {
                if (!result.TryGetValue(out var value))
                    return TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

                values.Add(value);
            }

            return TOutResult.FromValue(values);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <summary>
    /// Collects all values into one result, or every error: a <see cref="ValidationErrors"/> when all of them are
    /// validation failures, otherwise an <see cref="AggregateException"/>.
    /// </summary>
    public static TOutResult CombineAll<TSelf, T, TError, TOutResult>(IEnumerable<TSelf> results)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, IReadOnlyList<T>, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(results);

        try
        {
            var (values, errors) = Partition<TSelf, T, TError>(results);

            return errors.Count == 0
                ? TOutResult.FromValue(values)
                : TOutResult.FromError(Flow.Combine<TOutResult, TError>(errors));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <summary>
    /// Splits the results into values and errors in one pass.
    /// </summary>
    public static (IReadOnlyList<T> Values, IReadOnlyList<TError> Errors) Partition<TSelf, T, TError>(IEnumerable<TSelf> results)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = new List<T>();
        var errors = new List<TError>();
        foreach (var result in results)
        {
            if (result.TryGetValue(out var value))
                values.Add(value);
            else
                errors.Add(Flow.ErrorOf<TSelf, T, TError>(result));
        }

        return (values, errors);
    }

    /// <summary>
    /// Returns the values of all successful results.
    /// </summary>
    public static IEnumerable<T> Values<TSelf, T, TError>(IEnumerable<TSelf> results)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(results);

        return Iterate(results);

        static IEnumerable<T> Iterate(IEnumerable<TSelf> results)
        {
            foreach (var result in results)
            {
                if (result.TryGetValue(out var value))
                    yield return value;
            }
        }
    }

    /// <summary>
    /// Returns the errors of all failed results.
    /// </summary>
    public static IEnumerable<TError> Errors<TSelf, T, TError>(IEnumerable<TSelf> results)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(results);

        return Iterate(results);

        static IEnumerable<TError> Iterate(IEnumerable<TSelf> results)
        {
            foreach (var result in results)
            {
                if (!result.TryGetValue(out _))
                    yield return Flow.ErrorOf<TSelf, T, TError>(result);
            }
        }
    }
}
