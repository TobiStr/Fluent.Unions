using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class ValueRuleTests
{
    private static readonly Guid Id = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    [Flags]
    public enum Channels
    {
        Web = 1,
        Store = 2
    }

    public enum Status
    {
        Pending = 1,
        Shipped = 2
    }

    public sealed record RuleCase<T>(
        string Label,
        T Passing,
        T Failing,
        string Message,
        Func<T, Result<T>> OnValue,
        Func<Result<T>, Result<T>> OnResult);

    private static TestCaseData Case<T>(
        string label,
        T passing,
        T failing,
        string message,
        Func<T, Result<T>> onValue,
        Func<Result<T>, Result<T>> onResult) =>
        new TestCaseData(new RuleCase<T>(label, passing, failing, message, onValue, onResult)).SetName(label);

    private static IEnumerable<TestCaseData> GuidRules() =>
    [
        Case("GuidNotEmpty", Id, Guid.Empty, "Field must not be empty.", v => v.EnsureNotEmpty(name: "Field"), r => r.EnsureNotEmpty(name: "Field")),
        Case("GuidEmpty", Guid.Empty, Id, "Field must be empty.", v => v.EnsureEmpty(name: "Field"), r => r.EnsureEmpty(name: "Field")),
    ];

    private static IEnumerable<TestCaseData> BoolRules() =>
    [
        Case("True", true, false, "Field must be true.", v => v.EnsureTrue(name: "Field"), r => r.EnsureTrue(name: "Field")),
        Case("False", false, true, "Field must be false.", v => v.EnsureFalse(name: "Field"), r => r.EnsureFalse(name: "Field")),
    ];

    private static IEnumerable<TestCaseData> StatusRules() =>
    [
        Case("DefinedUndefined", Status.Shipped, (Status)99, "Field must be a defined value.", v => v.EnsureDefined(name: "Field"), r => r.EnsureDefined(name: "Field")),
    ];

    private static IEnumerable<TestCaseData> ChannelRules() =>
    [
        Case("DefinedCombinedFlags", Channels.Store, Channels.Web | Channels.Store, "Field must be a defined value.", v => v.EnsureDefined(name: "Field"), r => r.EnsureDefined(name: "Field")),
    ];

    private static IEnumerable<string> Members<T>(Result<T> result) =>
        ((ValidationException)result.GetError()).ValidationResult.MemberNames;

    private static void AssertPassing<T>(RuleCase<T> rule)
    {
        Assert.That(rule.OnValue(rule.Passing).GetOk(), NUnit.Framework.Is.EqualTo(rule.Passing));
        Assert.That(rule.OnResult(Result<T>.Ok(rule.Passing)).GetOk(), NUnit.Framework.Is.EqualTo(rule.Passing));
    }

    private static void AssertFailing<T>(RuleCase<T> rule)
    {
        Result<T> onValue = rule.OnValue(rule.Failing);
        Result<T> onResult = rule.OnResult(Result<T>.Ok(rule.Failing));

        Assert.That(onValue.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(onResult.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(Members(onValue), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
        Assert.That(Members(onResult), NUnit.Framework.Is.EqualTo(new[] { "Field" }));
    }

    private static void AssertPassThrough<T>(RuleCase<T> rule)
    {
        var failure = new InvalidOperationException("Out of stock.");
        Result<T> empty = default;

        Result<T> error = rule.OnResult(Result<T>.Error(failure));
        Result<T> stillEmpty = rule.OnResult(empty);

        Assert.That(error.GetError(), NUnit.Framework.Is.SameAs(failure));
        Assert.That(stillEmpty.Value, NUnit.Framework.Is.Null);
    }

    [TestCaseSource(nameof(GuidRules))]
    public void GuidPassingValueIsReturnedInBothForms(RuleCase<Guid> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(GuidRules))]
    public void GuidFailingValueGivesTheDefaultMessageInBothForms(RuleCase<Guid> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(GuidRules))]
    public void GuidErrorAndEmptyPassThroughTheResultForm(RuleCase<Guid> rule) => AssertPassThrough(rule);

    [TestCaseSource(nameof(BoolRules))]
    public void BoolPassingValueIsReturnedInBothForms(RuleCase<bool> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(BoolRules))]
    public void BoolFailingValueGivesTheDefaultMessageInBothForms(RuleCase<bool> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(BoolRules))]
    public void BoolErrorAndEmptyPassThroughTheResultForm(RuleCase<bool> rule) => AssertPassThrough(rule);

    [TestCaseSource(nameof(StatusRules))]
    public void StatusPassingValueIsReturnedInBothForms(RuleCase<Status> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(StatusRules))]
    public void StatusFailingValueGivesTheDefaultMessageInBothForms(RuleCase<Status> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(StatusRules))]
    public void StatusErrorAndEmptyPassThroughTheResultForm(RuleCase<Status> rule) => AssertPassThrough(rule);

    [TestCaseSource(nameof(ChannelRules))]
    public void FlagsPassingValueIsReturnedInBothForms(RuleCase<Channels> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(ChannelRules))]
    public void FlagsCombinedValueIsRejectedInBothForms(RuleCase<Channels> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(ChannelRules))]
    public void FlagsErrorAndEmptyPassThroughTheResultForm(RuleCase<Channels> rule) => AssertPassThrough(rule);

    [Test]
    public void DefinedUsesTheEnumTypeOfTheReceiver()
    {
        Result<Status> pending = Status.Pending.EnsureDefined();
        Result<Status> undefined = ((Status)99).EnsureDefined();
        Result<Channels> combined = (Channels.Web | Channels.Store).EnsureDefined();
        Result<Channels> web = Result<Channels>.Ok(Channels.Web).EnsureDefined();

        Assert.That(pending.GetOk(), NUnit.Framework.Is.EqualTo(Status.Pending));
        Assert.That(undefined.Message, NUnit.Framework.Is.EqualTo("Value must be a defined value."));
        Assert.That(combined.Success, NUnit.Framework.Is.False);
        Assert.That(web.GetOk(), NUnit.Framework.Is.EqualTo(Channels.Web));
    }

    [Test]
    public void CapturedNameFillsTheMessageOfTheValueForm()
    {
        var order = new Order(Guid.Empty, false, (Status)99);

        Result<Guid> id = order.Id.EnsureNotEmpty();
        Result<bool> paid = order.Paid.EnsureTrue();
        Result<Status> status = order.Status.EnsureDefined();

        Assert.That(id.Message, NUnit.Framework.Is.EqualTo("Id must not be empty."));
        Assert.That(Members(id), NUnit.Framework.Is.EqualTo(new[] { "Id" }));
        Assert.That(paid.Message, NUnit.Framework.Is.EqualTo("Paid must be true."));
        Assert.That(status.Message, NUnit.Framework.Is.EqualTo("Status must be a defined value."));
    }

    [Test]
    public void ResultFormUsesValueAsTheDefaultName()
    {
        Result<bool> result = Result<bool>.Ok(false).EnsureTrue();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must be true."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<Guid> onValue = Guid.Empty.EnsureNotEmpty("Pick a {Name}.", "Field");
        Result<Guid> onResult = Result<Guid>.Ok(Guid.Empty).EnsureNotEmpty("Pick a {Name}.", "Field");

        Assert.That(onValue.Message, NUnit.Framework.Is.EqualTo("Pick a {Name}."));
        Assert.That(onResult.Message, NUnit.Framework.Is.EqualTo("Pick a {Name}."));
    }

    [Test]
    public void GuidRulesDoNotChangeTheCollectionAndStringRules()
    {
        Result<string> text = "".EnsureNotEmpty(name: "Field");
        Result<List<int>> list = new List<int>().EnsureNotEmpty(name: "Field");

        Assert.That(text.Message, NUnit.Framework.Is.EqualTo("Field must not be empty."));
        Assert.That(list.Success, NUnit.Framework.Is.False);
    }

    [Test]
    public void ValidateCollectsTheFailuresOfSeveralRules()
    {
        var order = new Order(Guid.Empty, false, (Status)99);

        Result<Order> result = Result<Order>.Ok(order).Validate(
            o => o.Id.EnsureNotEmpty(),
            o => o.Paid.EnsureTrue(),
            o => o.Status.EnsureDefined());

        var error = (AggregateException)result.GetError();
        Assert.That(error.InnerExceptions.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[]
        {
            "Id must not be empty.",
            "Paid must be true.",
            "Status must be a defined value."
        }));
    }

    private sealed record Order(Guid Id, bool Paid, Status Status);
}
