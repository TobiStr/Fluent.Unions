namespace Fluent.Unions.Tests;

public class MatchTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void MatchResultCoversEveryState()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(ok.Match(v => $"ok {v}", e => e.Message), Is.EqualTo("ok 5"));
        Assert.That(error.Match(v => $"ok {v}", e => e.Message), Is.EqualTo("Failure"));
    }

    [Test]
    public void MatchNonGenericResult()
    {
        Assert.That(Result.Ok.Match(() => "ok", e => e.Message), Is.EqualTo("ok"));
        Assert.That(Result.Error(Failure).Match(() => "ok", e => e.Message), Is.EqualTo("Failure"));
        Assert.That(default(Result).Match(() => "ok", e => e.Message), Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void MatchOption()
    {
        Option<int> some = 5;

        Assert.That(some.Match(v => v * 2, () => -1), Is.EqualTo(10));
        Assert.That(((Option<int>)Option.None).Match(v => v * 2, () => -1), Is.EqualTo(-1));
        Assert.That(default(Option<int>).Match(v => v * 2, () => -1), Is.EqualTo(-1));
    }

    [Test]
    public void MatchCallbackExceptionsPropagate()
    {
        Result<int> ok = 5;

        Assert.Throws<InvalidOperationException>(() => ok.Match<int, int>(_ => throw Failure, _ => 0));
    }

    [Test]
    public async Task MatchEndsATaskPipelineWithMethodGroups()
    {
        var found = await Task.FromResult<Result<int>>(5).Match(Describe, Describe);
        var saved = await Task.FromResult(Result.Ok).Match(Empty, Describe);
        var missing = await Task.FromResult<Option<int>>(Option.None).Match(Describe, Empty);

        Assert.That(found, Is.EqualTo("value 5"));
        Assert.That(saved, Is.EqualTo("nothing"));
        Assert.That(missing, Is.EqualTo("nothing"));
    }

    private static string Describe(int value) => $"value {value}";

    private static string Describe(Exception error) => $"error {error.Message}";

    private static string Empty() => "nothing";
}
