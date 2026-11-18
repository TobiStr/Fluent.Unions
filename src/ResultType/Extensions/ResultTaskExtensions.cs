namespace ResultType.Extensions;

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
    public static Task<Result<TOut>> SelectMany<T, TOut>(this Task<Result<T>> task, Func<T, Result<TOut>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany{T, TMid, TOut}(Result{T}, Func{T, Result{TMid}}, Func{T, TMid, TOut})"/>
    public static Task<Result<TOut>> SelectMany<T, TMid, TOut>(
        this Task<Result<T>> task,
        Func<T, Result<TMid>> selector,
        Func<T, TMid, TOut> resultSelector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector, resultSelector));

    /// <summary>
    /// Chains an asynchronous operation and combines both values. Enables async query syntax.
    /// </summary>
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
    public static Task<Result> SelectMany<T>(this Task<Result<T>> task, Func<T, Result> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany(Result, Func{Result})"/>
    public static Task<Result> SelectMany(this Task<Result> task, Func<Result> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.SelectMany{TOut}(Result, Func{Result{TOut}})"/>
    public static Task<Result<TOut>> SelectMany<TOut>(this Task<Result> task, Func<Result<TOut>> selector) =>
        Capture.Then(Capture.Settle(task), result => result.SelectMany(selector));

    /// <inheritdoc cref="ResultQueryExtensions.Where{T}(Result{T}, Func{T, bool})"/>
    public static Task<Result<T>> Where<T>(this Task<Result<T>> task, Func<T, bool> predicate) =>
        Capture.Then(Capture.Settle(task), result => result.Where(predicate));

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

    /// <inheritdoc cref="ResultQueryExtensions.GetValueOrDefault{T}(Result{T}, T)"/>
    public static Task<T> GetValueOrDefault<T>(this Task<Result<T>> task, T fallback) =>
        Capture.Then(Capture.Settle(task), result => result.GetValueOrDefault(fallback));

    /// <inheritdoc cref="ResultQueryExtensions.DefaultIfEmpty{T}(Result{T}, T)"/>
    public static Task<Result<T>> DefaultIfEmpty<T>(this Task<Result<T>> task, T fallback) =>
        Capture.Then(Capture.Settle(task), result => result.DefaultIfEmpty(fallback));

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
    public static Task<Result<T>> Do<T>(this Task<Result<T>> task, Action<T> onNext) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onNext));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do{T}(Result{T}, Action{T}, Action{Exception})"/>
    public static Task<Result<T>> Do<T>(this Task<Result<T>> task, Action<T> onNext, Action<Exception> onError) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onNext, onError));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do(Result, Action)"/>
    public static Task<Result> Do(this Task<Result> task, Action onNext) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onNext));

    /// <inheritdoc cref="ResultSideEffectExtensions.Do(Result, Action, Action{Exception})"/>
    public static Task<Result> Do(this Task<Result> task, Action onNext, Action<Exception> onError) =>
        Capture.Then(Capture.Settle(task), result => result.Do(onNext, onError));

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
