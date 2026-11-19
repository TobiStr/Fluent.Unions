using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

public class IsNumberTests
{
    [TestCase(1, true)]
    [TestCase(0, false)]
    [TestCase(-1, false)]
    public void Positive(int value, bool expected) => Assert.That(Is.Positive(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(-1, true)]
    [TestCase(0, false)]
    [TestCase(1, false)]
    public void Negative(int value, bool expected) => Assert.That(Is.Negative(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(-1, true)]
    [TestCase(0, true)]
    [TestCase(1, false)]
    public void NonPositive(int value, bool expected) => Assert.That(Is.NonPositive(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(1, true)]
    [TestCase(0, true)]
    [TestCase(-1, false)]
    public void NonNegative(int value, bool expected) => Assert.That(Is.NonNegative(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(-1, false)]
    public void Zero(int value, bool expected) => Assert.That(Is.Zero(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(1, true)]
    [TestCase(-1, true)]
    [TestCase(0, false)]
    public void NonZero(int value, bool expected) => Assert.That(Is.NonZero(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(2, true)]
    [TestCase(0, true)]
    [TestCase(-2, true)]
    [TestCase(3, false)]
    [TestCase(-3, false)]
    public void Even(int value, bool expected) => Assert.That(Is.Even(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(3, true)]
    [TestCase(-3, true)]
    [TestCase(2, false)]
    [TestCase(0, false)]
    public void Odd(int value, bool expected) => Assert.That(Is.Odd(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void NaNFailsEverySignRule()
    {
        Assert.That(Is.Positive(double.NaN), NUnit.Framework.Is.False);
        Assert.That(Is.Negative(double.NaN), NUnit.Framework.Is.False);
        Assert.That(Is.NonPositive(double.NaN), NUnit.Framework.Is.False);
        Assert.That(Is.NonNegative(double.NaN), NUnit.Framework.Is.False);
        Assert.That(Is.Zero(double.NaN), NUnit.Framework.Is.False);
        Assert.That(Is.NonZero(double.NaN), NUnit.Framework.Is.False);
    }

    [Test]
    public void NegativeZeroIsZero()
    {
        Assert.That(Is.Zero(-0.0), NUnit.Framework.Is.True);
        Assert.That(Is.Negative(-0.0), NUnit.Framework.Is.False);
        Assert.That(Is.NonPositive(-0.0), NUnit.Framework.Is.True);
        Assert.That(Is.NonNegative(-0.0), NUnit.Framework.Is.True);
    }

    [Test]
    public void SignRulesWorkOnOtherNumberTypes()
    {
        Assert.That(Is.Positive(0.5m), NUnit.Framework.Is.True);
        Assert.That(Is.Negative(-1L), NUnit.Framework.Is.True);
        Assert.That(Is.NonNegative(0.0f), NUnit.Framework.Is.True);
    }

    [Test]
    public void SignRulesWorkAsMethodGroups()
    {
        Option<int> stock = Option.Some(-3).Where(Is.Positive);

        Assert.That(stock.HasValue, NUnit.Framework.Is.False);
    }

    [TestCase(4, false)]
    [TestCase(5, false)]
    [TestCase(6, true)]
    public void GreaterThan(int value, bool expected) => Assert.That(Is.GreaterThan(5)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(4, false)]
    [TestCase(5, true)]
    [TestCase(6, true)]
    public void GreaterThanOrEqual(int value, bool expected) => Assert.That(Is.GreaterThanOrEqual(5)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(4, true)]
    [TestCase(5, false)]
    [TestCase(6, false)]
    public void LessThan(int value, bool expected) => Assert.That(Is.LessThan(5)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(4, true)]
    [TestCase(5, true)]
    [TestCase(6, false)]
    public void LessThanOrEqual(int value, bool expected) => Assert.That(Is.LessThanOrEqual(5)(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(17, false)]
    [TestCase(18, true)]
    [TestCase(50, true)]
    [TestCase(120, true)]
    [TestCase(121, false)]
    public void InRangeIsInclusive(int value, bool expected) => Assert.That(Is.InRange(18, 120)(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void InRangeAcceptsEqualBounds()
    {
        Assert.That(Is.InRange(5, 5)(5), NUnit.Framework.Is.True);
        Assert.That(Is.InRange(5, 5)(6), NUnit.Framework.Is.False);
    }

    [Test]
    public void InRangeWithMinAboveMaxThrowsWhenCreated() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Is.InRange(120, 18));

    [Test]
    public void ComparisonsWorkOnStringsAndDates()
    {
        Assert.That(Is.GreaterThan("b")("c"), NUnit.Framework.Is.True);
        Assert.That(Is.LessThan(new DateOnly(2026, 1, 2))(new DateOnly(2026, 1, 1)), NUnit.Framework.Is.True);
    }

    [Test]
    public void ComparisonsFailForNull()
    {
        Assert.That(Is.GreaterThan("a")(null!), NUnit.Framework.Is.False);
        Assert.That(Is.InRange("a", "z")(null!), NUnit.Framework.Is.False);
    }

    [TestCase(6, true)]
    [TestCase(0, true)]
    [TestCase(-12, true)]
    [TestCase(7, false)]
    public void DivisibleBy(int value, bool expected) => Assert.That(Is.DivisibleBy(6)(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void DivisibleByZeroThrowsWhenCreated() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Is.DivisibleBy(0));
}
