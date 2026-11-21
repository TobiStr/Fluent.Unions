using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// LINQ-style operators that run only when a result holds a value. Errors pass through.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultQueryCore
{
    /// <summary>
    /// Projects the value into a new form. A <c>null</c> projection gives an error.
    /// </summary>
    public static TOutResult Select<TSelf, T, TError, TOutResult, TOut>(TSelf result, Func<T, TOut> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

        try
        {
            return TOutResult.FromValue(selector(value));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <summary>
    /// Produces a value after a successful result without a value.
    /// </summary>
    public static TOutResult Select<TSelf, TError, TOutResult, TOut>(TSelf result, Func<TOut> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
            return TOutResult.FromError(error);

        try
        {
            return TOutResult.FromValue(selector());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <summary>
    /// Chains an operation that returns a result itself.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TOutResult SelectMany<TSelf, T, TError, TOutResult, TOut>(TSelf result, Func<T, TOutResult> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

        try
        {
            return selector(value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <summary>
    /// Chains an operation and combines both values. Enables query syntax with multiple <c>from</c> clauses.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TOutResult SelectMany<TSelf, T, TError, TMidResult, TMid, TOutResult, TOut>(
        TSelf result,
        Func<T, TMidResult> selector,
        Func<T, TMid, TOut> resultSelector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TMidResult : struct, IFluentResult<TMidResult, TMid, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        if (!result.TryGetValue(out var value))
            return TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

        TMidResult middle;
        try
        {
            middle = selector(value);
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

    /// <summary>
    /// Chains an operation that returns a result without a value.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TUnit SelectMany<TSelf, T, TError, TUnit>(TSelf result, Func<T, TUnit> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return TUnit.FromError(Flow.ErrorOf<TSelf, T, TError>(result));

        try
        {
            return selector(value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TUnit, TError>(exception);
        }
    }

    /// <summary>
    /// Chains an operation after a successful result without a value.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TSelf SelectMany<TSelf, TError>(TSelf result, Func<TSelf> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out _))
            return result;

        try
        {
            return selector();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Chains an operation that produces a value after a successful result without a value.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TOutResult SelectMany<TSelf, TError, TOutResult, TOut>(TSelf result, Func<TOutResult> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
            return TOutResult.FromError(error);

        try
        {
            return selector();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <inheritdoc cref="SelectMany{TSelf, T, TError, TOutResult, TOut}(TSelf, Func{T, TOutResult})"/>
    public static TOutResult Chain<TSelf, T, TError, TOutResult, TOut>(TSelf result, Func<T, TOutResult> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull =>
        SelectMany<TSelf, T, TError, TOutResult, TOut>(result, selector);

    /// <inheritdoc cref="SelectMany{TSelf, T, TError, TUnit}(TSelf, Func{T, TUnit})"/>
    public static TUnit Chain<TSelf, T, TError, TUnit>(TSelf result, Func<T, TUnit> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull =>
        SelectMany<TSelf, T, TError, TUnit>(result, selector);

    /// <inheritdoc cref="SelectMany{TSelf, TError}(TSelf, Func{TSelf})"/>
    public static TSelf Chain<TSelf, TError>(TSelf result, Func<TSelf> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull =>
        SelectMany<TSelf, TError>(result, selector);

    /// <inheritdoc cref="SelectMany{TSelf, TError, TOutResult, TOut}(TSelf, Func{TOutResult})"/>
    public static TOutResult Chain<TSelf, TError, TOutResult, TOut>(TSelf result, Func<TOutResult> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull =>
        SelectMany<TSelf, TError, TOutResult, TOut>(result, selector);

    /// <summary>
    /// Combines two results into a tuple. The first error wins.
    /// </summary>
    public static TOutResult Zip<TFirst, T1, TSecond, T2, TError, TOutResult>(TFirst first, TSecond second)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1 First, T2 Second), TError>
        where TError : notnull
    {
        if (!first.TryGetValue(out var a))
            return TOutResult.FromError(Flow.ErrorOf<TFirst, T1, TError>(first));

        if (!second.TryGetValue(out var b))
            return TOutResult.FromError(Flow.ErrorOf<TSecond, T2, TError>(second));

        return TOutResult.FromValue((a, b));
    }

    /// <summary>
    /// Combines two results with a selector. The first error wins.
    /// </summary>
    public static TOutResult Zip<TFirst, T1, TSecond, T2, TError, TOutResult, TOut>(
        TFirst first,
        TSecond second,
        Func<T1, T2, TOut> resultSelector)
        where TFirst : struct, IFluentResult<TFirst, T1, TError>
        where TSecond : struct, IFluentResult<TSecond, T2, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(resultSelector);

        if (!first.TryGetValue(out var a))
            return TOutResult.FromError(Flow.ErrorOf<TFirst, T1, TError>(first));

        if (!second.TryGetValue(out var b))
            return TOutResult.FromError(Flow.ErrorOf<TSecond, T2, TError>(second));

        try
        {
            return TOutResult.FromValue(resultSelector(a, b));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TOutResult, TError>(exception);
        }
    }

    /// <summary>
    /// Returns the value, or <c>default</c> for an error.
    /// </summary>
    public static T? GetValueOrDefault<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        result.TryGetValue(out var value) ? value : default;

    /// <summary>
    /// Returns the value, or <paramref name="fallback"/> for an error.
    /// </summary>
    public static T GetValueOrDefault<TSelf, T, TError>(TSelf result, T fallback)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        result.TryGetValue(out var value) ? value : fallback;

    /// <summary>
    /// Returns the value, or the result of <paramref name="defaultValueFactory"/> for an error.
    /// The factory runs only when needed, and its exceptions propagate.
    /// </summary>
    public static T GetValueOrDefault<TSelf, T, TError>(TSelf result, Func<T> defaultValueFactory)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        return result.TryGetValue(out var value) ? value : defaultValueFactory();
    }

    /// <summary>
    /// Flattens a result of a result. Like <see cref="TaskExtensions.Unwrap{TResult}(Task{Task{TResult}})"/>.
    /// </summary>
    public static TInner Unwrap<TSelf, TInner, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, TInner, TError>
        where TInner : struct, IFluentResult<TInner, T, TError>
        where TError : notnull =>
        result.TryGetValue(out var inner) ? inner : TInner.FromError(Flow.ErrorOf<TSelf, TInner, TError>(result));

    /// <summary>
    /// Flattens a result of a result without a value.
    /// </summary>
    public static TUnit Unwrap<TSelf, TUnit, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, TUnit, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull =>
        result.TryGetValue(out var inner) ? inner : TUnit.FromError(Flow.ErrorOf<TSelf, TUnit, TError>(result));
}
