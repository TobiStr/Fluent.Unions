using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class CollectionRuleTests
{
    public sealed record RuleCase(
        string Label,
        List<int> Passing,
        List<int> Failing,
        string Message,
        Func<List<int>, Result<List<int>>> OnValue,
        Func<Result<List<int>>, Result<List<int>>> OnResult);

    private static TestCaseData Rule(
        string label,
        int[] passing,
        int[] failing,
        string message,
        Func<List<int>, Result<List<int>>> onValue,
        Func<Result<List<int>>, Result<List<int>>> onResult) =>
        new TestCaseData(new RuleCase(label, [.. passing], [.. failing], message, onValue, onResult)).SetName(label);

    private static IEnumerable<TestCaseData> Rules() =>
    [
        Rule("NotEmpty", [1], [], "Field must not be empty.", v => v.EnsureNotEmpty(name: "Field"), r => r.EnsureNotEmpty(name: "Field")),
        Rule("Empty", [], [1], "Field must be empty.", v => v.EnsureEmpty(name: "Field"), r => r.EnsureEmpty(name: "Field")),
        Rule("Unique", [1, 2], [1, 1], "Field must contain unique items.", v => v.EnsureUnique(name: "Field"), r => r.EnsureUnique(name: "Field")),
        Rule("UniqueWithComparer", [1, 2], [1, 1], "Field must contain unique items.", v => v.EnsureUnique(EqualityComparer<int>.Default, name: "Field"), r => r.EnsureUnique(EqualityComparer<int>.Default, name: "Field")),
        Rule("HasCount", [1, 2], [1], "Field must contain exactly 2 items.", v => v.EnsureHasCount(2, name: "Field"), r => r.EnsureHasCount(2, name: "Field")),
        Rule("HasMinCount", [1, 2], [1], "Field must contain at least 2 items.", v => v.EnsureHasMinCount(2, name: "Field"), r => r.EnsureHasMinCount(2, name: "Field")),
        Rule("HasMaxCount", [1, 2], [1, 2, 3], "Field must contain at most 2 items.", v => v.EnsureHasMaxCount(2, name: "Field"), r => r.EnsureHasMaxCount(2, name: "Field")),
        Rule("ContainsItem", [1, 2], [1], "Field must contain 2.", v => v.EnsureContainsItem(2, name: "Field"), r => r.EnsureContainsItem(2, name: "Field")),
        Rule("NotContainsItem", [1], [1, 2], "Field must not contain 2.", v => v.EnsureNotContainsItem(2, name: "Field"), r => r.EnsureNotContainsItem(2, name: "Field")),
        Rule("AllMatch", [2, 4], [2, 3], "Field must have only items that match the condition.", v => v.EnsureAllMatch((int x) => x % 2 == 0, name: "Field"), r => r.EnsureAllMatch((int x) => x % 2 == 0, name: "Field")),
        Rule("AnyMatch", [1, 2], [1, 3], "Field must have at least one item that matches the condition.", v => v.EnsureAnyMatch((int x) => x % 2 == 0, name: "Field"), r => r.EnsureAnyMatch((int x) => x % 2 == 0, name: "Field")),
        Rule("NoneMatch", [1, 3], [1, 2], "Field must have no item that matches the condition.", v => v.EnsureNoneMatch((int x) => x % 2 == 0, name: "Field"), r => r.EnsureNoneMatch((int x) => x % 2 == 0, name: "Field")),
    ];

    private static IEnumerable<string> Members<T>(Result<T> result) =>
        ((ValidationError)result.GetError()).MemberName is { } member ? new[] { member } : Array.Empty<string>();

    [TestCaseSource(nameof(Rules))]
    public void PassingCollectionIsReturnedInBothForms(RuleCase rule)
    {
        Assert.That(rule.OnValue(rule.Passing).GetValue(), NUnit.Framework.Is.SameAs(rule.Passing));
        Assert.That(rule.OnResult(Result<List<int>>.Ok(rule.Passing)).GetValue(), NUnit.Framework.Is.SameAs(rule.Passing));
    }

    [TestCaseSource(nameof(Rules))]
    public void FailingCollectionGivesTheDefaultMessageInBothForms(RuleCase rule)
    {
        Result<List<int>> onValue = rule.OnValue(rule.Failing);
        Result<List<int>> onResult = rule.OnResult(Result<List<int>>.Ok(rule.Failing));

        Assert.That(onValue.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(onValue.GetError().Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(onResult.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(onResult.GetError().Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(Members(onValue), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
        Assert.That(Members(onResult), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
    }

    [TestCaseSource(nameof(Rules))]
    public void NullCollectionIsAnErrorInTheValueForm(RuleCase rule)
    {
        Result<List<int>> result = rule.OnValue(null!);

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("Field must not be null."));
    }

    [TestCaseSource(nameof(Rules))]
    public void ErrorAndEmptyPassThroughTheResultForm(RuleCase rule)
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<List<int>> empty = default;

        Result<List<int>> error = rule.OnResult(Result<List<int>>.Error(failure));
        Result<List<int>> stillEmpty = rule.OnResult(empty);

        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(stillEmpty.Value, NUnit.Framework.Is.Null);
    }

    [Test]
    public void ResultOfListEnsureNotEmptyOnEmptyErrorAndNonEmptyCollections()
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<List<int>> emptyList = Result<List<int>>.Ok([]);
        Result<List<int>> items = Result<List<int>>.Ok([1]);
        Result<List<int>> emptyResult = default;

        Assert.That(emptyList.EnsureNotEmpty().GetError().Message, NUnit.Framework.Is.EqualTo("Value must not be empty."));
        Assert.That(items.EnsureNotEmpty().IsSuccess, NUnit.Framework.Is.True);
        Assert.That(Result<List<int>>.Error(failure).EnsureNotEmpty().GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(emptyResult.EnsureNotEmpty().Value, NUnit.Framework.Is.Null);
    }

    [Test]
    public void ConcreteCollectionTypeIsKept()
    {
        int[] array = [1, 2];
        var set = new HashSet<string> { "a" };

        Result<int[]> fromArray = array.EnsureHasMinCount(1);
        Result<HashSet<string>> fromSet = Result<HashSet<string>>.Ok(set).EnsureContainsItem("a");

        Assert.That(fromArray.GetValue(), NUnit.Framework.Is.SameAs(array));
        Assert.That(fromSet.GetValue(), NUnit.Framework.Is.SameAs(set));
    }

    [Test]
    public void StringOverloadsWinForStrings()
    {
        Result<string> onValue = "".EnsureNotEmpty(name: "Field");
        Result<string> onResult = Result<string>.Ok("").EnsureNotEmpty(name: "Field");

        Assert.That(onValue.GetError().Message, NUnit.Framework.Is.EqualTo("Field must not be empty."));
        Assert.That(onResult.GetError().Message, NUnit.Framework.Is.EqualTo("Field must not be empty."));
    }

    [Test]
    public void CapturedNameFillsTheMessage()
    {
        var order = new Order([]);

        Result<List<int>> result = order.Lines.EnsureNotEmpty();

        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("Lines must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Lines" }));
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<List<int>> onValue = new List<int>().EnsureNotEmpty("Add a {Name}.", "Field");
        Result<List<int>> onResult = Result<List<int>>.Ok([]).EnsureNotEmpty("Add a {Name}.", "Field");

        Assert.That(onValue.GetError().Message, NUnit.Framework.Is.EqualTo("Add a {Name}."));
        Assert.That(onResult.GetError().Message, NUnit.Framework.Is.EqualTo("Add a {Name}."));
    }

    [Test]
    public void ComparerIsUsedByEnsureUnique()
    {
        string[] names = ["a", "A"];

        Result<string[]> ordinal = names.EnsureUnique(StringComparer.Ordinal);
        Result<string[]> ignoreCase = names.EnsureUnique(StringComparer.OrdinalIgnoreCase, name: "Names");

        Assert.That(ordinal.IsSuccess, NUnit.Framework.Is.True);
        Assert.That(ignoreCase.GetError().Message, NUnit.Framework.Is.EqualTo("Names must contain unique items."));
    }

    [Test]
    public void LazySequenceIsEnumeratedOncePerRule()
    {
        var sequence = new CountingSequence(1, 2, 3);

        _ = sequence.EnsureNotEmpty();
        Assert.That(sequence.Enumerations, NUnit.Framework.Is.EqualTo(1));

        _ = Result<CountingSequence>.Ok(sequence).EnsureHasCount(3);
        Assert.That(sequence.Enumerations, NUnit.Framework.Is.EqualTo(2));

        _ = sequence.EnsureAllMatch((int x) => x > 0);
        Assert.That(sequence.Enumerations, NUnit.Framework.Is.EqualTo(3));

        _ = sequence.EnsureUnique();
        Assert.That(sequence.Enumerations, NUnit.Framework.Is.EqualTo(4));
    }

    [Test]
    public void ValidateCollectsTheFailuresOfSeveralRules()
    {
        var order = new Order([]);

        Result<Order> result = Result<Order>.Ok(order).Validate(
            o => o.Lines.EnsureNotEmpty(),
            o => o.Lines.EnsureHasMinCount(2));

        var error = (ValidationErrors)result.GetError();
        Assert.That(error.Errors.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[]
        {
            "Lines must not be empty.",
            "Lines must contain at least 2 items."
        }));
    }

    private sealed record Order(List<int> Lines);
}
