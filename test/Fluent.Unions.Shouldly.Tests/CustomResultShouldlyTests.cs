using Fluent.Unions;
using Fluent.Unions.Shouldly;
using NUnit.Framework;
using Shouldly;

namespace ShouldlyDomain.Tests;

public class CustomResultShouldlyTests
{
    private static Result<int> Find(int id) => id > 0 ? id : new DomainException("no order");

    [Test]
    public void ShouldBeOk_OnOkResult_ReturnsTheValue()
    {
        Find(7).ShouldBeOk().ShouldBe(7);
    }

    [Test]
    public void ShouldBeOk_OnErrorResult_NamesTheError()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Find(0).ShouldBeOk());

        exception.Message.ShouldContain("Expected an ok result, but it failed with DomainException: no order");
    }

    [Test]
    public void ShouldBeOk_WithCustomMessage_AppendsIt()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Find(0).ShouldBeOk("order must exist"));

        exception.Message.ShouldContain("order must exist");
    }

    [Test]
    public void ShouldBeOk_OnDefaultResult_Fails()
    {
        Should.Throw<ShouldAssertException>(() => default(Result<int>).ShouldBeOk());
    }

    [Test]
    public void ShouldBeError_OnErrorResult_ReturnsTheDomainError()
    {
        DomainException error = Find(0).ShouldBeError();

        error.Message.ShouldBe("no order");
    }

    [Test]
    public void ShouldBeError_OnOkResult_NamesTheValue()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Find(7).ShouldBeError());

        exception.Message.ShouldContain("Expected an error result, but it was ok with value 7");
    }

    [Test]
    public void ShouldBeOk_OnValuelessOkResult_ReturnsTheResult()
    {
        Result result = Result.Ok;

        result.ShouldBeOk().IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldBeOk_OnValuelessErrorResult_NamesTheError()
    {
        Result result = new DomainException("no order");

        var exception = Should.Throw<ShouldAssertException>(() => result.ShouldBeOk());

        exception.Message.ShouldContain("Expected an ok result, but it failed with DomainException: no order");
    }

    [Test]
    public void ShouldBeError_OnValuelessResult_ReturnsTheDomainError()
    {
        Result result = new DomainException("no order");

        DomainException error = result.ShouldBeError();

        error.Message.ShouldBe("no order");
    }

    [Test]
    public void ShouldBeError_OnValuelessOkResult_Fails()
    {
        Result result = Result.Ok;

        var exception = Should.Throw<ShouldAssertException>(() => result.ShouldBeError());

        exception.Message.ShouldContain("Expected an error result, but it was ok");
    }
}
