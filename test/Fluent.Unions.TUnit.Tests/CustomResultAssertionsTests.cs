using Fluent.Unions;
using Fluent.Unions.TUnit;
using TUnit.Assertions.Exceptions;
using Assert = TUnit.Assertions.Assert;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace TUnitDomain.Tests;

public class CustomResultAssertionsTests
{
    private static Result<int> Find(int id) => id > 0 ? id : new DomainException("no order");

    private static async Task<string> FailureOf(Func<Task> assertion)
    {
        var exception = await Assert.ThrowsAsync<AssertionException>(assertion);

        return exception!.Message;
    }

    [Test]
    public async Task IsOk_OnOkResult_ReturnsTheValue()
    {
        var value = await Assert.That(Find(7).AsResult()).IsOk();

        await Assert.That(value).IsEqualTo(7);
    }

    [Test]
    public async Task IsOk_OnErrorResult_NamesTheError()
    {
        var message = await FailureOf(async () => await Assert.That(Find(0).AsResult()).IsOk());

        await Assert.That(message).Contains("to be an ok result").And.Contains("it failed with DomainException: no order");
    }

    [Test]
    public async Task IsOk_OnDefaultResult_Fails()
    {
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(default(Result<int>).AsResult()).IsOk());
    }

    [Test]
    public async Task IsError_OnErrorResult_ReturnsTheDomainError()
    {
        DomainException error = await Assert.That(Find(0).AsResult()).IsError();

        await Assert.That(error.Message).IsEqualTo("no order");
    }

    [Test]
    public async Task IsError_OnOkResult_NamesTheValue()
    {
        var message = await FailureOf(async () => await Assert.That(Find(7).AsResult()).IsError());

        await Assert.That(message).Contains("to be an error result").And.Contains("it was ok with value 7");
    }

    [Test]
    public async Task IsOk_OnValuelessOkResult_Passes()
    {
        Result result = Result.Ok;

        await Assert.That(result.AsResult()).IsOk();
    }

    [Test]
    public async Task IsOk_OnValuelessErrorResult_NamesTheError()
    {
        Result result = new DomainException("no order");

        var message = await FailureOf(async () => await Assert.That(result.AsResult()).IsOk());

        await Assert.That(message).Contains("it failed with DomainException: no order");
    }

    [Test]
    public async Task IsError_OnValuelessErrorResult_ReturnsTheDomainError()
    {
        Result result = new DomainException("no order");

        DomainException error = await Assert.That(result.AsResult()).IsError();

        await Assert.That(error.Message).IsEqualTo("no order");
    }

    [Test]
    public async Task IsError_OnValuelessOkResult_Fails()
    {
        Result result = Result.Ok;

        var message = await FailureOf(async () => await Assert.That(result.AsResult()).IsError());

        await Assert.That(message).Contains("it was ok");
    }
}
