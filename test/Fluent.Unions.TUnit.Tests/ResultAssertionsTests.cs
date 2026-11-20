using TUnit.Assertions.Exceptions;
using Assert = TUnit.Assertions.Assert;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace Fluent.Unions.TUnit.Tests;

public class ResultAssertionsTests
{
    private static async Task<string> FailureOf(Func<Task> assertion)
    {
        var exception = await Assert.ThrowsAsync<AssertionException>(assertion);

        return exception!.Message;
    }

    [Test]
    public async Task IsOk_OnOkResult_Passes()
    {
        await Assert.That(Result.Ok).IsOk();
    }

    [Test]
    public async Task IsOk_OnErrorResult_NamesTheError()
    {
        Result result = new TimeoutException("too slow");

        var message = await FailureOf(async () => await Assert.That(result).IsOk());

        await Assert.That(message).Contains("to be an ok result").And.Contains("it failed with TimeoutException: too slow");
    }

    [Test]
    public async Task IsOk_OnDefaultResult_Fails()
    {
        await Assert.That(async () => await Assert.That(default(Result)).IsOk()).Throws<AssertionException>();
    }

    [Test]
    public async Task IsOkOfT_OnOkResult_ReturnsTheValue()
    {
        var value = await Assert.That(Result<int>.Ok(42)).IsOk();

        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task IsOkOfT_OnErrorResult_NamesTheError()
    {
        var result = Result<int>.Error(new InvalidOperationException("broken"));

        var message = await FailureOf(async () => await Assert.That(result).IsOk());

        await Assert.That(message).Contains("it failed with InvalidOperationException: broken");
    }

    [Test]
    public async Task IsError_OnErrorResult_ReturnsTheException()
    {
        var error = new TimeoutException("too slow");
        Result result = error;

        var actual = await Assert.That(result).IsError();

        await Assert.That(actual).IsSameReferenceAs(error);
    }

    [Test]
    public async Task IsError_OnOkResult_Fails()
    {
        var message = await FailureOf(async () => await Assert.That(Result.Ok).IsError());

        await Assert.That(message).Contains("to be an error result").And.Contains("it was ok");
    }

    [Test]
    public async Task IsError_OnDefaultResult_Passes()
    {
        await Assert.That(default(Result)).IsError();
    }

    [Test]
    public async Task IsErrorOfT_OnErrorResult_ReturnsTheException()
    {
        var error = new TimeoutException("too slow");

        var actual = await Assert.That(Result<int>.Error(error)).IsError();

        await Assert.That(actual).IsSameReferenceAs(error);
    }

    [Test]
    public async Task IsErrorOfT_OnOkResult_NamesTheValue()
    {
        var message = await FailureOf(async () => await Assert.That(Result<int>.Ok(42)).IsError());

        await Assert.That(message).Contains("it was ok with value 42");
    }

    [Test]
    public async Task IsErrorOfType_OnMatchingError_ReturnsTheTypedException()
    {
        Result result = new ArgumentException("bad", "name");

        var actual = await Assert.That(result).IsError<ArgumentException>();

        await Assert.That(actual.ParamName).IsEqualTo("name");
    }

    [Test]
    public async Task IsErrorOfType_OnDerivedError_Passes()
    {
        Result result = new ArgumentNullException("name");

        await Assert.That(result).IsError<ArgumentException>();
    }

    [Test]
    public async Task IsErrorOfType_OnOtherError_NamesTheActualError()
    {
        Result result = new TimeoutException("too slow");

        var message = await FailureOf(async () => await Assert.That(result).IsError<ArgumentException>());

        await Assert.That(message).Contains("to be an error of type").And.Contains("it failed with TimeoutException: too slow");
    }

    [Test]
    public async Task IsErrorOfType_OnOkResult_Fails()
    {
        var message = await FailureOf(async () => await Assert.That(Result.Ok).IsError<ArgumentException>());

        await Assert.That(message).Contains("it was ok");
    }

    [Test]
    public async Task IsErrorOfType_OnResultOfT_NeedsTheNonGenericResult()
    {
        Result result = Result<int>.Error(new TimeoutException("too slow"));

        await Assert.That(result).IsError<TimeoutException>();
    }

    [Test]
    public async Task IsSome_OnOption_ReturnsTheValue()
    {
        var value = await Assert.That(Option.Some("ada")).IsSome();

        await Assert.That(value).IsEqualTo("ada");
    }

    [Test]
    public async Task IsSome_OnNone_Fails()
    {
        var message = await FailureOf(async () => await Assert.That(default(Option<string>)).IsSome());

        await Assert.That(message).Contains("to be an option with a value").And.Contains("it was none");
    }

    [Test]
    public async Task IsNone_OnNoneAndDefault_Passes()
    {
        await Assert.That(default(Option<string>)).IsNone();
        await Assert.That(new Option<string>(Option.None)).IsNone();
    }

    [Test]
    public async Task IsNone_OnSome_NamesTheValue()
    {
        var message = await FailureOf(async () => await Assert.That(Option.Some("ada")).IsNone());

        await Assert.That(message).Contains("to be an option without a value").And.Contains("it was some with value ada");
    }
}
