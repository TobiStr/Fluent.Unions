namespace Fluent.Unions.Tests;

public class QueryOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void SelectProjectsValue()
    {
        Result<string> result = "abc";

        var projected = result.Select(value => value.Length);

        Assert.That(projected.GetValue(), Is.EqualTo(3));
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
    public void SelectPropagatesTheErrorOfADefaultResult()
    {
        var projected = default(Result<string>).Select(value => value.Length);

        Assert.That(projected.GetError().Message, Does.Contain("holds neither a value nor an error"));
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
    public void SelectReturningNullGivesAnError()
    {
        Result<string> result = "abc";

        var projected = result.Select(_ => (string?)null);

        Assert.True(projected.IsError);
        Assert.That(projected.GetError(), Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SelectWorksWithValueTypes()
    {
        Result<int> result = 0;

        var projected = result.Select(value => value + 1);

        Assert.That(projected.GetValue(), Is.EqualTo(1));
    }

    [Test]
    public void SelectOnNonGenericResult()
    {
        var okProjection = Result.Ok.Select(() => 42);
        var errorProjection = Result.Error(Failure).Select(() => 42);

        Assert.That(okProjection.GetValue(), Is.EqualTo(42));
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

        Assert.That(parsed.GetValue(), Is.EqualTo(42));
        Assert.That(failed.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public void SelectManyToNonGenericResult()
    {
        Result<string> result = "abc";
        Result<string> failed = Failure;

        Result ok = result.SelectMany(_ => Result.Ok);
        Result error = failed.SelectMany(_ => Result.Ok);

        Assert.True(ok.IsSuccess);
        Assert.That(error.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void SelectManyOnNonGenericResult()
    {
        var chained = Result.Ok.SelectMany(() => Result.Ok);
        var withValue = Result.Ok.SelectMany(() => Parse("7"));
        var skipped = Result.Error(Failure).SelectMany(() => Parse("7"));

        Assert.True(chained.IsSuccess);
        Assert.That(withValue.GetValue(), Is.EqualTo(7));
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

        Assert.That(parsed.GetValue(), Is.EqualTo(42));
        Assert.That(skipped.GetError(), Is.SameAs(Failure));
        Assert.That(empty.GetError().Message, Does.Contain("holds neither a value nor an error"));
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

        Assert.True(saved.IsSuccess);
        Assert.True(chained.IsSuccess);
        Assert.That(withValue.GetValue(), Is.EqualTo(7));
        Assert.That(skipped.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void QuerySyntaxCombinesResults()
    {
        var sum =
            from a in Parse("20")
            from b in Parse("22")
            select a + b;

        Assert.That(sum.GetValue(), Is.EqualTo(42));
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
    public void ZipCombinesValues()
    {
        Result<int> first = 1;
        Result<string> second = "a";

        var zipped = first.Zip(second);

        Assert.That(zipped.GetValue(), Is.EqualTo((1, "a")));
        Assert.That(first.Zip(second, (a, b) => $"{b}{a}").GetValue(), Is.EqualTo("a1"));
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
    public void ZipPrefersADefaultFirstOverALaterError()
    {
        Result<int> second = Failure;

        Assert.That(default(Result<int>).Zip(second).GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void ZipWithADefaultSideIsAnError()
    {
        Result<int> first = 1;

        Assert.That(first.Zip(default(Result<int>)).GetError().Message, Does.Contain("holds neither a value nor an error"));
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

    [Test]
    public void ZipEvaluatesItsArgumentEvenWhenTheReceiverFailed()
    {
        Result<int> failed = Failure;
        var calls = 0;

        var zipped = failed.Zip(Counted(ref calls));

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(zipped.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void ChainAppendDoesNotRunTheStepWhenTheReceiverFailed()
    {
        Result<int> failed = Failure;
        var calls = 0;

        var appended = failed.ChainAppend(_ => Counted(ref calls));

        Assert.That(calls, Is.Zero);
        Assert.That(appended.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void QuerySyntaxDoesNotRunTheSecondSourceWhenTheFirstFailed()
    {
        Result<int> failed = Failure;
        var calls = 0;

        var combined = from first in failed
                       from second in Counted(ref calls)
                       select first + second;

        Assert.That(calls, Is.Zero);
        Assert.That(combined.GetError(), Is.SameAs(Failure));
    }

    private static Result<int> Counted(ref int calls)
    {
        calls++;
        return 2;
    }}
