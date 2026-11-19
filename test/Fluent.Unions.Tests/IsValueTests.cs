using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

public class IsValueTests
{
    [Flags]
    private enum Channels
    {
        Web = 1,
        Store = 2
    }

    private enum Status
    {
        Pending = 1,
        Shipped = 2
    }

    [Test]
    public void GuidNotEmpty()
    {
        Assert.That(Is.NotEmpty(Guid.NewGuid()), NUnit.Framework.Is.True);
        Assert.That(Is.NotEmpty(Guid.Empty), NUnit.Framework.Is.False);
    }

    [Test]
    public void GuidEmpty()
    {
        Assert.That(Is.Empty(Guid.Empty), NUnit.Framework.Is.True);
        Assert.That(Is.Empty(Guid.NewGuid()), NUnit.Framework.Is.False);
    }

    [Test]
    public void GuidRulesWorkAsMethodGroups()
    {
        Func<Guid, bool> notEmpty = Is.NotEmpty;
        Func<string?, bool> notEmptyText = Is.NotEmpty;

        Assert.That(notEmpty(Guid.NewGuid()), NUnit.Framework.Is.True);
        Assert.That(notEmptyText("a"), NUnit.Framework.Is.True);
    }

    [Test]
    public void True()
    {
        Assert.That(Is.True(true), NUnit.Framework.Is.True);
        Assert.That(Is.True(false), NUnit.Framework.Is.False);
    }

    [Test]
    public void False()
    {
        Assert.That(Is.False(false), NUnit.Framework.Is.True);
        Assert.That(Is.False(true), NUnit.Framework.Is.False);
    }

    [Test]
    public void DefinedAcceptsNamedMembersOnly()
    {
        Assert.That(Is.Defined(Status.Pending), NUnit.Framework.Is.True);
        Assert.That(Is.Defined((Status)99), NUnit.Framework.Is.False);
        Assert.That(Is.Defined((Status)0), NUnit.Framework.Is.False);
    }

    [Test]
    public void DefinedRejectsCombinedFlagValues()
    {
        Assert.That(Is.Defined(Channels.Web), NUnit.Framework.Is.True);
        Assert.That(Is.Defined(Channels.Web | Channels.Store), NUnit.Framework.Is.False);
    }
}
