namespace Fluent.Unions;

/// <summary>
/// Operators for <see cref="Loadable{T}"/>.
/// </summary>
public static class LoadableExtensions
{
    /// <summary>
    /// Projects the loaded value. The other states pass through, and a <c>null</c> projection counts as not loaded.
    /// </summary>
    public static Loadable<TOut> Select<T, TOut>(this Loadable<T> loadable, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (loadable.TryGetValue(out var value))
            return selector(value) is { } projected ? new Loadable<TOut>(projected) : new Loadable<TOut>(Loadable.NotLoaded);

        return loadable.Value switch
        {
            Loading => new Loadable<TOut>(Loadable.Loading),
            Exception error => new Loadable<TOut>(error),
            _ => new Loadable<TOut>(Loadable.NotLoaded)
        };
    }

    /// <summary>
    /// Leaves the state with a value: one callback for each state, so a view handles all of them.
    /// </summary>
    public static TOut Match<T, TOut>(
        this Loadable<T> loadable,
        Func<TOut> onNotLoaded,
        Func<TOut> onLoading,
        Func<T, TOut> onLoaded,
        Func<Exception, TOut> onError)
    {
        ArgumentNullException.ThrowIfNull(onNotLoaded);
        ArgumentNullException.ThrowIfNull(onLoading);
        ArgumentNullException.ThrowIfNull(onLoaded);
        ArgumentNullException.ThrowIfNull(onError);

        if (loadable.TryGetValue(out var value))
            return onLoaded(value);

        return loadable.Value switch
        {
            Loading => onLoading(),
            Exception error => onError(error),
            _ => onNotLoaded()
        };
    }

    /// <summary>
    /// Returns the loaded value, or <paramref name="fallback"/> in every other state.
    /// </summary>
    public static T GetValueOrDefault<T>(this Loadable<T> loadable, T fallback) =>
        loadable.TryGetValue(out var value) ? value : fallback;

    /// <summary>
    /// Returns the loaded value as an option.
    /// </summary>
    public static Option<T> ToOption<T>(this Loadable<T> loadable) =>
        loadable.TryGetValue(out var value) ? Option<T>.Of(value) : Option<T>.NoValue;

    /// <summary>
    /// Turns a finished result into a loaded or failed state.
    /// </summary>
    public static Loadable<T> ToLoadable<T>(this Result<T> result) =>
        global::Fluent.Unions.Core.ResultConversionCore.ToLoadable<Result<T>, T, Exception>(result);

    /// <summary>
    /// Awaits the result and turns it into a loaded or failed state. A faulted task counts as failed.
    /// </summary>
    public static Task<Loadable<T>> ToLoadable<T>(this Task<Result<T>> task) =>
        global::Fluent.Unions.Core.ResultConversionCore.ToLoadable<Result<T>, T, Exception>(task);
}
