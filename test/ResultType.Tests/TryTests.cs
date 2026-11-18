namespace ResultType.Tests;

public class TryTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void TryReturnsValue()
    {
        var result = Result.Try(() => int.Parse("42"));

        Assert.That(result.GetOk(), Is.EqualTo(42));
    }

    [Test]
    public void TryCapturesThrownException()
    {
        var result = Result.Try(() => int.Parse("x"));

        Assert.That(result.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public void TryWithNullValueGivesEmptyResult()
    {
        var result = Result.Try(() => (string?)null);

        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void TryDoesNotNestResults()
    {
        Result<int> ok = Result.Try(() => Parse("7"));
        Result<int> failed = Result.Try(() => Parse("x"));
        Result<int> thrown = Result.Try(ThrowingParse);

        Assert.That(ok.GetOk(), Is.EqualTo(7));
        Assert.That(failed.GetError(), Is.TypeOf<FormatException>());
        Assert.That(thrown.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void TryWithActionReturnsOkOrError()
    {
        var calls = 0;

        var ok = Result.Try(() => { calls++; });
        var failed = Result.Try(() => throw Failure);

        Assert.True(ok.Success);
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void TryWithNonGenericResult()
    {
        Result ok = Result.Try(() => Result.Ok);
        Result failed = Result.Try(() => Result.Error(Failure));

        Assert.True(ok.Success);
        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void TryStartsAChain()
    {
        var result = Result.Try(() => "42").Chain(Parse);

        Assert.That(result.GetOk(), Is.EqualTo(42));
    }

    [Test]
    public void TryThrowsForNullBody()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Try((Func<int>)null!));
        Assert.Throws<ArgumentNullException>(() => Result.Try((Action)null!));
    }

    [Test]
    public async Task TryAsyncReturnsValueOrError()
    {
        var ok = await Result.TryAsync(async () =>
        {
            await Task.Yield();
            return 42;
        });
        var failed = await Result.TryAsync<int>(async () =>
        {
            await Task.Yield();
            throw Failure;
        });

        Assert.That(ok.GetOk(), Is.EqualTo(42));
        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task TryAsyncCapturesSynchronousThrow()
    {
        var result = await Result.TryAsync(ThrowBeforeTask);

        Assert.That(result.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task TryAsyncDoesNotNestResults()
    {
        var ok = await Result.TryAsync(() => ParseAsync("7"));
        var failed = await Result.TryAsync(() => ParseAsync("x"));

        Assert.That(ok.GetOk(), Is.EqualTo(7));
        Assert.That(failed.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public async Task TryAsyncWithTaskReturnsOkOrError()
    {
        var ok = await Result.TryAsync(() => Task.CompletedTask);
        var failed = await Result.TryAsync(() => Task.FromException(Failure));
        var plain = await Result.TryAsync(() => Task.FromResult(Result.Error(Failure)));

        Assert.True(ok.Success);
        Assert.That(failed.GetError(), Is.SameAs(Failure));
        Assert.That(plain.GetError(), Is.SameAs(Failure));
    }

    private static Result<int> Parse(string text) =>
        int.TryParse(text, out var value) ? value : new FormatException(text);

    private static Result<int> ThrowingParse() => throw Failure;

    private static async Task<Result<int>> ParseAsync(string text)
    {
        await Task.Yield();

        return Parse(text);
    }

    private static Task<int> ThrowBeforeTask() => throw Failure;
}
