using Fluent.Unions.Abstractions;
using Is = NUnit.Framework.Is;

namespace OwnResultSample.Tests;

public sealed class BrokenMappingError : IFromException<BrokenMappingError>
{
    public static readonly InvalidOperationException MappingFailure = new("The mapping failed.");

    public static BrokenMappingError FromException(Exception exception) => Fail(exception);

    private static BrokenMappingError Fail(Exception exception) => throw MappingFailure;
}

[FluentResult<BrokenMappingError>]
public readonly partial struct BrokenMappingResult<T>;

public sealed class NullMappingError : IFromException<NullMappingError>
{
    public static NullMappingError FromException(Exception exception) => Nothing();

    private static NullMappingError Nothing() => null!;
}

[FluentResult<NullMappingError>]
public readonly partial struct NullMappingResult<T>;

public sealed class CountingMappingError(Exception exception) : IFromException<CountingMappingError>
{
    public static int Calls;

    public Exception Exception { get; } = exception;

    public static CountingMappingError FromException(Exception exception)
    {
        Interlocked.Increment(ref Calls);
        return new CountingMappingError(exception);
    }
}

[FluentResult<CountingMappingError>]
public readonly partial struct CountingMappingResult<T>;

public class ExceptionMappingTests
{
    private static readonly ArgumentException Original = new("Original failure.");

    [Test]
    public void FromExceptionThatThrowsEscapesWithTheOriginalAndTheMappingFailure()
    {
        BrokenMappingResult<int> result = 1;

        var thrown = Assert.Throws<InvalidOperationException>(() => result.Select(_ => ThrowOriginal()));

        Assert.That(thrown!.Message, Does.Contain("FromException threw while mapping ArgumentException"));
        var inner = (AggregateException)thrown.InnerException!;
        Assert.That(inner.InnerExceptions, Is.EqualTo(new Exception[] { Original, BrokenMappingError.MappingFailure }));
    }

    [Test]
    public void FromExceptionThatReturnsNullThrowsWithTheOriginalAsInner()
    {
        NullMappingResult<int> result = 1;

        var thrown = Assert.Throws<InvalidOperationException>(() => result.Select(_ => ThrowOriginal()));

        Assert.That(thrown!.Message, Does.Contain("FromException returned null"));
        Assert.That(thrown.InnerException, Is.SameAs(Original));
    }

    [Test]
    public void FromExceptionIsNotCalledForCancellation()
    {
        CountingMappingResult<int> result = 1;
        var before = CountingMappingError.Calls;

        Assert.Throws<OperationCanceledException>(() => result.Select(_ => ThrowCancelled()));

        Assert.That(CountingMappingError.Calls, Is.EqualTo(before));
    }

    [Test]
    public void FromExceptionReceivesTheOriginalExceptionWithItsStackTrace()
    {
        CountingMappingResult<int> result = 1;

        var failed = result.Select(_ => Throw());

        Assert.That(failed.GetError().Exception, Is.TypeOf<ArgumentException>());
        Assert.That(failed.GetError().Exception.StackTrace, Does.Contain(nameof(Throw)));
    }

    private static int Throw() => throw new ArgumentException("Thrown here.");

    private static int ThrowOriginal() => throw Original;

    private static int ThrowCancelled() => throw new OperationCanceledException();
}
