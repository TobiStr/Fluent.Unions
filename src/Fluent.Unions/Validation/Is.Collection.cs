using System.Collections;

namespace Fluent.Unions.Validation;

public static partial class Is
{
    /// <summary>True if the sequence is not null and has an item.</summary>
    public static bool NotEmpty<TItem>(IEnumerable<TItem>? value) => NotEmptySequence(value);

    /// <summary>True if the sequence is null or has no item.</summary>
    public static bool Empty<TItem>(IEnumerable<TItem>? value) => value is null || !NotEmptySequence(value);

    /// <summary>True if the sequence is not null and no two items are equal.</summary>
    public static bool Unique<TItem>(IEnumerable<TItem>? value) => AllDistinct(value, EqualityComparer<TItem>.Default);

    /// <summary>Creates a predicate for sequences whose items are all different under the comparer.</summary>
    public static Func<IEnumerable<TItem>?, bool> Unique<TItem>(IEqualityComparer<TItem> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        return value => AllDistinct(value, comparer);
    }

    /// <summary>Creates a predicate for sequences with exactly the given number of items. Throws if the count is negative.</summary>
    public static Func<IEnumerable?, bool> HasCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return value => value is not null && CountUpTo(value, count + 1L) == count;
    }

    /// <summary>Creates a predicate for sequences with at least the given number of items. Throws if the count is negative.</summary>
    public static Func<IEnumerable?, bool> HasMinCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return value => value is not null && CountUpTo(value, count) >= count;
    }

    /// <summary>Creates a predicate for sequences with at most the given number of items. Throws if the count is negative.</summary>
    public static Func<IEnumerable?, bool> HasMaxCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return value => value is not null && CountUpTo(value, count + 1L) <= count;
    }

    /// <summary>Creates a predicate for sequences that contain the item.</summary>
    public static Func<IEnumerable<TItem>?, bool> ContainsItem<TItem>(TItem item) =>
        value => value is not null && value.Contains(item);

    /// <summary>Creates a predicate for sequences that do not contain the item.</summary>
    public static Func<IEnumerable<TItem>?, bool> NotContainsItem<TItem>(TItem item) =>
        value => value is not null && !value.Contains(item);

    /// <summary>Creates a predicate for sequences where every item matches. An empty sequence passes.</summary>
    public static Func<IEnumerable<TItem>?, bool> AllMatch<TItem>(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return value => value is not null && value.All(predicate);
    }

    /// <summary>Creates a predicate for sequences where at least one item matches.</summary>
    public static Func<IEnumerable<TItem>?, bool> AnyMatch<TItem>(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return value => value is not null && value.Any(predicate);
    }

    /// <summary>Creates a predicate for sequences where no item matches. An empty sequence passes.</summary>
    public static Func<IEnumerable<TItem>?, bool> NoneMatch<TItem>(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return value => value is not null && !value.Any(predicate);
    }

    internal static bool NotEmptySequence(IEnumerable? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is ICollection collection)
        {
            return collection.Count > 0;
        }

        var enumerator = value.GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    internal static bool UniqueSequence(IEnumerable? value) =>
        value is not null && AllDistinct(value.Cast<object?>(), EqualityComparer<object?>.Default);

    private static bool AllDistinct<TItem>(IEnumerable<TItem>? value, IEqualityComparer<TItem> comparer)
    {
        if (value is null)
        {
            return false;
        }

        var seen = new HashSet<TItem>(comparer);
        foreach (var item in value)
        {
            if (!seen.Add(item))
            {
                return false;
            }
        }

        return true;
    }

    private static long CountUpTo(IEnumerable value, long limit)
    {
        if (value is ICollection collection)
        {
            return collection.Count;
        }

        long count = 0;
        var enumerator = value.GetEnumerator();
        try
        {
            while (count < limit && enumerator.MoveNext())
            {
                count++;
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return count;
    }
}
