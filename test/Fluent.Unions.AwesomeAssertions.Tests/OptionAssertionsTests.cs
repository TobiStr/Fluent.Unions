using AwesomeAssertions;
using NUnit.Framework;
using Fluent.Unions.AwesomeAssertions;

namespace Fluent.Unions.AwesomeAssertions.Tests;

public class OptionAssertionsTests
{
    private static int Calls;

    private static bool Track()
    {
        Calls++;
        return true;
    }

    private static string FailureOf(Action action) => Assert.Catch<Exception>(() => action())!.Message;

    [Test]
    public void BeSome_ExposesTheValueThroughWhich()
    {
        Option<string> nickname = "ada";

        nickname.Should().BeSome().Which.Should().Be("ada");
    }

    [Test]
    public void BeSome_FailsForNone()
    {
        Option<string> nickname = Option.None;

        var message = FailureOf(() => nickname.Should().BeSome("the customer set one"));

        Assert.That(message, Does.Contain("to be some because the customer set one"));
        Assert.That(message, Does.Contain("but it was none"));
    }

    [Test]
    public void BeSomeWithValue_WrongValue_IncludesTheBecauseClause()
    {
        Option<int> stock = 1;

        var message = FailureOf(() => stock.Should().BeSomeWithValue(2, "the stock is {0}", "two"));

        Assert.That(message, Does.Contain("because the stock is two"));
        Assert.That(message, Does.Contain("but it was some with value 1"));
    }

    [Test]
    public void BeSomeMatching_PredicateFails_IncludesTheBecauseClause()
    {
        Option<int> stock = 1;

        var message = FailureOf(() => stock.Should().BeSomeMatching(value => value > 5, "the stock is {0}", "big"));

        Assert.That(message, Does.Contain("because the stock is big"));
        Assert.That(message, Does.Contain("but it was some with value 1"));
    }

    [Test]
    public void BeSome_FailsForDefault()
    {
        var message = FailureOf(() => default(Option<int>).Should().BeSome());

        Assert.That(message, Does.Contain("but it was none"));
    }

    [Test]
    public void BeSomeWithValue_PassesAndFails()
    {
        Option<int> stock = 7;

        stock.Should().BeSomeWithValue(7);

        var message = FailureOf(() => stock.Should().BeSomeWithValue(8));

        Assert.That(message, Does.Contain("value 8"));
        Assert.That(message, Does.Contain("value 7"));
    }

    [Test]
    public void BeSomeWithValue_FailsForNone()
    {
        Option<int> stock = Option.None;

        Assert.That(FailureOf(() => stock.Should().BeSomeWithValue(1)), Does.Contain("but it was none"));
    }

    [Test]
    public void NotBeSomeWithValue_PassesForNoneAndOtherValues()
    {
        Option<int> none = Option.None;
        Option<int> stock = 7;

        none.Should().NotBeSomeWithValue(7);
        stock.Should().NotBeSomeWithValue(8);

        var message = FailureOf(() => stock.Should().NotBeSomeWithValue(7));

        Assert.That(message, Does.Contain("Did not expect"));
        Assert.That(message, Does.Contain("value 7"));
    }

    [Test]
    public void BeSomeMatching_PassesAndFails()
    {
        Option<int> stock = 12;

        stock.Should().BeSomeMatching(value => value > 10);

        var message = FailureOf(() => stock.Should().BeSomeMatching(value => value > 20));

        Assert.That(message, Does.Contain("value > 20"));
    }

    [Test]
    public void BeSomeMatching_DoesNotRunThePredicateForNone()
    {
        Calls = 0;
        Option<int> none = Option.None;

        var message = FailureOf(() => none.Should().BeSomeMatching(_ => Track()));

        Assert.That(Calls, Is.EqualTo(0));
        Assert.That(message, Does.Contain("but it was none"));
    }

    [Test]
    public void BeSomeSatisfying_RunsTheAssertionOnTheValue()
    {
        Option<string> nickname = "ada";
        var seen = "";

        nickname.Should().BeSomeSatisfying(value => seen = value);

        Assert.That(seen, Is.EqualTo("ada"));
        Assert.That(
            FailureOf(() => nickname.Should().BeSomeSatisfying(value => value.Should().Be("grace"))),
            Does.Contain("grace"));
    }

    [Test]
    public void BeNone_PassesForNoneAndDefault()
    {
        Option<int> none = Option.None;

        none.Should().BeNone();
        default(Option<int>).Should().BeNone();
    }

    [Test]
    public void BeNone_FailsForSome()
    {
        Option<int> stock = 7;

        var message = FailureOf(() => stock.Should().BeNone());

        Assert.That(message, Does.Contain("to be none"));
        Assert.That(message, Does.Contain("some with value 7"));
    }
}
