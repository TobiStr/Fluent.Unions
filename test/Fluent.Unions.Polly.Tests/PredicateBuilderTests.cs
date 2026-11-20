using NUnit.Framework;
using Polly;
using Polly.Retry;
using Fluent.Unions.Polly;

namespace Fluent.Unions.Polly.Tests;

public class PredicateBuilderTests
{
    private static ResiliencePipeline<Result<int>> RetryPipeline(PredicateBuilder<Result<int>> shouldHandle, int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder<Result<int>>()
            .AddRetry(new RetryStrategyOptions<Result<int>>
            {
                ShouldHandle = shouldHandle,
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.Zero,
            })
            .Build();
    }

    [Test]
    public async Task HandleError_RetriesAnErrorUpToTheMaximum()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError());

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Result<int>.Error(new InvalidOperationException("offline")));
        });

        Assert.That(attempts, Is.EqualTo(4));
        Assert.That(result.Value, Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public async Task HandleError_RetriesUntilTheOperationSucceeds()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError());

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(attempts < 3 ? Result<int>.Error(new InvalidOperationException()) : Result<int>.Ok(7));
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.GetValue(), Is.EqualTo(7));
    }

    [Test]
    public async Task HandleError_WithPredicate_StopsWhenThePredicateIsFalse()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError(error => error is TimeoutException));

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Result<int>.Error(new ArgumentException("bad input")));
        });

        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(result.Value, Is.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task HandleError_WithPredicate_RetriesWhenThePredicateIsTrue()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError(error => error is TimeoutException), 2);

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Result<int>.Error(new TimeoutException()));
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.Value, Is.InstanceOf<TimeoutException>());
    }

    [Test]
    public async Task HandleError_DoesNotRetryAnOkResult()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError());

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Result<int>.Ok(5));
        });

        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(result.GetValue(), Is.EqualTo(5));
    }

    [Test]
    public async Task HandleError_RetriesADefaultResultAsAnError()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError());

        var result = await pipeline.ExecuteResultAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(default(Result<int>));
        });

        Assert.That(attempts, Is.EqualTo(4));
        Assert.That(result.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task HandleError_WithPredicate_CallsThePredicateForDefaultButNotForOk()
    {
        var predicateCalls = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<Result<int>>().HandleError(_ =>
        {
            predicateCalls++;
            return true;
        }));

        var ok = await pipeline.ExecuteResultAsync(_ => ValueTask.FromResult(Result<int>.Ok(1)));
        var empty = await pipeline.ExecuteResultAsync(_ => ValueTask.FromResult(default(Result<int>)));

        Assert.That(predicateCalls, Is.EqualTo(4));
        Assert.That(ok.IsSuccess, Is.True);
        Assert.That(empty.IsError, Is.True);
    }

    [Test]
    public void HandleError_NullPredicate_Throws()
    {
        var builder = new PredicateBuilder<Result<int>>();

        Assert.Throws<ArgumentNullException>(() => builder.HandleError(null!));
    }
}
