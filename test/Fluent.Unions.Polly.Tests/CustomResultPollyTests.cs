using NUnit.Framework;
using Polly;
using Polly.Retry;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Polly;

namespace Fluent.Unions.Polly.Tests;

public record Transient(string Reason);

public record Permanent(string Reason);

public record Unexpected(Exception Exception);

public union CallError(Transient, Permanent, Unexpected) : IFromException<CallError>
{
    public static CallError FromException(Exception exception) => new Unexpected(exception);
}

public union CallResult<T>(T, CallError) : IResult<CallResult<T>, T, CallError>
{
    public static CallResult<T> FromValue(T value) => value;

    public static CallResult<T> FromError(CallError error) => error;

    public static CallError FromException(Exception exception) => new Unexpected(exception);
}

public class CustomResultPollyTests
{
    private static CallResult<int> Ok(int value) => value;

    private static CallResult<int> Failed(string reason) => new CallError(new Transient(reason));

    private static CallResult<int> Rejected(string reason) => new CallError(new Permanent(reason));

    private static ResiliencePipeline<CallResult<int>> RetryPipeline(PredicateBuilder<CallResult<int>> shouldHandle, int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder<CallResult<int>>()
            .AddRetry(new RetryStrategyOptions<CallResult<int>>
            {
                ShouldHandle = shouldHandle,
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.Zero,
            })
            .Build();
    }

    private static Exception ExceptionOf(CallResult<int> result) => ((Unexpected)((CallError)result.Value!).Value!).Exception;

    [Test]
    public async Task HandleError_RetriesADataErrorUpToTheMaximum()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<CallResult<int>>().HandleError<CallResult<int>, CallError>());

        var result = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Failed("offline"));
        });

        Assert.That(attempts, Is.EqualTo(4));
        Assert.That(((CallError)result.Value!).Value, Is.EqualTo(new Transient("offline")));
    }

    [Test]
    public async Task HandleError_RetriesUntilTheOperationSucceeds()
    {
        var attempts = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<CallResult<int>>().HandleError<CallResult<int>, CallError>());

        var result = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(attempts < 3 ? Failed("offline") : Ok(7));
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.Value, Is.EqualTo(7));
    }

    [Test]
    public async Task HandleError_WithPredicate_RetriesOnlyTheMatchingCase()
    {
        var transientAttempts = 0;
        var permanentAttempts = 0;
        var pipeline = RetryPipeline(
            new PredicateBuilder<CallResult<int>>().HandleError<CallResult<int>, CallError>(error => error.Value is Transient),
            2);

        await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            transientAttempts++;
            return ValueTask.FromResult(Failed("offline"));
        });
        var rejected = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            permanentAttempts++;
            return ValueTask.FromResult(Rejected("bad input"));
        });

        Assert.That(transientAttempts, Is.EqualTo(3));
        Assert.That(permanentAttempts, Is.EqualTo(1));
        Assert.That(((CallError)rejected.Value!).Value, Is.EqualTo(new Permanent("bad input")));
    }

    [Test]
    public async Task HandleError_DoesNotRetryOkOrEmptyAndDoesNotCallThePredicate()
    {
        var attempts = 0;
        var predicateCalls = 0;
        var pipeline = RetryPipeline(new PredicateBuilder<CallResult<int>>().HandleError<CallResult<int>, CallError>(_ =>
        {
            predicateCalls++;
            return true;
        }));

        var ok = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Ok(5));
        });
        var empty = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            attempts++;
            return ValueTask.FromResult(default(CallResult<int>));
        });

        Assert.That(attempts, Is.EqualTo(2));
        Assert.That(predicateCalls, Is.EqualTo(0));
        Assert.That(ok.Value, Is.EqualTo(5));
        Assert.That(empty.Value, Is.Null);
    }

    [Test]
    public void HandleError_NullPredicate_Throws()
    {
        var builder = new PredicateBuilder<CallResult<int>>();

        Assert.Throws<ArgumentNullException>(() => builder.HandleError<CallResult<int>, CallError>(null!));
    }

    [Test]
    public async Task HandleError_WorksForResultOfTError()
    {
        var attempts = 0;
        var pipeline = new ResiliencePipelineBuilder<Result<int, CallError>>()
            .AddRetry(new RetryStrategyOptions<Result<int, CallError>>
            {
                ShouldHandle = new PredicateBuilder<Result<int, CallError>>().HandleError<Result<int, CallError>, CallError>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
            })
            .Build();

        var result = await pipeline.ExecuteResultAsync<Result<int, CallError>, CallError>(_ =>
        {
            attempts++;
            return ValueTask.FromResult((Result<int, CallError>)new CallError(new Transient("offline")));
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(result.GetError().Value, Is.EqualTo(new Transient("offline")));
    }

    [Test]
    public async Task ExecuteResultAsync_ValueErrorAndEmptyPassThrough()
    {
        var pipeline = ResiliencePipeline<CallResult<int>>.Empty;

        var ok = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ => ValueTask.FromResult(Ok(3)));
        var error = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ => ValueTask.FromResult(Rejected("no")));
        var empty = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ => ValueTask.FromResult(default(CallResult<int>)));

        Assert.That(ok.Value, Is.EqualTo(3));
        Assert.That(((CallError)error.Value!).Value, Is.EqualTo(new Permanent("no")));
        Assert.That(empty.Value, Is.Null);
    }

    [Test]
    public async Task ExecuteResultAsync_ThrowingOperation_BecomesTheErrorFromFromException()
    {
        var failure = new InvalidOperationException("boom");

        var result = await ResiliencePipeline<CallResult<int>>.Empty.ExecuteResultAsync<CallResult<int>, CallError>(_ => throw failure);

        Assert.That(ExceptionOf(result), Is.SameAs(failure));
    }

    [Test]
    public async Task ExecuteResultAsync_CancelledToken_BecomesCancellationError()
    {
        var pipeline = RetryPipeline(new PredicateBuilder<CallResult<int>>().HandleError<CallResult<int>, CallError>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(
            token =>
            {
                token.ThrowIfCancellationRequested();
                return ValueTask.FromResult(Ok(1));
            },
            cancellation.Token);

        Assert.That(ExceptionOf(result), Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task ExecuteResultAsync_PassesTheTokenToTheOperation()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken received = default;

        await ResiliencePipeline<CallResult<int>>.Empty.ExecuteResultAsync<CallResult<int>, CallError>(
            token =>
            {
                received = token;
                return ValueTask.FromResult(Ok(1));
            },
            cancellation.Token);

        Assert.That(received, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task ExecuteResultAsync_NonGenericPipeline_PassesThroughAndCapturesThrows()
    {
        var ok = await ResiliencePipeline.Empty.ExecuteResultAsync<CallResult<int>, CallError>(_ => ValueTask.FromResult(Ok(9)));
        var empty = await ResiliencePipeline.Empty.ExecuteResultAsync<CallResult<int>, CallError>(_ => ValueTask.FromResult(default(CallResult<int>)));
        var thrown = await ResiliencePipeline.Empty.ExecuteResultAsync<CallResult<int>, CallError>(_ => throw new TimeoutException());

        Assert.That(ok.Value, Is.EqualTo(9));
        Assert.That(empty.Value, Is.Null);
        Assert.That(ExceptionOf(thrown), Is.InstanceOf<TimeoutException>());
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

        var result = await pipeline.ExecuteResultAsync<CallResult<int>, CallError>(_ =>
        {
            attempts++;
            throw new TimeoutException();
        });

        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(ExceptionOf(result), Is.InstanceOf<TimeoutException>());
    }

    [Test]
    public void ToResult_OutcomeWithException_BecomesTheErrorFromFromException()
    {
        var failure = new InvalidOperationException("failed");

        var result = Outcome.FromException<CallResult<int>>(failure).ToResult<CallResult<int>, CallError>();

        Assert.That(ExceptionOf(result), Is.SameAs(failure));
    }

    [Test]
    public void ToResult_OutcomeWithResult_GivesTheResult()
    {
        var ok = Outcome.FromResult(Ok(4)).ToResult<CallResult<int>, CallError>();
        var error = Outcome.FromResult(Rejected("no")).ToResult<CallResult<int>, CallError>();
        var empty = Outcome.FromResult(default(CallResult<int>)).ToResult<CallResult<int>, CallError>();

        Assert.That(ok.Value, Is.EqualTo(4));
        Assert.That(((CallError)error.Value!).Value, Is.EqualTo(new Permanent("no")));
        Assert.That(empty.Value, Is.Null);
    }

    [Test]
    public async Task ExistingOverloadsStillWinForResultOfT()
    {
        var pipeline = new ResiliencePipelineBuilder<Result<int>>()
            .AddRetry(new RetryStrategyOptions<Result<int>>
            {
                ShouldHandle = new PredicateBuilder<Result<int>>().HandleError(error => error is TimeoutException),
                MaxRetryAttempts = 1,
                Delay = TimeSpan.Zero,
            })
            .Build();

        var result = await pipeline.ExecuteResultAsync(_ => ValueTask.FromResult(Result<int>.Error(new TimeoutException())));

        Assert.That(result.Value, Is.InstanceOf<TimeoutException>());
        Assert.That(Outcome.FromResult(Result<int>.Ok(1)).ToResult().GetOk(), Is.EqualTo(1));
    }
}
