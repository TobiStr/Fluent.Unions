#pragma warning disable FLU0004
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.TUnit;
using TUnit.Assertions.Exceptions;
using Assert = TUnit.Assertions.Assert;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace TUnitDomain.Tests;

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

public class UnionErrorAssertionsTests
{
    [Test]
    public async Task ErrorCase_ReturnsTheCaseOfAnErrorUnion()
    {
        var found = await Assert.That(AppResult<int>.Error(new NotFound("42")).AsResult().ErrorCase<NotFound>()).IsSome();

        await Assert.That(found.Id).IsEqualTo("42");
    }

    [Test]
    public async Task ErrorCase_OnAnotherCaseOrAnOkResult_IsNone()
    {
        await Assert.That(AppResult<int>.Error(new Conflict("7")).AsResult().ErrorCase<NotFound>()).IsNone();
        await Assert.That(AppResult<int>.Ok(1).AsResult().ErrorCase<NotFound>()).IsNone();
    }

    [Test]
    public async Task ResultWhoseValueHasTheErrorType_IsToldApartByItsState()
    {
        await Assert.That(await Assert.That(SResult<string>.Ok("fine").AsResult()).IsOk()).IsEqualTo("fine");
        await Assert.That(await Assert.That(SResult<string>.Error("broken").AsResult()).IsError()).IsEqualTo("broken");
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(SResult<string>.Ok("fine").AsResult()).IsError());
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(SResult<string>.Error("broken").AsResult()).IsOk());
    }
}
