using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class StringRuleTests
{
    private static readonly Regex UpperPair = new("^[A-Z]{2}$");

    public sealed record RuleCase(
        string Label,
        string Passing,
        string Failing,
        string Message,
        Func<string?, Result<string>> OnValue,
        Func<Result<string>, Result<string>> OnResult);

    private static TestCaseData Rule(
        string label,
        string passing,
        string failing,
        string message,
        Func<string?, Result<string>> onValue,
        Func<Result<string>, Result<string>> onResult) =>
        new TestCaseData(new RuleCase(label, passing, failing, message, onValue, onResult)).SetName(label);

    private static IEnumerable<TestCaseData> Rules() =>
    [
        Rule("NotEmpty", "a", "", "Field must not be empty.", v => v.EnsureNotEmpty(name: "Field"), r => r.EnsureNotEmpty(name: "Field")),
        Rule("Empty", "", "a", "Field must be empty.", v => v.EnsureEmpty(name: "Field"), r => r.EnsureEmpty(name: "Field")),
        Rule("NotWhiteSpace", "a", " ", "Field must not be empty or white space.", v => v.EnsureNotWhiteSpace(name: "Field"), r => r.EnsureNotWhiteSpace(name: "Field")),
        Rule("WhiteSpace", " ", "a", "Field must be empty or white space.", v => v.EnsureWhiteSpace(name: "Field"), r => r.EnsureWhiteSpace(name: "Field")),
        Rule("MatchesEmail", "jane@shop.test", "jane", "Field must be a valid email address.", v => v.EnsureMatchesEmail(name: "Field"), r => r.EnsureMatchesEmail(name: "Field")),
        Rule("MatchesUrl", "https://shop.test", "shop.test", "Field must be a valid http or https URL.", v => v.EnsureMatchesUrl(name: "Field"), r => r.EnsureMatchesUrl(name: "Field")),
        Rule("MatchesGuid", "0f8fad5b-d9cb-469f-a165-70867728950e", "nope", "Field must be a valid GUID.", v => v.EnsureMatchesGuid(name: "Field"), r => r.EnsureMatchesGuid(name: "Field")),
        Rule("MatchesIpAddress", "10.0.0.1", "999.1.1.1", "Field must be a valid IP address.", v => v.EnsureMatchesIpAddress(name: "Field"), r => r.EnsureMatchesIpAddress(name: "Field")),
        Rule("MatchesPhoneNumber", "+49 30 1234567", "abc", "Field must be a valid phone number.", v => v.EnsureMatchesPhoneNumber(name: "Field"), r => r.EnsureMatchesPhoneNumber(name: "Field")),
        Rule("Alpha", "abc", "ab1", "Field must contain only letters.", v => v.EnsureAlpha(name: "Field"), r => r.EnsureAlpha(name: "Field")),
        Rule("Alphanumeric", "ab1", "ab-", "Field must contain only letters and digits.", v => v.EnsureAlphanumeric(name: "Field"), r => r.EnsureAlphanumeric(name: "Field")),
        Rule("Numeric", "123", "12a", "Field must contain only digits.", v => v.EnsureNumeric(name: "Field"), r => r.EnsureNumeric(name: "Field")),
        Rule("Base64", "aGVsbG8=", "not base64!", "Field must be a valid Base64 string.", v => v.EnsureBase64(name: "Field"), r => r.EnsureBase64(name: "Field")),
        Rule("IncludesDigit", "a1", "ab", "Field must include a digit.", v => v.EnsureIncludesDigit(name: "Field"), r => r.EnsureIncludesDigit(name: "Field")),
        Rule("IncludesLetter", "1a", "12", "Field must include a letter.", v => v.EnsureIncludesLetter(name: "Field"), r => r.EnsureIncludesLetter(name: "Field")),
        Rule("IncludesLetterOrDigit", "-a", "--", "Field must include a letter or a digit.", v => v.EnsureIncludesLetterOrDigit(name: "Field"), r => r.EnsureIncludesLetterOrDigit(name: "Field")),
        Rule("IncludesLower", "aB", "AB", "Field must include a lowercase letter.", v => v.EnsureIncludesLower(name: "Field"), r => r.EnsureIncludesLower(name: "Field")),
        Rule("IncludesUpper", "aB", "ab", "Field must include an uppercase letter.", v => v.EnsureIncludesUpper(name: "Field"), r => r.EnsureIncludesUpper(name: "Field")),
        Rule("IncludesPunctuation", "a!", "a+", "Field must include a punctuation character.", v => v.EnsureIncludesPunctuation(name: "Field"), r => r.EnsureIncludesPunctuation(name: "Field")),
        Rule("IncludesSpecial", "a-", "a b1", "Field must include a special character.", v => v.EnsureIncludesSpecial(name: "Field"), r => r.EnsureIncludesSpecial(name: "Field")),
        Rule("IncludesSymbol", "a+", "a!", "Field must include a symbol.", v => v.EnsureIncludesSymbol(name: "Field"), r => r.EnsureIncludesSymbol(name: "Field")),
        Rule("IncludesWhiteSpace", "a b", "ab", "Field must include a white space character.", v => v.EnsureIncludesWhiteSpace(name: "Field"), r => r.EnsureIncludesWhiteSpace(name: "Field")),
        Rule("HasLength", "abc", "ab", "Field must be exactly 3 characters long.", v => v.EnsureHasLength(3, name: "Field"), r => r.EnsureHasLength(3, name: "Field")),
        Rule("LongerThan", "abcd", "abc", "Field must be longer than 3 characters.", v => v.EnsureLongerThan(3, name: "Field"), r => r.EnsureLongerThan(3, name: "Field")),
        Rule("LongerThanOrEqual", "abc", "ab", "Field must be at least 3 characters long.", v => v.EnsureLongerThanOrEqual(3, name: "Field"), r => r.EnsureLongerThanOrEqual(3, name: "Field")),
        Rule("ShorterThan", "ab", "abc", "Field must be shorter than 3 characters.", v => v.EnsureShorterThan(3, name: "Field"), r => r.EnsureShorterThan(3, name: "Field")),
        Rule("ShorterThanOrEqual", "abc", "abcd", "Field must be at most 3 characters long.", v => v.EnsureShorterThanOrEqual(3, name: "Field"), r => r.EnsureShorterThanOrEqual(3, name: "Field")),
        Rule("Contains", "xaby", "xy", "Field must contain 'ab'.", v => v.EnsureContains("ab", name: "Field"), r => r.EnsureContains("ab", name: "Field")),
        Rule("NotContains", "xy", "xaby", "Field must not contain 'ab'.", v => v.EnsureNotContains("ab", name: "Field"), r => r.EnsureNotContains("ab", name: "Field")),
        Rule("StartsWith", "abx", "xab", "Field must start with 'ab'.", v => v.EnsureStartsWith("ab", name: "Field"), r => r.EnsureStartsWith("ab", name: "Field")),
        Rule("NotStartsWith", "xab", "abx", "Field must not start with 'ab'.", v => v.EnsureNotStartsWith("ab", name: "Field"), r => r.EnsureNotStartsWith("ab", name: "Field")),
        Rule("EndsWith", "xab", "abx", "Field must end with 'ab'.", v => v.EnsureEndsWith("ab", name: "Field"), r => r.EnsureEndsWith("ab", name: "Field")),
        Rule("NotEndsWith", "abx", "xab", "Field must not end with 'ab'.", v => v.EnsureNotEndsWith("ab", name: "Field"), r => r.EnsureNotEndsWith("ab", name: "Field")),
        Rule("MatchesRegex", "AB", "ab", "Field must match the pattern '^[A-Z]{2}$'.", v => v.EnsureMatches(UpperPair, name: "Field"), r => r.EnsureMatches(UpperPair, name: "Field")),
        Rule("MatchesPattern", "AB", "ab", "Field must match the pattern '^[A-Z]{2}$'.", v => v.EnsureMatches("^[A-Z]{2}$", name: "Field"), r => r.EnsureMatches("^[A-Z]{2}$", name: "Field")),
        Rule("NotMatchesRegex", "ab", "AB", "Field must not match the pattern '^[A-Z]{2}$'.", v => v.EnsureNotMatches(UpperPair, name: "Field"), r => r.EnsureNotMatches(UpperPair, name: "Field")),
        Rule("NotMatchesPattern", "ab", "AB", "Field must not match the pattern '^[A-Z]{2}$'.", v => v.EnsureNotMatches("^[A-Z]{2}$", name: "Field"), r => r.EnsureNotMatches("^[A-Z]{2}$", name: "Field")),
    ];

    private static IEnumerable<string> Members(Result<string> result) =>
        ((ValidationException)result.GetError()).ValidationResult.MemberNames;

    [TestCaseSource(nameof(Rules))]
    public void PassingValueIsReturnedInBothForms(RuleCase rule)
    {
        Assert.That(rule.OnValue(rule.Passing).GetOk(), NUnit.Framework.Is.EqualTo(rule.Passing));
        Assert.That(rule.OnResult(Result<string>.Ok(rule.Passing)).GetOk(), NUnit.Framework.Is.EqualTo(rule.Passing));
    }

    [TestCaseSource(nameof(Rules))]
    public void FailingValueGivesTheDefaultMessageInBothForms(RuleCase rule)
    {
        Result<string> onValue = rule.OnValue(rule.Failing);
        Result<string> onResult = rule.OnResult(Result<string>.Ok(rule.Failing));

        Assert.That(onValue.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(onResult.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(Members(onValue), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
        Assert.That(Members(onResult), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
    }

    [TestCaseSource(nameof(Rules))]
    public void NullStringIsAnErrorInTheValueForm(RuleCase rule)
    {
        Result<string> result = rule.OnValue(null);

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Field must not be null."));
    }

    [TestCaseSource(nameof(Rules))]
    public void ErrorAndEmptyPassThroughTheResultForm(RuleCase rule)
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<string> empty = default;

        Result<string> error = rule.OnResult(Result<string>.Error(failure));
        Result<string> stillEmpty = rule.OnResult(empty);

        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(stillEmpty.Value, NUnit.Framework.Is.Null);
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<string> onValue = "".EnsureNotEmpty("Please enter a {Name}.", "Field");
        Result<string> onResult = Result<string>.Ok("").EnsureNotEmpty("Please enter a {Name}.", "Field");
        Result<string> onNull = ((string?)null).EnsureNotEmpty("Please enter a {Name}.", "Field");

        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo("Please enter a {Name}."));
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo("Please enter a {Name}."));
        Assert.That(onNull.Message, NUnit.Framework.Is.EqualTo("Please enter a {Name}."));
    }

    [Test]
    public void ComparisonIsPassedToTheRule()
    {
        Result<string> strict = "xAby".EnsureContains("ab", name: "Field");
        Result<string> relaxed = "xAby".EnsureContains("ab", StringComparison.OrdinalIgnoreCase, name: "Field");

        Assert.That(strict.Success, NUnit.Framework.Is.False);
        Assert.That(relaxed.Success, NUnit.Framework.Is.True);
    }

    [Test]
    public void PatternOptionsArePassedToTheRule()
    {
        Result<string> result = "AB".EnsureMatches("^ab$", RegexOptions.IgnoreCase, name: "Field");

        Assert.That(result.Success, NUnit.Framework.Is.True);
    }

    [Test]
    public void ErrorResultPassesThroughAnInvalidPattern()
    {
        var error = new InvalidOperationException("boom");

        Result<string> matches = Result<string>.Error(error).EnsureMatches("(", name: "Field");
        Result<string> notMatches = Result<string>.Error(error).EnsureNotMatches("(", name: "Field");

        Assert.That(matches.GetError(), NUnit.Framework.Is.SameAs(error));
        Assert.That(notMatches.GetError(), NUnit.Framework.Is.SameAs(error));
        Assert.Throws<RegexParseException>(() => Result<string>.Ok("a").EnsureMatches("("));
    }

    [Test]
    public void NamePlaceholderInUserArgumentsIsNotReplaced()
    {
        Result<string> contains = "abc".EnsureContains("{Name}", name: "Field");
        Result<string> matches = "abc".EnsureMatches("{Name}", name: "Field");

        Assert.That(contains.GetError().Message, NUnit.Framework.Is.EqualTo("Field must contain '{Name}'."));
        Assert.That(matches.GetError().Message, NUnit.Framework.Is.EqualTo("Field must match the pattern '{Name}'."));
    }

    [Test]
    public void RegexTimeoutBecomesAnError()
    {
        Result<string> result = (new string('a', 40) + "!").EnsureMatches("^(a+)+$", name: "Field");

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<RegexMatchTimeoutException>());
    }

    [Test]
    public void ValidateCollectsTheFailuresOfSeveralRules()
    {
        var customer = new Customer("", "jane");

        Result<Customer> result = Result<Customer>.Ok(customer).Validate(
            c => c.Name.EnsureNotEmpty(),
            c => c.Email.EnsureMatchesEmail());

        var error = (AggregateException)result.GetError();
        Assert.That(error.InnerExceptions.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[]
        {
            "Name must not be empty.",
            "Email must be a valid email address."
        }));
    }

    [Test]
    public void ValidateKeepsAValidValue()
    {
        var customer = new Customer("Jane", "jane@shop.test");

        Result<Customer> result = Result<Customer>.Ok(customer).Validate(
            c => c.Name.EnsureNotEmpty(),
            c => c.Email.EnsureMatchesEmail());

        Assert.That(result.GetOk(), NUnit.Framework.Is.EqualTo(customer));
    }

    private sealed record Customer(string Name, string Email);
}
