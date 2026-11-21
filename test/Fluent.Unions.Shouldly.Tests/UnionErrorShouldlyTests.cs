#pragma warning disable FLU0004
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Shouldly;
using NUnit.Framework;
using Shouldly;

namespace ShouldlyDomain.Tests;

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

public class UnionErrorShouldlyTests
{
    [Test]
    public void ShouldBeErrorOfCase_ReturnsTheCaseOfAnErrorUnion()
    {
        var result = AppResult<int>.Error(new NotFound("42"));

        result.ShouldBeError<NotFound>().Id.ShouldBe("42");
        result.ShouldBeError<AppError>().As<NotFound>().GetValue().ShouldBe(new NotFound("42"));
    }

    [Test]
    public void ShouldBeErrorOfCase_OnAnotherCase_NamesTheError()
    {
        var exception = Should.Throw<ShouldAssertException>(() => AppResult<int>.Error(new Conflict("7")).ShouldBeError<NotFound>());

        exception.Message.ShouldContain("Expected an error of type NotFound, but it failed with");
    }

    [Test]
    public void ShouldBeErrorOfCase_OnAnOkResult_Fails()
    {
        var exception = Should.Throw<ShouldAssertException>(() => AppResult<int>.Ok(1).ShouldBeError<NotFound>());

        exception.Message.ShouldContain("Expected an error of type NotFound, but the result was ok");
    }

    [Test]
    public void ShouldBeErrorOfCase_OnAnOption_Fails()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Option.Some(1).ShouldBeError<NotFound>());

        exception.Message.ShouldContain("Expected a result");
    }

    [Test]
    public void ResultWhoseValueHasTheErrorType_IsToldApartByItsState()
    {
        SResult<string>.Ok("fine").ShouldBeOk().ShouldBe("fine");
        SResult<string>.Error("broken").ShouldBeError().ShouldBe("broken");
        Should.Throw<ShouldAssertException>(() => SResult<string>.Ok("fine").ShouldBeError());
        Should.Throw<ShouldAssertException>(() => SResult<string>.Error("broken").ShouldBeOk());
    }
}
