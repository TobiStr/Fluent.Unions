namespace ResultType.Tests;

public class MatchTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void MatchResultCoversEveryState()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;
        Result<int> filtered = ok.Where(v => v > 10);

        Assert.That(ok.Match(v => $"ok {v}", e => e.Message, () => "empty"), Is.EqualTo("ok 5"));
        Assert.That(error.Match(v => $"ok {v}", e => e.Message, () => "empty"), Is.EqualTo("Failure"));
        Assert.That(filtered.Match(v => $"ok {v}", e => e.Message, () => "empty"), Is.EqualTo("empty"));
    }

    [Test]
    public void MatchNonGenericResult()
    {
        Assert.That(Result.Ok.Match(() => "ok", e => e.Message, () => "empty"), Is.EqualTo("ok"));
        Assert.That(Result.Error(Failure).Match(() => "ok", e => e.Message, () => "empty"), Is.EqualTo("Failure"));
        Assert.That(default(Result).Match(() => "ok", e => e.Message, () => "empty"), Is.EqualTo("empty"));
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

        Assert.Throws<InvalidOperationException>(() => ok.Match<int, int>(_ => throw Failure, _ => 0, () => 0));
    }

    [Test]
    public async Task MatchEndsATaskPipelineWithMethodGroups()
    {
        var found = await Task.FromResult<Result<int>>(5).Match(Describe, Describe, Empty);
        var saved = await Task.FromResult(Result.Ok).Match(Empty, Describe, Empty);
        var missing = await Task.FromResult<Option<int>>(Option.None).Match(Describe, Empty);

        Assert.That(found, Is.EqualTo("value 5"));
        Assert.That(saved, Is.EqualTo("nothing"));
        Assert.That(missing, Is.EqualTo("nothing"));
    }

    private static string Describe(int value) => $"value {value}";

    private static string Describe(Exception error) => $"error {error.Message}";

    private static string Empty() => "nothing";
}
