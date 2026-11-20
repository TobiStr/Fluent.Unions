using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class TimeRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);
    private static readonly FixedTimeProvider Clock = new(Now);

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

    private static IEnumerable<TestCaseData> DurationRules() =>
    [
        Case("Positive", Second, TimeSpan.Zero, "Field must be positive.", v => v.EnsurePositive(name: "Field"), r => r.EnsurePositive(name: "Field")),
        Case("Negative", -Second, TimeSpan.Zero, "Field must be negative.", v => v.EnsureNegative(name: "Field"), r => r.EnsureNegative(name: "Field")),
        Case("NonPositive", TimeSpan.Zero, Second, "Field must not be positive.", v => v.EnsureNonPositive(name: "Field"), r => r.EnsureNonPositive(name: "Field")),
        Case("NonNegative", TimeSpan.Zero, -Second, "Field must not be negative.", v => v.EnsureNonNegative(name: "Field"), r => r.EnsureNonNegative(name: "Field")),
        Case("Zero", TimeSpan.Zero, Second, "Field must be zero.", v => v.EnsureZero(name: "Field"), r => r.EnsureZero(name: "Field")),
        Case("NonZero", Second, TimeSpan.Zero, "Field must not be zero.", v => v.EnsureNonZero(name: "Field"), r => r.EnsureNonZero(name: "Field")),
    ];

    private static IEnumerable<TestCaseData> InstantRules() =>
    [
        Case("InPastProvider", Now - Second, Now, "Field must be in the past.", v => v.EnsureInPast(Clock, name: "Field"), r => r.EnsureInPast(Clock, name: "Field")),
        Case("InPastOrPresentProvider", Now, Now + Second, "Field must be in the past or present.", v => v.EnsureInPastOrPresent(Clock, name: "Field"), r => r.EnsureInPastOrPresent(Clock, name: "Field")),
        Case("InFutureProvider", Now + Second, Now, "Field must be in the future.", v => v.EnsureInFuture(Clock, name: "Field"), r => r.EnsureInFuture(Clock, name: "Field")),
        Case("InFutureOrPresentProvider", Now, Now - Second, "Field must be in the future or present.", v => v.EnsureInFutureOrPresent(Clock, name: "Field"), r => r.EnsureInFutureOrPresent(Clock, name: "Field")),
        Case("InPastNow", Now - Second, Now, "Field must be in the past.", v => v.EnsureInPast(Now, name: "Field"), r => r.EnsureInPast(Now, name: "Field")),
        Case("InPastOrPresentNow", Now, Now + Second, "Field must be in the past or present.", v => v.EnsureInPastOrPresent(Now, name: "Field"), r => r.EnsureInPastOrPresent(Now, name: "Field")),
        Case("InFutureNow", Now + Second, Now, "Field must be in the future.", v => v.EnsureInFuture(Now, name: "Field"), r => r.EnsureInFuture(Now, name: "Field")),
        Case("InFutureOrPresentNow", Now, Now - Second, "Field must be in the future or present.", v => v.EnsureInFutureOrPresent(Now, name: "Field"), r => r.EnsureInFutureOrPresent(Now, name: "Field")),
    ];

    private static IEnumerable<TestCaseData> DateRules()
    {
        var now = Now.UtcDateTime;
        return
        [
            Case("InPastDate", now - Second, now, "Field must be in the past.", v => v.EnsureInPast(now, name: "Field"), r => r.EnsureInPast(now, name: "Field")),
            Case("InPastOrPresentDate", now, now + Second, "Field must be in the past or present.", v => v.EnsureInPastOrPresent(now, name: "Field"), r => r.EnsureInPastOrPresent(now, name: "Field")),
            Case("InFutureDate", now + Second, now, "Field must be in the future.", v => v.EnsureInFuture(now, name: "Field"), r => r.EnsureInFuture(now, name: "Field")),
            Case("InFutureOrPresentDate", now, now - Second, "Field must be in the future or present.", v => v.EnsureInFutureOrPresent(now, name: "Field"), r => r.EnsureInFutureOrPresent(now, name: "Field")),
        ];
    }

    private static IEnumerable<string> Members<T>(Result<T> result) =>
        ((ValidationError)result.GetError()).MemberName is { } member ? new[] { member } : Array.Empty<string>();

    private static void AssertPassing<T>(RuleCase<T> rule)
    {
        Assert.That(rule.OnValue(rule.Passing).GetValue(), NUnit.Framework.Is.EqualTo(rule.Passing));
        Assert.That(rule.OnResult(Result<T>.Ok(rule.Passing)).GetValue(), NUnit.Framework.Is.EqualTo(rule.Passing));
    }

    private static void AssertFailing<T>(RuleCase<T> rule)
    {
        Result<T> onValue = rule.OnValue(rule.Failing);
        Result<T> onResult = rule.OnResult(Result<T>.Ok(rule.Failing));

        Assert.That(onValue.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(onValue.GetError().Message, NUnit.Framework.Is.EqualTo(rule.Message));
        Assert.That(onResult.GetError(), NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(onResult.GetError().Message, NUnit.Framework.Is.EqualTo(rule.Message));
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

    [TestCaseSource(nameof(DurationRules))]
    public void DurationPassingValueIsReturnedInBothForms(RuleCase<TimeSpan> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(DurationRules))]
    public void DurationFailingValueGivesTheDefaultMessageInBothForms(RuleCase<TimeSpan> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(DurationRules))]
    public void DurationErrorAndEmptyPassThroughTheResultForm(RuleCase<TimeSpan> rule) => AssertPassThrough(rule);

    [TestCaseSource(nameof(InstantRules))]
    public void InstantPassingValueIsReturnedInBothForms(RuleCase<DateTimeOffset> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(InstantRules))]
    public void InstantFailingValueGivesTheDefaultMessageInBothForms(RuleCase<DateTimeOffset> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(InstantRules))]
    public void InstantErrorAndEmptyPassThroughTheResultForm(RuleCase<DateTimeOffset> rule) => AssertPassThrough(rule);

    [TestCaseSource(nameof(DateRules))]
    public void DatePassingValueIsReturnedInBothForms(RuleCase<DateTime> rule) => AssertPassing(rule);

    [TestCaseSource(nameof(DateRules))]
    public void DateFailingValueGivesTheDefaultMessageInBothForms(RuleCase<DateTime> rule) => AssertFailing(rule);

    [TestCaseSource(nameof(DateRules))]
    public void DateErrorAndEmptyPassThroughTheResultForm(RuleCase<DateTime> rule) => AssertPassThrough(rule);

    [Test]
    public void CapturedNameFillsTheMessageOfTheValueForm()
    {
        var order = new Order(Now + Second);

        Result<DateTimeOffset> result = order.PlacedAt.EnsureInPast(Clock);

        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("PlacedAt must be in the past."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "PlacedAt" }));
    }

    [Test]
    public void ResultFormUsesValueAsTheDefaultName()
    {
        Result<DateTimeOffset> result = Result<DateTimeOffset>.Ok(Now).EnsureInFuture(Clock);

        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("Value must be in the future."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void MessageReplacesTheTemplateVerbatim()
    {
        Result<DateTimeOffset> result = Now.EnsureInFuture(Clock, "Pick a date after {Name}.", "Field");

        Assert.That(result.GetError().Message, NUnit.Framework.Is.EqualTo("Pick a date after {Name}."));
    }

    [Test]
    public void ProviderIsReadWhenTheValueIsChecked()
    {
        var provider = new FixedTimeProvider(Now);
        var deadline = Now + Second;

        Result<DateTimeOffset> before = deadline.EnsureInFuture(provider);
        provider.Now = deadline;
        Result<DateTimeOffset> after = deadline.EnsureInFuture(provider);

        Assert.That(before.IsSuccess, NUnit.Framework.Is.True);
        Assert.That(after.IsSuccess, NUnit.Framework.Is.False);
    }

    [Test]
    public void NullProviderThrows()
    {
        Assert.Throws<ArgumentNullException>(() => Now.EnsureInPast((TimeProvider)null!));
        Assert.Throws<ArgumentNullException>(() => Result<DateTimeOffset>.Ok(Now).EnsureInPast((TimeProvider)null!));
    }

    [Test]
    public void ValidateCollectsTheFailuresOfSeveralRules()
    {
        var order = new Order(Now + Second);

        Result<Order> result = Result<Order>.Ok(order).Validate(
            o => o.PlacedAt.EnsureInPast(Clock),
            o => o.PlacedAt.EnsureInPastOrPresent(Clock));

        var error = (ValidationErrors)result.GetError();
        Assert.That(error.Errors.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[]
        {
            "PlacedAt must be in the past.",
            "PlacedAt must be in the past or present."
        }));
    }

    private sealed record Order(DateTimeOffset PlacedAt);
}
