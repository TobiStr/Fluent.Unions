using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;

namespace Fluent.Unions.Validation;

public static partial class Is
{
    private static readonly EmailAddressAttribute EmailValidator = new();
    private static readonly PhoneAttribute PhoneValidator = new();
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>True if the string is not null and not empty.</summary>
    public static bool NotEmpty(string? value) => !string.IsNullOrEmpty(value);

    /// <summary>True if the string is null or empty.</summary>
    public static bool Empty(string? value) => string.IsNullOrEmpty(value);

    /// <summary>True if the string has a character that is not white space.</summary>
    public static bool NotWhiteSpace(string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>True if the string is null, empty or only white space.</summary>
    public static bool WhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>True if the string has exactly one <c>@</c> that is neither the first nor the last character (the DataAnnotations email rule). The domain is not checked.</summary>
    public static bool MatchesEmail(string? value) => !string.IsNullOrEmpty(value) && EmailValidator.IsValid(value);

    /// <summary>True if the string is an absolute http or https URL.</summary>
    public static bool MatchesUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>True if the string parses as a GUID.</summary>
    public static bool MatchesGuid(string? value) => Guid.TryParse(value, out _);

    /// <summary>True if the string parses as an IP address.</summary>
    public static bool MatchesIpAddress(string? value) => IPAddress.TryParse(value, out _);

    /// <summary>True if the string has only digits, spaces and <c>+ - . ( )</c>, with an optional extension (the DataAnnotations phone rule).</summary>
    public static bool MatchesPhoneNumber(string? value) => !string.IsNullOrEmpty(value) && PhoneValidator.IsValid(value);

    /// <summary>True if the string is not empty and has only letters.</summary>
    public static bool Alpha(string? value) => AllCharacters(value, char.IsLetter);

    /// <summary>True if the string is not empty and has only letters and digits.</summary>
    public static bool Alphanumeric(string? value) => AllCharacters(value, char.IsLetterOrDigit);

    /// <summary>True if the string is not empty and has only digits.</summary>
    public static bool Numeric(string? value) => AllCharacters(value, char.IsDigit);

    /// <summary>True if the string is not empty and valid Base64.</summary>
    public static bool Base64(string? value) =>
        !string.IsNullOrEmpty(value) && Convert.TryFromBase64String(value, new byte[value.Length * 3 / 4], out _);

    /// <summary>True if the string has a digit.</summary>
    public static bool IncludesDigit(string? value) => AnyCharacter(value, char.IsDigit);

    /// <summary>True if the string has a letter.</summary>
    public static bool IncludesLetter(string? value) => AnyCharacter(value, char.IsLetter);

    /// <summary>True if the string has a letter or a digit.</summary>
    public static bool IncludesLetterOrDigit(string? value) => AnyCharacter(value, char.IsLetterOrDigit);

    /// <summary>True if the string has a lowercase letter.</summary>
    public static bool IncludesLower(string? value) => AnyCharacter(value, char.IsLower);

    /// <summary>True if the string has an uppercase letter.</summary>
    public static bool IncludesUpper(string? value) => AnyCharacter(value, char.IsUpper);

    /// <summary>True if the string has a punctuation character.</summary>
    public static bool IncludesPunctuation(string? value) => AnyCharacter(value, char.IsPunctuation);

    /// <summary>True if the string has a character that is not a letter, digit or white space.</summary>
    public static bool IncludesSpecial(string? value) =>
        AnyCharacter(value, character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character));

    /// <summary>True if the string has a symbol character.</summary>
    public static bool IncludesSymbol(string? value) => AnyCharacter(value, char.IsSymbol);

    /// <summary>True if the string has a white space character.</summary>
    public static bool IncludesWhiteSpace(string? value) => AnyCharacter(value, char.IsWhiteSpace);

    /// <summary>Creates a predicate for strings with exactly the given length.</summary>
    public static Func<string?, bool> HasLength(int length) => value => value is not null && value.Length == length;

    /// <summary>Creates a predicate for strings longer than the given length.</summary>
    public static Func<string?, bool> LongerThan(int length) => value => value is not null && value.Length > length;

    /// <summary>Creates a predicate for strings at least as long as the given length.</summary>
    public static Func<string?, bool> LongerThanOrEqual(int length) => value => value is not null && value.Length >= length;

    /// <summary>Creates a predicate for strings shorter than the given length.</summary>
    public static Func<string?, bool> ShorterThan(int length) => value => value is not null && value.Length < length;

    /// <summary>Creates a predicate for strings at most as long as the given length.</summary>
    public static Func<string?, bool> ShorterThanOrEqual(int length) => value => value is not null && value.Length <= length;

    /// <summary>Creates a predicate for strings that contain the text.</summary>
    public static Func<string?, bool> Contains(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(text);
        return value => value is not null && value.Contains(text, comparison);
    }

    /// <summary>Creates a predicate for strings that do not contain the text.</summary>
    public static Func<string?, bool> NotContains(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(text);
        return value => value is not null && !value.Contains(text, comparison);
    }

    /// <summary>Creates a predicate for strings that start with the text.</summary>
    public static Func<string?, bool> StartsWith(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(text);
        return value => value is not null && value.StartsWith(text, comparison);
    }

    /// <summary>Creates a predicate for strings that do not start with the text.</summary>
    public static Func<string?, bool> NotStartsWith(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(text);
        return value => value is not null && !value.StartsWith(text, comparison);
    }

    /// <summary>Creates a predicate for strings that end with the text.</summary>
    public static Func<string?, bool> EndsWith(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(text);
        return value => value is not null && value.EndsWith(text, comparison);
    }

    /// <summary>Creates a predicate for strings that do not end with the text.</summary>
    public static Func<string?, bool> NotEndsWith(string text, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(text);
        return value => value is not null && !value.EndsWith(text, comparison);
    }

    /// <summary>Creates a predicate for strings that match the regular expression.</summary>
    public static Func<string?, bool> Matches(Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);
        return value => value is not null && regex.IsMatch(value);
    }

    /// <summary>Creates a predicate for strings that match the pattern. The pattern compiles once and a match times out after one second.</summary>
    public static Func<string?, bool> Matches(string pattern, RegexOptions options = RegexOptions.None) =>
        Matches(new Regex(pattern, options, RegexTimeout));

    /// <summary>Creates a predicate for strings that do not match the regular expression.</summary>
    public static Func<string?, bool> NotMatches(Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);
        return value => value is not null && !regex.IsMatch(value);
    }

    /// <summary>Creates a predicate for strings that do not match the pattern. The pattern compiles once and a match times out after one second.</summary>
    public static Func<string?, bool> NotMatches(string pattern, RegexOptions options = RegexOptions.None) =>
        NotMatches(new Regex(pattern, options, RegexTimeout));

    private static bool AllCharacters(string? value, Func<char, bool> test) => !string.IsNullOrEmpty(value) && value.All(test);

    private static bool AnyCharacter(string? value, Func<char, bool> test) => value is not null && value.Any(test);
}
