using FluentAssertions;
using Fluent.Unions.Abstractions;
using NUnit.Framework;
using Fluent.Unions.FluentAssertions;

namespace Fluent.Unions.FluentAssertions.Tests;

public record OutOfStock(string Sku);

public record PaymentDeclined(string Reason);

public record Unexpected(Exception Exception);

public union StockError(OutOfStock, PaymentDeclined, Unexpected) : IFromException<StockError>
{
    public static StockError FromException(Exception exception) => new Unexpected(exception);
}

public union StockResult<T>(T, StockError) : IResult<StockResult<T>, T, StockError>
{
    public static StockResult<T> FromValue(T value) => value;

    public static StockResult<T> FromError(StockError error) => error;

    public static StockError FromException(Exception exception) => new Unexpected(exception);
}

public class CustomResultAssertionsTests
{
    private static int Calls;

    private static bool Track()
    {
        Calls++;
        return true;
    }

    private static string FailureOf(Action action) => Assert.Catch<Exception>(() => action())!.Message;

    private static StockResult<int> Ok(int value) => value;

    private static StockResult<int> Missing(string sku) => new StockError(new OutOfStock(sku));

    private static Result<int, StockError> ResultOk(int value) => value;

    private static Result<int, StockError> ResultMissing(string sku) => new StockError(new OutOfStock(sku));

    [Test]
    public void Should_PicksTheCustomAssertionsOverTheObjectOverload()
    {
        CustomResultAssertions<StockResult<int>, int, StockError> custom = Ok(1).Should();
        CustomResultAssertions<Result<int, StockError>, int, StockError> library = ResultOk(1).Should();

        Assert.That(custom.Subject.Value, Is.EqualTo(1));
        Assert.That(library.Subject.Value, Is.EqualTo(1));
    }

    [Test]
    public void BeOk_ExposesTheValueThroughWhich()
    {
        Ok(5).Should().BeOk().Which.Should().Be(5);
        ResultOk(5).Should().BeOk().Which.Should().Be(5);
    }

    [Test]
    public void BeOk_WhichFailureNamesTheValue()
    {
        var result = Ok(5);

        var message = FailureOf(() => result.Should().BeOk().Which.Should().Be(6));

        Assert.That(message, Does.Contain("result.Value"));
    }

    [Test]
    public void BeOk_FailsForADataErrorWithTypeAndContent()
    {
        var message = FailureOf(() => Missing("KB-1").Should().BeOk("the stock was filled"));

        Assert.That(message, Does.Contain("to be Ok because the stock was filled"));
        Assert.That(message, Does.Contain(typeof(OutOfStock).FullName));
        Assert.That(message, Does.Contain("KB-1"));
        Assert.That(FailureOf(() => ResultMissing("KB-1").Should().BeOk()), Does.Contain("KB-1"));
    }

    [Test]
    public void BeOk_FailsForAnExceptionErrorAndNamesIt()
    {
        StockResult<int> result = StockResult<int>.FromError(StockResult<int>.FromException(new InvalidOperationException("boom")));

        var message = FailureOf(() => result.Should().BeOk());

        Assert.That(message, Does.Contain(typeof(Unexpected).FullName));
    }

    [Test]
    public void BeOk_FailsForDefault()
    {
        Assert.That(FailureOf(() => default(StockResult<int>).Should().BeOk()), Does.Contain("holds neither a value nor an error"));
        Assert.That(FailureOf(() => default(Result<int, StockError>).Should().BeOk()), Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void BeOkWithValue_PassesAndFails()
    {
        Ok(5).Should().BeOkWithValue(5);
        ResultOk(5).Should().BeOkWithValue(5);

        var message = FailureOf(() => Ok(5).Should().BeOkWithValue(6));

        Assert.That(message, Does.Contain("value 6"));
        Assert.That(message, Does.Contain("value 5"));
    }

    [Test]
    public void BeOkWithValue_FailsForErrorAndDefault()
    {
        Assert.That(FailureOf(() => Missing("KB-1").Should().BeOkWithValue(1)), Does.Contain("KB-1"));
        Assert.That(FailureOf(() => default(StockResult<int>).Should().BeOkWithValue(1)), Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void BeOkMatching_PassesAndFails()
    {
        Ok(12).Should().BeOkMatching(value => value > 10);
        ResultOk(12).Should().BeOkMatching(value => value > 10);

        var message = FailureOf(() => Ok(12).Should().BeOkMatching(value => value > 20));

        Assert.That(message, Does.Contain("value > 20"));
        Assert.That(message, Does.Contain("12"));
    }

    [Test]
    public void BeOkMatching_FailsForError()
    {
        Assert.That(FailureOf(() => Missing("KB-1").Should().BeOkMatching(value => value > 0)), Does.Contain("KB-1"));
    }

    [Test]
    public void BeError_ExposesTheErrorThroughWhich()
    {
        var error = Missing("KB-1").Should().BeError().Which;

        Assert.That(error.Value, Is.EqualTo(new OutOfStock("KB-1")));
        Assert.That(ResultMissing("KB-1").Should().BeError().Which.Value, Is.EqualTo(new OutOfStock("KB-1")));
    }

    [Test]
    public void BeError_FailsForOkAndPassesForDefault()
    {
        Assert.That(FailureOf(() => Ok(1).Should().BeError()), Does.Contain("but it was Ok"));
        default(StockResult<int>).Should().BeError();
    }

    [Test]
    public void BeErrorOfCase_MatchesACaseInsideTheErrorUnion()
    {
        var stock = Missing("KB-1").Should().BeError<OutOfStock>().Which;

        Assert.That(stock.Sku, Is.EqualTo("KB-1"));
        Assert.That(ResultMissing("KB-2").Should().BeError<OutOfStock>().Which.Sku, Is.EqualTo("KB-2"));
    }

    [Test]
    public void BeErrorOfCase_FailsForAnotherCase()
    {
        var message = FailureOf(() => Missing("KB-1").Should().BeError<PaymentDeclined>("it was paid"));

        Assert.That(message, Does.Contain(typeof(PaymentDeclined).FullName));
        Assert.That(message, Does.Contain("because it was paid"));
        Assert.That(message, Does.Contain(typeof(OutOfStock).FullName));
    }

    [Test]
    public void BeErrorOfCase_FailsForOk()
    {
        Assert.That(FailureOf(() => Ok(1).Should().BeError<OutOfStock>()), Does.Contain("but it was Ok"));
    }

    [Test]
    public void BeErrorOfCase_AcceptsTheWholeErrorType()
    {
        var error = Missing("KB-1").Should().BeError<StockError>().Which;

        Assert.That(error.Value, Is.EqualTo(new OutOfStock("KB-1")));
    }

    [Test]
    public void BeErrorWithPredicate_PassesWhenMatching()
    {
        Missing("KB-1").Should().BeError<OutOfStock>(stock => stock.Sku == "KB-1");
        ResultMissing("KB-1").Should().BeError<OutOfStock>(stock => stock.Sku == "KB-1");
    }

    [Test]
    public void BeErrorWithPredicate_FailsWhenNotMatching()
    {
        var message = FailureOf(() => Missing("KB-1").Should().BeError<OutOfStock>(stock => stock.Sku == "KB-2"));

        Assert.That(message, Does.Contain("matching"));
        Assert.That(message, Does.Contain("stock.Sku == \"KB-2\""));
    }

    [Test]
    public void BeErrorWithPredicate_DoesNotRunThePredicateForAnotherCase()
    {
        Calls = 0;

        var message = FailureOf(() => Missing("KB-1").Should().BeError<PaymentDeclined>(_ => Track()));

        Assert.That(message, Does.Contain(typeof(PaymentDeclined).FullName));
        Assert.That(Calls, Is.EqualTo(0));
    }

    [Test]
    public void BeError_MatchesAnExceptionError()
    {
        StockResult<int> result = StockResult<int>.FromError(StockResult<int>.FromException(new InvalidOperationException("boom")));

        var cause = result.Should().BeError<Unexpected>().Which.Exception;

        Assert.That(cause.Message, Is.EqualTo("boom"));
    }
}
