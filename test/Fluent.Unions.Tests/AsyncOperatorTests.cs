namespace Fluent.Unions.Tests;

public class AsyncOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public async Task TaskPipelineStaysOneExpression()
    {
        var length = await GetNameAsync("Ada")
            .Ensure(name => name.Length > 0, "empty")
            .Select(name => name.Length)
            .Catch(_ => -1);

        Assert.That(length.GetValue(), Is.EqualTo(3));
    }

    [Test]
    public async Task TaskPipelinePassesErrorThrough()
    {
        var calls = 0;

        var length = await GetNameAsync(null)
            .Select(name => ++calls);

        Assert.That(length.GetError(), Is.SameAs(Failure));
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public async Task FaultedTaskBecomesErrorResult()
    {
        var faulted = Task.FromException<Result<string>>(Failure);

        var result = await faulted.Select(name => name.Length);

        Assert.That(result.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task AsyncQuerySyntaxCombinesResults()
    {
        var greeting = await (
            from name in GetNameAsync("Ada")
            from age in GetAgeAsync(name)
            select $"{name} ({age})");

        Assert.That(greeting.GetValue(), Is.EqualTo("Ada (36)"));
    }

    [Test]
    public async Task AsyncQuerySyntaxStopsAtFirstError()
    {
        var greeting = await (
            from name in GetNameAsync(null)
            from age in GetAgeAsync(name)
            select $"{name} ({age})");

        Assert.That(greeting.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task SelectAwaitProjectsValue()
    {
        Result<string> result = "Ada";

        var age = await result.SelectAwait(async name =>
        {
            await Task.Yield();
            return name.Length * 12;
        });

        Assert.That(age.GetValue(), Is.EqualTo(36));
    }

    [Test]
    public async Task SelectAwaitCapturesFaultedLambda()
    {
        Result<string> result = "Ada";

        var age = await result.SelectAwait<string, int>(async _ =>
        {
            await Task.Yield();
            throw Failure;
        });

        Assert.That(age.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task ChainOnTaskResult()
    {
        var length = await GetNameAsync("Ada").Chain(name => (Result<int>)name.Length);
        var failed = await GetNameAsync(null).Chain(name => (Result<int>)name.Length);

        Assert.That(length.GetValue(), Is.EqualTo(3));
        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task ChainAwaitChainsAsyncFunctions()
    {
        Result<string> name = "Ada";

        var age = await name.ChainAwait(GetAgeAsync);
        var saved = await GetNameAsync("Ada").ChainAwait(SaveAsync);
        var failed = await GetNameAsync(null).ChainAwait(GetAgeAsync);

        Assert.That(age.GetValue(), Is.EqualTo(36));
        Assert.True(saved.IsSuccess);
        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task DoAwaitAndFinallyAwaitRun()
    {
        var log = new List<string>();

        var result = await GetNameAsync("Ada")
            .DoAwait(async name =>
            {
                await Task.Yield();
                log.Add($"next {name}");
            })
            .FinallyAwait(async () =>
            {
                await Task.Yield();
                log.Add("finally");
            });

        Assert.That(result.GetValue(), Is.EqualTo("Ada"));
        Assert.That(log, Is.EqualTo(new[] { "next Ada", "finally" }));
    }

    [Test]
    public async Task CatchAwaitRecovers()
    {
        var recovered = await GetNameAsync(null).CatchAwait(async _ =>
        {
            await Task.Yield();
            return "Anonymous";
        });
        var typed = await GetNameAsync(null).CatchAwait((IOException _) => Task.FromResult<Result<string>>("x"));

        Assert.That(recovered.GetValue(), Is.EqualTo("Anonymous"));
        Assert.That(typed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task NonGenericResultAwaitOperators()
    {
        var log = new List<string>();

        var value = await Task.FromResult(Result.Ok)
            .DoAwait(() =>
            {
                log.Add("saved");
                return Task.CompletedTask;
            })
            .SelectAwait(() => Task.FromResult(42));
        var recovered = await Task.FromResult(Result.Error(Failure))
            .CatchAwait(_ => Task.FromResult(Result.Ok));

        Assert.That(value.GetValue(), Is.EqualTo(42));
        Assert.True(recovered.IsSuccess);
        Assert.That(log, Is.EqualTo(new[] { "saved" }));
    }

    [Test]
    public void TaskCheckThrowsOriginalException()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await GetNameAsync(null).Check());

        Assert.That(ex, Is.SameAs(Failure));
    }

    [Test]
    public async Task TaskCheckReturnsSuccessfulResult()
    {
        var result = await GetNameAsync("Ada").Check();

        Assert.That(result.GetValue(), Is.EqualTo("Ada"));
    }

    [Test]
    public void TaskOperatorsRejectNullDelegatesImmediately()
    {
        var results = Task.FromResult<Result<string>>("Ada");
        var options = Task.FromResult<Option<string>>("Ada");

        Assert.Throws<ArgumentNullException>(() => results.Select((Func<string, int>)null!));
        Assert.Throws<ArgumentNullException>(() => results.Chain((Func<string, Result<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => results.SelectAwait((Func<string, Task<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => results.ChainAwait((Func<string, Task<Result<int>>>)null!));
        Assert.Throws<ArgumentNullException>(() => results.Do((Action<string>)null!));
        Assert.Throws<ArgumentNullException>(() => results.Ensure(null!, "message"));
        Assert.Throws<ArgumentNullException>(() => options.Select((Func<string, int>)null!));
        Assert.Throws<ArgumentNullException>(() => options.Where(null!));
        Assert.Throws<ArgumentNullException>(() => options.SelectAwait((Func<string, Task<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => options.ChainAwait((Func<string, Task<Option<int>>>)null!));
        Assert.Throws<ArgumentNullException>(() => options.WhereAwait(null!));
        Assert.Throws<ArgumentNullException>(() => options.DoAwait(null!));
    }

    private static async Task<Result<string>> GetNameAsync(string? name)
    {
        await Task.Yield();

        return name is null ? Failure : name;
    }

    private static async Task<Result<int>> GetAgeAsync(string name)
    {
        await Task.Yield();

        return name.Length * 12;
    }

    private static Task<Result> SaveAsync(string name) => Task.FromResult(Result.Ok);

    [Test]
    public async Task ChainAwaitWithResultSelectorCombinesBothValues()
    {
        Result<string> name = "Ada";

        var described = await name.ChainAwait(GetAgeAsync, (n, age) => $"{n} is {age}");

        Assert.That(described.GetValue(), Is.EqualTo("Ada is 36"));
    }

    [Test]
    public async Task ChainAwaitWithResultSelectorSkipsTheStepOnError()
    {
        Result<string> failed = Failure;
        var steps = 0;

        var described = await failed.ChainAwait(n => { steps++; return GetAgeAsync(n); }, (n, age) => $"{n} is {age}");

        Assert.That(steps, Is.Zero);
        Assert.That(described.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task ChainAwaitWithResultSelectorCapturesAFaultedTask()
    {
        Result<string> name = "Ada";

        var described = await name.ChainAwait(_ => Task.FromException<Result<int>>(Failure), (n, age) => $"{n} is {age}");

        Assert.That(described.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task ChainAwaitWithResultSelectorOnATaskReceiver()
    {
        var name = Task.FromResult<Result<string>>("Ada");

        var described = await name.ChainAwait(GetAgeAsync, (n, age) => $"{n} is {age}");

        Assert.That(described.GetValue(), Is.EqualTo("Ada is 36"));
    }

    [Test]
    public void ChainAwaitWithResultSelectorPropagatesCancellation()
    {
        Result<string> name = "Ada";

        Assert.ThrowsAsync<TaskCanceledException>(() =>
            name.ChainAwait(_ => Task.FromCanceled<Result<int>>(new CancellationToken(true)), (n, age) => $"{n} is {age}"));
    }}
