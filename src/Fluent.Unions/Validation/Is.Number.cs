using System.Numerics;

namespace Fluent.Unions.Validation;

public static partial class Is
{
    /// <summary>True if the number is greater than zero. NaN fails.</summary>
    public static bool Positive<T>(T value) where T : INumberBase<T> =>
        !T.IsNaN(value) && T.IsPositive(value) && !T.IsZero(value);

    /// <summary>True if the number is less than zero. NaN fails.</summary>
    public static bool Negative<T>(T value) where T : INumberBase<T> =>
        !T.IsNaN(value) && T.IsNegative(value) && !T.IsZero(value);

    /// <summary>True if the number is zero or less. NaN fails.</summary>
    public static bool NonPositive<T>(T value) where T : INumberBase<T> =>
        !T.IsNaN(value) && (T.IsNegative(value) || T.IsZero(value));

    /// <summary>True if the number is zero or greater. NaN fails.</summary>
    public static bool NonNegative<T>(T value) where T : INumberBase<T> =>
        !T.IsNaN(value) && (T.IsPositive(value) || T.IsZero(value));

    /// <summary>True if the number is zero. NaN fails.</summary>
    public static bool Zero<T>(T value) where T : INumberBase<T> => T.IsZero(value);

    /// <summary>True if the number is not zero. NaN fails.</summary>
    public static bool NonZero<T>(T value) where T : INumberBase<T> => !T.IsNaN(value) && !T.IsZero(value);

    /// <summary>True if the integer is even.</summary>
    public static bool Even<T>(T value) where T : IBinaryInteger<T> => T.IsEvenInteger(value);

    /// <summary>True if the integer is odd.</summary>
    public static bool Odd<T>(T value) where T : IBinaryInteger<T> => T.IsOddInteger(value);

    /// <summary>Creates a predicate for values greater than the bound.</summary>
    public static Func<T, bool> GreaterThan<T>(T bound) where T : IComparable<T> =>
        value => value is not null && value.CompareTo(bound) > 0;

    /// <summary>Creates a predicate for values greater than or equal to the bound.</summary>
    public static Func<T, bool> GreaterThanOrEqual<T>(T bound) where T : IComparable<T> =>
        value => value is not null && value.CompareTo(bound) >= 0;

    /// <summary>Creates a predicate for values less than the bound.</summary>
    public static Func<T, bool> LessThan<T>(T bound) where T : IComparable<T> =>
        value => value is not null && value.CompareTo(bound) < 0;

    /// <summary>Creates a predicate for values less than or equal to the bound.</summary>
    public static Func<T, bool> LessThanOrEqual<T>(T bound) where T : IComparable<T> =>
        value => value is not null && value.CompareTo(bound) <= 0;

    /// <summary>Creates a predicate for values between the bounds, both inclusive. Throws if <paramref name="min"/> is greater than <paramref name="max"/>.</summary>
    public static Func<T, bool> InRange<T>(T min, T max) where T : IComparable<T>
    {
        if (min.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "The minimum must not be greater than the maximum.");
        }

        return value => value is not null && value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    /// <summary>Creates a predicate for integers divisible by the divisor. Throws if the divisor is zero.</summary>
    public static Func<T, bool> DivisibleBy<T>(T divisor) where T : IBinaryInteger<T>
    {
        if (T.IsZero(divisor))
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), "The divisor must not be zero.");
        }

        return value => T.IsZero(value % divisor);
    }
}
