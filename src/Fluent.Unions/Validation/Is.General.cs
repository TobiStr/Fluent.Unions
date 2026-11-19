namespace Fluent.Unions.Validation;

public static partial class Is
{
    /// <summary>True if the value is null.</summary>
    public static bool Null<T>(T? value) => value is null;

    /// <summary>True if the value is not null.</summary>
    public static bool NotNull<T>(T? value) => value is not null;

    /// <summary>True if the option has a value.</summary>
    public static bool Some<T>(Option<T> value) => value.HasValue;

    /// <summary>True if the option has no value.</summary>
    public static bool None<T>(Option<T> value) => !value.HasValue;

    /// <summary>Creates a predicate for values equal to the expected value. Without a comparer the default comparer is used.</summary>
    public static Func<T, bool> EqualTo<T>(T expected, IEqualityComparer<T>? comparer = null)
    {
        var equality = comparer ?? EqualityComparer<T>.Default;
        return value => equality.Equals(value, expected);
    }

    /// <summary>Creates a predicate for values not equal to the other value. Without a comparer the default comparer is used.</summary>
    public static Func<T, bool> NotEqualTo<T>(T other, IEqualityComparer<T>? comparer = null)
    {
        var equality = comparer ?? EqualityComparer<T>.Default;
        return value => !equality.Equals(value, other);
    }

    /// <summary>Creates a predicate for values that are one of the allowed values. With no values it never passes.</summary>
    public static Func<T, bool> In<T>(params T[] allowed)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        T[] snapshot = [.. allowed];
        return value => Array.IndexOf(snapshot, value) >= 0;
    }

    /// <summary>Creates a predicate for values that are none of the given values. With no values it always passes.</summary>
    public static Func<T, bool> NotIn<T>(params T[] disallowed)
    {
        ArgumentNullException.ThrowIfNull(disallowed);
        T[] snapshot = [.. disallowed];
        return value => Array.IndexOf(snapshot, value) < 0;
    }
}
