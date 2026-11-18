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
    /// Returns the only element, or an option without a value for an empty sequence.
    /// Like <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})"/>, it throws when there are several.
    /// </summary>
    /// <exception cref="InvalidOperationException">The sequence contains more than one element.</exception>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source) => source.SingleOrNone(_ => true);

    /// <summary>
    /// Returns the only element that satisfies the predicate, or an option without a value.
    /// Like <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>, it throws when several match.
    /// </summary>
    /// <exception cref="InvalidOperationException">More than one element satisfies the predicate.</exception>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var found = Option<T>.NoValue;
        var matched = false;

        foreach (var item in source)
        {
            if (!predicate(item))
                continue;

            if (matched)
                throw new InvalidOperationException("Sequence contains more than one matching element.");

            found = Option<T>.Of(item);
            matched = true;
        }

        return found;
    }

    /// <summary>
    /// Returns the element at <paramref name="index"/>, or an option without a value when the index is out of range.
    /// </summary>
    public static Option<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (index < 0)
            return Option<T>.NoValue;

        if (source is IReadOnlyList<T> list)
            return index < list.Count ? Option<T>.Of(list[index]) : Option<T>.NoValue;

        foreach (var item in source)
        {
            if (index-- == 0)
                return Option<T>.Of(item);
        }

        return Option<T>.NoValue;
    }

    /// <summary>
    /// Returns the value for <paramref name="key"/>, or an option without a value when the key is missing.
    /// </summary>
    public static Option<TValue> GetValueOrNone<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        return dictionary.TryGetValue(key, out var value) ? Option<TValue>.Of(value) : Option<TValue>.NoValue;
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
