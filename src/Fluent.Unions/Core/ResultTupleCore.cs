using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Operators that append a value to a tuple, and that attach or discard the value of a result.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultTupleCore
{
    /// <summary>
    /// Runs a dependent step and keeps both values as a tuple. An error passes through and the
    /// selector does not run. An empty selector result makes the result empty.
    /// </summary>
    public static TOutResult BindAppend<TSelf, T, TError, TNextResult, TNext, TOutResult>(TSelf result, Func<T, TNextResult> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

        TNextResult outcome;
        try
        {
            outcome = selector(value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }

        if (!outcome.TryGetValue(out var next))
            return TOutResult.FromError(Flow.ErrorOf<TNextResult, TNext, TError>(outcome));

        return TOutResult.FromValue((value, next));
    }

    /// <summary>
    /// Runs a dependent step with the tuple elements and appends its value to the tuple.
    /// </summary>
    public static TOutResult BindAppend<TSelf, T1, T2, TError, TNextResult, TNext, TOutResult>(
        TSelf result,
        Func<T1, T2, TNextResult> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return TOutResult.FromError(Flow.ErrorOf<TSelf, (T1, T2), TError>(result));

        TNextResult outcome;
        try
        {
            outcome = selector(value.Item1, value.Item2);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }

        if (!outcome.TryGetValue(out var next))
            return TOutResult.FromError(Flow.ErrorOf<TNextResult, TNext, TError>(outcome));

        return TOutResult.FromValue((value.Item1, value.Item2, next));
    }

    /// <summary>
    /// Runs a dependent step with the tuple elements and appends its value to the tuple.
    /// </summary>
    public static TOutResult BindAppend<TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult>(
        TSelf result,
        Func<T1, T2, T3, TNextResult> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2, T3), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return TOutResult.FromError(Flow.ErrorOf<TSelf, (T1, T2, T3), TError>(result));

        TNextResult outcome;
        try
        {
            outcome = selector(value.Item1, value.Item2, value.Item3);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }

        if (!outcome.TryGetValue(out var next))
            return TOutResult.FromError(Flow.ErrorOf<TNextResult, TNext, TError>(outcome));

        return TOutResult.FromValue((value.Item1, value.Item2, value.Item3, next));
    }

    /// <summary>
    /// Gives a successful result without a value the value. An error passes through and an empty result stays empty.
    /// </summary>
    public static TOutResult WithValue<TSelf, TError, TOutResult, T>(TSelf result, T value)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return result.TryGetError(out var error) ? TOutResult.FromError(error) : TOutResult.FromValue(value);
    }

    /// <summary>
    /// Drops the value and keeps success, the error or the empty state.
    /// </summary>
    public static TUnit DiscardValue<TSelf, T, TError, TUnit>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull =>
        result.TryGetValue(out _) ? TUnit.Success : TUnit.FromError(Flow.ErrorOf<TSelf, T, TError>(result));
}
