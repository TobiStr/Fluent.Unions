using System.ComponentModel;

namespace Fluent.Unions;

/// <summary>
/// The fluent operators on <see cref="Task{TResult}"/> of a result, so a pipeline stays one expression.
/// A faulted task becomes an error result.
/// </summary>
public static class ResultTaskExtensions
{
    /// <inheritdoc cref="ResultQueryExtensions.Select{T, TOut}(Result{T}, Func{T, TOut})"/>
    public static Task<Result<TOut>> Select<T, TOut>(this Task<Result<T>> task, Func<T, TOut> selector) =>
        Capture.Then(Capture.Settle(task), result => result.Select(selector));

    /// <inheritdoc cref="ResultQueryExtensions.Select{TOut}(Result, Func{TOut})"/>
    public static Task<Result<TOut>> Select<TOut>(this Task<Result> task, Func<TOut> selector) =>
        Capture.Then(Capture.Settle(task), result => result.Select(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany{T, TOut}(Result{T}, Func{T, Result{TOut}})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TOut>> SelectMany<T, TOut>(this Task<Result<T>> task, Func<T, Result<TOut>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany{T, TMid, TOut}(Result{T}, Func{T, Result{TMid}}, Func{T, TMid, TOut})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TOut>> SelectMany<T, TMid, TOut>(
        this Task<Result<T>> task,
        Func<T, Result<TMid>> selector,
        Func<T, TMid, TOut> resultSelector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector, resultSelector));

    /// <summary>
    /// Chains an asynchronous operation and combines both values. Enables async query syntax.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TOut>> SelectMany<T, TMid, TOut>(
        this Task<Result<T>> task,
        Func<T, Task<Result<TMid>>> selector,
        Func<T, TMid, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return task.SelectManyAwait(value => selector(value).Select(mid => resultSelector(value, mid)));
    }

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany{T}(Result{T}, Func{T, Result})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> SelectMany<T>(this Task<Result<T>> task, Func<T, Result> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany(Result, Func{Result})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> SelectMany(this Task<Result> task, Func<Result> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany{TOut}(Result, Func{Result{TOut}})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TOut>> SelectMany<TOut>(this Task<Result> task, Func<Result<TOut>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.Chain{T, TOut}(Result{T}, Func{T, Result{TOut}})"/>
    public static Task<Result<TOut>> Chain<T, TOut>(this Task<Result<T>> task, Func<T, Result<TOut>> selector) =>
        task.SelectMany(selector);

    /// <inheritdoc cref="ResultQueryExtensions.Chain{T}(Result{T}, Func{T, Result})"/>
    public static Task<Result> Chain<T>(this Task<Result<T>> task, Func<T, Result> selector) =>
        task.SelectMany(selector);

    /// <inheritdoc cref="ResultQueryExtensions.Chain(Result, Func{Result})"/>
    public static Task<Result> Chain(this Task<Result> task, Func<Result> selector) =>
        task.SelectMany(selector);

    /// <inheritdoc cref="ResultQueryExtensions.Chain{TOut}(Result, Func{Result{TOut}})"/>
    public static Task<Result<TOut>> Chain<TOut>(this Task<Result> task, Func<Result<TOut>> selector) =>
        task.SelectMany(selector);

    /// <inheritdoc cref="ResultEnsureExtensions.Ensure{T}(Result{T}, Func{T, bool}, Func{T, Exception})"/>
    public static Task<Result<T>> Ensure<T>(this Task<Result<T>> task, Func<T, bool> predicate, Func<T, Exception> error) =>
        Capture.Then(Capture.Settle(task), result => result.Ensure(predicate, error));

    /// <inheritdoc cref="ResultEnsureExtensions.Ensure{T}(Result{T}, Func{T, bool}, string)"/>
    public static Task<Result<T>> Ensure<T>(this Task<Result<T>> task, Func<T, bool> predicate, string message) =>
        Capture.Then(Capture.Settle(task), result => result.Ensure(predicate, message));

    /// <inheritdoc cref="ResultTupleExtensions.BindAppend{T, TNext}(Result{T}, Func{T, Result{TNext}})"/>
    public static Task<Result<(T, TNext)>> BindAppend<T, TNext>(this Task<Result<T>> task, Func<T, Result<TNext>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.BindAppend(selector));

    /// <inheritdoc cref="ResultTupleExtensions.BindAppend{T1, T2, TNext}(Result{ValueTuple{T1, T2}}, Func{T1, T2, Result{TNext}})"/>
    public static Task<Result<(T1, T2, TNext)>> BindAppend<T1, T2, TNext>(
        this Task<Result<(T1, T2)>> task,
        Func<T1, T2, Result<TNext>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.BindAppend(selector));

    /// <inheritdoc cref="ResultTupleExtensions.BindAppend{T1, T2, T3, TNext}(Result{ValueTuple{T1, T2, T3}}, Func{T1, T2, T3, Result{TNext}})"/>
    public static Task<Result<(T1, T2, T3, TNext)>> BindAppend<T1, T2, T3, TNext>(
        this Task<Result<(T1, T2, T3)>> task,
        Func<T1, T2, T3, Result<TNext>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.BindAppend(selector));

    /// <inheritdoc cref="ResultTupleExtensions.WithValue{T}(Result, T)"/>
    public static Task<Result<T>> WithValue<T>(this Task<Result> task, T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Capture.Then(Capture.Settle(task), result => result.WithValue(value));
    }

    /// <inheritdoc cref="ResultTupleExtensions.DiscardValue{T}(Result{T})"/>
    public static Task<Result> DiscardValue<T>(this Task<Result<T>> task) =>
        Capture.Then(Capture.Settle(task), result => result.DiscardValue());

    /// <inheritdoc cref="ResultQueryExtensions.Zip{T1, T2}(Result{T1}, Result{T2})"/>
    public static Task<Result<(T1 First, T2 Second)>> Zip<T1, T2>(this Task<Result<T1>> first, Result<T2> second) =>
        Capture.Then(Capture.Settle(first), result => result.Zip(second));

    /// <inheritdoc cref="ResultQueryExtensions.Zip{T1, T2, TOut}(Result{T1}, Result{T2}, Func{T1, T2, TOut})"/>
    public static Task<Result<TOut>> Zip<T1, T2, TOut>(
        this Task<Result<T1>> first,
        Result<T2> second,
        Func<T1, T2, TOut> resultSelector) =>
        Capture.Then(Capture.Settle(first), result => result.Zip(second, resultSelector));

    /// <inheritdoc cref="ResultQueryExtensions.GetValueOrDefault{T}(Result{T})"/>
    public static Task<T?> GetValueOrDefault<T>(this Task<Result<T>> task) =>
        Capture.Then(Capture.Settle(task), result => result.GetValueOrDefault());

    /// <inheritdoc cref="ResultQueryExtensions.GetValueOrDefault{T}(Result{T}, Func{T})"/>
    public static Task<T> GetValueOrDefault<T>(this Task<Result<T>> task, Func<T> defaultValueFactory) =>
        Capture.Then(Capture.Settle(task), result => result.GetValueOrDefault(defaultValueFactory));

    /// <inheritdoc cref="ResultExtensions.Match{T, TOut}(Result{T}, Func{T, TOut}, Func{Exception, TOut})"/>
    public static Task<TOut> Match<T, TOut>(
        this Task<Result<T>> task,
        Func<T, TOut> onSuccess,
        Func<Exception, TOut> onError) =>
        Capture.Then(Capture.Settle(task), result => result.Match(onSuccess, onError));

    /// <inheritdoc cref="ResultExtensions.Match{TOut}(Result, Func{TOut}, Func{Exception, TOut})"/>
    public static Task<TOut> Match<TOut>(
        this Task<Result> task,
        Func<TOut> onSuccess,
        Func<Exception, TOut> onError) =>
        Capture.Then(Capture.Settle(task), result => result.Match(onSuccess, onError));

    /// <inheritdoc cref="ResultValidationExtensions.Validate{T}(Result{T}, Func{T, Result}[])"/>
    public static Task<Result<T>> Validate<T>(this Task<Result<T>> task, params Func<T, Result>[] rules) =>
        Capture.Then(Capture.Settle(task), result => result.Validate(rules));

    /// <inheritdoc cref="ResultErrorExtensions.Catch{T}(Result{T}, Func{Exception, Result{T}})"/>
    public static Task<Result<T>> Catch<T>(this Task<Result<T>> task, Func<Exception, Result<T>> handler) =>
        Capture.Then(Capture.Settle(task), result => result.Catch(handler));

    /// <inheritdoc cref="ResultErrorExtensions.Catch{T, TException}(Result{T}, Func{TException, Result{T}})"/>
    public static Task<Result<T>> Catch<T, TException>(this Task<Result<T>> task, Func<TException, Result<T>> handler)
        where TException : Exception =>
        Capture.Then(Capture.Settle(task), result => result.Catch(handler));

    /// <inheritdoc cref="ResultErrorExtensions.Catch{T}(Result{T}, Result{T})"/>
    public static Task<Result<T>> Catch<T>(this Task<Result<T>> task, Result<T> fallback) =>
        Capture.Then(Capture.Settle(task), result => result.Catch(fallback));

    /// <inheritdoc cref="ResultErrorExtensions.Catch(Result, Func{Exception, Result})"/>
    public static Task<Result> Catch(this Task<Result> task, Func<Exception, Result> handler) =>
        Capture.Then(Capture.Settle(task), result => result.Catch(handler));

    /// <inheritdoc cref="ResultErrorExtensions.Catch{TException}(Result, Func{TException, Result})"/>
    public static Task<Result> Catch<TException>(this Task<Result> task, Func<TException, Result> handler)
        where TException : Exception =>
        Capture.Then(Capture.Settle(task), result => result.Catch(handler));

    /// <inheritdoc cref="ResultErrorExtensions.Catch(Result, Result)"/>
    public static Task<Result> Catch(this Task<Result> task, Result fallback) =>
        Capture.Then(Capture.Settle(task), result => result.Catch(fallback));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do{T}(Result{T}, Action{T})"/>
    public static Task<Result<T>> Do<T>(this Task<Result<T>> task, Action<T> onSuccess) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onSuccess));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do{T}(Result{T}, Action{T}, Action{Exception})"/>
    public static Task<Result<T>> Do<T>(this Task<Result<T>> task, Action<T> onSuccess, Action<Exception> onError) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onSuccess, onError));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do(Result, Action)"/>
    public static Task<Result> Do(this Task<Result> task, Action onSuccess) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onSuccess));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do(Result, Action, Action{Exception})"/>
    public static Task<Result> Do(this Task<Result> task, Action onSuccess, Action<Exception> onError) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onSuccess, onError));

    /// <inheritdoc cref="ResultSideEffectExtensions.OnErrorDo{T}(Result{T}, Action{Exception})"/>
    public static Task<Result<T>> OnErrorDo<T>(this Task<Result<T>> task, Action<Exception> onError) =>
        Capture.Then(Capture.Settle(task), result => result.OnErrorDo(onError));

    /// <inheritdoc cref="ResultSideEffectExtensions.OnErrorDo(Result, Action{Exception})"/>
    public static Task<Result> OnErrorDo(this Task<Result> task, Action<Exception> onError) =>
        Capture.Then(Capture.Settle(task), result => result.OnErrorDo(onError));

    /// <summary>
    /// Captures the value for later steps of the chain. <paramref name="value"/> is assigned at once
    /// and filled when the task completes with a value, before any later step runs.
    /// </summary>
    public static Task<Result<T>> Out<T>(this Task<Result<T>> task, out Captured<T> value)
    {
        var captured = new Captured<T>();
        value = captured;

        return Capture.Then(Capture.Settle(task), result =>
        {
            captured.SetFrom(result);
            return result;
        });
    }

    /// <inheritdoc cref="ResultSideEffectExtensions.Finally{T}(Result{T}, Action)"/>
    public static Task<Result<T>> Finally<T>(this Task<Result<T>> task, Action action) =>
        Capture.Then(Capture.Settle(task), result => result.Finally(action));

    /// <inheritdoc cref="ResultSideEffectExtensions.Finally(Result, Action)"/>
    public static Task<Result> Finally(this Task<Result> task, Action action) =>
        Capture.Then(Capture.Settle(task), result => result.Finally(action));

    /// <summary>
    /// Awaits the result and throws its exception if it was not successful, preserving the stack trace.
    /// </summary>
    public static async Task<Result<T>> Check<T>(this Task<Result<T>> task) =>
        (await task.ConfigureAwait(false)).Check();

    /// <summary>
    /// Awaits the result and throws its exception if it was not successful, preserving the stack trace.
    /// </summary>
    public static async Task<Result> Check(this Task<Result> task) =>
        (await task.ConfigureAwait(false)).Check();
}
