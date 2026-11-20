using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named string rules in two forms. The form on a string returns a <see cref="Result{T}"/>, a null string is an error,
/// and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes an optional name,
/// and an error or empty result passes through. <c>message</c> replaces the default message.
/// </summary>
public static class StringRuleExtensions
{
    /// <summary>Fails unless the string is not empty.</summary>
    public static Result<string> EnsureNotEmpty(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotEmpty(message, name);

    /// <summary>Fails unless the string is not empty.</summary>
    public static Result<string> EnsureNotEmpty(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotEmpty, "{Name} must not be empty.", message, name);

    /// <summary>Fails unless the string is empty.</summary>
    public static Result<string> EnsureEmpty(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureEmpty(message, name);

    /// <summary>Fails unless the string is empty.</summary>
    public static Result<string> EnsureEmpty(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Empty, "{Name} must be empty.", message, name);

    /// <summary>Fails unless the string has a character that is not white space.</summary>
    public static Result<string> EnsureNotWhiteSpace(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotWhiteSpace(message, name);

    /// <summary>Fails unless the string has a character that is not white space.</summary>
    public static Result<string> EnsureNotWhiteSpace(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotWhiteSpace, "{Name} must not be empty or white space.", message, name);

    /// <summary>Fails unless the string is empty or only white space.</summary>
    public static Result<string> EnsureWhiteSpace(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureWhiteSpace(message, name);

    /// <summary>Fails unless the string is empty or only white space.</summary>
    public static Result<string> EnsureWhiteSpace(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.WhiteSpace, "{Name} must be empty or white space.", message, name);

    /// <summary>Fails unless the string has exactly one <c>@</c> that is neither the first nor the last character (the DataAnnotations email rule). The domain is not checked.</summary>
    public static Result<string> EnsureMatchesEmail(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatchesEmail(message, name);

    /// <summary>Fails unless the string has exactly one <c>@</c> that is neither the first nor the last character (the DataAnnotations email rule). The domain is not checked.</summary>
    public static Result<string> EnsureMatchesEmail(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.MatchesEmail, "{Name} must be a valid email address.", message, name);

    /// <summary>Fails unless the string is an absolute http or https URL.</summary>
    public static Result<string> EnsureMatchesUrl(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatchesUrl(message, name);

    /// <summary>Fails unless the string is an absolute http or https URL.</summary>
    public static Result<string> EnsureMatchesUrl(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.MatchesUrl, "{Name} must be a valid http or https URL.", message, name);

    /// <summary>Fails unless the string parses as a GUID.</summary>
    public static Result<string> EnsureMatchesGuid(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatchesGuid(message, name);

    /// <summary>Fails unless the string parses as a GUID.</summary>
    public static Result<string> EnsureMatchesGuid(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.MatchesGuid, "{Name} must be a valid GUID.", message, name);

    /// <summary>Fails unless the string parses as an IP address.</summary>
    public static Result<string> EnsureMatchesIpAddress(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatchesIpAddress(message, name);

    /// <summary>Fails unless the string parses as an IP address.</summary>
    public static Result<string> EnsureMatchesIpAddress(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.MatchesIpAddress, "{Name} must be a valid IP address.", message, name);

    /// <summary>Fails unless the string has only digits, spaces and <c>+ - . ( )</c>, with an optional extension (the DataAnnotations phone rule).</summary>
    public static Result<string> EnsureMatchesPhoneNumber(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatchesPhoneNumber(message, name);

    /// <summary>Fails unless the string has only digits, spaces and <c>+ - . ( )</c>, with an optional extension (the DataAnnotations phone rule).</summary>
    public static Result<string> EnsureMatchesPhoneNumber(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.MatchesPhoneNumber, "{Name} must be a valid phone number.", message, name);

    /// <summary>Fails unless the string has only letters.</summary>
    public static Result<string> EnsureAlpha(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureAlpha(message, name);

    /// <summary>Fails unless the string has only letters.</summary>
    public static Result<string> EnsureAlpha(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Alpha, "{Name} must contain only letters.", message, name);

    /// <summary>Fails unless the string has only letters and digits.</summary>
    public static Result<string> EnsureAlphanumeric(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureAlphanumeric(message, name);

    /// <summary>Fails unless the string has only letters and digits.</summary>
    public static Result<string> EnsureAlphanumeric(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Alphanumeric, "{Name} must contain only letters and digits.", message, name);

    /// <summary>Fails unless the string has only digits.</summary>
    public static Result<string> EnsureNumeric(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNumeric(message, name);

    /// <summary>Fails unless the string has only digits.</summary>
    public static Result<string> EnsureNumeric(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Numeric, "{Name} must contain only digits.", message, name);

    /// <summary>Fails unless the string is valid Base64.</summary>
    public static Result<string> EnsureBase64(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureBase64(message, name);

    /// <summary>Fails unless the string is valid Base64.</summary>
    public static Result<string> EnsureBase64(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Base64, "{Name} must be a valid Base64 string.", message, name);

    /// <summary>Fails unless the string has a digit.</summary>
    public static Result<string> EnsureIncludesDigit(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesDigit(message, name);

    /// <summary>Fails unless the string has a digit.</summary>
    public static Result<string> EnsureIncludesDigit(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesDigit, "{Name} must include a digit.", message, name);

    /// <summary>Fails unless the string has a letter.</summary>
    public static Result<string> EnsureIncludesLetter(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesLetter(message, name);

    /// <summary>Fails unless the string has a letter.</summary>
    public static Result<string> EnsureIncludesLetter(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesLetter, "{Name} must include a letter.", message, name);

    /// <summary>Fails unless the string has a letter or a digit.</summary>
    public static Result<string> EnsureIncludesLetterOrDigit(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesLetterOrDigit(message, name);

    /// <summary>Fails unless the string has a letter or a digit.</summary>
    public static Result<string> EnsureIncludesLetterOrDigit(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesLetterOrDigit, "{Name} must include a letter or a digit.", message, name);

    /// <summary>Fails unless the string has a lowercase letter.</summary>
    public static Result<string> EnsureIncludesLower(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesLower(message, name);

    /// <summary>Fails unless the string has a lowercase letter.</summary>
    public static Result<string> EnsureIncludesLower(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesLower, "{Name} must include a lowercase letter.", message, name);

    /// <summary>Fails unless the string has an uppercase letter.</summary>
    public static Result<string> EnsureIncludesUpper(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesUpper(message, name);

    /// <summary>Fails unless the string has an uppercase letter.</summary>
    public static Result<string> EnsureIncludesUpper(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesUpper, "{Name} must include an uppercase letter.", message, name);

    /// <summary>Fails unless the string has a punctuation character.</summary>
    public static Result<string> EnsureIncludesPunctuation(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesPunctuation(message, name);

    /// <summary>Fails unless the string has a punctuation character.</summary>
    public static Result<string> EnsureIncludesPunctuation(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesPunctuation, "{Name} must include a punctuation character.", message, name);

    /// <summary>Fails unless the string has a character that is not a letter, digit or white space.</summary>
    public static Result<string> EnsureIncludesSpecial(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesSpecial(message, name);

    /// <summary>Fails unless the string has a character that is not a letter, digit or white space.</summary>
    public static Result<string> EnsureIncludesSpecial(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesSpecial, "{Name} must include a special character.", message, name);

    /// <summary>Fails unless the string has a symbol character.</summary>
    public static Result<string> EnsureIncludesSymbol(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesSymbol(message, name);

    /// <summary>Fails unless the string has a symbol character.</summary>
    public static Result<string> EnsureIncludesSymbol(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesSymbol, "{Name} must include a symbol.", message, name);

    /// <summary>Fails unless the string has a white space character.</summary>
    public static Result<string> EnsureIncludesWhiteSpace(this string? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureIncludesWhiteSpace(message, name);

    /// <summary>Fails unless the string has a white space character.</summary>
    public static Result<string> EnsureIncludesWhiteSpace(this Result<string> result, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.IncludesWhiteSpace, "{Name} must include a white space character.", message, name);

    /// <summary>Fails unless the string has exactly the given length.</summary>
    public static Result<string> EnsureHasLength(this string? value, int length, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureHasLength(length, message, name);

    /// <summary>Fails unless the string has exactly the given length.</summary>
    public static Result<string> EnsureHasLength(this Result<string> result, int length, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.HasLength(length), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be exactly {length} characters long."), message, name);

    /// <summary>Fails unless the string is longer than the given length.</summary>
    public static Result<string> EnsureLongerThan(this string? value, int length, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureLongerThan(length, message, name);

    /// <summary>Fails unless the string is longer than the given length.</summary>
    public static Result<string> EnsureLongerThan(this Result<string> result, int length, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.LongerThan(length), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be longer than {length} characters."), message, name);

    /// <summary>Fails unless the string is at least as long as the given length.</summary>
    public static Result<string> EnsureLongerThanOrEqual(this string? value, int length, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureLongerThanOrEqual(length, message, name);

    /// <summary>Fails unless the string is at least as long as the given length.</summary>
    public static Result<string> EnsureLongerThanOrEqual(this Result<string> result, int length, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.LongerThanOrEqual(length), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be at least {length} characters long."), message, name);

    /// <summary>Fails unless the string is shorter than the given length.</summary>
    public static Result<string> EnsureShorterThan(this string? value, int length, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureShorterThan(length, message, name);

    /// <summary>Fails unless the string is shorter than the given length.</summary>
    public static Result<string> EnsureShorterThan(this Result<string> result, int length, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.ShorterThan(length), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be shorter than {length} characters."), message, name);

    /// <summary>Fails unless the string is at most as long as the given length.</summary>
    public static Result<string> EnsureShorterThanOrEqual(this string? value, int length, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureShorterThanOrEqual(length, message, name);

    /// <summary>Fails unless the string is at most as long as the given length.</summary>
    public static Result<string> EnsureShorterThanOrEqual(this Result<string> result, int length, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.ShorterThanOrEqual(length), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must be at most {length} characters long."), message, name);

    /// <summary>Fails unless the string contains the text.</summary>
    public static Result<string> EnsureContains(this string? value, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureContains(text, comparison, message, name);

    /// <summary>Fails unless the string contains the text.</summary>
    public static Result<string> EnsureContains(this Result<string> result, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Contains(text, comparison), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must contain '{text}'."), message, name);

    /// <summary>Fails unless the string does not contain the text.</summary>
    public static Result<string> EnsureNotContains(this string? value, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotContains(text, comparison, message, name);

    /// <summary>Fails unless the string does not contain the text.</summary>
    public static Result<string> EnsureNotContains(this Result<string> result, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotContains(text, comparison), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not contain '{text}'."), message, name);

    /// <summary>Fails unless the string starts with the text.</summary>
    public static Result<string> EnsureStartsWith(this string? value, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureStartsWith(text, comparison, message, name);

    /// <summary>Fails unless the string starts with the text.</summary>
    public static Result<string> EnsureStartsWith(this Result<string> result, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.StartsWith(text, comparison), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must start with '{text}'."), message, name);

    /// <summary>Fails unless the string does not start with the text.</summary>
    public static Result<string> EnsureNotStartsWith(this string? value, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotStartsWith(text, comparison, message, name);

    /// <summary>Fails unless the string does not start with the text.</summary>
    public static Result<string> EnsureNotStartsWith(this Result<string> result, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotStartsWith(text, comparison), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not start with '{text}'."), message, name);

    /// <summary>Fails unless the string ends with the text.</summary>
    public static Result<string> EnsureEndsWith(this string? value, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureEndsWith(text, comparison, message, name);

    /// <summary>Fails unless the string ends with the text.</summary>
    public static Result<string> EnsureEndsWith(this Result<string> result, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.EndsWith(text, comparison), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must end with '{text}'."), message, name);

    /// <summary>Fails unless the string does not end with the text.</summary>
    public static Result<string> EnsureNotEndsWith(this string? value, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotEndsWith(text, comparison, message, name);

    /// <summary>Fails unless the string does not end with the text.</summary>
    public static Result<string> EnsureNotEndsWith(this Result<string> result, string text, StringComparison comparison = StringComparison.Ordinal, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotEndsWith(text, comparison), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not end with '{text}'."), message, name);

    /// <summary>Fails unless the string matches the regular expression.</summary>
    public static Result<string> EnsureMatches(this string? value, Regex regex, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatches(regex, message, name);

    /// <summary>Fails unless the string matches the regular expression.</summary>
    public static Result<string> EnsureMatches(this Result<string> result, Regex regex, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.Matches(regex), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must match the pattern '{regex}'."), message, name);

    /// <summary>Fails unless the string matches the pattern.</summary>
    public static Result<string> EnsureMatches(this string? value, string pattern, RegexOptions options = RegexOptions.None, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureMatches(pattern, options, message, name);

    /// <summary>Fails unless the string matches the pattern.</summary>
    public static Result<string> EnsureMatches(this Result<string> result, string pattern, RegexOptions options = RegexOptions.None, string? message = null, string? name = null) =>
        RuleFailure.Check(result, () => Is.Matches(pattern, options), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must match the pattern '{pattern}'."), message, name);

    /// <summary>Fails unless the string does not match the regular expression.</summary>
    public static Result<string> EnsureNotMatches(this string? value, Regex regex, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotMatches(regex, message, name);

    /// <summary>Fails unless the string does not match the regular expression.</summary>
    public static Result<string> EnsureNotMatches(this Result<string> result, Regex regex, string? message = null, string? name = null) =>
        RuleFailure.Check(result, Is.NotMatches(regex), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not match the pattern '{regex}'."), message, name);

    /// <summary>Fails unless the string does not match the pattern.</summary>
    public static Result<string> EnsureNotMatches(this string? value, string pattern, RegexOptions options = RegexOptions.None, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        RuleFailure.Lift(value, message, name).EnsureNotMatches(pattern, options, message, name);

    /// <summary>Fails unless the string does not match the pattern.</summary>
    public static Result<string> EnsureNotMatches(this Result<string> result, string pattern, RegexOptions options = RegexOptions.None, string? message = null, string? name = null) =>
        RuleFailure.Check(result, () => Is.NotMatches(pattern, options), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not match the pattern '{pattern}'."), message, name);
}
