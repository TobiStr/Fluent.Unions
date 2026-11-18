namespace ResultType.Tests;

public class SideEffectOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void DoRunsForValueAndReturnsSameResult()
    {
        Result<int> result = 5;
        var seen = 0;

        var same = result.Do(value => seen = value);

        Assert.That(seen, Is.EqualTo(5));
        Assert.That(same.GetOk(), Is.EqualTo(5));
    }

    [Test]
    public void DoSkipsErrorAndEmpty()
    {
        Result<int> error = Failure;
        var calls = 0;

        error.Do(_ => calls++);
        default(Result<int>).Do(_ => calls++);

        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void DoWithOnErrorRunsMatchingCallback()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;
        var log = new List<string>();

        ok.Do(value => log.Add($"next {value}"), e => log.Add($"error {e.Message}"));
        error.Do(value => log.Add($"next {value}"), e => log.Add($"error {e.Message}"));
        default(Result<int>).Do(value => log.Add("next"), e => log.Add("error"));

        Assert.That(log, Is.EqualTo(new[] { "next 5", "error Failure" }));
    }

    [Test]
    public void DoCapturesExceptionFromAction()
    {
        Result<int> result = 5;

        var failed = result.Do(_ => throw Failure);

        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void DoCallsRunInChainOrder()
    {
        Result<int> result = 1;
        var log = new List<string>();

        result
            .Do(value => log.Add($"before {value}"))
            .Select(value => value * 10)
            .Do(value => log.Add($"after {value}"));

        Assert.That(log, Is.EqualTo(new[] { "before 1", "after 10" }));
    }

    [Test]
    public void DoOnNonGenericResult()
    {
        var log = new List<string>();

        Result.Ok.Do(() => log.Add("ok"));
        Result.Error(Failure).Do(() => log.Add("ok"));
        Result.Ok.Do(() => log.Add("next"), _ => log.Add("error"));
        Result.Error(Failure).Do(() => log.Add("next"), _ => log.Add("error"));

        Assert.That(log, Is.EqualTo(new[] { "ok", "next", "error" }));
    }

    [Test]
    public void OnErrorDoRunsOnlyForErrorAndReturnsSameResult()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;
        var seen = new List<Exception>();

        var okResult = ok.OnErrorDo(seen.Add);
        var errorResult = error.OnErrorDo(seen.Add);
        default(Result<int>).OnErrorDo(seen.Add);

        Assert.That(seen, Is.EqualTo(new[] { Failure }));
        Assert.That(okResult.GetOk(), Is.EqualTo(5));
        Assert.That(errorResult.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void OnErrorDoCapturesExceptionFromAction()
    {
        Result<int> error = Failure;
        var replacement = new ArgumentException("Replacement");

        var failed = error.OnErrorDo(_ => throw replacement);

        Assert.That(failed.GetError(), Is.SameAs(replacement));
    }

    [Test]
    public void OnErrorDoOnNonGenericResult()
    {
        var seen = new List<Exception>();

        var ok = Result.Ok.OnErrorDo(seen.Add);
        var error = Result.Error(Failure).OnErrorDo(seen.Add);

        Assert.That(seen, Is.EqualTo(new[] { Failure }));
        Assert.True(ok.Success);
        Assert.That(error.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task OnErrorDoOnTaskResult()
    {
        var seen = new List<Exception>();

        var typed = await Task.FromResult<Result<int>>(Failure).OnErrorDo(seen.Add);
        var plain = await Task.FromResult(Result.Error(Failure)).OnErrorDo(seen.Add);
        var faulted = await Task.FromException<Result<int>>(Failure).OnErrorDo(seen.Add);

        Assert.That(seen, Is.EqualTo(new[] { Failure, Failure, Failure }));
        Assert.That(typed.GetError(), Is.SameAs(Failure));
        Assert.That(plain.GetError(), Is.SameAs(Failure));
        Assert.That(faulted.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void FinallyRunsForEveryState()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;
        var calls = 0;

        var okResult = ok.Finally(() => calls++);
        var errorResult = error.Finally(() => calls++);
        default(Result<int>).Finally(() => calls++);
        Result.Ok.Finally(() => calls++);

        Assert.That(calls, Is.EqualTo(4));
        Assert.That(okResult.GetOk(), Is.EqualTo(5));
        Assert.That(errorResult.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void FinallyCapturesExceptionFromAction()
    {
        Result<int> result = 5;

        var failed = result.Finally(() => throw Failure);

        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }
}
