using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class GeneralRuleTests
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
        Rule("EqualTo", 5, 6, "Field must be equal to 5.", v => v.EnsureEqualTo(5, name: "Field"), r => r.EnsureEqualTo(5, name: "Field")),
        Rule("NotEqualTo", 6, 5, "Field must not be equal to 5.", v => v.EnsureNotEqualTo(5, name: "Field"), r => r.EnsureNotEqualTo(5, name: "Field")),
        Rule("In", 2, 4, "Field must be one of the allowed values.", v => v.EnsureIn([1, 2, 3], name: "Field"), r => r.EnsureIn([1, 2, 3], name: "Field")),
        Rule("NotIn", 4, 2, "Field must not be one of the disallowed values.", v => v.EnsureNotIn([1, 2, 3], name: "Field"), r => r.EnsureNotIn([1, 2, 3], name: "Field")),
    ];

    private static IEnumerable<string> Members<T>(Result<T> result) =>
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
    public void ResultReceiverResolvesToTheResultForm()
    {
        var failure = new InvalidOperationException("Out of stock.");

        Result<int> equal = Result<int>.Ok(1).EnsureEqualTo(1);
        Result<int> notIn = Result<int>.Ok(1).EnsureNotIn([2, 3]);
        Result<int> error = Result<int>.Error(failure).EnsureEqualTo(1);

        Assert.That(equal.GetOk(), NUnit.Framework.Is.EqualTo(1));
        Assert.That(notIn.GetOk(), NUnit.Framework.Is.EqualTo(1));
        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
    }

    [Test]
    public void ResultFormUsesValueAsTheDefaultName()
    {
        Result<int> result = Result<int>.Ok(1).EnsureEqualTo(2);

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must be equal to 2."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void CapturedNameFillsTheMessageOfTheValueForm()
    {
        var order = new Order("NEW");

        Result<string> result = order.Status.EnsureIn(["PAID", "SHIPPED"]);

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Status must be one of the allowed values."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Status" }));
    }

    [Test]
    public void ComparerIsUsed()
    {
        Result<string> onValue = "A".EnsureEqualTo("a", StringComparer.OrdinalIgnoreCase);
        Result<string> onResult = Result<string>.Ok("A").EnsureNotEqualTo("a", StringComparer.Ordinal);

        Assert.That(onValue.GetOk(), NUnit.Framework.Is.EqualTo("A"));
        Assert.That(onResult.GetOk(), NUnit.Framework.Is.EqualTo("A"));
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<int> onValue = 1.EnsureEqualTo(2, message: "Wrong {Name}.", name: "Field");
        Result<int> onResult = Result<int>.Ok(1).EnsureEqualTo(2, message: "Wrong {Name}.", name: "Field");

        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo("Wrong {Name}."));
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo("Wrong {Name}."));
    }

    [Test]
    public void NullValueIsAnErrorInTheValueForm()
    {
        string? missing = null;

        Result<string> equal = missing!.EnsureEqualTo("a", name: "Field");
        Result<string> notIn = missing!.EnsureNotIn(["a"], name: "Field");

        Assert.That(equal.Message, NUnit.Framework.Is.EqualTo("Field must not be null."));
        Assert.That(notIn.Message, NUnit.Framework.Is.EqualTo("Field must not be null."));
    }

    [Test]
    public void EnsureNotNull()
    {
        string? missing = null;
        string? present = "a";

        Result<string> failed = missing.EnsureNotNull();
        Result<string> passed = present.EnsureNotNull();
        Result<string> custom = missing.EnsureNotNull("Enter a name.");

        Assert.That(failed.Message, NUnit.Framework.Is.EqualTo("missing must not be null."));
        Assert.That(Members(failed), NUnit.Framework.Is.EqualTo(new[] { "missing" }));
        Assert.That(passed.GetOk(), NUnit.Framework.Is.EqualTo("a"));
        Assert.That(custom.Message, NUnit.Framework.Is.EqualTo("Enter a name."));
    }

    [Test]
    public void EnsureSomeAndEnsureNoneOnOption()
    {
        Option<int> some = Option.Some(5);
        Option<int> none = default;

        Assert.That(some.EnsureSome().Success, NUnit.Framework.Is.True);
        Assert.That(none.EnsureSome().Message, NUnit.Framework.Is.EqualTo("none must have a value."));
        Assert.That(none.EnsureNone().Success, NUnit.Framework.Is.True);
        Assert.That(some.EnsureNone().Message, NUnit.Framework.Is.EqualTo("some must not have a value."));
        Assert.That(Members(none.EnsureSome()), NUnit.Framework.Is.EqualTo(new[] { "none" }));
    }

    [Test]
    public void EnsureSomeAndEnsureNoneOnResultOfOption()
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<Option<int>> empty = default;

        Result<Option<int>> some = Result<Option<int>>.Ok(Option.Some(5)).EnsureSome(name: "Stock");
        Result<Option<int>> notSome = Result<Option<int>>.Ok(default(Option<int>)).EnsureSome(name: "Stock");
        Result<Option<int>> notNone = Result<Option<int>>.Ok(Option.Some(5)).EnsureNone(message: "Gone.");
        Result<Option<int>> error = Result<Option<int>>.Error(failure).EnsureSome();

        Assert.That(some.Success, NUnit.Framework.Is.True);
        Assert.That(notSome.Message, NUnit.Framework.Is.EqualTo("Stock must have a value."));
        Assert.That(notNone.Message, NUnit.Framework.Is.EqualTo("Gone."));
        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(empty.EnsureSome().Value, NUnit.Framework.Is.Null);
        Assert.That(empty.EnsureNone().Value, NUnit.Framework.Is.Null);
    }

    private sealed record Order(string Status);
}
