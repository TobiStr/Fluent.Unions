using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named numeric rules in two forms. The form on a value returns a <see cref="Result{T}"/>, a null value is an error,
/// and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes an optional name,
/// and an error or empty result passes through. <c>message</c> replaces the default message.
/// </summary>
public static class NumberRuleExtensions
{
    /// <summary>Fails unless the value is greater than the bound.</summary>
    public static Result<T> EnsureGreaterThan<T>(this T value, T bound, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IComparable<T> =>
        RuleFailure.Lift(value, message, name).EnsureGreaterThan(bound, message, name);

    /// <summary>Fails unless the value is greater than the bound.</summary>
    public static Result<T> EnsureGreaterThan<T>(this Result<T> result, T bound, string? message = null, string? name = null) where T : IComparable<T> =>
        RuleFailure.Check(result, Is.GreaterThan(bound), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be greater than {bound}."), message, name);

    /// <summary>Fails unless the value is greater than or equal to the bound.</summary>
    public static Result<T> EnsureGreaterThanOrEqual<T>(this T value, T bound, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IComparable<T> =>
        RuleFailure.Lift(value, message, name).EnsureGreaterThanOrEqual(bound, message, name);

    /// <summary>Fails unless the value is greater than or equal to the bound.</summary>
    public static Result<T> EnsureGreaterThanOrEqual<T>(this Result<T> result, T bound, string? message = null, string? name = null) where T : IComparable<T> =>
        RuleFailure.Check(result, Is.GreaterThanOrEqual(bound), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be greater than or equal to {bound}."), message, name);

    /// <summary>Fails unless the value is less than the bound.</summary>
    public static Result<T> EnsureLessThan<T>(this T value, T bound, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IComparable<T> =>
        RuleFailure.Lift(value, message, name).EnsureLessThan(bound, message, name);

    /// <summary>Fails unless the value is less than the bound.</summary>
    public static Result<T> EnsureLessThan<T>(this Result<T> result, T bound, string? message = null, string? name = null) where T : IComparable<T> =>
        RuleFailure.Check(result, Is.LessThan(bound), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be less than {bound}."), message, name);

    /// <summary>Fails unless the value is less than or equal to the bound.</summary>
    public static Result<T> EnsureLessThanOrEqual<T>(this T value, T bound, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IComparable<T> =>
        RuleFailure.Lift(value, message, name).EnsureLessThanOrEqual(bound, message, name);

    /// <summary>Fails unless the value is less than or equal to the bound.</summary>
    public static Result<T> EnsureLessThanOrEqual<T>(this Result<T> result, T bound, string? message = null, string? name = null) where T : IComparable<T> =>
        RuleFailure.Check(result, Is.LessThanOrEqual(bound), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be less than or equal to {bound}."), message, name);

    /// <summary>Fails unless the value is between the bounds, both inclusive.</summary>
    public static Result<T> EnsureInRange<T>(this T value, T min, T max, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IComparable<T> =>
        RuleFailure.Lift(value, message, name).EnsureInRange(min, max, message, name);

    /// <summary>Fails unless the value is between the bounds, both inclusive.</summary>
    public static Result<T> EnsureInRange<T>(this Result<T> result, T min, T max, string? message = null, string? name = null) where T : IComparable<T> =>
        RuleFailure.Check(result, () => Is.InRange(min, max), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be between {min} and {max}."), message, name);

    /// <summary>Fails unless the number is greater than zero.</summary>
    public static Result<T> EnsurePositive<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : INumberBase<T> =>
        RuleFailure.Lift(value, message, name).EnsurePositive(message, name);

    /// <summary>Fails unless the number is greater than zero.</summary>
    public static Result<T> EnsurePositive<T>(this Result<T> result, string? message = null, string? name = null) where T : INumberBase<T> =>
        RuleFailure.Check(result, Is.Positive, "{Name} must be positive.", message, name);

    /// <summary>Fails unless the number is less than zero.</summary>
    public static Result<T> EnsureNegative<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : INumberBase<T> =>
        RuleFailure.Lift(value, message, name).EnsureNegative(message, name);

    /// <summary>Fails unless the number is less than zero.</summary>
    public static Result<T> EnsureNegative<T>(this Result<T> result, string? message = null, string? name = null) where T : INumberBase<T> =>
        RuleFailure.Check(result, Is.Negative, "{Name} must be negative.", message, name);

    /// <summary>Fails unless the number is zero or less.</summary>
    public static Result<T> EnsureNonPositive<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : INumberBase<T> =>
        RuleFailure.Lift(value, message, name).EnsureNonPositive(message, name);

    /// <summary>Fails unless the number is zero or less.</summary>
    public static Result<T> EnsureNonPositive<T>(this Result<T> result, string? message = null, string? name = null) where T : INumberBase<T> =>
        RuleFailure.Check(result, Is.NonPositive, "{Name} must not be positive.", message, name);

    /// <summary>Fails unless the number is zero or greater.</summary>
    public static Result<T> EnsureNonNegative<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : INumberBase<T> =>
        RuleFailure.Lift(value, message, name).EnsureNonNegative(message, name);

    /// <summary>Fails unless the number is zero or greater.</summary>
    public static Result<T> EnsureNonNegative<T>(this Result<T> result, string? message = null, string? name = null) where T : INumberBase<T> =>
        RuleFailure.Check(result, Is.NonNegative, "{Name} must not be negative.", message, name);

    /// <summary>Fails unless the number is zero.</summary>
    public static Result<T> EnsureZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : INumberBase<T> =>
        RuleFailure.Lift(value, message, name).EnsureZero(message, name);

    /// <summary>Fails unless the number is zero.</summary>
    public static Result<T> EnsureZero<T>(this Result<T> result, string? message = null, string? name = null) where T : INumberBase<T> =>
        RuleFailure.Check(result, Is.Zero, "{Name} must be zero.", message, name);

    /// <summary>Fails unless the number is not zero.</summary>
    public static Result<T> EnsureNonZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : INumberBase<T> =>
        RuleFailure.Lift(value, message, name).EnsureNonZero(message, name);

    /// <summary>Fails unless the number is not zero.</summary>
    public static Result<T> EnsureNonZero<T>(this Result<T> result, string? message = null, string? name = null) where T : INumberBase<T> =>
        RuleFailure.Check(result, Is.NonZero, "{Name} must not be zero.", message, name);

    /// <summary>Fails unless the integer is even.</summary>
    public static Result<T> EnsureEven<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IBinaryInteger<T> =>
        RuleFailure.Lift(value, message, name).EnsureEven(message, name);

    /// <summary>Fails unless the integer is even.</summary>
    public static Result<T> EnsureEven<T>(this Result<T> result, string? message = null, string? name = null) where T : IBinaryInteger<T> =>
        RuleFailure.Check(result, Is.Even, "{Name} must be even.", message, name);

    /// <summary>Fails unless the integer is odd.</summary>
    public static Result<T> EnsureOdd<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IBinaryInteger<T> =>
        RuleFailure.Lift(value, message, name).EnsureOdd(message, name);

    /// <summary>Fails unless the integer is odd.</summary>
    public static Result<T> EnsureOdd<T>(this Result<T> result, string? message = null, string? name = null) where T : IBinaryInteger<T> =>
        RuleFailure.Check(result, Is.Odd, "{Name} must be odd.", message, name);

    /// <summary>Fails unless the integer is divisible by the divisor.</summary>
    public static Result<T> EnsureDivisibleBy<T>(this T value, T divisor, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where T : IBinaryInteger<T> =>
        RuleFailure.Lift(value, message, name).EnsureDivisibleBy(divisor, message, name);

    /// <summary>Fails unless the integer is divisible by the divisor.</summary>
    public static Result<T> EnsureDivisibleBy<T>(this Result<T> result, T divisor, string? message = null, string? name = null) where T : IBinaryInteger<T> =>
        RuleFailure.Check(result, () => Is.DivisibleBy(divisor), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be divisible by {divisor}."), message, name);
}
