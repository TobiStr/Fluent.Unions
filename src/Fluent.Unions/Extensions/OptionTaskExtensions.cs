using System.ComponentModel;

namespace Fluent.Unions;

/// <summary>
/// The fluent operators on <see cref="Task{TResult}"/> of an option, so a pipeline stays one expression.
/// A faulted task stays faulted, because an option has no error state.
/// </summary>
public static class OptionTaskExtensions
{
    /// <inheritdoc cref="OptionQueryExtensions.Select{T, TOut}(Option{T}, Func{T, TOut})"/>
    public static Task<Option<TOut>> Select<T, TOut>(this Task<Option<T>> task, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return Then(task, option => option.Select(selector));
    }

    /// <inheritdoc cref="OptionQueryExtensions.SelectMany{T, TOut}(Option{T}, Func{T, Option{TOut}})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Option<TOut>> SelectMany<T, TOut>(this Task<Option<T>> task, Func<T, Option<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return Then(task, option => option.SelectMany(selector));
    }

    /// <inheritdoc cref="OptionQueryExtensions.SelectMany{T, TMid, TOut}(Option{T}, Func{T, Option{TMid}}, Func{T, TMid, TOut})"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Option<TOut>> SelectMany<T, TMid, TOut>(
        this Task<Option<T>> task,
        Func<T, Option<TMid>> selector,
        Func<T, TMid, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return Then(task, option => option.SelectMany(selector, resultSelector));
    }

    /// <summary>
    /// Chains an asynchronous operation and combines both values. Enables async query syntax.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Option<TOut>> SelectMany<T, TMid, TOut>(
        this Task<Option<T>> task,
        Func<T, Task<Option<TMid>>> selector,
        Func<T, TMid, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return task.ChainAwait(value => selector(value).Select(mid => resultSelector(value, mid)));
    }

    /// <inheritdoc cref="OptionQueryExtensions.Chain{T, TOut}(Option{T}, Func{T, Option{TOut}})"/>
    public static Task<Option<TOut>> Chain<T, TOut>(this Task<Option<T>> task, Func<T, Option<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return Then(task, option => option.SelectMany(selector));
    }

    /// <inheritdoc cref="OptionQueryExtensions.Where{T}(Option{T}, Func{T, bool})"/>
    public static Task<Option<T>> Where<T>(this Task<Option<T>> task, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Then(task, option => option.Where(predicate));
    }

    /// <inheritdoc cref="OptionQueryExtensions.Zip{T1, T2}(Option{T1}, Option{T2})"/>
    public static Task<Option<(T1 First, T2 Second)>> Zip<T1, T2>(this Task<Option<T1>> first, Option<T2> second) =>
        Then(first, option => option.Zip(second));

    /// <inheritdoc cref="OptionQueryExtensions.GetValueOrDefault{T}(Option{T})"/>
    public static Task<T?> GetValueOrDefault<T>(this Task<Option<T>> task) =>
        Then(task, option => option.GetValueOrDefault());

    /// <inheritdoc cref="OptionQueryExtensions.GetValueOrDefault{T}(Option{T}, T)"/>
    public static Task<T> GetValueOrDefault<T>(this Task<Option<T>> task, T fallback) =>
        Then(task, option => option.GetValueOrDefault(fallback));

    /// <inheritdoc cref="OptionQueryExtensions.DefaultIfEmpty{T}(Option{T}, T)"/>
    public static Task<Option<T>> DefaultIfEmpty<T>(this Task<Option<T>> task, T fallback) =>
        Then(task, option => option.DefaultIfEmpty(fallback));

    /// <inheritdoc cref="OptionQueryExtensions.DefaultIfEmpty{T}(Option{T}, Func{T})"/>
    public static Task<Option<T>> DefaultIfEmpty<T>(this Task<Option<T>> task, Func<T> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        return Then(task, option => option.DefaultIfEmpty(defaultValueFactory));
    }

    /// <inheritdoc cref="OptionQueryExtensions.GetValueOrDefault{T}(Option{T}, Func{T})"/>
    public static Task<T> GetValueOrDefault<T>(this Task<Option<T>> task, Func<T> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        return Then(task, option => option.GetValueOrDefault(defaultValueFactory));
    }

    /// <inheritdoc cref="OptionQueryExtensions.Or{T}(Option{T}, Func{Option{T}})"/>
    public static Task<Option<T>> Or<T>(this Task<Option<T>> task, Func<Option<T>> alternativeFactory)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);

        return Then(task, option => option.Or(alternativeFactory));
    }

    /// <inheritdoc cref="OptionQueryExtensions.Match{T, TOut}(Option{T}, Func{T, TOut}, Func{TOut})"/>
    public static Task<TOut> Match<T, TOut>(this Task<Option<T>> task, Func<T, TOut> onSome, Func<TOut> onNone)
    {
        ArgumentNullException.ThrowIfNull(onSome);
        ArgumentNullException.ThrowIfNull(onNone);

        return Then(task, option => option.Match(onSome, onNone));
    }

    /// <inheritdoc cref="OptionSideEffectExtensions.Do{T}(Option{T}, Action{T})"/>
    public static Task<Option<T>> Do<T>(this Task<Option<T>> task, Action<T> onSome)
    {
        ArgumentNullException.ThrowIfNull(onSome);

        return Then(task, option => option.Do(onSome));
    }

    /// <inheritdoc cref="OptionSideEffectExtensions.Do{T}(Option{T}, Action{T}, Action)"/>
    public static Task<Option<T>> Do<T>(this Task<Option<T>> task, Action<T> onSome, Action onNone)
    {
        ArgumentNullException.ThrowIfNull(onSome);
        ArgumentNullException.ThrowIfNull(onNone);

        return Then(task, option => option.Do(onSome, onNone));
    }

    /// <inheritdoc cref="OptionSideEffectExtensions.OnNoneDo{T}(Option{T}, Action)"/>
    public static Task<Option<T>> OnNoneDo<T>(this Task<Option<T>> task, Action onNone)
    {
        ArgumentNullException.ThrowIfNull(onNone);

        return Then(task, option => option.OnNoneDo(onNone));
    }

    /// <summary>
    /// Captures the value for later steps of the chain. <paramref name="value"/> is assigned at once
    /// and filled when the task completes with a value, before any later step runs.
    /// </summary>
    public static Task<Option<T>> Out<T>(this Task<Option<T>> task, out Captured<T> value)
    {
        var captured = new Captured<T>();
        value = captured;

        return Then(task, option =>
        {
            captured.SetFrom(option);
            return option.Normalize();
        });
    }

    /// <inheritdoc cref="OptionSideEffectExtensions.Finally{T}(Option{T}, Action)"/>
    public static Task<Option<T>> Finally<T>(this Task<Option<T>> task, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return Then(task, option => option.Finally(action));
    }

    /// <inheritdoc cref="OptionConversionExtensions.ToResult{T}(Option{T}, Func{Exception})"/>
    public static Task<Result<T>> ToResult<T>(this Task<Option<T>> task, Func<Exception> error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Then(task, option => option.ToResult(error));
    }

    private static Task<TOut> Then<TIn, TOut>(Task<TIn> task, Func<TIn, TOut> next)
    {
        ArgumentNullException.ThrowIfNull(task);

        return Capture.Then(task, next);
    }
}
