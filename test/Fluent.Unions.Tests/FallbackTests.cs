namespace Fluent.Unions.Tests;

public class FallbackTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void OptionFactoriesRunOnlyWithoutValue()
    {
        var calls = 0;
        Option<int> some = 5;
        Option<int> none = Option.None;

        Assert.That(some.GetValueOrDefault(() => ++calls), Is.EqualTo(5));
        Assert.That(some.DefaultIfEmpty(() => ++calls).GetValue(), Is.EqualTo(5));
        Assert.That(calls, Is.EqualTo(0));
        Assert.That(none.GetValueOrDefault(() => ++calls), Is.EqualTo(1));
        Assert.That(default(Option<int>).DefaultIfEmpty(() => ++calls).GetValue(), Is.EqualTo(2));
    }

    [Test]
    public void OptionFactoryAcceptsMethodGroups()
    {
        Option<int> none = Option.None;

        Assert.That(none.GetValueOrDefault(Seven), Is.EqualTo(7));
        Assert.That(none.DefaultIfEmpty(Seven).GetValue(), Is.EqualTo(7));
    }

    [Test]
    public void OptionFactoryExceptionsPropagate()
    {
        Option<int> none = Option.None;

        Assert.Throws<InvalidOperationException>(() => none.GetValueOrDefault(() => throw Failure));
        Assert.Throws<InvalidOperationException>(() => none.DefaultIfEmpty(() => throw Failure));
    }

    [Test]
    public void OrUsesTheAlternativeOnlyWithoutValue()
    {
        var calls = 0;
        Option<string> cached = "cache";
        Option<string> missing = Option.None;

        var fromCache = cached.Or(() =>
        {
            calls++;
            return "db";
        });
        var fromDb = missing.Or(() => "db");
        var stillMissing = missing.Or(() => default);

        Assert.That(fromCache.GetValue(), Is.EqualTo("cache"));
        Assert.That(calls, Is.EqualTo(0));
        Assert.That(fromDb.GetValue(), Is.EqualTo("db"));
        Assert.That(stillMissing.Value, Is.TypeOf<None>());
    }

    [Test]
    public void ResultGetValueOrDefaultFactoryRunsForErrorAndEmpty()
    {
        var calls = 0;
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(ok.GetValueOrDefault(() => ++calls), Is.EqualTo(5));
        Assert.That(error.GetValueOrDefault(() => ++calls), Is.EqualTo(1));
        Assert.That(default(Result<int>).GetValueOrDefault(() => ++calls), Is.EqualTo(2));
        Assert.Throws<InvalidOperationException>(() => error.GetValueOrDefault(() => throw Failure));
    }

    [Test]
    public void ResultDefaultIfEmptyFactoryRunsOnlyForEmptyAndCapturesExceptions()
    {
        var calls = 0;
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(ok.DefaultIfEmpty(() => ++calls).GetOk(), Is.EqualTo(5));
        Assert.That(error.DefaultIfEmpty(() => ++calls).GetError(), Is.SameAs(Failure));
        Assert.That(calls, Is.EqualTo(0));
        Assert.That(default(Result<int>).DefaultIfEmpty(() => 9).GetOk(), Is.EqualTo(9));
        Assert.That(default(Result<int>).DefaultIfEmpty(() => throw Failure).GetError(), Is.SameAs(Failure));
    }

    [Test]
    public async Task TaskOverloads()
    {
        Assert.That(await Task.FromResult<Option<int>>(Option.None).GetValueOrDefault(Seven), Is.EqualTo(7));
        Assert.That((await Task.FromResult<Option<int>>(Option.None).DefaultIfEmpty(Seven)).GetValue(), Is.EqualTo(7));
        Assert.That((await Task.FromResult<Option<int>>(Option.None).Or(() => 3)).GetValue(), Is.EqualTo(3));
        Assert.That(await Task.FromResult<Result<int>>(Failure).GetValueOrDefault(Seven), Is.EqualTo(7));
        Assert.That((await Task.FromResult(default(Result<int>)).DefaultIfEmpty(Seven)).GetOk(), Is.EqualTo(7));
    }

    private static int Seven() => 7;
}
