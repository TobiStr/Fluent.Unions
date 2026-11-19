using NUnit.Framework;
using Polly;
using Polly.Retry;
using Fluent.Unions.Polly;

namespace Fluent.Unions.Polly.Tests;

public class ResiliencePipelineTests
{
    [Test]
    public async Task ExecuteResultAsync_OkResult_PassesThrough()
    {
        var result = await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync(_ => ValueTask.FromResult(Result<int>.Ok(3)));

        Assert.That(result.GetOk(), Is.EqualTo(3));
    }

    [Test]
    public async Task ExecuteResultAsync_EmptyResult_StaysEmpty()
    {
        var result = await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync(_ => ValueTask.FromResult(default(Result<int>)));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task ExecuteResultAsync_ThrowingOperation_BecomesError()
    {
        var result = await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync(
            _ => throw new InvalidOperationException("boom"));

        Assert.That(result.Value, Is.InstanceOf<InvalidOperationException>());
        Assert.That(result.GetError().Message, Is.EqualTo("boom"));
    }

    [Test]
    public async Task ExecuteResultAsync_CancelledToken_BecomesCancellationError()
    {
        var pipeline = new ResiliencePipelineBuilder<Result<int>>()
            .AddRetry(new RetryStrategyOptions<Result<int>>
            {
                ShouldHandle = new PredicateBuilder<Result<int>>().HandleError(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero,
            })
            .Build();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await pipeline.ExecuteResultAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                return ValueTask.FromResult(Result<int>.Ok(1));
            },
            cancellation.Token);

        Assert.That(result.Value, Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task ExecuteResultAsync_PassesTheTokenToTheOperation()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken received = default;

        var result = await ResiliencePipeline<Result<int>>.Empty.ExecuteResultAsync(
            token =>
            {
                received = token;
                return ValueTask.FromResult(Result<int>.Ok(1));
            },
            cancellation.Token);

        Assert.That(result.Success, Is.True);
        Assert.That(received, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task ExecuteResultAsync_ThrowingOperation_IsRetriedByAPollyHandler()
    {
        var attempts = 0;
        var pipeline = new ResiliencePipelineBuilder<Result<int>>()
            .AddRetry(new RetryStrategyOptions<Result<int>>
            {
                ShouldHandle = new PredicateBuilder<Result<int>>().Handle<InvalidOperationException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
            })
            .Build();

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            throw new InvalidOperationException();
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.Value, Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public async Task ExecuteResultAsync_NonGenericPipeline_PassesThroughAndCapturesThrows()
    {
        var ok = await ResiliencePipeline.Empty.ExecuteResultAsync(_ => ValueTask.FromResult(Result<int>.Ok(9)));
        var empty = await ResiliencePipeline.Empty.ExecuteResultAsync(_ => ValueTask.FromResult(default(Result<int>)));
        var thrown = await ResiliencePipeline.Empty.ExecuteResultAsync<int>(_ => throw new TimeoutException());

        Assert.That(ok.GetOk(), Is.EqualTo(9));
        Assert.That(empty.Value, Is.Null);
        Assert.That(thrown.Value, Is.InstanceOf<TimeoutException>());
    }

    [Test]
    public async Task ExecuteResultAsync_NonGenericPipeline_RetriesAThrownException()
    {
        var attempts = 0;
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
            })
            .Build();

        var result = await pipeline.ExecuteResultAsync<int>(_ =>
        {
            attempts++;
            throw new TimeoutException();
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.Value, Is.InstanceOf<TimeoutException>());
    }

    [Test]
    public void ToResult_OutcomeWithException_BecomesError()
    {
        var outcome = Outcome.FromException<Result<int>>(new InvalidOperationException("failed"));

        var result = outcome.ToResult();

        Assert.That(result.Value, Is.InstanceOf<InvalidOperationException>());
        Assert.That(result.GetError().Message, Is.EqualTo("failed"));
    }

    [Test]
    public void ToResult_OutcomeWithOkResult_GivesTheResult()
    {
        var result = Outcome.FromResult(Result<int>.Ok(4)).ToResult();

        Assert.That(result.GetOk(), Is.EqualTo(4));
    }

    [Test]
    public void ToResult_OutcomeWithErrorResult_KeepsTheError()
    {
        var error = new ArgumentException("bad");

        var result = Outcome.FromResult(Result<int>.Error(error)).ToResult();

        Assert.That(result.GetError(), Is.SameAs(error));
    }

    [Test]
    public void ToResult_OutcomeWithEmptyResult_IsEmpty()
    {
        var result = Outcome.FromResult(default(Result<int>)).ToResult();

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Success, Is.False);
    }
}
