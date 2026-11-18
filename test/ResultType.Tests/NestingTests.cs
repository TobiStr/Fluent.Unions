namespace ResultType.Tests;

public class NestingTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void UnwrapFlattensNestedOptions()
    {
        Option<Option<int>> both = new Option<Option<int>>(5);
        Option<Option<int>> innerNone = new Option<Option<int>>(Option.None);
        Option<Option<int>> outerNone = Option.None;

        Assert.That(both.Unwrap().GetValue(), Is.EqualTo(5));
        Assert.That(innerNone.Unwrap().Value, Is.TypeOf<None>());
        Assert.That(outerNone.Unwrap().Value, Is.TypeOf<None>());
    }

    [Test]
    public void UnwrapFlattensNestedResults()
    {
        Result<Result<int>> ok = new Result<Result<int>>(5);
        Result<Result<int>> innerError = new Result<Result<int>>(new Result<int>(Failure));
        Result<Result<int>> outerError = Failure;

        Assert.That(ok.Unwrap().GetOk(), Is.EqualTo(5));
        Assert.That(innerError.Unwrap().GetError(), Is.SameAs(Failure));
        Assert.That(outerError.Unwrap().GetError(), Is.SameAs(Failure));
        Assert.That(default(Result<Result<int>>).Unwrap().Value, Is.Null);
    }

    [Test]
    public void UnwrapFlattensSelectIntoNonGenericResult()
    {
        Result<string> name = "Ada";

        Result saved = name.Select(_ => Result.Ok).Unwrap();
        Result failed = name.Select(_ => Result.Error(Failure)).Unwrap();

        Assert.True(saved.Success);
        Assert.That(failed.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void TransposeOptionOfResult()
    {
        Option<Result<int>> none = Option.None;
        Option<Result<int>> ok = new Option<Result<int>>(5);
        Option<Result<int>> error = new Option<Result<int>>(new Result<int>(Failure));
        Option<Result<int>> empty = new Option<Result<int>>(default(Result<int>));

        Assert.That(none.Transpose().GetOk().Value, Is.TypeOf<None>());
        Assert.That(ok.Transpose().GetOk().GetValue(), Is.EqualTo(5));
        Assert.That(error.Transpose().GetError(), Is.SameAs(Failure));
        Assert.That(empty.Transpose().Value, Is.Null);
    }

    [Test]
    public void TransposeResultOfOption()
    {
        Result<Option<int>> none = new Result<Option<int>>(Option.None);
        Result<Option<int>> some = new Result<Option<int>>(5);
        Result<Option<int>> error = Failure;

        Assert.That(none.Transpose().Value, Is.TypeOf<None>());
        Assert.That(some.Transpose().GetValue().GetOk(), Is.EqualTo(5));
        Assert.That(error.Transpose().GetValue().GetError(), Is.SameAs(Failure));
        Assert.That(default(Result<Option<int>>).Transpose().GetValue().Value, Is.Null);
    }

    [Test]
    public void TransposeRoundTripsKeepTheState()
    {
        Assert.That(default(Result<Option<int>>).Transpose().Transpose().Value, Is.Null);
        Assert.That(((Result<Option<int>>)Failure).Transpose().Transpose().GetError(), Is.SameAs(Failure));
        Assert.That(new Result<Option<int>>(5).Transpose().Transpose().GetOk().GetValue(), Is.EqualTo(5));
        Assert.False(new Result<Option<int>>(Option.None).Transpose().Transpose().GetOk().HasValue);
    }
}
