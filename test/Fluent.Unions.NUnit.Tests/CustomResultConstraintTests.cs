using Fluent.Unions;
using Fluent.Unions.NUnit;
using NUnit.Framework;

namespace NUnitDomain.Tests;

public class CustomResultConstraintTests
{
    private static Result<int> Find(int id) => id > 0 ? id : new DomainException("no order");

    private static string FailureOf(Action assertion) => Assert.Throws<AssertionException>(assertion)!.Message;

    [Test]
    public void Ok_OnOkResults_Passes()
    {
        Assert.That(Find(7), IsResult.Ok);
        Assert.That(Result.Ok, IsResult.Ok);
    }

    [Test]
    public void Ok_OnErrorResult_NamesTheError()
    {
        var message = FailureOf(() => Assert.That(Find(0), IsResult.Ok));

        Assert.That(message, Does.Contain("Expected: an ok result").And.Contain("But was:  failed with DomainException: no order"));
    }

    [Test]
    public void Ok_OnDefaultResult_Fails()
    {
        var message = FailureOf(() => Assert.That(default(Result<int>), IsResult.Ok));

        Assert.That(message, Does.Contain("a default result, which is an error"));
    }

    [Test]
    public void Error_OnErrorResults_Passes()
    {
        Assert.That(Find(0), IsResult.Error);
        Assert.That(new Result(new DomainException("no order")), IsResult.Error);
        Assert.That(default(Result), IsResult.Error);
    }

    [Test]
    public void Error_OnOkResult_NamesTheValue()
    {
        var message = FailureOf(() => Assert.That(Find(7), IsResult.Error));

        Assert.That(message, Does.Contain("Expected: an error result").And.Contain("But was:  ok with value 7"));
    }

    [Test]
    public void OkWithValue_OnCustomResult_ComparesTheValue()
    {
        Assert.That(Find(7), IsResult.OkWith(Is.EqualTo(7)));

        var message = FailureOf(() => Assert.That(Find(7), IsResult.OkWith(Is.EqualTo(8))));

        Assert.That(message, Does.Contain("ok with value 7"));
    }

    [Test]
    public void ErrorOfType_OnCustomResult_ChecksTheErrorType()
    {
        Assert.That(Find(0), IsResult.ErrorOfType<DomainException>());
    }

    [Test]
    public void OtherValues_AreNeitherOkNorError()
    {
        var message = FailureOf(() => Assert.That(42, IsResult.Ok));

        Assert.That(message, Does.Contain("which is not a result or an option"));
    }
}
