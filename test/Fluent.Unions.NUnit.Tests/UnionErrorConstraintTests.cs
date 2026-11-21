#pragma warning disable FLU0004
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.NUnit;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NUnitDomain.Tests;

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

public class UnionErrorConstraintTests
{
    private static string FailureOf(Action assertion) => Assert.Throws<AssertionException>(assertion)!.Message;

    [Test]
    public void ErrorOfType_LooksInsideAnErrorUnion()
    {
        var result = AppResult<int>.Error(new NotFound("42"));

        Assert.That(result, IsResult.ErrorOfType<NotFound>());
        Assert.That(result, IsResult.ErrorOfType<AppError>());
        Assert.That(result, new NotConstraint(IsResult.ErrorOfType<Conflict>()));
    }

    [Test]
    public void ErrorWith_AppliesTheConstraintToTheCase()
    {
        var result = AppResult<int>.Error(new NotFound("42"));

        Assert.That(result, IsResult.ErrorWith(Is.EqualTo(new NotFound("42"))));
        Assert.That(result, IsResult.ErrorWith(Has.Property("Id").EqualTo("42")));
        Assert.That(result, new NotConstraint(IsResult.ErrorWith(Is.EqualTo(new Conflict("42")))));
    }

    [Test]
    public void ErrorOfType_OnAnOkResult_Fails()
    {
        var message = FailureOf(() => Assert.That(AppResult<int>.Ok(7), IsResult.ErrorOfType<NotFound>()));

        Assert.That(message, Does.Contain("an error result of type NotFound"));
    }

    [Test]
    public void ResultWhoseValueHasTheErrorType_Ok_IsOk()
    {
        var result = SResult<string>.Ok("fine");

        Assert.That(result, IsResult.Ok);
        Assert.That(result, IsResult.OkWith(Is.EqualTo("fine")));
        Assert.That(result, new NotConstraint(IsResult.Error));
    }

    [Test]
    public void ResultWhoseValueHasTheErrorType_Error_IsError()
    {
        var result = SResult<string>.Error("broken");

        Assert.That(result, IsResult.Error);
        Assert.That(result, IsResult.ErrorWith(Is.EqualTo("broken")));
        Assert.That(result, new NotConstraint(IsResult.Ok));
    }
}
