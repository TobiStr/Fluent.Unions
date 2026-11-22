using Fluent.Unions.Abstractions;
using Is = NUnit.Framework.Is;

namespace OwnResultSample.Tests;

[FluentResult<DomainException>]
public readonly partial struct StrictResult<T>
{
    static bool IErrorResult<StrictResult<T>, DomainException>.Captures(Exception exception) => exception is not NullReferenceException;
}

[FluentResult<DomainException>(Capture = ExceptionCapture.Declared)]
public readonly partial struct DeclaredResult<T>;

[FluentResult<DomainException>(Propagate = [typeof(NullReferenceException), typeof(ArgumentException)])]
public readonly partial struct GuardedResult<T>;

public class CapturePolicyTests
{
    [Test]
    public void DeclaredPolicyCapturesTheErrorTypeAndItsSubtypes()
    {
        DeclaredResult<int> one = 1;

        var failed = one.Select(_ => ThrowNotFound());

        Assert.That(failed.GetError(), Is.TypeOf<NotFoundException>());
    }

    [Test]
    public void DeclaredPolicyLetsUndeclaredExceptionsEscape()
    {
        DeclaredResult<int> one = 1;

        Assert.Throws<TimeoutException>(() => one.Select(_ => ThrowTimeout()));
        Assert.Throws<NullReferenceException>(() => DeclaredResult.Try(() => ThrowNullReference()));
    }

    [Test]
    public void PropagateListLetsTheListedTypesAndTheirSubtypesEscape()
    {
        GuardedResult<int> one = 1;

        Assert.Throws<NullReferenceException>(() => one.Select(_ => ThrowNullReference()));
        Assert.Throws<ArgumentNullException>(() => one.Select(_ => ThrowArgumentNull()));
        Assert.That(one.Select(_ => ThrowTimeout()).GetError(), Is.TypeOf<UnexpectedDomainException>());
    }

    private static readonly StrictResult<int> One = 1;

    [Test]
    public void ExcludedExceptionEscapesSelectWithItsOriginalStackTrace()
    {
        var thrown = Assert.Throws<NullReferenceException>(() => One.Select(_ => ThrowNullReference()));

        Assert.That(thrown!.StackTrace, Does.Contain(nameof(ThrowNullReference)));
    }

    [Test]
    public void CapturedExceptionStillBecomesAnError()
    {
        var failed = One.Select(_ => ThrowTimeout());

        Assert.That(failed.GetError(), Is.TypeOf<UnexpectedDomainException>());
        Assert.That(failed.GetError().InnerException, Is.TypeOf<TimeoutException>());
    }

    [Test]
    public void ExcludedExceptionFaultsTheTaskOfSelectAwait()
    {
        Assert.ThrowsAsync<NullReferenceException>(() => One.SelectAwait(async _ =>
        {
            await Task.Yield();
            return ThrowNullReference();
        }));
    }

    [Test]
    public void ExcludedExceptionEscapesDoAndChain()
    {
        Assert.Throws<NullReferenceException>(() => One.Do(_ => ThrowNullReference()));
        Assert.Throws<NullReferenceException>(() => One.Chain(_ => (StrictResult<int>)ThrowNullReference()));
    }

    [Test]
    public void ExcludedExceptionFromAFaultedTaskEscapes()
    {
        var faulted = Task.FromException<StrictResult<int>>(new NullReferenceException());

        Assert.ThrowsAsync<NullReferenceException>(() => faulted.Select(value => value + 1));
    }

    [Test]
    public void ExcludedExceptionEscapesRetryAsync()
    {
        var attempts = 0;

        Assert.ThrowsAsync<NullReferenceException>(() => StrictResult.RetryAsync<int>(_ =>
        {
            attempts++;
            throw new NullReferenceException();
        }, maxAttempts: 3));
        Assert.That(attempts, Is.EqualTo(1));
    }

    [Test]
    public void CancellationStillPropagates()
    {
        Assert.Throws<OperationCanceledException>(() => One.Select(_ => ThrowCancelled()));
    }

    [Test]
    public void EnsureMessageStillBecomesAnErrorUnderAStrictPolicy()
    {
        var failed = One.Ensure(value => value > 5, "Too small.");

        Assert.That(failed.GetError().Message, Is.EqualTo("Too small."));
    }

    private static int ThrowNullReference() => throw new NullReferenceException("Bug.");

    private static int ThrowTimeout() => throw new TimeoutException("Slow.");

    private static int ThrowNotFound() => throw new NotFoundException("No order.");

    private static int ThrowArgumentNull() => throw new ArgumentNullException("id");

    private static int ThrowCancelled() => throw new OperationCanceledException();
}
