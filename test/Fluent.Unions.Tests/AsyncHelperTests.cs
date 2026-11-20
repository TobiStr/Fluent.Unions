namespace Fluent.Unions.Tests;

public class AsyncHelperTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public async Task WhenAllCombinesValuesInOrder()
    {
        Task<Result<int>>[] tasks = [DelayedAsync(3, 30), DelayedAsync(1, 10)];

        var result = await Result.WhenAll(tasks);

        Assert.That(result.GetValue(), Is.EqualTo(new[] { 3, 1 }));
    }

    [Test]
    public async Task WhenAllReturnsTheFirstErrorAndTurnsFaultsIntoErrors()
    {
        var other = new ArgumentException("Other");
        Task<Result<int>>[] withError = [Task.FromResult<Result<int>>(1), Task.FromResult<Result<int>>(Failure), Task.FromResult<Result<int>>(other)];
        Task<Result<int>>[] faulted = [Task.FromResult<Result<int>>(1), Task.FromException<Result<int>>(other)];

        Assert.That((await Result.WhenAll(withError)).GetError(), Is.SameAs(Failure));
        Assert.That((await Result.WhenAll(faulted)).GetError(), Is.SameAs(other));
    }

    [Test]
    public async Task WhenAllForNonGenericResults()
    {
        Task<Result>[] ok = [Task.FromResult(Result.Ok), Task.FromResult(Result.Ok)];
        Task<Result>[] failed = [Task.FromResult(Result.Ok), Task.FromResult(Result.Error(Failure))];

        Assert.True((await Result.WhenAll(ok)).IsSuccess);
        Assert.That((await Result.WhenAll(failed)).GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task AsyncStreamOperators()
    {
        Assert.That(await ToListAsync(Stream(1, Failure, 2).Values()), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(await ToListAsync(Stream(1, Failure, 2).Errors()), Is.EqualTo(new[] { Failure }));
        Assert.That((await Stream(1, 2).CombineAsync()).GetValue(), Is.EqualTo(new[] { 1, 2 }));
        Assert.That((await Stream(1, Failure, 2).CombineAsync()).GetError(), Is.SameAs(Failure));
        Assert.That(await ToListAsync(OptionStream().Values()), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public async Task CombineAsyncCapturesStreamExceptions()
    {
        var result = await ThrowingStream().CombineAsync();

        Assert.That(result.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void RetryStopsAtTheFirstSuccess()
    {
        var calls = 0;

        var result = Result.Retry<int>(() => ++calls < 3 ? Failure : calls, maxAttempts: 5);

        Assert.That(result.GetValue(), Is.EqualTo(3));
        Assert.That(calls, Is.EqualTo(3));
    }

    [Test]
    public void RetryGivesUpAfterMaxAttemptsAndCapturesThrows()
    {
        var calls = 0;

        var result = Result.Retry<int>(() =>
        {
            calls++;
            throw Failure;
        }, maxAttempts: 3);

        Assert.That(result.GetError(), Is.SameAs(Failure));
        Assert.That(calls, Is.EqualTo(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => Result.Retry(() => (Result<int>)1, 0));
    }

    [Test]
    public void RetryOnlyRetriesMatchingErrors()
    {
        var calls = 0;

        var result = Result.Retry<int>(() =>
        {
            calls++;
            return new ArgumentException("Permanent");
        }, maxAttempts: 5, shouldRetry: e => e is TimeoutException);

        Assert.That(result.GetError(), Is.TypeOf<ArgumentException>());
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task RetryAsyncRetriesWithDelay()
    {
        var calls = 0;

        var result = await Result.RetryAsync<int>(async _ =>
        {
            await Task.Yield();
            return ++calls < 2 ? new TimeoutException() : calls;
        }, maxAttempts: 3, delay: TimeSpan.FromMilliseconds(1));

        Assert.That(result.GetValue(), Is.EqualTo(2));
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void RetryAsyncThrowsOnCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var calls = 0;

        Assert.CatchAsync<OperationCanceledException>(async () => await Result.RetryAsync<int>(_ =>
        {
            calls++;
            cancellation.Cancel();
            return Task.FromResult<Result<int>>(Failure);
        }, maxAttempts: 5, cancellationToken: cancellation.Token));

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task TimeoutGivesATimeoutError()
    {
        var slow = DelayedAsync(1, 5_000);

        var timedOut = await slow.Timeout(TimeSpan.FromMilliseconds(20));
        var inTime = await DelayedAsync(2, 1).Timeout(TimeSpan.FromSeconds(5));
        var plain = await Task.FromResult(Result.Ok).Timeout(TimeSpan.FromSeconds(5));

        Assert.That(timedOut.GetError(), Is.TypeOf<TimeoutException>());
        Assert.That(inTime.GetValue(), Is.EqualTo(2));
        Assert.True(plain.IsSuccess);
    }

    private static async Task<Result<int>> DelayedAsync(int value, int milliseconds)
    {
        await Task.Delay(milliseconds);
        return value;
    }

    private static async IAsyncEnumerable<Result<int>> Stream(params Result<int>[] results)
    {
        foreach (var result in results)
        {
            await Task.Yield();
            yield return result;
        }
    }

    private static async IAsyncEnumerable<Option<int>> OptionStream()
    {
        await Task.Yield();
        yield return 1;
        yield return Option.None;
        yield return 3;
    }

    private static async IAsyncEnumerable<Result<int>> ThrowingStream()
    {
        await Task.Yield();
        yield return 1;
        throw Failure;
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }
}
