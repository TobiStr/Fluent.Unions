using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using Fluent.Unions.AwesomeAssertions;
using Fluent.Unions.Extensions;
using Fluent.Unions.Validation;

namespace Fluent.Unions.AwesomeAssertions.Tests;

public class ErrorAssertionsTests
{
    private sealed record Customer(string Name, string Email);

    private static string FailureOf(Action action) => Assert.Catch<Exception>(() => action())!.Message;

    private static Result<Customer> InvalidCustomer()
    {
        return Result<Customer>.Ok(new Customer("", "not-an-email"))
            .Validate(c => c.Name.EnsureNotEmpty(), c => c.Email.EnsureMatchesEmail());
    }

    [Test]
    public void WithMessage_MatchesWildcardsIgnoringCase()
    {
        var result = Result<int>.Error(new InvalidOperationException("Order 1042 was not found"));

        result.Should().BeError<InvalidOperationException>().WithMessage("order * was NOT found");
        result.Should().BeError().WithMessage("Order ???? was not found");
    }

    [Test]
    public void WithMessage_FailsAndShowsTheActualMessage()
    {
        var result = Result<int>.Error(new InvalidOperationException("Order 1042 was not found"));

        var message = FailureOf(() => result.Should().BeError<InvalidOperationException>().WithMessage("Customer *", "of the lookup"));

        Assert.That(message, Does.Contain("to match \"Customer *\" because of the lookup"));
        Assert.That(message, Does.Contain("Order 1042 was not found"));
    }

    [Test]
    public void WithMessage_TreatsRegexCharactersLiterally()
    {
        var result = Result<int>.Error(new InvalidOperationException("price (EUR) [net]"));

        result.Should().BeError().WithMessage("price (EUR) [net]");
    }

    [Test]
    public void WithMessageContaining_PassesAndFails()
    {
        var result = Result<int>.Error(new InvalidOperationException("Order 1042 was not found"));

        result.Should().BeError().WithMessageContaining("1042");

        var message = FailureOf(() => result.Should().BeError().WithMessageContaining("2024"));

        Assert.That(message, Does.Contain("to contain \"2024\""));
    }

    [Test]
    public void ChainedAssertions_ContinueThroughAnd()
    {
        var result = Result<int>.Error(new InvalidOperationException("Order 1042 was not found"));

        result.Should().BeError<InvalidOperationException>()
            .WithMessage("Order *")
            .And.WithMessageContaining("1042");
    }

    [Test]
    public void Which_ReturnsTheException()
    {
        var failure = new InvalidOperationException("boom");
        var result = Result<int>.Error(failure);

        var exception = result.Should().BeError<InvalidOperationException>().Which;

        Assert.That(exception, NUnit.Framework.Is.SameAs(failure));
    }

    [Test]
    public void ErrorAssertions_InsideAScope_DoNotThrowWhenTheStateIsWrong()
    {
        var message = FailureOf(() =>
        {
            using var scope = new AssertionScope();

            Result<int>.Ok(1).Should().BeError<InvalidOperationException>().WithMessage("boom");
        });

        Assert.That(message, Does.Contain("to be an error of type System.InvalidOperationException"));
    }

    [Test]
    public void BeAggregateError_ChecksTheInnerCountOfValidate()
    {
        var result = InvalidCustomer();

        result.Should().BeAggregateError().WithInnerCount(2);
    }

    [Test]
    public void WithInnerCount_FailsAndListsTheInnerExceptions()
    {
        var result = InvalidCustomer();

        var message = FailureOf(() => result.Should().BeAggregateError().WithInnerCount(3));

        Assert.That(message, Does.Contain("to have 3 inner exception(s)"));
        Assert.That(message, Does.Contain("but it had 2"));
        Assert.That(message, Does.Contain("System.ComponentModel.DataAnnotations.ValidationException"));
    }

    [Test]
    public void WithInner_ReturnsTheFirstMatch()
    {
        var result = InvalidCustomer();

        var inner = result.Should().BeAggregateError().WithInner<ValidationException>().Which;

        Assert.That(inner, NUnit.Framework.Is.SameAs(((AggregateException)result.GetError()).Flatten().InnerExceptions[0]));
    }

    [Test]
    public void WithInner_FailsForAMissingType()
    {
        var result = InvalidCustomer();

        var message = FailureOf(() => result.Should().BeAggregateError().WithInner<TimeoutException>());

        Assert.That(message, Does.Contain("inner exception of type System.TimeoutException"));
    }

    [Test]
    public void WithInnerMessage_MatchesAnyInnerException()
    {
        var failures = new AggregateException(new InvalidOperationException("stock is gone"), new TimeoutException("payment timed out"));
        var result = Result<int>.Error(failures);

        result.Should().BeAggregateError().WithInnerMessage("payment *");

        var message = FailureOf(() => result.Should().BeAggregateError().WithInnerMessage("shipping *"));

        Assert.That(message, Does.Contain("matching \"shipping *\""));
        Assert.That(message, Does.Contain("payment timed out"));
    }

    [Test]
    public void AggregateAssertions_FlattenNestedAggregates()
    {
        var nested = new AggregateException(new InvalidOperationException("a"), new AggregateException(new TimeoutException("b")));
        var result = Result<int>.Error(nested);

        result.Should().BeAggregateError().WithInnerCount(2);
    }

    [Test]
    public void BeAggregateError_FailsForOkAndForPlainErrors()
    {
        Assert.That(FailureOf(() => Result<int>.Ok(1).Should().BeAggregateError()), Does.Contain("but it was Ok"));
        Assert.That(
            FailureOf(() => Result<int>.Error(new InvalidOperationException("boom")).Should().BeAggregateError()),
            Does.Contain("System.InvalidOperationException"));
    }
}
