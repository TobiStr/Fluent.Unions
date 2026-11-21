using Fluent.Unions;
using Fluent.Unions.Polly;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace PollyDomain.Tests;

public class GeneratedResultPollyTests
{
    private static Result<int> Failed(string reason) => new TransientException(reason);

    private static Result<int> Rejected(string reason) => new PermanentException(reason);

    private static ResiliencePipeline<Result<int>> RetryPipeline(PredicateBuilder<Result<int>> shouldHandle, int maxRetryAttempts = 3) =>
        new ResiliencePipelineBuilder<Result<int>>()
            .AddRetry(new RetryStrategyOptions<Result<int>>
            {
                ShouldHandle = shouldHandle,
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.Zero,
            })
            .Build();

    [Test]
    public async Task HandleError_RetriesAnErrorUpToTheMaximum()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError<Result<int>, DomainException>());

        var result = await pipeline.ExecuteResultAsync<Result<int>, DomainException>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Failed("offline"));
        });

        Assert.That(attempts, Is.EqualTo(4));
        Assert.That(result.GetError(), Is.TypeOf<TransientException>());
    }

    [Test]
    public async Task HandleError_RetriesUntilTheOperationSucceeds()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError<Result<int>, DomainException>());

        var result = await pipeline.ExecuteResultAsync<Result<int>, DomainException>(_ =>
            ValueTask.FromResult(++attempts < 3 ? Failed("offline") : (Result<int>)42));

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.GetValue(), Is.EqualTo(42));
    }

    [Test]
    public async Task HandleError_WithPredicate_RetriesOnlyTheMatchingError()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError<Result<int>, DomainException>(error => error is TransientException));

        var result = await pipeline.ExecuteResultAsync<Result<int>, DomainException>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Rejected("no"));
        });

        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(result.GetError(), Is.TypeOf<PermanentException>());
    }

    [Test]
    public async Task HandleError_RetriesADefaultResultAsAnErrorButNotAValue()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError<Result<int>, DomainException>(), maxRetryAttempts: 2);

        await pipeline.ExecuteResultAsync<Result<int>, DomainException>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(default(Result<int>));
        });
        var valueAttempts = 0;
        await pipeline.ExecuteResultAsync<Result<int>, DomainException>(_ =>
        {
            valueAttempts++;
            return ValueTask.FromResult((Result<int>)1);
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(valueAttempts, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteResultAsync_ThrowingOperation_BecomesTheErrorFromFromException()
    {
        var failure = new InvalidOperationException("boom");

        var result = await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync<Result<int>, DomainException>(_ => throw failure);

        Assert.That(result.GetError(), Is.TypeOf<DomainException>());
        Assert.That(result.GetError().InnerException, Is.SameAs(failure));
    }

    [Test]
    public async Task ExecuteResultAsync_ThrownDomainException_IsKept()
    {
        var failure = new PermanentException("no");

        var result = await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync<Result<int>, DomainException>(_ => throw failure);

        Assert.That(result.GetError(), Is.SameAs(failure));
    }

    [Test]
    public void ExecuteResultAsync_CancelledToken_Throws()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync<Result<int>, DomainException>(
                token =>
                {
                    token.ThrowIfCancellationRequested();
                    return ValueTask.FromResult((Result<int>)1);
                },
                cancellation.Token));
    }

    [Test]
    public async Task ExecuteResultAsync_NonGenericPipeline_RetriesAThrownException()
    {
        var attempts = 0;
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 2, Delay = TimeSpan.Zero })
            .Build();

        var result = await pipeline.ExecuteResultAsync<Result<int>, DomainException>(_ =>
        {
            attempts++;
            return attempts < 3 ? throw new IOException("flaky") : ValueTask.FromResult((Result<int>)7);
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.GetValue(), Is.EqualTo(7));
    }

    [Test]
    public void ToResult_OutcomeWithException_BecomesTheErrorFromFromException()
    {
        var failure = new IOException("disk");

        var result = Outcome.FromException<Result<int>>(failure).ToResult<Result<int>, DomainException>();

        Assert.That(result.GetError().InnerException, Is.SameAs(failure));
    }
}
