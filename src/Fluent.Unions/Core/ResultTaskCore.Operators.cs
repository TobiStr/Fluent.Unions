using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// The fluent operators on <see cref="Task{TResult}"/> of a result, so a pipeline stays one expression.
/// A faulted task becomes an error result.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class ResultTaskCore
{
    /// <inheritdoc cref="ResultQueryCore.Select{TSelf, T, TError, TOutResult, TOut}(TSelf, Func{T, TOut})"/>
    public static Task<TOutResult> Select<TSelf, T, TError, TOutResult, TOut>(Task<TSelf> task, Func<T, TOut> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, TOut> selector) =>
            ResultQueryCore.Select<TSelf, T, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.Select{TSelf, TError, TOutResult, TOut}(TSelf, Func{TOut})"/>
    public static Task<TOutResult> Select<TSelf, TError, TOutResult, TOut>(Task<TSelf> task, Func<TOut> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<TOut> selector) =>
            ResultQueryCore.Select<TSelf, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.SelectMany{TSelf, T, TError, TOutResult, TOut}(TSelf, Func{T, TOutResult})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TOutResult> SelectMany<TSelf, T, TError, TOutResult, TOut>(Task<TSelf> task, Func<T, TOutResult> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, TOutResult> selector) =>
            ResultQueryCore.SelectMany<TSelf, T, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.SelectMany{TSelf, T, TError, TMidResult, TMid, TOutResult, TOut}(TSelf, Func{T, TMidResult}, Func{T, TMid, TOut})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TOutResult> SelectMany<TSelf, T, TError, TMidResult, TMid, TOutResult, TOut>(Task<TSelf> task, Func<T, TMidResult> selector, Func<T, TMid, TOut> resultSelector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TMidResult : struct, IFluentResult<TMidResult, TMid, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector, resultSelector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, TMidResult> selector, Func<T, TMid, TOut> resultSelector) =>
            ResultQueryCore.SelectMany<TSelf, T, TError, TMidResult, TMid, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector, resultSelector);
    }

    /// <inheritdoc cref="ResultQueryCore.SelectMany{TSelf, T, TError, TUnit}(TSelf, Func{T, TUnit})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TUnit> SelectMany<TSelf, T, TError, TUnit>(Task<TSelf> task, Func<T, TUnit> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TUnit> Run(Task<TSelf> task, Func<T, TUnit> selector) =>
            ResultQueryCore.SelectMany<TSelf, T, TError, TUnit>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.SelectMany{TSelf, TError}(TSelf, Func{TSelf})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TSelf> SelectMany<TSelf, TError>(Task<TSelf> task, Func<TSelf> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TSelf> selector) =>
            ResultQueryCore.SelectMany<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.SelectMany{TSelf, TError, TOutResult, TOut}(TSelf, Func{TOutResult})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TOutResult> SelectMany<TSelf, TError, TOutResult, TOut>(Task<TSelf> task, Func<TOutResult> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<TOutResult> selector) =>
            ResultQueryCore.SelectMany<TSelf, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.Chain{TSelf, T, TError, TOutResult, TOut}(TSelf, Func{T, TOutResult})"/>
    public static Task<TOutResult> Chain<TSelf, T, TError, TOutResult, TOut>(Task<TSelf> task, Func<T, TOutResult> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, TOutResult> selector) =>
            ResultQueryCore.Chain<TSelf, T, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.Chain{TSelf, T, TError, TUnit}(TSelf, Func{T, TUnit})"/>
    public static Task<TUnit> Chain<TSelf, T, TError, TUnit>(Task<TSelf> task, Func<T, TUnit> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TUnit> Run(Task<TSelf> task, Func<T, TUnit> selector) =>
            ResultQueryCore.Chain<TSelf, T, TError, TUnit>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.Chain{TSelf, TError}(TSelf, Func{TSelf})"/>
    public static Task<TSelf> Chain<TSelf, TError>(Task<TSelf> task, Func<TSelf> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TSelf> selector) =>
            ResultQueryCore.Chain<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultQueryCore.Chain{TSelf, TError, TOutResult, TOut}(TSelf, Func{TOutResult})"/>
    public static Task<TOutResult> Chain<TSelf, TError, TOutResult, TOut>(Task<TSelf> task, Func<TOutResult> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<TOutResult> selector) =>
            ResultQueryCore.Chain<TSelf, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultEnsureCore.Ensure{TSelf, T, TError}(TSelf, Func{T, bool}, Func{T, TError})"/>
    public static Task<TSelf> Ensure<TSelf, T, TError>(Task<TSelf> task, Func<T, bool> predicate, Func<T, TError> error)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, predicate, error);

        static async Task<TSelf> Run(Task<TSelf> task, Func<T, bool> predicate, Func<T, TError> error) =>
            ResultEnsureCore.Ensure<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), predicate, error);
    }

    /// <inheritdoc cref="ResultEnsureCore.Ensure{TSelf, T, TError}(TSelf, Func{T, bool}, string)"/>
    public static Task<TSelf> Ensure<TSelf, T, TError>(Task<TSelf> task, Func<T, bool> predicate, string message)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, predicate, message);

        static async Task<TSelf> Run(Task<TSelf> task, Func<T, bool> predicate, string message) =>
            ResultEnsureCore.Ensure<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), predicate, message);
    }

    /// <inheritdoc cref="ResultTupleCore.ChainAppend{TSelf, T, TError, TNextResult, TNext, TOutResult}(TSelf, Func{T, TNextResult})"/>
    public static Task<TOutResult> ChainAppend<TSelf, T, TError, TNextResult, TNext, TOutResult>(Task<TSelf> task, Func<T, TNextResult> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, TNextResult> selector) =>
            ResultTupleCore.ChainAppend<TSelf, T, TError, TNextResult, TNext, TOutResult>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultTupleCore.ChainAppend{TSelf, T1, T2, TError, TNextResult, TNext, TOutResult}(TSelf, Func{T1, T2, TNextResult})"/>
    public static Task<TOutResult> ChainAppend<TSelf, T1, T2, TError, TNextResult, TNext, TOutResult>(Task<TSelf> task, Func<T1, T2, TNextResult> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T1, T2, TNextResult> selector) =>
            ResultTupleCore.ChainAppend<TSelf, T1, T2, TError, TNextResult, TNext, TOutResult>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultTupleCore.ChainAppend{TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult}(TSelf, Func{T1, T2, T3, TNextResult})"/>
    public static Task<TOutResult> ChainAppend<TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult>(Task<TSelf> task, Func<T1, T2, T3, TNextResult> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2, T3), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T1, T2, T3, TNextResult> selector) =>
            ResultTupleCore.ChainAppend<TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector);
    }

    /// <inheritdoc cref="ResultTupleCore.WithValue{TSelf, TError, TOutResult, T}(TSelf, T)"/>
    public static Task<TOutResult> WithValue<TSelf, TError, TOutResult, T>(Task<TSelf> task, T value)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, value);

        static async Task<TOutResult> Run(Task<TSelf> task, T value) =>
            ResultTupleCore.WithValue<TSelf, TError, TOutResult, T>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), value);
    }

    /// <inheritdoc cref="ResultTupleCore.DiscardValue{TSelf, T, TError, TUnit}(TSelf)"/>
    public static Task<TUnit> DiscardValue<TSelf, T, TError, TUnit>(Task<TSelf> task)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Run(task);

        static async Task<TUnit> Run(Task<TSelf> task) =>
            ResultTupleCore.DiscardValue<TSelf, T, TError, TUnit>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false));
    }

    /// <inheritdoc cref="ResultQueryCore.GetValueOrDefault{TSelf, T, TError}(TSelf)"/>
    public static Task<T?> GetValueOrDefault<TSelf, T, TError>(Task<TSelf> task)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Run(task);

        static async Task<T?> Run(Task<TSelf> task) =>
            ResultQueryCore.GetValueOrDefault<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false));
    }

    /// <inheritdoc cref="ResultQueryCore.GetValueOrDefault{TSelf, T, TError}(TSelf, Func{T})"/>
    public static Task<T> GetValueOrDefault<TSelf, T, TError>(Task<TSelf> task, Func<T> defaultValueFactory)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, defaultValueFactory);

        static async Task<T> Run(Task<TSelf> task, Func<T> defaultValueFactory) =>
            ResultQueryCore.GetValueOrDefault<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), defaultValueFactory);
    }

    /// <inheritdoc cref="ResultCore.Match{TSelf, T, TError, TOut}(TSelf, Func{T, TOut}, Func{TError, TOut})"/>
    public static Task<TOut> Match<TSelf, T, TError, TOut>(Task<TSelf> task, Func<T, TOut> onSuccess, Func<TError, TOut> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess, onError);

        static async Task<TOut> Run(Task<TSelf> task, Func<T, TOut> onSuccess, Func<TError, TOut> onError) =>
            ResultCore.Match<TSelf, T, TError, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess, onError);
    }

    /// <inheritdoc cref="ResultCore.Match{TSelf, TError, TOut}(TSelf, Func{TOut}, Func{TError, TOut})"/>
    public static Task<TOut> Match<TSelf, TError, TOut>(Task<TSelf> task, Func<TOut> onSuccess, Func<TError, TOut> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess, onError);

        static async Task<TOut> Run(Task<TSelf> task, Func<TOut> onSuccess, Func<TError, TOut> onError) =>
            ResultCore.Match<TSelf, TError, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess, onError);
    }

    /// <inheritdoc cref="ResultValidationCore.Validate{TSelf, T, TError, TRule}(TSelf, Func{T, TRule}[])"/>
    public static Task<TSelf> Validate<TSelf, T, TError, TRule>(Task<TSelf> task, params Func<T, TRule>[] rules)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TRule : struct, IFluentUnitResult<TRule, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, rules);

        static async Task<TSelf> Run(Task<TSelf> task, Func<T, TRule>[] rules) =>
            ResultValidationCore.Validate<TSelf, T, TError, TRule>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), rules);
    }

    /// <inheritdoc cref="ResultAnnotationCore.ValidateAnnotations{TSelf, T, TError}(TSelf)"/>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(ResultAnnotationCore.TrimmingMessage)]
    public static Task<TSelf> ValidateAnnotations<TSelf, T, TError>(Task<TSelf> task)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Run(task);

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(ResultAnnotationCore.TrimmingMessage)]
        static async Task<TSelf> Run(Task<TSelf> task) =>
            ResultAnnotationCore.ValidateAnnotations<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false));
    }

    /// <inheritdoc cref="ResultErrorCore.Catch{TSelf, T, TError}(TSelf, Func{TError, TSelf})"/>
    public static Task<TSelf> Catch<TSelf, T, TError>(Task<TSelf> task, Func<TError, TSelf> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TError, TSelf> handler) =>
            ResultErrorCore.Catch<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler);
    }

    /// <inheritdoc cref="ResultErrorCore.Catch{TSelf, T, TError, TException}(TSelf, Func{TException, TSelf})"/>
    public static Task<TSelf> Catch<TSelf, T, TError, TException>(Task<TSelf> task, Func<TException, TSelf> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TException, TSelf> handler) =>
            ResultErrorCore.Catch<TSelf, T, TError, TException>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler);
    }

    /// <inheritdoc cref="ResultErrorCore.Catch{TSelf, T, TError}(TSelf, TSelf)"/>
    public static Task<TSelf> Catch<TSelf, T, TError>(Task<TSelf> task, TSelf fallback)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, fallback);

        static async Task<TSelf> Run(Task<TSelf> task, TSelf fallback) =>
            ResultErrorCore.Catch<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), fallback);
    }

    /// <inheritdoc cref="ResultErrorCore.Catch{TSelf, TError}(TSelf, Func{TError, TSelf})"/>
    public static Task<TSelf> Catch<TSelf, TError>(Task<TSelf> task, Func<TError, TSelf> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TError, TSelf> handler) =>
            ResultErrorCore.Catch<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler);
    }

    /// <inheritdoc cref="ResultErrorCore.CatchCase{TSelf, TError, TException}(TSelf, Func{TException, TSelf})"/>
    [OperatorName("Catch")]
    public static Task<TSelf> CatchCase<TSelf, TError, TException>(Task<TSelf> task, Func<TException, TSelf> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TException, TSelf> handler) =>
            ResultErrorCore.CatchCase<TSelf, TError, TException>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler);
    }

    /// <inheritdoc cref="ResultErrorCore.Catch{TSelf, TError}(TSelf, TSelf)"/>
    public static Task<TSelf> Catch<TSelf, TError>(Task<TSelf> task, TSelf fallback)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, fallback);

        static async Task<TSelf> Run(Task<TSelf> task, TSelf fallback) =>
            ResultErrorCore.Catch<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), fallback);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Do{TSelf, T, TError}(TSelf, Action{T})"/>
    public static Task<TSelf> Do<TSelf, T, TError>(Task<TSelf> task, Action<T> onSuccess)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess);

        static async Task<TSelf> Run(Task<TSelf> task, Action<T> onSuccess) =>
            ResultSideEffectCore.Do<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Do{TSelf, T, TError}(TSelf, Action{T}, Action{TError})"/>
    public static Task<TSelf> Do<TSelf, T, TError>(Task<TSelf> task, Action<T> onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action<T> onSuccess, Action<TError> onError) =>
            ResultSideEffectCore.Do<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess, onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Do{TSelf, TError}(TSelf, Action)"/>
    public static Task<TSelf> Do<TSelf, TError>(Task<TSelf> task, Action onSuccess)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess);

        static async Task<TSelf> Run(Task<TSelf> task, Action onSuccess) =>
            ResultSideEffectCore.Do<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Do{TSelf, TError}(TSelf, Action, Action{TError})"/>
    public static Task<TSelf> Do<TSelf, TError>(Task<TSelf> task, Action onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action onSuccess, Action<TError> onError) =>
            ResultSideEffectCore.Do<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess, onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.OnErrorDo{TSelf, T, TError}(TSelf, Action{TError})"/>
    public static Task<TSelf> OnErrorDo<TSelf, T, TError>(Task<TSelf> task, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action<TError> onError) =>
            ResultSideEffectCore.OnErrorDo<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.OnErrorDo{TSelf, TError}(TSelf, Action{TError})"/>
    public static Task<TSelf> OnErrorDo<TSelf, TError>(Task<TSelf> task, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action<TError> onError) =>
            ResultSideEffectCore.OnErrorDo<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Tap{TSelf, T, TError}(TSelf, Action{T})"/>
    public static Task<TSelf> Tap<TSelf, T, TError>(Task<TSelf> task, Action<T> onSuccess)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess);

        static async Task<TSelf> Run(Task<TSelf> task, Action<T> onSuccess) =>
            ResultSideEffectCore.Tap<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Tap{TSelf, T, TError}(TSelf, Action{T}, Action{TError})"/>
    public static Task<TSelf> Tap<TSelf, T, TError>(Task<TSelf> task, Action<T> onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action<T> onSuccess, Action<TError> onError) =>
            ResultSideEffectCore.Tap<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess, onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.TapError{TSelf, T, TError}(TSelf, Action{TError})"/>
    public static Task<TSelf> TapError<TSelf, T, TError>(Task<TSelf> task, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action<TError> onError) =>
            ResultSideEffectCore.TapError<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Tap{TSelf, TError}(TSelf, Action)"/>
    public static Task<TSelf> Tap<TSelf, TError>(Task<TSelf> task, Action onSuccess)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess);

        static async Task<TSelf> Run(Task<TSelf> task, Action onSuccess) =>
            ResultSideEffectCore.Tap<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Tap{TSelf, TError}(TSelf, Action, Action{TError})"/>
    public static Task<TSelf> Tap<TSelf, TError>(Task<TSelf> task, Action onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action onSuccess, Action<TError> onError) =>
            ResultSideEffectCore.Tap<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess, onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.TapError{TSelf, TError}(TSelf, Action{TError})"/>
    public static Task<TSelf> TapError<TSelf, TError>(Task<TSelf> task, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onError);

        static async Task<TSelf> Run(Task<TSelf> task, Action<TError> onError) =>
            ResultSideEffectCore.TapError<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onError);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Finally{TSelf, T, TError}(TSelf, Action)"/>
    public static Task<TSelf> Finally<TSelf, T, TError>(Task<TSelf> task, Action action)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, action);

        static async Task<TSelf> Run(Task<TSelf> task, Action action) =>
            ResultSideEffectCore.Finally<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), action);
    }

    /// <inheritdoc cref="ResultSideEffectCore.Finally{TSelf, TError}(TSelf, Action)"/>
    public static Task<TSelf> Finally<TSelf, TError>(Task<TSelf> task, Action action)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, action);

        static async Task<TSelf> Run(Task<TSelf> task, Action action) =>
            ResultSideEffectCore.Finally<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), action);
    }
}
