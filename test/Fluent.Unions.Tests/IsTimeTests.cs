using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

public class IsTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [TestCase(1, true)]
    [TestCase(0, false)]
    [TestCase(-1, false)]
    public void TimeSpanPositive(int seconds, bool expected) =>
        Assert.That(Is.Positive(TimeSpan.FromSeconds(seconds)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(-1, true)]
    [TestCase(0, false)]
    [TestCase(1, false)]
    public void TimeSpanNegative(int seconds, bool expected) =>
        Assert.That(Is.Negative(TimeSpan.FromSeconds(seconds)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(-1, true)]
    [TestCase(0, true)]
    [TestCase(1, false)]
    public void TimeSpanNonPositive(int seconds, bool expected) =>
        Assert.That(Is.NonPositive(TimeSpan.FromSeconds(seconds)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(1, true)]
    [TestCase(0, true)]
    [TestCase(-1, false)]
    public void TimeSpanNonNegative(int seconds, bool expected) =>
        Assert.That(Is.NonNegative(TimeSpan.FromSeconds(seconds)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(-1, false)]
    public void TimeSpanZero(int seconds, bool expected) =>
        Assert.That(Is.Zero(TimeSpan.FromSeconds(seconds)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(1, true)]
    [TestCase(-1, true)]
    [TestCase(0, false)]
    public void TimeSpanNonZero(int seconds, bool expected) =>
        Assert.That(Is.NonZero(TimeSpan.FromSeconds(seconds)), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void TimeSpanRulesWorkAsMethodGroups()
    {
        Func<TimeSpan, bool> positive = Is.Positive;
        Func<int, bool> positiveNumber = Is.Positive;

        Assert.That(positive(Second), NUnit.Framework.Is.True);
        Assert.That(positiveNumber(1), NUnit.Framework.Is.True);
    }

    [Test]
    public void InPastIsStrictAroundTheProviderInstant()
    {
        var predicate = Is.InPast(new FixedTimeProvider(Now));

        Assert.That(predicate(Now - Second), NUnit.Framework.Is.True);
        Assert.That(predicate(Now), NUnit.Framework.Is.False);
        Assert.That(predicate(Now + Second), NUnit.Framework.Is.False);
    }

    [Test]
    public void InPastOrPresentIncludesTheProviderInstant()
    {
        var predicate = Is.InPastOrPresent(new FixedTimeProvider(Now));

        Assert.That(predicate(Now - Second), NUnit.Framework.Is.True);
        Assert.That(predicate(Now), NUnit.Framework.Is.True);
        Assert.That(predicate(Now + Second), NUnit.Framework.Is.False);
    }

    [Test]
    public void InFutureIsStrictAroundTheProviderInstant()
    {
        var predicate = Is.InFuture(new FixedTimeProvider(Now));

        Assert.That(predicate(Now + Second), NUnit.Framework.Is.True);
        Assert.That(predicate(Now), NUnit.Framework.Is.False);
        Assert.That(predicate(Now - Second), NUnit.Framework.Is.False);
    }

    [Test]
    public void InFutureOrPresentIncludesTheProviderInstant()
    {
        var predicate = Is.InFutureOrPresent(new FixedTimeProvider(Now));

        Assert.That(predicate(Now + Second), NUnit.Framework.Is.True);
        Assert.That(predicate(Now), NUnit.Framework.Is.True);
        Assert.That(predicate(Now - Second), NUnit.Framework.Is.False);
    }

    [Test]
    public void ProviderIsReadAtCheckTime()
    {
        var provider = new FixedTimeProvider(Now);
        var predicate = Is.InFuture(provider);
        var deadline = Now + Second;

        Assert.That(predicate(deadline), NUnit.Framework.Is.True);

        provider.Now = deadline;

        Assert.That(predicate(deadline), NUnit.Framework.Is.False);
    }

    [Test]
    public void ExplicitInstantOverloadsCompareAgainstTheGivenInstant()
    {
        Assert.That(Is.InPast(Now)(Now - Second), NUnit.Framework.Is.True);
        Assert.That(Is.InPast(Now)(Now), NUnit.Framework.Is.False);
        Assert.That(Is.InPastOrPresent(Now)(Now), NUnit.Framework.Is.True);
        Assert.That(Is.InPastOrPresent(Now)(Now + Second), NUnit.Framework.Is.False);
        Assert.That(Is.InFuture(Now)(Now + Second), NUnit.Framework.Is.True);
        Assert.That(Is.InFuture(Now)(Now), NUnit.Framework.Is.False);
        Assert.That(Is.InFutureOrPresent(Now)(Now), NUnit.Framework.Is.True);
        Assert.That(Is.InFutureOrPresent(Now)(Now - Second), NUnit.Framework.Is.False);
    }

    [Test]
    public void ExplicitDateOverloadsCompareAgainstTheGivenDate()
    {
        var now = Now.UtcDateTime;
        var second = Second;

        Assert.That(Is.InPast(now)(now - second), NUnit.Framework.Is.True);
        Assert.That(Is.InPast(now)(now), NUnit.Framework.Is.False);
        Assert.That(Is.InPastOrPresent(now)(now), NUnit.Framework.Is.True);
        Assert.That(Is.InPastOrPresent(now)(now + second), NUnit.Framework.Is.False);
        Assert.That(Is.InFuture(now)(now + second), NUnit.Framework.Is.True);
        Assert.That(Is.InFuture(now)(now), NUnit.Framework.Is.False);
        Assert.That(Is.InFutureOrPresent(now)(now), NUnit.Framework.Is.True);
        Assert.That(Is.InFutureOrPresent(now)(now - second), NUnit.Framework.Is.False);
    }

    [Test]
    public void NullProviderThrowsWhenTheRuleIsCreated()
    {
        Assert.Throws<ArgumentNullException>(() => Is.InPast((TimeProvider)null!));
        Assert.Throws<ArgumentNullException>(() => Is.InPastOrPresent((TimeProvider)null!));
        Assert.Throws<ArgumentNullException>(() => Is.InFuture((TimeProvider)null!));
        Assert.Throws<ArgumentNullException>(() => Is.InFutureOrPresent((TimeProvider)null!));
    }
}
