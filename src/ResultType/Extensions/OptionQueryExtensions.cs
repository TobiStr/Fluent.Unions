namespace ResultType.Extensions;

/// <summary>
/// LINQ-style operators that run only when an option holds a value. Options without a value pass through.
/// </summary>
public static class OptionQueryExtensions
{
    /// <summary>
    /// Projects the value into a new form. A <c>null</c> projection gives an option without a value.
    /// </summary>
    public static Option<TOut> Select<T, TOut>(this Option<T> option, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value) ? Option<TOut>.Of(selector(value)) : Option<TOut>.NoValue;
    }

    /// <summary>
    /// Chains an operation that returns an option itself.
    /// </summary>
    public static Option<TOut> SelectMany<T, TOut>(this Option<T> option, Func<T, Option<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value) ? selector(value).Normalize() : Option<TOut>.NoValue;
    }

    /// <summary>
    /// Chains an operation and combines both values. Enables query syntax with multiple <c>from</c> clauses.
    /// </summary>
    public static Option<TOut> SelectMany<T, TMid, TOut>(
        this Option<T> option,
        Func<T, Option<TMid>> selector,
        Func<T, TMid, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return option.SelectMany(value => selector(value).Select(mid => resultSelector(value, mid)));
    }

    /// <inheritdoc cref="SelectMany{T, TOut}(Option{T}, Func{T, Option{TOut}})"/>
    public static Option<TOut> Chain<T, TOut>(this Option<T> option, Func<T, Option<TOut>> selector) =>
        option.SelectMany(selector);

    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the option holds no value.
    /// </summary>
    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return option.TryGetValue(out var value) && predicate(value) ? option : Option<T>.NoValue;
    }

    /// <summary>
    /// Combines two options into a tuple. Holds a value only when both do.
    /// </summary>
    public static Option<(T1 First, T2 Second)> Zip<T1, T2>(this Option<T1> first, Option<T2> second) =>
        first.Zip(second, (a, b) => (a, b));

    /// <summary>
    /// Combines two options with a selector. Holds a value only when both do.
    /// </summary>
    public static Option<TOut> Zip<T1, T2, TOut>(
        this Option<T1> first,
        Option<T2> second,
        Func<T1, T2, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(resultSelector);

        return first.TryGetValue(out var a) && second.TryGetValue(out var b)
            ? Option<TOut>.Of(resultSelector(a, b))
            : Option<TOut>.NoValue;
    }

    /// <summary>
    /// Returns the value, or <c>default</c> when the option holds none.
    /// </summary>
    public static T? GetValueOrDefault<T>(this Option<T> option) =>
        option.TryGetValue(out var value) ? value : default;

    /// <summary>
    /// Returns the value, or <paramref name="fallback"/> when the option holds none.
    /// </summary>
    public static T GetValueOrDefault<T>(this Option<T> option, T fallback) =>
        option.TryGetValue(out var value) ? value : fallback;

    /// <summary>
    /// Replaces an option without a value with <paramref name="fallback"/>.
    /// </summary>
    public static Option<T> DefaultIfEmpty<T>(this Option<T> option, T fallback) =>
        option.HasValue ? option : Option<T>.Of(fallback);
}
