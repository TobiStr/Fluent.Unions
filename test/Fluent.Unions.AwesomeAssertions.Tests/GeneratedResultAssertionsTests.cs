using AwesomeAssertions;
using Fluent.Unions;
using Fluent.Unions.AwesomeAssertions;
using NUnit.Framework;

namespace AwesomeAssertionsDomain.Tests;

public class GeneratedResultAssertionsTests
{
    private static Result<int> Find(int id) => id > 0 ? id : new DomainException("no order");

    private static string FailureOf(Action action) => Assert.Catch<Exception>(() => action())!.Message;

    [Test]
    public void Should_PicksTheCustomAssertionsForAGeneratedResult()
    {
        CustomResultAssertions<Result<int>, int, DomainException> assertions = Find(1).Should();

        Assert.That(assertions.Subject.Value, Is.EqualTo(1));
    }

    [Test]
    public void BeOk_ExposesTheValueThroughWhich()
    {
        Find(5).Should().BeOk().Which.Should().Be(5);
    }

    [Test]
    public void BeOk_FailsForAnErrorAndNamesIt()
    {
        var message = FailureOf(() => Find(0).Should().BeOk("the order exists"));

        Assert.That(message, Does.Contain("to be Ok because the order exists"));
        Assert.That(message, Does.Contain("no order"));
    }

    [Test]
    public void BeOkWithValue_ComparesTheValue()
    {
        Find(5).Should().BeOkWithValue(5);

        Assert.That(FailureOf(() => Find(5).Should().BeOkWithValue(6)), Does.Contain("5"));
    }

    [Test]
    public void BeOkMatching_ChecksThePredicate()
    {
        Find(5).Should().BeOkMatching(value => value > 3);

        Assert.That(FailureOf(() => Find(5).Should().BeOkMatching(value => value > 9)), Does.Contain("value > 9"));
    }

    [Test]
    public void BeError_ReturnsTheDomainError()
    {
        Find(0).Should().BeError().Which.Message.Should().Be("no order");
    }

    [Test]
    public void BeError_ChecksTheCaseType()
    {
        Find(0).Should().BeError<DomainException>().Which.Message.Should().Be("no order");

        Assert.That(FailureOf(() => Find(0).Should().BeError<ArgumentException>()), Does.Contain("ArgumentException"));
    }

    [Test]
    public void BeError_FailsForAnOkResult()
    {
        var message = FailureOf(() => Find(5).Should().BeError());

        Assert.That(message, Does.Contain("to be an error of type").And.Contain("but it was Ok"));
    }

    [Test]
    public void BeOk_FailsForADefaultResult()
    {
        Assert.That(FailureOf(() => default(Result<int>).Should().BeOk()), Does.Contain("to be Ok"));
    }
}
