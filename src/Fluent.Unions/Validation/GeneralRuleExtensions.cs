using System.Globalization;
using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named rules for any value, and for options, in two forms. The form on a value returns a <see cref="Result{T}"/>, a null value
/// is an error, and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes an optional name,
/// and an error or empty result passes through. <c>message</c> replaces the default message.
/// </summary>
public static class GeneralRuleExtensions
{
    /// <summary>Fails unless the value equals the expected value.</summary>
    public static Result<T> EnsureEqualTo<T>(this T value, T expected, IEqualityComparer<T>? comparer = null, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureEqualTo(expected, comparer, message, name);

    /// <summary>Fails unless the value equals the expected value.</summary>
    public static Result<T> EnsureEqualTo<T>(this Result<T> result, T expected, IEqualityComparer<T>? comparer = null, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.EqualTo(expected, comparer), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be equal to {expected}."), message, name);

    /// <summary>Fails if the value equals the other value.</summary>
    public static Result<T> EnsureNotEqualTo<T>(this T value, T other, IEqualityComparer<T>? comparer = null, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotEqualTo(other, comparer, message, name);

    /// <summary>Fails if the value equals the other value.</summary>
    public static Result<T> EnsureNotEqualTo<T>(this Result<T> result, T other, IEqualityComparer<T>? comparer = null, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotEqualTo(other, comparer), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not be equal to {other}."), message, name);

    /// <summary>Fails unless the value is one of the allowed values.</summary>
    public static Result<T> EnsureIn<T>(this T value, T[] allowed, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIn(allowed, message, name);

    /// <summary>Fails unless the value is one of the allowed values.</summary>
    public static Result<T> EnsureIn<T>(this Result<T> result, T[] allowed, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.In(allowed), "{Name} must be one of the allowed values.", message, name);

    /// <summary>Fails if the value is one of the disallowed values.</summary>
    public static Result<T> EnsureNotIn<T>(this T value, T[] disallowed, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotIn(disallowed, message, name);

    /// <summary>Fails if the value is one of the disallowed values.</summary>
    public static Result<T> EnsureNotIn<T>(this Result<T> result, T[] disallowed, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotIn(disallowed), "{Name} must not be one of the disallowed values.", message, name);

    /// <summary>Fails if the value is null. A <see cref="Result{T}"/> never holds null, so there is no form on it.</summary>
    public static Result<T> EnsureNotNull<T>(this T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name);

    /// <summary>Fails unless the option has a value.</summary>
    public static Result<Option<T>> EnsureSome<T>(this Option<T> value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        Result<Option<T>>.Ok(value).EnsureSome(message, name);

    /// <summary>Fails unless the option has a value.</summary>
    public static Result<Option<T>> EnsureSome<T>(this Result<Option<T>> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Some, "{Name} must have a value.", message, name);

    /// <summary>Fails unless the option has no value.</summary>
    public static Result<Option<T>> EnsureNone<T>(this Option<T> value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        Result<Option<T>>.Ok(value).EnsureNone(message, name);

    /// <summary>Fails unless the option has no value.</summary>
    public static Result<Option<T>> EnsureNone<T>(this Result<Option<T>> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.None, "{Name} must not have a value.", message, name);
}
