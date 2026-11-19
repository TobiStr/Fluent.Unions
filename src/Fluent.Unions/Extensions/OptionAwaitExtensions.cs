namespace Fluent.Unions.Extensions;

/// <summary>
/// Fluent operators on options that take asynchronous lambdas, named after System.Linq.Async (<c>SelectAwait</c>, ...).
/// </summary>
public static class OptionAwaitExtensions
{
    /// <summary>
    /// Projects the value with an asynchronous selector.
    /// </summary>
    public static Task<Option<TOut>> SelectAwait<T, TOut>(this Option<T> option, Func<T, Task<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value) ? Run(value, selector) : Task.FromResult(Option<TOut>.NoValue);

        static async Task<Option<TOut>> Run(T value, Func<T, Task<TOut>> selector) =>
            Option<TOut>.Of(await selector(value).ConfigureAwait(false));
    }

    /// <summary>
    /// Chains an asynchronous operation that returns an option itself.
    /// </summary>
    public static Task<Option<TOut>> SelectManyAwait<T, TOut>(this Option<T> option, Func<T, Task<Option<TOut>>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value) ? Run(value, selector) : Task.FromResult(Option<TOut>.NoValue);

        static async Task<Option<TOut>> Run(T value, Func<T, Task<Option<TOut>>> selector) =>
            (await selector(value).ConfigureAwait(false)).Normalize();
    }

    /// <inheritdoc cref="SelectManyAwait{T, TOut}(Option{T}, Func{T, Task{Option{TOut}}})"/>
    public static Task<Option<TOut>> ChainAwait<T, TOut>(this Option<T> option, Func<T, Task<Option<TOut>>> selector) =>
        option.SelectManyAwait(selector);

    /// <summary>
    /// Keeps the value if the asynchronous predicate is satisfied, otherwise the option holds no value.
    /// </summary>
    public static Task<Option<T>> WhereAwait<T>(this Option<T> option, Func<T, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return option.TryGetValue(out var value) ? Run(option, value, predicate) : Task.FromResult(Option<T>.NoValue);

        static async Task<Option<T>> Run(Option<T> option, T value, Func<T, Task<bool>> predicate) =>
            await predicate(value).ConfigureAwait(false) ? option : Option<T>.NoValue;
    }

    /// <summary>
    /// Runs an asynchronous side effect with the value and returns the same option.
    /// </summary>
    public static Task<Option<T>> DoAwait<T>(this Option<T> option, Func<T, Task> onNext)
    {
        ArgumentNullException.ThrowIfNull(onNext);

        return option.TryGetValue(out var value) ? Run(option, value, onNext) : Task.FromResult(Option<T>.NoValue);

        static async Task<Option<T>> Run(Option<T> option, T value, Func<T, Task> onNext)
        {
            await onNext(value).ConfigureAwait(false);
            return option;
        }
    }

    /// <inheritdoc cref="SelectAwait{T, TOut}(Option{T}, Func{T, Task{TOut}})"/>
    public static Task<Option<TOut>> SelectAwait<T, TOut>(this Task<Option<T>> task, Func<T, Task<TOut>> selector) =>
        ThenAwait(task, option => option.SelectAwait(selector));

    /// <inheritdoc cref="SelectManyAwait{T, TOut}(Option{T}, Func{T, Task{Option{TOut}}})"/>
    public static Task<Option<TOut>> SelectManyAwait<T, TOut>(this Task<Option<T>> task, Func<T, Task<Option<TOut>>> selector) =>
        ThenAwait(task, option => option.SelectManyAwait(selector));

    /// <inheritdoc cref="SelectManyAwait{T, TOut}(Option{T}, Func{T, Task{Option{TOut}}})"/>
    public static Task<Option<TOut>> ChainAwait<T, TOut>(this Task<Option<T>> task, Func<T, Task<Option<TOut>>> selector) =>
        ThenAwait(task, option => option.SelectManyAwait(selector));

    /// <inheritdoc cref="WhereAwait{T}(Option{T}, Func{T, Task{bool}})"/>
    public static Task<Option<T>> WhereAwait<T>(this Task<Option<T>> task, Func<T, Task<bool>> predicate) =>
        ThenAwait(task, option => option.WhereAwait(predicate));

    /// <inheritdoc cref="DoAwait{T}(Option{T}, Func{T, Task})"/>
    public static Task<Option<T>> DoAwait<T>(this Task<Option<T>> task, Func<T, Task> onNext) =>
        ThenAwait(task, option => option.DoAwait(onNext));

    private static Task<TOut> ThenAwait<TIn, TOut>(Task<TIn> task, Func<TIn, Task<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(task);

        return Capture.ThenAwait(task, next);
    }
}
