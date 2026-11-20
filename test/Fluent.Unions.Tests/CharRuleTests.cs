using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class CharRuleTests
{
    public sealed record RuleCase(
        string Label,
        char Passing,
        char Failing,
        string Message,
        Func<char, Result<char>> OnValue,
        Func<Result<char>, Result<char>> OnResult);

    private static TestCaseData Rule(
        string label,
        char passing,
        char failing,
        string message,
        Func<char, Result<char>> onValue,
        Func<Result<char>, Result<char>> onResult) =>
        new TestCaseData(new RuleCase(label, passing, failing, message, onValue, onResult)).SetName(label);

    private static IEnumerable<TestCaseData> Rules() =>
    [
        Rule("Digit", '5', 'a', "Field must be a digit.", v => v.EnsureDigit(name: "Field"), r => r.EnsureDigit(name: "Field")),
        Rule("Letter", 'a', '5', "Field must be a letter.", v => v.EnsureLetter(name: "Field"), r => r.EnsureLetter(name: "Field")),
        Rule("LetterOrDigit", '5', '-', "Field must be a letter or a digit.", v => v.EnsureLetterOrDigit(name: "Field"), r => r.EnsureLetterOrDigit(name: "Field")),
        Rule("Lower", 'a', 'A', "Field must be lowercase.", v => v.EnsureLower(name: "Field"), r => r.EnsureLower(name: "Field")),
        Rule("Upper", 'A', 'a', "Field must be uppercase.", v => v.EnsureUpper(name: "Field"), r => r.EnsureUpper(name: "Field")),
        Rule("Punctuation", '!', '+', "Field must be punctuation.", v => v.EnsurePunctuation(name: "Field"), r => r.EnsurePunctuation(name: "Field")),
        Rule("Special", '$', 'a', "Field must be a special character.", v => v.EnsureSpecial(name: "Field"), r => r.EnsureSpecial(name: "Field")),
        Rule("Symbol", '+', '!', "Field must be a symbol.", v => v.EnsureSymbol(name: "Field"), r => r.EnsureSymbol(name: "Field")),
        Rule("WhiteSpace", ' ', 'a', "Field must be white space.", v => v.EnsureWhiteSpace(name: "Field"), r => r.EnsureWhiteSpace(name: "Field")),
    ];

    private static IEnumerable<string> Members(Result<char> result) =>
        ((ValidationError)result.GetError()).MemberName is { } member ? new[] { member } : Array.Empty<string>();

    [TestCaseSource(nameof(Rules))]
    public void PassingValueIsReturnedInBothForms(RuleCase rule)
    {
        Assert.That(rule.OnValue(rule.Passing).GetValue(), NUnit.Framework.Is.EqualTo(rule.Passing));
        Assert.That(rule.OnResult(Result<char>.Ok(rule.Passing)).GetValue(), NUnit.Framework.Is.EqualTo(rule.Passing));
    }

    [TestCaseSource(nameof(Rules))]
    public void FailingValueGivesTheDefaultMessageInBothForms(RuleCase rule)
    {
        Result<char> onValue = rule.OnValue(rule.Failing);
        Result<char> onResult = rule.OnResult(Result<char>.Ok(rule.Failing));

        Assert.That(onValue.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(onValue.GetError().Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(onResult.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(onResult.GetError().Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(Members(onValue), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
        Assert.That(Members(onResult), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
    }

    [TestCaseSource(nameof(Rules))]
    public void ErrorAndEmptyPassThroughTheResultForm(RuleCase rule)
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<char> empty = default;

        Result<char> error = rule.OnResult(Result<char>.Error(failure));
        Result<char> stillEmpty = rule.OnResult(empty);

        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(stillEmpty.Value, NUnit.Framework.Is.Null);
    }

    [Test]
    public void CapturedNameFillsTheMessageOfTheValueForm()
    {
        var code = new Coupon('x');

        Result<char> result = code.Prefix.EnsureUpper();

        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("Prefix must be uppercase."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Prefix" }));
    }

    [Test]
    public void ResultFormUsesValueAsTheDefaultName()
    {
        Result<char> result = Result<char>.Ok('x').EnsureDigit();

        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("Value must be a digit."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<char> onValue = 'x'.EnsureDigit("Enter a {Name}.", "Field");
        Result<char> onResult = Result<char>.Ok('x').EnsureDigit("Enter a {Name}.", "Field");

        Assert.That(onValue.GetError().Message, NUnit.Framework.Is.EqualTo("Enter a {Name}."));
        Assert.That(onResult.GetError().Message, NUnit.Framework.Is.EqualTo("Enter a {Name}."));
    }

    private sealed record Coupon(char Prefix);
}
