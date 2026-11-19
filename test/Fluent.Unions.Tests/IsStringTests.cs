using System.Text.RegularExpressions;
using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

public class IsStringTests
{
    [TestCase("a", true)]
    [TestCase(" ", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void NotEmpty(string? value, bool expected) => Assert.That(Is.NotEmpty(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("", true)]
    [TestCase(null, true)]
    [TestCase("a", false)]
    public void Empty(string? value, bool expected) => Assert.That(Is.Empty(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("a", true)]
    [TestCase(" ", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void NotWhiteSpace(string? value, bool expected) => Assert.That(Is.NotWhiteSpace(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(" ", true)]
    [TestCase("", true)]
    [TestCase(null, true)]
    [TestCase("a", false)]
    public void WhiteSpace(string? value, bool expected) => Assert.That(Is.WhiteSpace(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("jane@shop.test", true)]
    [TestCase("jane", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void MatchesEmail(string? value, bool expected) => Assert.That(Is.MatchesEmail(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("https://shop.test/orders", true)]
    [TestCase("http://shop.test", true)]
    [TestCase("ftp://shop.test", false)]
    [TestCase("shop.test", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void MatchesUrl(string? value, bool expected) => Assert.That(Is.MatchesUrl(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("0f8fad5b-d9cb-469f-a165-70867728950e", true)]
    [TestCase("nope", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void MatchesGuid(string? value, bool expected) => Assert.That(Is.MatchesGuid(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("192.168.0.1", true)]
    [TestCase("::1", true)]
    [TestCase("999.1.1.1", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void MatchesIpAddress(string? value, bool expected) => Assert.That(Is.MatchesIpAddress(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("+49 30 1234567", true)]
    [TestCase("abc", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void MatchesPhoneNumber(string? value, bool expected) => Assert.That(Is.MatchesPhoneNumber(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abc", true)]
    [TestCase("ab1", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Alpha(string? value, bool expected) => Assert.That(Is.Alpha(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("ab1", true)]
    [TestCase("ab-", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Alphanumeric(string? value, bool expected) => Assert.That(Is.Alphanumeric(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("123", true)]
    [TestCase("12a", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Numeric(string? value, bool expected) => Assert.That(Is.Numeric(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("aGVsbG8=", true)]
    [TestCase("not base64!", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Base64(string? value, bool expected) => Assert.That(Is.Base64(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("a1", true)]
    [TestCase("ab", false)]
    [TestCase(null, false)]
    public void IncludesDigit(string? value, bool expected) => Assert.That(Is.IncludesDigit(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("1a", true)]
    [TestCase("12", false)]
    [TestCase(null, false)]
    public void IncludesLetter(string? value, bool expected) => Assert.That(Is.IncludesLetter(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("-a", true)]
    [TestCase("--", false)]
    [TestCase(null, false)]
    public void IncludesLetterOrDigit(string? value, bool expected) => Assert.That(Is.IncludesLetterOrDigit(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("aB", true)]
    [TestCase("AB", false)]
    [TestCase(null, false)]
    public void IncludesLower(string? value, bool expected) => Assert.That(Is.IncludesLower(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("aB", true)]
    [TestCase("ab", false)]
    [TestCase(null, false)]
    public void IncludesUpper(string? value, bool expected) => Assert.That(Is.IncludesUpper(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("a!", true)]
    [TestCase("a+", false)]
    [TestCase(null, false)]
    public void IncludesPunctuation(string? value, bool expected) => Assert.That(Is.IncludesPunctuation(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("a-", true)]
    [TestCase("a b1", false)]
    [TestCase(null, false)]
    public void IncludesSpecial(string? value, bool expected) => Assert.That(Is.IncludesSpecial(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("a+", true)]
    [TestCase("a!", false)]
    [TestCase(null, false)]
    public void IncludesSymbol(string? value, bool expected) => Assert.That(Is.IncludesSymbol(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("a b", true)]
    [TestCase("ab", false)]
    [TestCase(null, false)]
    public void IncludesWhiteSpace(string? value, bool expected) => Assert.That(Is.IncludesWhiteSpace(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abc", true)]
    [TestCase("ab", false)]
    [TestCase("abcd", false)]
    [TestCase(null, false)]
    public void HasLength(string? value, bool expected) => Assert.That(Is.HasLength(3)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abcd", true)]
    [TestCase("abc", false)]
    [TestCase(null, false)]
    public void LongerThan(string? value, bool expected) => Assert.That(Is.LongerThan(3)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abc", true)]
    [TestCase("ab", false)]
    [TestCase(null, false)]
    public void LongerThanOrEqual(string? value, bool expected) => Assert.That(Is.LongerThanOrEqual(3)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("ab", true)]
    [TestCase("abc", false)]
    [TestCase(null, false)]
    public void ShorterThan(string? value, bool expected) => Assert.That(Is.ShorterThan(3)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abc", true)]
    [TestCase("abcd", false)]
    [TestCase(null, false)]
    public void ShorterThanOrEqual(string? value, bool expected) => Assert.That(Is.ShorterThanOrEqual(3)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("xaby", true)]
    [TestCase("xAby", false)]
    [TestCase(null, false)]
    public void Contains(string? value, bool expected) => Assert.That(Is.Contains("ab")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("xy", true)]
    [TestCase("xaby", false)]
    [TestCase(null, false)]
    public void NotContains(string? value, bool expected) => Assert.That(Is.NotContains("ab")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abx", true)]
    [TestCase("xab", false)]
    [TestCase(null, false)]
    public void StartsWith(string? value, bool expected) => Assert.That(Is.StartsWith("ab")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("xab", true)]
    [TestCase("abx", false)]
    [TestCase(null, false)]
    public void NotStartsWith(string? value, bool expected) => Assert.That(Is.NotStartsWith("ab")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("xab", true)]
    [TestCase("abx", false)]
    [TestCase(null, false)]
    public void EndsWith(string? value, bool expected) => Assert.That(Is.EndsWith("ab")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("abx", true)]
    [TestCase("xab", false)]
    [TestCase(null, false)]
    public void NotEndsWith(string? value, bool expected) => Assert.That(Is.NotEndsWith("ab")(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void ComparisonIgnoresCaseWhenAsked()
    {
        Assert.That(Is.Contains("AB", StringComparison.OrdinalIgnoreCase)("xaby"), NUnit.Framework.Is.True);
        Assert.That(Is.StartsWith("AB", StringComparison.OrdinalIgnoreCase)("abx"), NUnit.Framework.Is.True);
        Assert.That(Is.EndsWith("AB", StringComparison.OrdinalIgnoreCase)("xab"), NUnit.Framework.Is.True);
    }

    [TestCase("AB12", true)]
    [TestCase("ab12", false)]
    [TestCase(null, false)]
    public void MatchesPattern(string? value, bool expected) => Assert.That(Is.Matches("^[A-Z]{2}\\d+$")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("AB12", true)]
    [TestCase("ab12", false)]
    public void MatchesRegex(string value, bool expected) => Assert.That(Is.Matches(new Regex("^[A-Z]{2}\\d+$"))(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void MatchesPatternHonoursOptions() =>
        Assert.That(Is.Matches("^ab$", RegexOptions.IgnoreCase)("AB"), NUnit.Framework.Is.True);

    [TestCase("ab12", true)]
    [TestCase("AB12", false)]
    [TestCase(null, false)]
    public void NotMatchesPattern(string? value, bool expected) => Assert.That(Is.NotMatches("^[A-Z]{2}\\d+$")(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase("ab12", true)]
    [TestCase("AB12", false)]
    public void NotMatchesRegex(string value, bool expected) => Assert.That(Is.NotMatches(new Regex("^[A-Z]{2}\\d+$"))(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void FactoriesRejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => Is.Contains(null!));
        Assert.Throws<ArgumentNullException>(() => Is.NotContains(null!));
        Assert.Throws<ArgumentNullException>(() => Is.StartsWith(null!));
        Assert.Throws<ArgumentNullException>(() => Is.NotStartsWith(null!));
        Assert.Throws<ArgumentNullException>(() => Is.EndsWith(null!));
        Assert.Throws<ArgumentNullException>(() => Is.NotEndsWith(null!));
        Assert.Throws<ArgumentNullException>(() => Is.Matches((Regex)null!));
        Assert.Throws<ArgumentNullException>(() => Is.NotMatches((Regex)null!));
    }

    [Test]
    public void InvalidPatternThrowsWhenTheRuleIsCreated() =>
        Assert.Catch<ArgumentException>(() => Is.Matches("("));

    [Test]
    public void OptionWhereAcceptsAMethodGroup()
    {
        Option<string> empty = Option.Some("").Where(Is.NotEmpty);
        Option<string> filled = Option.Some("lamp").Where(Is.NotEmpty);

        Assert.That(empty.HasValue, NUnit.Framework.Is.False);
        Assert.That(filled.HasValue, NUnit.Framework.Is.True);
    }
}
