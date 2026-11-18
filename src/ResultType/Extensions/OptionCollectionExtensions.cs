namespace ResultType.Extensions;

/// <summary>
/// Operators between sequences and options.
/// </summary>
public static class OptionCollectionExtensions
{
    /// <summary>
    /// Returns the values of all options that hold one.
    /// </summary>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Iterate(options);

        static IEnumerable<T> Iterate(IEnumerable<Option<T>> options)
        {
            foreach (var option in options)
            {
                if (option.TryGetValue(out var value))
                    yield return value;
            }
        }
    }

    /// <summary>
    /// Collects all values into one option. Holds no value if any option holds none.
    /// </summary>
    public static Option<IReadOnlyList<T>> Combine<T>(this IEnumerable<Option<T>> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var values = new List<T>();

        foreach (var option in options)
        {
            if (!option.TryGetValue(out var value))
                return Option<IReadOnlyList<T>>.NoValue;

            values.Add(value);
        }

        return new Option<IReadOnlyList<T>>(values);
    }

    /// <summary>
    /// Returns the first element, or an option without a value for an empty sequence.
    /// </summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
            return Option<T>.Of(item);

        return Option<T>.NoValue;
    }

    /// <summary>
    /// Returns the first element that satisfies the predicate, or an option without a value.
    /// </summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
                return Option<T>.Of(item);
        }

        return Option<T>.NoValue;
    }

    /// <summary>
    /// Returns the last element, or an option without a value for an empty sequence.
    /// </summary>
    public static Option<T> LastOrNone<T>(this IEnumerable<T> source) => source.LastOrNone(_ => true);

    /// <summary>
    /// Returns the last element that satisfies the predicate, or an option without a value.
    /// </summary>
    public static Option<T> LastOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var last = Option<T>.NoValue;

        foreach (var item in source)
        {
            if (predicate(item))
                last = Option<T>.Of(item);
        }

        return last;
    }
}
