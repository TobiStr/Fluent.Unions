using Fluent.Unions;
using Fluent.Unions.Xunit;
using Xunit;
using Xunit.Sdk;

namespace XunitDomain.Tests;

public class CustomResultAssertTests
{
    private static Result<int> Find(int id) => id > 0 ? id : new DomainException("no order");

    private static string FailureOf(Action assertion) => Assert.Throws<FailException>(assertion).Message;

    [Fact]
    public void Ok_OnOkResult_ReturnsTheValue()
    {
        Assert.Equal(7, ResultAssert.Ok(Find(7)));
    }

    [Fact]
    public void Ok_OnErrorResult_NamesTheError()
    {
        var message = FailureOf(() => ResultAssert.Ok(Find(0)));

        Assert.Equal("Expected an ok result, but it failed with DomainException: no order", message);
    }

    [Fact]
    public void Ok_OnDefaultResult_Fails()
    {
        Assert.Throws<FailException>(() => ResultAssert.Ok(default(Result<int>)));
    }

    [Fact]
    public void Error_OnErrorResult_ReturnsTheDomainError()
    {
        DomainException error = ResultAssert.Error(Find(0));

        Assert.Equal("no order", error.Message);
    }

    [Fact]
    public void Error_OnOkResult_NamesTheValue()
    {
        var message = FailureOf(() => ResultAssert.Error(Find(7)));

        Assert.Equal("Expected an error result, but it was ok with value 7", message);
    }

    [Fact]
    public void Ok_OnValuelessOkResult_Passes()
    {
        ResultAssert.Ok(Result.Ok);
    }

    [Fact]
    public void Ok_OnValuelessErrorResult_NamesTheError()
    {
        Result result = new DomainException("no order");

        var message = FailureOf(() => ResultAssert.Ok(result));

        Assert.Equal("Expected an ok result, but it failed with DomainException: no order", message);
    }

    [Fact]
    public void Error_OnValuelessErrorResult_ReturnsTheDomainError()
    {
        Result result = new DomainException("no order");

        DomainException error = ResultAssert.Error(result);

        Assert.Equal("no order", error.Message);
    }

    [Fact]
    public void Error_OnValuelessOkResult_Fails()
    {
        var message = FailureOf(() => ResultAssert.Error(Result.Ok));

        Assert.Equal("Expected an error result, but it was ok", message);
    }
}
