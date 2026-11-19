using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named Guid, bool and enum rules in two forms. The form on a value returns a <see cref="Result{T}"/>,
/// and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes an optional name,
/// and an error or empty result passes through. <c>message</c> replaces the default message.
/// </summary>
public static class ValueRuleExtensions
{
    /// <summary>Fails unless the Guid is not empty.</summary>
    public static Result<Guid> EnsureNotEmpty(this Guid value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotEmpty(message, name);

    /// <summary>Fails unless the Guid is not empty.</summary>
    public static Result<Guid> EnsureNotEmpty(this Result<Guid> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotEmpty, "{Name} must not be empty.", message, name);

    /// <summary>Fails unless the Guid is empty.</summary>
    public static Result<Guid> EnsureEmpty(this Guid value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureEmpty(message, name);

    /// <summary>Fails unless the Guid is empty.</summary>
    public static Result<Guid> EnsureEmpty(this Result<Guid> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Empty, "{Name} must be empty.", message, name);

    /// <summary>Fails unless the value is true.</summary>
    public static Result<bool> EnsureTrue(this bool value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureTrue(message, name);

    /// <summary>Fails unless the value is true.</summary>
    public static Result<bool> EnsureTrue(this Result<bool> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.True, "{Name} must be true.", message, name);

    /// <summary>Fails unless the value is false.</summary>
    public static Result<bool> EnsureFalse(this bool value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureFalse(message, name);

    /// <summary>Fails unless the value is false.</summary>
    public static Result<bool> EnsureFalse(this Result<bool> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.False, "{Name} must be false.", message, name);

    /// <summary>Fails unless the value is a named member of the enumeration. Combined flag values are rejected.</summary>
    public static Result<TEnum> EnsureDefined<TEnum>(this TEnum value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TEnum : struct, Enum =>
        RuleFailure.Lift(value, message, name).EnsureDefined(message, name);

    /// <summary>Fails unless the value is a named member of the enumeration. Combined flag values are rejected.</summary>
    public static Result<TEnum> EnsureDefined<TEnum>(this Result<TEnum> result, string? message = null, string? name = null) where TEnum : struct, Enum =>
        RuleFailure.Check(result, Is.Defined, "{Name} must be a defined value.", message, name);
}
