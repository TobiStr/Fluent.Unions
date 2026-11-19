using System.ComponentModel.DataAnnotations;

namespace Fluent.Unions.Tests;

public class EnsureTests
{
    private static Result<int> Empty => default;

    [Test]
    public void PassingPredicateKeepsTheValue()
    {
        Result<int> result = Result<int>.Ok(5).Ensure(v => v > 3, "Too small.");

        Assert.That(result.GetOk(), Is.EqualTo(5));
    }

    [Test]
    public void FailingPredicateGivesValidationExceptionWithTheMessage()
    {
        Result<int> result = Result<int>.Ok(2).Ensure(v => v > 3, "Quantity must be above 3.");

        Assert.That(result.GetError(), Is.TypeOf<ValidationException>());
        Assert.That(result.Message, Is.EqualTo("Quantity must be above 3."));
    }

    [Test]
    public void FailingPredicateGivesTheSuppliedException()
    {
        Result<int> result = Result<int>.Ok(2).Ensure(v => v > 3, v => new ArgumentOutOfRangeException("quantity", v, "Too small."));

        var error = (ArgumentOutOfRangeException)result.GetError();
        Assert.That(error.ActualValue, Is.EqualTo(2));
    }

    [Test]
    public void ErrorPassesThroughWithoutCallingThePredicate()
    {
        var calls = 0;
        var failure = new InvalidOperationException("Out of stock.");

        Result<int> result = Result<int>.Error(failure).Ensure(v =>
        {
            calls++;
            return true;
        }, "Never used.");

        Assert.That(result.GetError(), Is.SameAs(failure));
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void EmptyPassesThroughWithoutCallingThePredicateOrTheErrorFactory()
    {
        var calls = 0;

        Result<int> result = Empty.Ensure(v =>
        {
            calls++;
            return false;
        }, v =>
        {
            calls++;
            return new Exception();
        });

        Assert.That(result.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void ThrowingPredicateBecomesTheError()
    {
        Result<int> result = Result<int>.Ok(1).Ensure(_ => throw new FormatException("Broken."), "Unused.");

        Assert.That(result.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public void ThrowingErrorFactoryBecomesTheError()
    {
        Result<int> result = Result<int>.Ok(1).Ensure(_ => false, _ => throw new FormatException("Broken."));

        Assert.That(result.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Ok(1).Ensure(null!, "Message."));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Ok(1).Ensure(_ => true, (string)null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Ok(1).Ensure(_ => true, (Func<int, Exception>)null!));
    }

    [Test]
    public async Task TaskFormsEnsureTheValue()
    {
        Result<int> passed = await Task.FromResult(Result<int>.Ok(5)).Ensure(v => v > 3, "Too small.");
        Result<int> failed = await Task.FromResult(Result<int>.Ok(2)).Ensure(v => v > 3, "Too small.");
        Result<int> custom = await Task.FromResult(Result<int>.Ok(2)).Ensure(v => v > 3, _ => new KeyNotFoundException());

        Assert.That(passed.GetOk(), Is.EqualTo(5));
        Assert.That(failed.GetError(), Is.TypeOf<ValidationException>());
        Assert.That(custom.GetError(), Is.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public async Task TaskFormTurnsAFaultedTaskIntoAnError()
    {
        Result<int> result = await Task.FromException<Result<int>>(new TimeoutException()).Ensure(_ => true, "Unused.");

        Assert.That(result.GetError(), Is.TypeOf<TimeoutException>());
    }

    [Test]
    public async Task TaskFormKeepsEmpty()
    {
        Result<int> result = await Task.FromResult(Empty).Ensure(_ => false, "Unused.");

        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public async Task EnsureAwaitChecksWithAnAsynchronousPredicate()
    {
        Result<int> passed = await Result<int>.Ok(5).EnsureAwait(v => Task.FromResult(v > 3), "Too small.");
        Result<int> failed = await Result<int>.Ok(2).EnsureAwait(v => Task.FromResult(v > 3), "Too small.");
        Result<int> custom = await Result<int>.Ok(2).EnsureAwait(v => Task.FromResult(v > 3), _ => new KeyNotFoundException());

        Assert.That(passed.GetOk(), Is.EqualTo(5));
        Assert.That(failed.GetError(), Is.TypeOf<ValidationException>());
        Assert.That(failed.Message, Is.EqualTo("Too small."));
        Assert.That(custom.GetError(), Is.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public async Task EnsureAwaitPassesErrorAndEmptyThroughWithoutCallingThePredicate()
    {
        var calls = 0;
        var failure = new InvalidOperationException("Out of stock.");

        Result<int> error = await Result<int>.Error(failure).EnsureAwait(_ =>
        {
            calls++;
            return Task.FromResult(true);
        }, "Unused.");
        Result<int> empty = await Empty.EnsureAwait(_ =>
        {
            calls++;
            return Task.FromResult(true);
        }, "Unused.");

        Assert.That(error.GetError(), Is.SameAs(failure));
        Assert.That(empty.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public async Task EnsureAwaitCapturesAThrowingPredicate()
    {
        Result<int> thrown = await Result<int>.Ok(1).EnsureAwait(_ => throw new FormatException(), "Unused.");
        Result<int> faulted = await Result<int>.Ok(1).EnsureAwait(
            _ => Task.FromException<bool>(new TimeoutException()), "Unused.");

        Assert.That(thrown.GetError(), Is.TypeOf<FormatException>());
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
    }

    [Test]
    public async Task EnsureAwaitOnATaskReceiver()
    {
        Result<int> passed = await Task.FromResult(Result<int>.Ok(5)).EnsureAwait(v => Task.FromResult(v > 3), "Too small.");
        Result<int> failed = await Task.FromResult(Result<int>.Ok(2)).EnsureAwait(v => Task.FromResult(v > 3), "Too small.");
        Result<int> custom = await Task.FromResult(Result<int>.Ok(2)).EnsureAwait(
            v => Task.FromResult(v > 3), _ => new KeyNotFoundException());
        Result<int> empty = await Task.FromResult(Empty).EnsureAwait(_ => Task.FromResult(false), "Unused.");
        Result<int> faulted = await Task.FromException<Result<int>>(new TimeoutException())
            .EnsureAwait(_ => Task.FromResult(true), "Unused.");

        Assert.That(passed.GetOk(), Is.EqualTo(5));
        Assert.That(failed.GetError(), Is.TypeOf<ValidationException>());
        Assert.That(custom.GetError(), Is.TypeOf<KeyNotFoundException>());
        Assert.That(empty.Value, Is.Null);
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
    }
}
