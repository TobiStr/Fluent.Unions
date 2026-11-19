using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

public class IsGeneralTests
{
    [Test]
    public void NullAndNotNull()
    {
        Assert.That(Is.Null<string>(null), NUnit.Framework.Is.True);
        Assert.That(Is.Null("a"), NUnit.Framework.Is.False);
        Assert.That(Is.NotNull<string>(null), NUnit.Framework.Is.False);
        Assert.That(Is.NotNull("a"), NUnit.Framework.Is.True);
        Assert.That(Is.NotNull(0), NUnit.Framework.Is.True);
    }

    [Test]
    public void EqualToUsesTheDefaultComparer()
    {
        Assert.That(Is.EqualTo("a")("a"), NUnit.Framework.Is.True);
        Assert.That(Is.EqualTo("a")("A"), NUnit.Framework.Is.False);
        Assert.That(Is.EqualTo(5)(5), NUnit.Framework.Is.True);
    }

    [Test]
    public void EqualToUsesTheGivenComparer()
    {
        Assert.That(Is.EqualTo("a", StringComparer.OrdinalIgnoreCase)("A"), NUnit.Framework.Is.True);
        Assert.That(Is.NotEqualTo("a", StringComparer.OrdinalIgnoreCase)("A"), NUnit.Framework.Is.False);
    }

    [Test]
    public void NotEqualTo()
    {
        Assert.That(Is.NotEqualTo(5)(6), NUnit.Framework.Is.True);
        Assert.That(Is.NotEqualTo(5)(5), NUnit.Framework.Is.False);
    }

    [Test]
    public void In()
    {
        Assert.That(Is.In(1, 2, 3)(2), NUnit.Framework.Is.True);
        Assert.That(Is.In(1, 2, 3)(4), NUnit.Framework.Is.False);
        Assert.That(Is.In("a", "b")("b"), NUnit.Framework.Is.True);
    }

    [Test]
    public void NotIn()
    {
        Assert.That(Is.NotIn(1, 2, 3)(4), NUnit.Framework.Is.True);
        Assert.That(Is.NotIn(1, 2, 3)(2), NUnit.Framework.Is.False);
    }

    [Test]
    public void InWithNoValuesNeverPassesAndNotInAlwaysPasses()
    {
        Assert.That(Is.In<int>()(1), NUnit.Framework.Is.False);
        Assert.That(Is.NotIn<int>()(1), NUnit.Framework.Is.True);
    }

    [Test]
    public void InIgnoresLaterChangesToTheArray()
    {
        int[] allowed = [1, 2];
        var rule = Is.In(allowed);

        allowed[0] = 9;

        Assert.That(rule(1), NUnit.Framework.Is.True);
        Assert.That(rule(9), NUnit.Framework.Is.False);
    }

    [Test]
    public void SomeAndNone()
    {
        Assert.That(Is.Some(Option.Some(5)), NUnit.Framework.Is.True);
        Assert.That(Is.Some(default(Option<int>)), NUnit.Framework.Is.False);
        Assert.That(Is.None(Option.Some(5)), NUnit.Framework.Is.False);
        Assert.That(Is.None(default(Option<int>)), NUnit.Framework.Is.True);
    }
}
