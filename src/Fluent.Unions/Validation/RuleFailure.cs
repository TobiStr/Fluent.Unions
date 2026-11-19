using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Extensions;

namespace Fluent.Unions.Validation;

internal static class RuleFailure
{
    private const string NamePlaceholder = "{Name}";
    private const string FallbackName = "Value";
    internal const string NullTemplate = "{Name} must not be null.";

    internal static string? TrimName(string? expression)
    {
        if (expression is null)
        {
            return null;
        }

        var end = expression.Length;
        while (end > 0 && (expression[end - 1] == '!' || char.IsWhiteSpace(expression[end - 1])))
        {
            end--;
        }

        var segment = expression.AsSpan(0, end);
        segment = segment[(segment.LastIndexOf('.') + 1)..].Trim();

        return IsIdentifier(segment) ? segment.ToString() : null;
    }

    internal static Exception Create(string template, string? message, string? name)
    {
        var member = TrimName(name);
        var text = message ?? (template.StartsWith(NamePlaceholder, StringComparison.Ordinal)
            ? string.Concat(member ?? FallbackName, template.AsSpan(NamePlaceholder.Length))
            : template);
        string[] memberNames = member is null ? [] : [member];

        return new ValidationException(new ValidationResult(text, memberNames), null, null);
    }

    internal static Result<T> Lift<T>(T? value, string? message, string? name) =>
        value is null ? Result<T>.Error(Create(NullTemplate, message, name)) : Result<T>.Ok(value);

    internal static Result<T> Check<T>(Result<T> result, Func<T, bool> rule, string template, string? message, string? name) =>
        result.Ensure(rule, _ => Create(template, message, name));

    internal static Result<T> Check<T>(Result<T> result, Func<Func<T, bool>> ruleFactory, string template, string? message, string? name) =>
        result.Value is Exception or not T ? result : Check(result, ruleFactory(), template, message, name);

    private static bool IsIdentifier(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || !(char.IsLetter(text[0]) || text[0] == '_'))
        {
            return false;
        }

        foreach (var character in text[1..])
        {
            if (!(char.IsLetterOrDigit(character) || character == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
