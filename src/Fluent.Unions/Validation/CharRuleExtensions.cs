using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named character rules in two forms. The form on a value returns a <see cref="Result{T}"/>,
/// and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes an optional name,
/// and an error or empty result passes through. <c>message</c> replaces the default message.
/// </summary>
public static class CharRuleExtensions
{
    /// <summary>Fails unless the character is a decimal digit.</summary>
    public static Result<char> EnsureDigit(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureDigit(message, name);

    /// <summary>Fails unless the character is a decimal digit.</summary>
    public static Result<char> EnsureDigit(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Digit, "{Name} must be a digit.", message, name);

    /// <summary>Fails unless the character is a letter.</summary>
    public static Result<char> EnsureLetter(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureLetter(message, name);

    /// <summary>Fails unless the character is a letter.</summary>
    public static Result<char> EnsureLetter(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Letter, "{Name} must be a letter.", message, name);

    /// <summary>Fails unless the character is a letter or a decimal digit.</summary>
    public static Result<char> EnsureLetterOrDigit(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureLetterOrDigit(message, name);

    /// <summary>Fails unless the character is a letter or a decimal digit.</summary>
    public static Result<char> EnsureLetterOrDigit(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.LetterOrDigit, "{Name} must be a letter or a digit.", message, name);

    /// <summary>Fails unless the character is a lowercase letter.</summary>
    public static Result<char> EnsureLower(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureLower(message, name);

    /// <summary>Fails unless the character is a lowercase letter.</summary>
    public static Result<char> EnsureLower(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Lower, "{Name} must be lowercase.", message, name);

    /// <summary>Fails unless the character is an uppercase letter.</summary>
    public static Result<char> EnsureUpper(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureUpper(message, name);

    /// <summary>Fails unless the character is an uppercase letter.</summary>
    public static Result<char> EnsureUpper(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Upper, "{Name} must be uppercase.", message, name);

    /// <summary>Fails unless the character is punctuation.</summary>
    public static Result<char> EnsurePunctuation(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsurePunctuation(message, name);

    /// <summary>Fails unless the character is punctuation.</summary>
    public static Result<char> EnsurePunctuation(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Punctuation, "{Name} must be punctuation.", message, name);

    /// <summary>Fails unless the character is not a letter, digit or white space.</summary>
    public static Result<char> EnsureSpecial(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureSpecial(message, name);

    /// <summary>Fails unless the character is not a letter, digit or white space.</summary>
    public static Result<char> EnsureSpecial(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Special, "{Name} must be a special character.", message, name);

    /// <summary>Fails unless the character is a symbol.</summary>
    public static Result<char> EnsureSymbol(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureSymbol(message, name);

    /// <summary>Fails unless the character is a symbol.</summary>
    public static Result<char> EnsureSymbol(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Symbol, "{Name} must be a symbol.", message, name);

    /// <summary>Fails unless the character is white space.</summary>
    public static Result<char> EnsureWhiteSpace(this char value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureWhiteSpace(message, name);

    /// <summary>Fails unless the character is white space.</summary>
    public static Result<char> EnsureWhiteSpace(this Result<char> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.WhiteSpace, "{Name} must be white space.", message, name);
}
