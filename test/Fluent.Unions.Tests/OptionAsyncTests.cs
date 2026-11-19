namespace Fluent.Unions.Tests;

public class OptionAsyncTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public async Task TaskPipelineStaysOneExpression()
    {
        var log = new List<string>();

        var length = await FindNameAsync("Ada")
            .Where(name => name.Length > 0)
            .Do(name => log.Add(name))
            .Select(name => name.Length)
            .GetValueOrDefault(-1);
        var missing = await FindNameAsync(null)
            .Select(name => name.Length)
            .OnNoneDo(() => log.Add("none"))
            .GetValueOrDefault(-1);

        Assert.That(length, Is.EqualTo(3));
        Assert.That(missing, Is.EqualTo(-1));
        Assert.That(log, Is.EqualTo(new[] { "Ada", "none" }));
    }

    [Test]
    public async Task AsyncQuerySyntaxCombinesOptions()
    {
        var greeting = await (
            from name in FindNameAsync("Ada")
            from age in FindAgeAsync(name)
            where age > 18
            select $"{name} ({age})");
        var missing = await (
            from name in FindNameAsync(null)
            from age in FindAgeAsync(name)
            select $"{name} ({age})");

        Assert.That(greeting.GetValue(), Is.EqualTo("Ada (36)"));
        Assert.False(missing.HasValue);
    }

    [Test]
    public async Task AwaitOperatorsRunOnlyWithValue()
    {
        var log = new List<string>();

        var age = await FindNameAsync("Ada")
            .ChainAwait(FindAgeAsync)
            .WhereAwait(async value =>
            {
                await Task.Yield();
                return value > 18;
            })
            .DoAwait(async value =>
            {
                await Task.Yield();
                log.Add($"age {value}");
            })
            .SelectAwait(async value =>
            {
                await Task.Yield();
                return value * 2;
            });
        var skipped = await FindNameAsync(null).ChainAwait(name =>
        {
            log.Add("should not run");
            return FindAgeAsync(name);
        });

        Assert.That(age.GetValue(), Is.EqualTo(72));
        Assert.False(skipped.HasValue);
        Assert.That(log, Is.EqualTo(new[] { "age 36" }));
    }

    [Test]
    public async Task TaskToResult()
    {
        var ok = await FindNameAsync("Ada").ToResult(() => Failure);
        var error = await FindNameAsync(null).ToResult(() => Failure);

        Assert.That(ok.GetOk(), Is.EqualTo("Ada"));
        Assert.That(error.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void FaultedTaskStaysFaulted()
    {
        var faulted = Task.FromException<Option<string>>(Failure);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await faulted.Select(name => name.Length));

        Assert.That(ex, Is.SameAs(Failure));
    }

    private static async Task<Option<string>> FindNameAsync(string? name)
    {
        await Task.Yield();

        return name.ToOption();
    }

    private static async Task<Option<int>> FindAgeAsync(string name)
    {
        await Task.Yield();

        return name.Length * 12;
    }
}
