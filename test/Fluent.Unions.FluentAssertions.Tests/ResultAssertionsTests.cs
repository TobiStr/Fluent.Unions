using FluentAssertions;
using NUnit.Framework;
using Fluent.Unions.FluentAssertions;

namespace Fluent.Unions.FluentAssertions.Tests;

public class ResultAssertionsTests
{
    private static int Calls;

    private static bool Track()
    {
        Calls++;
        return true;
    }

    private static string FailureOf(Action action) => Assert.Catch<Exception>(() => action())!.Message;

    [Test]
    public void Result_BeOk_PassesForOk()
    {
        Result.Ok.Should().BeOk();
    }

    [Test]
    public void Result_BeOk_FailsForErrorWithTypeAndMessage()
    {
        Result result = Result.Error(new InvalidOperationException("payment declined"));

        var message = FailureOf(() => result.Should().BeOk("the order was paid"));

        Assert.That(message, Does.Contain("to be Ok because the order was paid"));
        Assert.That(message, Does.Contain("System.InvalidOperationException"));
        Assert.That(message, Does.Contain("payment declined"));
    }

    [Test]
    public void Result_BeOk_FailsForDefault()
    {
        var message = FailureOf(() => default(Result).Should().BeOk());

        Assert.That(message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void Result_BeError_PassesForAnyException()
    {
        Result result = Result.Error(new KeyNotFoundException("no such order"));

        result.Should().BeError().WithMessage("no such*");
    }

    [Test]
    public void Result_BeError_FailsForOk()
    {
        var message = FailureOf(() => Result.Ok.Should().BeError());

        Assert.That(message, Does.Contain("to be an error of type System.Exception"));
        Assert.That(message, Does.Contain("but it was Ok"));
    }

    [Test]
    public void Result_BeErrorOfType_PassesForDerivedType()
    {
        Result result = Result.Error(new ArgumentNullException("sku"));

        result.Should().BeError<ArgumentException>();
    }

    [Test]
    public void Result_BeErrorOfType_FailsForWrongType()
    {
        Result result = Result.Error(new KeyNotFoundException("no such order"));

        var message = FailureOf(() => result.Should().BeError<InvalidOperationException>());

        Assert.That(message, Does.Contain("System.InvalidOperationException"));
        Assert.That(message, Does.Contain("System.Collections.Generic.KeyNotFoundException"));
    }

    [Test]
    public void Result_BeErrorWithPredicate_PassesWhenMatching()
    {
        Result result = Result.Error(new ArgumentException("bad sku", "sku"));

        result.Should().BeError<ArgumentException>(error => error.ParamName == "sku");
    }

    [Test]
    public void Result_BeErrorWithPredicate_FailsWhenNotMatching()
    {
        Result result = Result.Error(new ArgumentException("bad sku", "sku"));

        var message = FailureOf(() => result.Should().BeError<ArgumentException>(error => error.ParamName == "quantity"));

        Assert.That(message, Does.Contain("matching"));
        Assert.That(message, Does.Contain("error.ParamName == \"quantity\""));
    }

    [Test]
    public void Result_BeAggregateError_FailsForNonAggregate()
    {
        Result result = Result.Error(new InvalidOperationException("boom"));

        var message = FailureOf(() => result.Should().BeAggregateError());

        Assert.That(message, Does.Contain("aggregate error"));
    }

    [Test]
    public void ResultOfT_BeOk_ExposesTheValueThroughWhich()
    {
        var result = Result<int>.Ok(5);

        result.Should().BeOk().Which.Should().Be(5);
    }

    [Test]
    public void ResultOfT_BeOk_WhichFailureNamesTheValue()
    {
        var result = Result<int>.Ok(5);

        var message = FailureOf(() => result.Should().BeOk().Which.Should().Be(6));

        Assert.That(message, Does.Contain("result.Value"));
    }

    [Test]
    public void ResultOfT_BeOk_FailsForError()
    {
        var result = Result<int>.Error(new InvalidOperationException("out of stock"));

        var message = FailureOf(() => result.Should().BeOk());

        Assert.That(message, Does.Contain("out of stock"));
    }

    [Test]
    public void ResultOfT_BeOk_FailsForDefaultAndNamesTheState()
    {
        var message = FailureOf(() => default(Result<int>).Should().BeOk());

        Assert.That(message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void ResultOfT_BeOkWithValue_PassesAndFails()
    {
        var result = Result<string>.Ok("keyboard");

        result.Should().BeOkWithValue("keyboard");

        var message = FailureOf(() => result.Should().BeOkWithValue("mouse"));

        Assert.That(message, Does.Contain("\"mouse\""));
        Assert.That(message, Does.Contain("\"keyboard\""));
    }

    [Test]
    public void ResultOfT_BeOkWithValue_FailsForErrorAndDefault()
    {
        var error = Result<int>.Error(new InvalidOperationException("boom"));

        Assert.That(FailureOf(() => error.Should().BeOkWithValue(1)), Does.Contain("boom"));
        Assert.That(FailureOf(() => default(Result<int>).Should().BeOkWithValue(1)), Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void ResultOfT_BeOkMatching_PassesAndFails()
    {
        var result = Result<int>.Ok(12);

        result.Should().BeOkMatching(value => value > 10);

        var message = FailureOf(() => result.Should().BeOkMatching(value => value > 20));

        Assert.That(message, Does.Contain("value > 20"));
        Assert.That(message, Does.Contain("12"));
    }

    [Test]
    public void ResultOfT_BeOkSatisfying_RunsTheAssertionOnTheValue()
    {
        var result = Result<string>.Ok("keyboard");
        var seen = "";

        result.Should().BeOkSatisfying(value => seen = value);

        Assert.That(seen, Is.EqualTo("keyboard"));
        Assert.That(
            FailureOf(() => result.Should().BeOkSatisfying(value => value.Should().Be("mouse"))),
            Does.Contain("mouse"));
    }

    [Test]
    public void ResultOfT_BeOkSatisfying_DoesNotRunTheAssertionForDefault()
    {
        var calls = 0;

        var message = FailureOf(() => default(Result<int>).Should().BeOkSatisfying(_ => calls++));

        Assert.That(calls, Is.EqualTo(0));
        Assert.That(message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void ResultOfT_BeError_PassesForErrorAndForDefault()
    {
        var error = Result<int>.Error(new TimeoutException("slow"));

        error.Should().BeError<TimeoutException>().WithMessageContaining("slow");

        default(Result<int>).Should().BeError<InvalidOperationException>().WithMessageContaining("holds neither a value nor an error");
        default(Result<int>).Should().BeError();
        Assert.That(FailureOf(() => default(Result<int>).Should().BeError<TimeoutException>()), Does.Contain("holds neither a value nor an error"));
        Assert.That(FailureOf(() => default(Result<int>).Should().BeAggregateError()), Does.Contain("aggregate error"));
    }

    [Test]
    public void ResultOfT_BeErrorWithPredicate_RunsThePredicateForDefault()
    {
        Calls = 0;

        default(Result<int>).Should().BeError<Exception>(_ => Track());

        Assert.That(Calls, Is.EqualTo(1));
    }

    [Test]
    public void Failure_IncludesTheBecauseClause()
    {
        var message = FailureOf(() => Result<int>.Ok(1).Should().BeError("the stock is {0}", "gone"));

        Assert.That(message, Does.Contain("because the stock is gone"));
    }

    [Test]
    public void BeOkWithValue_WrongValue_IncludesTheBecauseClause()
    {
        var message = FailureOf(() => Result<int>.Ok(1).Should().BeOkWithValue(2, "the stock is {0}", "two"));

        Assert.That(message, Does.Contain("because the stock is two"));
        Assert.That(message, Does.Contain("but it was Ok with value 1"));
    }

    [Test]
    public void BeOkMatching_PredicateFails_IncludesTheBecauseClause()
    {
        var message = FailureOf(() => Result<int>.Ok(1).Should().BeOkMatching(value => value > 5, "the stock is {0}", "big"));

        Assert.That(message, Does.Contain("because the stock is big"));
        Assert.That(message, Does.Contain("but it was Ok with value 1"));
    }

    [Test]
    public void BeError_PredicateFails_IncludesTheBecauseClause()
    {
        var result = Result<int>.Error(new InvalidOperationException("payment declined"));

        var message = FailureOf(() => result.Should().BeError<InvalidOperationException>(error => error.Message == "other", "the stock is {0}", "gone"));

        Assert.That(message, Does.Contain("because the stock is gone"));
        Assert.That(message, Does.Contain("an error matching"));
    }

    [Test]
    public void IndependentAssertionsAfterAWhichChainStartFresh()
    {
        var first = Result<int>.Ok(1);
        var second = Result<int>.Ok(2);

        first.Should().BeOk();
        second.Should().BeOk().Which.Should().Be(2);

        var message = FailureOf(() => second.Should().BeOkWithValue(3));

        Assert.That(message, Does.Contain("second"));
    }
}
