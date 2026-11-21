#pragma warning disable FLU0004
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Xunit;
using Xunit;
using Xunit.Sdk;

namespace XunitDomain.Tests;

public record NotFound(string Id);

public record Conflict(string Id);

public union AppError(NotFound, Conflict) : IFromException<AppError>
{
    public static AppError FromException(Exception exception) => new Conflict(exception.Message);
}

[FluentResult<AppError>]
public readonly partial struct AppResult<T>;

[FluentResult<string>]
public readonly partial struct SResult<T>
{
    public static string FromException(Exception exception) => exception.Message;
}

public class UnionErrorAssertTests
{
    private static string FailureOf(Action assertion) => Assert.Throws<FailException>(assertion).Message;

    [Fact]
    public void ErrorOfCase_ReturnsTheCaseOfAnErrorUnion()
    {
        var result = AppResult<int>.Error(new NotFound("42"));

        Assert.Equal("42", ResultAssert.Error<NotFound>(result).Id);
        Assert.Equal(new NotFound("42"), ResultAssert.Error<AppError>(result).As<NotFound>().GetValue());
    }

    [Fact]
    public void ErrorOfCase_OnAnotherCase_NamesTheError()
    {
        var message = FailureOf(() => ResultAssert.Error<NotFound>(AppResult<int>.Error(new Conflict("7"))));

        Assert.StartsWith("Expected an error of type NotFound, but it failed with", message);
    }

    [Fact]
    public void ErrorOfCase_OnAnOkResult_Fails()
    {
        var message = FailureOf(() => ResultAssert.Error<NotFound>(AppResult<int>.Ok(1)));

        Assert.Equal("Expected an error of type NotFound, but the result was ok", message);
    }

    [Fact]
    public void ErrorOfCase_OnAnOption_Fails()
    {
        var message = FailureOf(() => ResultAssert.Error<NotFound>(Option.Some(1)));

        Assert.StartsWith("Expected a result", message);
    }

    [Fact]
    public void ResultWhoseValueHasTheErrorType_IsToldApartByItsState()
    {
        Assert.Equal("fine", ResultAssert.Ok(SResult<string>.Ok("fine")));
        Assert.Equal("broken", ResultAssert.Error(SResult<string>.Error("broken")));
        Assert.Throws<FailException>(() => ResultAssert.Error(SResult<string>.Ok("fine")));
        Assert.Throws<FailException>(() => ResultAssert.Ok(SResult<string>.Error("broken")));
    }
}
