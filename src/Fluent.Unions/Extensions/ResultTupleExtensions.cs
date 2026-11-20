using System.Diagnostics;

namespace Fluent.Unions;

/// <summary>
/// Operators that append a value to a tuple, and that attach or discard the value of a result.
/// </summary>
public static class ResultTupleExtensions
{
    /// <summary>
    /// Runs a dependent step and keeps both values as a tuple. An error passes through and the
    /// selector does not run. An empty selector result makes the result empty.
    /// </summary>
    public static Result<(T, TNext)> BindAppend<T, TNext>(this Result<T> result, Func<T, Result<TNext>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.SelectMany(value => selector(value).Select(next => (value, next)));
    }

    /// <summary>
    /// Runs a dependent step with the tuple elements and appends its value to the tuple.
    /// </summary>
    public static Result<(T1, T2, TNext)> BindAppend<T1, T2, TNext>(
        this Result<(T1, T2)> result,
        Func<T1, T2, Result<TNext>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result
            .BindAppend(value => selector(value.Item1, value.Item2))
            .Select(pair => (pair.Item1.Item1, pair.Item1.Item2, pair.Item2));
    }

    /// <summary>
    /// Runs a dependent step with the tuple elements and appends its value to the tuple.
    /// </summary>
    public static Result<(T1, T2, T3, TNext)> BindAppend<T1, T2, T3, TNext>(
        this Result<(T1, T2, T3)> result,
        Func<T1, T2, T3, Result<TNext>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result
            .BindAppend(value => selector(value.Item1, value.Item2, value.Item3))
            .Select(pair => (pair.Item1.Item1, pair.Item1.Item2, pair.Item1.Item3, pair.Item2));
    }

    /// <summary>
    /// Gives a successful <see cref="Result"/> the value. An error passes through and an empty result stays empty.
    /// </summary>
    public static Result<T> WithValue<T>(this Result result, T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return result.Case switch
        {
            Exception error => error,
            Ok => Result<T>.Of(value),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Drops the value and keeps success, the error or the empty state.
    /// </summary>
    public static Result DiscardValue<T>(this Result<T> result) => result;
}
