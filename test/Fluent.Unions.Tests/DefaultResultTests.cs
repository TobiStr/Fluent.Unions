namespace Fluent.Unions.Tests;

public class DefaultResultTests
{
    private const string Uninitialized = "holds neither a value nor an error";

    [Test]
    public void DefaultResultIsAnError()
    {
        var result = default(Result);

        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>());
        Assert.That(result.GetError().Message, Does.Contain(Uninitialized));
        Assert.True(result.TryGetError(out var error));
        Assert.That(error!.Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void DefaultResultOfTIsAnError()
    {
        var result = default(Result<int>);

        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>());
        Assert.That(result.GetError().Message, Does.Contain(Uninitialized));
        Assert.True(result.TryGetError(out var error));
        Assert.That(error!.Message, Does.Contain(Uninitialized));
        Assert.False(result.TryGetValue(out _));
    }

    [Test]
    public void DefaultDataErrorResultIsAnErrorThroughFromException()
    {
        var result = default(CheckoutResult<int>);

        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.True(result.TryGetError(out var error));
        Assert.That(error.Value, Is.TypeOf<Unexpected>());
        Assert.That(((Unexpected)error.Value!).Exception.Message, Does.Contain(Uninitialized));
        Assert.That(((Unexpected)result.GetError().Value!).Exception.Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void SelectAndChainPropagateTheErrorOfADefaultResult()
    {
        var selected = default(Result<int>).Select(value => value + 1);
        var chained = default(Result<int>).Chain(value => Result<int>.Ok(value + 1));
        var chainedToPlain = default(Result<int>).Chain(_ => Result.Ok);

        Assert.That(selected.GetError().Message, Does.Contain(Uninitialized));
        Assert.That(chained.GetError().Message, Does.Contain(Uninitialized));
        Assert.That(chainedToPlain.GetError().Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void SelectAndChainPropagateTheErrorOfADefaultDataErrorResult()
    {
        var selected = default(CheckoutResult<int>).Select(value => value + 1);
        var chained = default(CheckoutResult<int>).Chain(value => (CheckoutResult<int>)(value + 1));

        Assert.That(((Unexpected)selected.GetError().Value!).Exception.Message, Does.Contain(Uninitialized));
        Assert.That(((Unexpected)chained.GetError().Value!).Exception.Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void MatchCallsOnErrorForDefaultResults()
    {
        Assert.That(default(Result).Match(() => "ok", _ => "error"), Is.EqualTo("error"));
        Assert.That(default(Result<int>).Match(_ => "ok", _ => "error"), Is.EqualTo("error"));
        Assert.That(default(CheckoutResult<int>).Match(_ => "ok", _ => "error"), Is.EqualTo("error"));
    }

    [Test]
    public void CheckThrowsForDefaultResults()
    {
        Assert.Throws<InvalidOperationException>(() => default(Result).Check());
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Check());
        Assert.Throws<ResultErrorException>(() => default(CheckoutResult<int>).Check());
    }

    [Test]
    public void NullPayloadIsTreatedLikeADefaultResult()
    {
        Result<string> fromNull = (string)null!;
        var selected = Result<int>.Ok(1).Select(_ => (string?)null);
        var tried = Result.Try(() => (string?)null);

        Assert.That(fromNull.GetError().Message, Does.Contain(Uninitialized));
        Assert.That(selected.GetError().Message, Does.Contain(Uninitialized));
        Assert.That(tried.GetError().Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void TryGetValueAndTryGetErrorReportExactlyOneState()
    {
        Result<int> ok = 5;
        Result<int> failed = new InvalidOperationException("Boom");

        Assert.True(ok.TryGetValue(out var value));
        Assert.That(value, Is.EqualTo(5));
        Assert.False(ok.TryGetError(out var noError));
        Assert.That(noError, Is.Null);
        Assert.False(failed.TryGetValue(out var noValue));
        Assert.That(noValue, Is.EqualTo(0));
        Assert.True(failed.TryGetError(out var error));
        Assert.That(error!.Message, Is.EqualTo("Boom"));
    }

    [Test]
    public void DeconstructSetsExactlyOneOfValueAndError()
    {
        var (value, noError) = (Result<string>)"Ada";
        var (noValue, error) = (Result<string>)new InvalidOperationException("Boom");

        Assert.That(value, Is.EqualTo("Ada"));
        Assert.That(noError, Is.Null);
        Assert.That(noValue, Is.Null);
        Assert.That(error!.Message, Is.EqualTo("Boom"));
    }

    [Test]
    public void GetValueOnAnErrorThrowsWithTheErrorAsInnerExceptionForResultOfT()
    {
        var failure = new InvalidOperationException("Boom");
        Result<int> failed = failure;
        CheckoutResult<int> data = (CheckoutError)new CartEmpty();

        var ex = Assert.Throws<InvalidOperationException>(() => failed.GetValue());
        var dataEx = Assert.Throws<InvalidOperationException>(() => data.GetValue());

        Assert.That(ex!.InnerException, Is.SameAs(failure));
        Assert.That(dataEx!.Message, Does.Contain("holds an error, not a value"));
    }
}
