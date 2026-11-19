using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named date, time and duration rules in two forms. The form on a value returns a <see cref="Result{T}"/>,
/// and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes an optional name,
/// and an error or empty result passes through. Date rules take a <see cref="TimeProvider"/> or an explicit
/// instant. <c>message</c> replaces the default message.
/// </summary>
public static class TimeRuleExtensions
{
    /// <summary>Fails unless the duration is greater than zero.</summary>
    public static Result<TimeSpan> EnsurePositive(this TimeSpan value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsurePositive(message, name);

    /// <summary>Fails unless the duration is greater than zero.</summary>
    public static Result<TimeSpan> EnsurePositive(this Result<TimeSpan> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Positive, "{Name} must be positive.", message, name);

    /// <summary>Fails unless the duration is less than zero.</summary>
    public static Result<TimeSpan> EnsureNegative(this TimeSpan value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNegative(message, name);

    /// <summary>Fails unless the duration is less than zero.</summary>
    public static Result<TimeSpan> EnsureNegative(this Result<TimeSpan> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Negative, "{Name} must be negative.", message, name);

    /// <summary>Fails unless the duration is zero or less.</summary>
    public static Result<TimeSpan> EnsureNonPositive(this TimeSpan value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNonPositive(message, name);

    /// <summary>Fails unless the duration is zero or less.</summary>
    public static Result<TimeSpan> EnsureNonPositive(this Result<TimeSpan> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NonPositive, "{Name} must not be positive.", message, name);

    /// <summary>Fails unless the duration is zero or greater.</summary>
    public static Result<TimeSpan> EnsureNonNegative(this TimeSpan value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNonNegative(message, name);

    /// <summary>Fails unless the duration is zero or greater.</summary>
    public static Result<TimeSpan> EnsureNonNegative(this Result<TimeSpan> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NonNegative, "{Name} must not be negative.", message, name);

    /// <summary>Fails unless the duration is zero.</summary>
    public static Result<TimeSpan> EnsureZero(this TimeSpan value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureZero(message, name);

    /// <summary>Fails unless the duration is zero.</summary>
    public static Result<TimeSpan> EnsureZero(this Result<TimeSpan> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Zero, "{Name} must be zero.", message, name);

    /// <summary>Fails unless the duration is not zero.</summary>
    public static Result<TimeSpan> EnsureNonZero(this TimeSpan value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNonZero(message, name);

    /// <summary>Fails unless the duration is not zero.</summary>
    public static Result<TimeSpan> EnsureNonZero(this Result<TimeSpan> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NonZero, "{Name} must not be zero.", message, name);

    /// <summary>Fails unless the instant is in the past according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInPast(this DateTimeOffset value, TimeProvider timeProvider, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInPast(timeProvider, message, name);

    /// <summary>Fails unless the instant is in the past according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInPast(this Result<DateTimeOffset> result, TimeProvider timeProvider, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InPast(timeProvider), "{Name} must be in the past.", message, name);

    /// <summary>Fails unless the instant is in the past relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInPast(this DateTimeOffset value, DateTimeOffset now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInPast(now, message, name);

    /// <summary>Fails unless the instant is in the past relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInPast(this Result<DateTimeOffset> result, DateTimeOffset now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InPast(now), "{Name} must be in the past.", message, name);

    /// <summary>Fails unless the date is in the past relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInPast(this DateTime value, DateTime now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInPast(now, message, name);

    /// <summary>Fails unless the date is in the past relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInPast(this Result<DateTime> result, DateTime now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InPast(now), "{Name} must be in the past.", message, name);

    /// <summary>Fails unless the instant is in the past or present according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInPastOrPresent(this DateTimeOffset value, TimeProvider timeProvider, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInPastOrPresent(timeProvider, message, name);

    /// <summary>Fails unless the instant is in the past or present according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInPastOrPresent(this Result<DateTimeOffset> result, TimeProvider timeProvider, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InPastOrPresent(timeProvider), "{Name} must be in the past or present.", message, name);

    /// <summary>Fails unless the instant is in the past or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInPastOrPresent(this DateTimeOffset value, DateTimeOffset now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInPastOrPresent(now, message, name);

    /// <summary>Fails unless the instant is in the past or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInPastOrPresent(this Result<DateTimeOffset> result, DateTimeOffset now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InPastOrPresent(now), "{Name} must be in the past or present.", message, name);

    /// <summary>Fails unless the date is in the past or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInPastOrPresent(this DateTime value, DateTime now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInPastOrPresent(now, message, name);

    /// <summary>Fails unless the date is in the past or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInPastOrPresent(this Result<DateTime> result, DateTime now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InPastOrPresent(now), "{Name} must be in the past or present.", message, name);

    /// <summary>Fails unless the instant is in the future according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInFuture(this DateTimeOffset value, TimeProvider timeProvider, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInFuture(timeProvider, message, name);

    /// <summary>Fails unless the instant is in the future according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInFuture(this Result<DateTimeOffset> result, TimeProvider timeProvider, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InFuture(timeProvider), "{Name} must be in the future.", message, name);

    /// <summary>Fails unless the instant is in the future relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInFuture(this DateTimeOffset value, DateTimeOffset now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInFuture(now, message, name);

    /// <summary>Fails unless the instant is in the future relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInFuture(this Result<DateTimeOffset> result, DateTimeOffset now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InFuture(now), "{Name} must be in the future.", message, name);

    /// <summary>Fails unless the date is in the future relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInFuture(this DateTime value, DateTime now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInFuture(now, message, name);

    /// <summary>Fails unless the date is in the future relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInFuture(this Result<DateTime> result, DateTime now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InFuture(now), "{Name} must be in the future.", message, name);

    /// <summary>Fails unless the instant is in the future or present according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInFutureOrPresent(this DateTimeOffset value, TimeProvider timeProvider, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInFutureOrPresent(timeProvider, message, name);

    /// <summary>Fails unless the instant is in the future or present according to the provider.</summary>
    public static Result<DateTimeOffset> EnsureInFutureOrPresent(this Result<DateTimeOffset> result, TimeProvider timeProvider, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InFutureOrPresent(timeProvider), "{Name} must be in the future or present.", message, name);

    /// <summary>Fails unless the instant is in the future or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInFutureOrPresent(this DateTimeOffset value, DateTimeOffset now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInFutureOrPresent(now, message, name);

    /// <summary>Fails unless the instant is in the future or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTimeOffset> EnsureInFutureOrPresent(this Result<DateTimeOffset> result, DateTimeOffset now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InFutureOrPresent(now), "{Name} must be in the future or present.", message, name);

    /// <summary>Fails unless the date is in the future or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInFutureOrPresent(this DateTime value, DateTime now, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureInFutureOrPresent(now, message, name);

    /// <summary>Fails unless the date is in the future or present relative to <paramref name="now"/>.</summary>
    public static Result<DateTime> EnsureInFutureOrPresent(this Result<DateTime> result, DateTime now, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.InFutureOrPresent(now), "{Name} must be in the future or present.", message, name);
}
