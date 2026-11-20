namespace Fluent.Unions.Tests;

public class CancellationTests
{
    private static readonly OperationCanceledException Cancelled = new();

    [Test]
    public void SelectPropagatesCancellation()
    {
        Result<int> result = 1;

        Assert.Throws<OperationCanceledException>(() => result.Select<int, int>(_ => throw Cancelled));
    }

    [Test]
    public void ChainPropagatesCancellation()
    {
        Result<int> result = 1;

        Assert.Throws<OperationCanceledException>(() => result.Chain(Result<int> (_) => throw Cancelled));
    }

    [Test]
    public void TryPropagatesCancellation()
    {
        Assert.Throws<OperationCanceledException>(() => Result.Try(() => Cancel()));
        Assert.Throws<OperationCanceledException>(() => Result.Try(() => { throw Cancelled; }));
    }

    [Test]
    public void TryStillCapturesOtherExceptions()
    {
        var failure = new InvalidOperationException("Boom");

        var result = Result.Try<int>(() => throw failure);

        Assert.That(result.GetError(), Is.SameAs(failure));
    }

    [Test]
    public void TryAsyncPropagatesCancellation()
    {
        Assert.CatchAsync<OperationCanceledException>(async () => await Result.TryAsync(async () =>
        {
            await Task.Yield();
            throw Cancelled;
        }));
        Assert.CatchAsync<OperationCanceledException>(async () => await Result.TryAsync<int>(async () =>
        {
            await Task.Yield();
            throw Cancelled;
        }));
    }

    [Test]
    public void ChainAwaitPropagatesCancellation()
    {
        Result<int> result = 1;

        Assert.CatchAsync<OperationCanceledException>(async () =>
            await result.ChainAwait<int, int>(async _ =>
            {
                await Task.Yield();
                throw Cancelled;
            }));
    }

    [Test]
    public void RetryAsyncThrowsWithACancelledToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(async () => await Result.RetryAsync<int>(
            token =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult<Result<int>>(1);
            },
            maxAttempts: 3,
            cancellationToken: cancellation.Token));
    }

    [Test]
    public void WhenAllThrowsForACancelledTask()
    {
        Task<Result<int>>[] tasks = [Task.FromResult<Result<int>>(1), Task.FromCanceled<Result<int>>(new CancellationToken(true))];

        Assert.CatchAsync<OperationCanceledException>(async () => await Result.WhenAll(tasks));
    }

    [Test]
    public void WhenAllStillTurnsOtherFaultsIntoErrors()
    {
        var failure = new InvalidOperationException("Boom");
        Task<Result<int>>[] tasks = [Task.FromResult<Result<int>>(1), Task.FromException<Result<int>>(failure)];

        var result = Result.WhenAll(tasks).GetAwaiter().GetResult();

        Assert.That(result.GetError(), Is.SameAs(failure));
    }

    private static int Cancel() => throw Cancelled;
}
