using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class NumberRuleTests
{
    public sealed record RuleCase(
        string Label,
        int Passing,
        int Failing,
        string Message,
        Func<int, Result<int>> OnValue,
        Func<Result<int>, Result<int>> OnResult);

    private static TestCaseData Rule(
        string label,
        int passing,
        int failing,
        string message,
        Func<int, Result<int>> onValue,
        Func<Result<int>, Result<int>> onResult) =>
        new TestCaseData(new RuleCase(label, passing, failing, message, onValue, onResult)).SetName(label);

    private static IEnumerable<TestCaseData> Rules() =>
    [
        Rule("GreaterThan", 6, 5, "Field must be greater than 5.", v => v.EnsureGreaterThan(5, name: "Field"), r => r.EnsureGreaterThan(5, name: "Field")),
        Rule("GreaterThanOrEqual", 5, 4, "Field must be greater than or equal to 5.", v => v.EnsureGreaterThanOrEqual(5, name: "Field"), r => r.EnsureGreaterThanOrEqual(5, name: "Field")),
        Rule("LessThan", 4, 5, "Field must be less than 5.", v => v.EnsureLessThan(5, name: "Field"), r => r.EnsureLessThan(5, name: "Field")),
        Rule("LessThanOrEqual", 5, 6, "Field must be less than or equal to 5.", v => v.EnsureLessThanOrEqual(5, name: "Field"), r => r.EnsureLessThanOrEqual(5, name: "Field")),
        Rule("InRange", 120, 121, "Field must be between 18 and 120.", v => v.EnsureInRange(18, 120, name: "Field"), r => r.EnsureInRange(18, 120, name: "Field")),
        Rule("InRangeBelow", 18, 17, "Field must be between 18 and 120.", v => v.EnsureInRange(18, 120, name: "Field"), r => r.EnsureInRange(18, 120, name: "Field")),
        Rule("Positive", 1, 0, "Field must be positive.", v => v.EnsurePositive(name: "Field"), r => r.EnsurePositive(name: "Field")),
        Rule("Negative", -1, 0, "Field must be negative.", v => v.EnsureNegative(name: "Field"), r => r.EnsureNegative(name: "Field")),
        Rule("NonPositive", 0, 1, "Field must not be positive.", v => v.EnsureNonPositive(name: "Field"), r => r.EnsureNonPositive(name: "Field")),
        Rule("NonNegative", 0, -1, "Field must not be negative.", v => v.EnsureNonNegative(name: "Field"), r => r.EnsureNonNegative(name: "Field")),
        Rule("Zero", 0, 1, "Field must be zero.", v => v.EnsureZero(name: "Field"), r => r.EnsureZero(name: "Field")),
        Rule("NonZero", 1, 0, "Field must not be zero.", v => v.EnsureNonZero(name: "Field"), r => r.EnsureNonZero(name: "Field")),
        Rule("Even", 2, 3, "Field must be even.", v => v.EnsureEven(name: "Field"), r => r.EnsureEven(name: "Field")),
        Rule("Odd", 3, 2, "Field must be odd.", v => v.EnsureOdd(name: "Field"), r => r.EnsureOdd(name: "Field")),
        Rule("DivisibleBy", 12, 13, "Field must be divisible by 6.", v => v.EnsureDivisibleBy(6, name: "Field"), r => r.EnsureDivisibleBy(6, name: "Field")),
    ];

    private static IEnumerable<string> Members(Result<int> result) =>
        ((ValidationException)result.GetError()).ValidationResult.MemberNames;

    [TestCaseSource(nameof(Rules))]
    public void PassingValueIsReturnedInBothForms(RuleCase rule)
    {
        Assert.That(rule.OnValue(rule.Passing).GetOk(), NUnit.Framework.Is.EqualTo(rule.Passing));
        Assert.That(rule.OnResult(Result<int>.Ok(rule.Passing)).GetOk(), NUnit.Framework.Is.EqualTo(rule.Passing));
    }

    [TestCaseSource(nameof(Rules))]
    public void FailingValueGivesTheDefaultMessageInBothForms(RuleCase rule)
    {
        Result<int> onValue = rule.OnValue(rule.Failing);
        Result<int> onResult = rule.OnResult(Result<int>.Ok(rule.Failing));

        Assert.That(onValue.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(onResult.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(Members(onValue), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
        Assert.That(Members(onResult), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
    }

    [TestCaseSource(nameof(Rules))]
    public void ErrorAndEmptyPassThroughTheResultForm(RuleCase rule)
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<int> empty = default;

        Result<int> error = rule.OnResult(Result<int>.Error(failure));
        Result<int> stillEmpty = rule.OnResult(empty);

        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(stillEmpty.Value, NUnit.Framework.Is.Null);
    }

    [Test]
    public void CapturedNameFillsTheMessageOfTheValueForm()
    {
        var customer = new Customer(17);

        Result<int> result = customer.Age.EnsureInRange(18, 120);

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Age must be between 18 and 120."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Age" }));
    }

    [Test]
    public void ResultFormUsesValueAsTheDefaultName()
    {
        Result<int> result = Result<int>.Ok(0).EnsurePositive();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must be positive."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<int> onValue = 0.EnsurePositive("Enter a {Name}.", "Field");
        Result<int> onResult = Result<int>.Ok(0).EnsurePositive("Enter a {Name}.", "Field");

        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo("Enter a {Name}."));
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo("Enter a {Name}."));
    }

    [Test]
    public void NaNFailsTheSignRulesWithDoubles()
    {
        Result<double> positive = double.NaN.EnsurePositive(name: "Price");
        Result<double> nonNegative = Result<double>.Ok(double.NaN).EnsureNonNegative(name: "Price");

        Assert.That(positive.Message, NUnit.Framework.Is.EqualTo("Price must be positive."));
        Assert.That(nonNegative.Message, NUnit.Framework.Is.EqualTo("Price must not be negative."));
    }

    [Test]
    public void BoundsAreFormattedWithTheInvariantCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            Result<double> result = 1.0.EnsureGreaterThan(2.5, name: "Price");

            Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Price must be greater than 2.5."));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void ErrorResultPassesThroughAnInvalidRangeOrDivisor()
    {
        var error = new InvalidOperationException("boom");

        Result<int> range = Result<int>.Error(error).EnsureInRange(10, 1);
        Result<int> divisor = Result<int>.Error(error).EnsureDivisibleBy(0);
        Result<int> empty = default(Result<int>).EnsureInRange(10, 1);

        Assert.That(range.GetError(), NUnit.Framework.Is.SameAs(error));
        Assert.That(divisor.GetError(), NUnit.Framework.Is.SameAs(error));
        Assert.That(empty.Value, NUnit.Framework.Is.Null);
    }

    [Test]
    public void InvalidArgumentsThrowWhenTheRuleIsCreated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 5.EnsureInRange(10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Result<int>.Ok(5).EnsureInRange(10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => 5.EnsureDivisibleBy(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Result<int>.Ok(5).EnsureDivisibleBy(0));
    }

    [Test]
    public void ValidateCollectsTheFailuresOfSeveralRules()
    {
        var customer = new Customer(17);

        Result<Customer> result = Result<Customer>.Ok(customer).Validate(
            c => c.Age.EnsureInRange(18, 120),
            c => c.Age.EnsureEven());

        var error = (AggregateException)result.GetError();
        Assert.That(error.InnerExceptions.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[]
        {
            "Age must be between 18 and 120.",
            "Age must be even."
        }));
    }

    private sealed record Customer(int Age);
}
