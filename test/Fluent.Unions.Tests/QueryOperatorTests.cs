namespace Fluent.Unions.Tests;

public class QueryOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void SelectProjectsValue()
    {
        Result<string> result = "abc";

        var projected = result.Select(value => value.Length);

        Assert.That(projected.GetOk(), Is.EqualTo(3));
    }

    [Test]
    public void SelectPassesErrorThroughWithoutRunning()
    {
        Result<string> result = Failure;
        var calls = 0;

        var projected = result.Select(value => ++calls);

        Assert.That(projected.GetError(), Is.SameAs(Failure));
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void SelectKeepsEmptyResultEmpty()
    {
        var projected = default(Result<string>).Select(value => value.Length);

        Assert.That(projected.Value, Is.Null);
    }

    [Test]
    public void SelectCapturesThrownException()
    {
        Result<string> result = "abc";
        var thrown = new FormatException("Bad format");

        var projected = result.Select<string, int>(_ => throw thrown);

        Assert.That(projected.GetError(), Is.SameAs(thrown));
    }

    [Test]
    public void SelectReturningNullGivesEmptyResult()
    {
        Result<string> result = "abc";

        var projected = result.Select(_ => (string?)null);

        Assert.False(projected.Success);
        Assert.That(projected.Value, Is.Null);
    }

    [Test]
    public void SelectWorksWithValueTypes()
    {
        Result<int> result = 0;

        var projected = result.Select(value => value + 1);

        Assert.That(projected.GetOk(), Is.EqualTo(1));
    }

    [Test]
    public void SelectOnNonGenericResult()
    {
        var okProjection = Result.Ok.Select(() => 42);
        var errorProjection = Result.Error(Failure).Select(() => 42);

        Assert.That(okProjection.GetOk(), Is.EqualTo(42));
        Assert.That(errorProjection.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void SelectThrowsForNullSelector()
    {
        Result<string> result = "abc";

        Assert.Throws<ArgumentNullException>(() => result.Select((Func<string, int>)null!));
    }

    [Test]
    public void SelectManyChainsResults()
    {
        Result<string> result = "42";

        var parsed = result.SelectMany(Parse);
        var failed = ((Result<string>)"x").SelectMany(Parse);

        Assert.That(parsed.GetOk(), Is.EqualTo(42));
        Assert.That(failed.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public void SelectManyToNonGenericResult()
    {
        Result<string> result = "abc";
        Result<string> failed = Failure;

        Result ok = result.SelectMany(_ => Result.Ok);
        Result error = failed.SelectMany(_ => Result.Ok);

        Assert.True(ok.Success);
        Assert.That(error.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void SelectManyOnNonGenericResult()
    {
        var chained = Result.Ok.SelectMany(() => Result.Ok);
        var withValue = Result.Ok.SelectMany(() => Parse("7"));
        var skipped = Result.Error(Failure).SelectMany(() => Parse("7"));

        Assert.True(chained.Success);
        Assert.That(withValue.GetOk(), Is.EqualTo(7));
        Assert.That(skipped.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void ChainRunsNextOnlyAfterSuccess()
    {
        Result<string> ok = "42";
        Result<string> failed = Failure;
        var calls = 0;

        var parsed = ok.Chain(Parse);
        var skipped = failed.Chain(value =>
        {
            calls++;
            return Parse(value);
        });
        var empty = default(Result<string>).Chain(Parse);

        Assert.That(parsed.GetOk(), Is.EqualTo(42));
        Assert.That(skipped.GetError(), Is.SameAs(Failure));
        Assert.That(empty.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void ChainBetweenGenericAndNonGenericResults()
    {
        Result<string> result = "7";

        Result saved = result.Chain(_ => Result.Ok);
        var chained = Result.Ok.Chain(() => Result.Ok);
        var withValue = Result.Ok.Chain(() => Parse("7"));
        var skipped = Result.Error(Failure).Chain(() => Parse("7"));

        Assert.True(saved.Success);
        Assert.True(chained.Success);
        Assert.That(withValue.GetOk(), Is.EqualTo(7));
        Assert.That(skipped.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void QuerySyntaxCombinesResults()
    {
        var sum =
            from a in Parse("20")
            from b in Parse("22")
            where a < b
            select a + b;

        Assert.That(sum.GetOk(), Is.EqualTo(42));
    }

    [Test]
    public void QuerySyntaxStopsAtFirstError()
    {
        var sum =
            from a in Parse("20")
            from b in Parse("x")
            select a + b;

        Assert.That(sum.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public void WhereKeepsMatchingValue()
    {
        Result<int> result = 5;

        Assert.That(result.Where(value => value > 3).GetOk(), Is.EqualTo(5));
    }

    [Test]
    public void WhereMakesNonMatchingValueEmpty()
    {
        Result<int> result = 5;

        var filtered = result.Where(value => value > 10);

        Assert.False(filtered.Success);
        Assert.That(filtered.Value, Is.Null);
    }

    [Test]
    public void WherePassesErrorThrough()
    {
        Result<int> result = Failure;

        Assert.That(result.Where(_ => true).GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void WhereCapturesThrownException()
    {
        Result<int> result = 5;

        var filtered = result.Where(_ => throw Failure);

        Assert.That(filtered.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void ZipCombinesValues()
    {
        Result<int> first = 1;
        Result<string> second = "a";

        var zipped = first.Zip(second);

        Assert.That(zipped.GetOk(), Is.EqualTo((1, "a")));
        Assert.That(first.Zip(second, (a, b) => $"{b}{a}").GetOk(), Is.EqualTo("a1"));
    }

    [Test]
    public void ZipPrefersFirstError()
    {
        var firstError = new InvalidOperationException("first");
        var secondError = new InvalidOperationException("second");
        Result<int> first = firstError;
        Result<int> second = secondError;

        Assert.That(first.Zip(second).GetError(), Is.SameAs(firstError));
    }

    [Test]
    public void ZipPrefersErrorOverEmpty()
    {
        Result<int> second = Failure;

        Assert.That(default(Result<int>).Zip(second).GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void ZipWithEmptySideIsEmpty()
    {
        Result<int> first = 1;

        Assert.That(first.Zip(default(Result<int>)).Value, Is.Null);
    }

    [Test]
    public void GetValueOrDefaultReturnsValueOrDefault()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(ok.GetValueOrDefault(), Is.EqualTo(5));
        Assert.That(error.GetValueOrDefault(), Is.EqualTo(0));
        Assert.That(default(Result<string>).GetValueOrDefault(), Is.Null);
    }

    [Test]
    public void GetValueOrDefaultReturnsFallback()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(ok.GetValueOrDefault(9), Is.EqualTo(5));
        Assert.That(error.GetValueOrDefault(9), Is.EqualTo(9));
        Assert.That(default(Result<int>).GetValueOrDefault(9), Is.EqualTo(9));
    }

    [Test]
    public void DefaultIfEmptyReplacesOnlyEmpty()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(default(Result<int>).DefaultIfEmpty(9).GetOk(), Is.EqualTo(9));
        Assert.That(ok.DefaultIfEmpty(9).GetOk(), Is.EqualTo(5));
        Assert.That(error.DefaultIfEmpty(9).GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void WhereThenDefaultIfEmpty()
    {
        Result<int> result = 5;

        Assert.That(result.Where(value => value > 10).DefaultIfEmpty(0).GetOk(), Is.EqualTo(0));
    }

    private static Result<int> Parse(string text)
    {
        try
        {
            return int.Parse(text);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
