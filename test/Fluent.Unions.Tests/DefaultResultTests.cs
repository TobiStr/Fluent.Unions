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

    [Test]
    public void SwitchExpressionOverADefaultResultThrows()
    {
        Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(() => _ = DefaultOf<Result<int>>() switch
        {
            int => "ok",
            Exception => "error"
        });
        Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(() => _ = DefaultOf<Result>() switch
        {
            Ok => "ok",
            Exception => "error"
        });
        Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(() => _ = DefaultOf<CheckoutResult<int>>() switch
        {
            int => "ok",
            CheckoutError => "error"
        });
    }

    [Test]
    public void SwitchExpressionWithANullArmHandlesADefaultResult()
    {
        var described = DefaultOf<Result<int>>() switch
        {
            int => "ok",
            Exception => "error",
            null => "default"
        };

        Assert.That(described, Is.EqualTo("default"));
    }

    [Test]
    public void SwitchStatementOverADefaultResultMatchesNoCase()
    {
        var described = "none";
        switch (DefaultOf<Result<int>>())
        {
            case int:
                described = "ok";
                break;
            case Exception:
                described = "error";
                break;
        }

        Assert.That(described, Is.EqualTo("none"));
    }

    [Test]
    public void IsPatternsSeeADefaultResultAsNull()
    {
        var result = DefaultOf<Result<int>>();

        Assert.False(result is int);
        Assert.False(result is Exception);
        Assert.True(result is null);
        Assert.That(result.Value, Is.Null);
        Assert.That(DefaultOf<CheckoutResult<int>>().Value, Is.Null);
    }

    [Test]
    public void GetValueOnADefaultResultThrowsWithTheUninitializedError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => DefaultOf<Result<int>>().GetValue());

        Assert.That(ex!.InnerException!.Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void DeconstructOfADefaultResultGivesOnlyTheError()
    {
        var (value, error) = DefaultOf<Result<string>>();

        Assert.That(value, Is.Null);
        Assert.That(error!.Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void ToStringOfADefaultResultDescribesTheUninitializedError()
    {
        var checkout = DefaultOf<CheckoutResult<int>>();

        Assert.That(DefaultOf<Result<int>>().ToString(), Does.Contain(Uninitialized));
        Assert.That(DefaultOf<Result>().ToString(), Does.Contain(Uninitialized));
        Assert.That(checkout.ToString(), Is.EqualTo(checkout.GetError().ToString()));
    }

    [Test]
    public void DefaultResultsAreEqualAndShareAHashCode()
    {
        Assert.That(DefaultOf<Result<int>>(), Is.EqualTo(DefaultOf<Result<int>>()));
        Assert.That(DefaultOf<Result<int>>().GetHashCode(), Is.EqualTo(DefaultOf<Result<int>>().GetHashCode()));
        Assert.That(DefaultOf<Result>(), Is.EqualTo(DefaultOf<Result>()));
    }

    [Test]
    public void DefaultResultEqualsANullPayloadResult()
    {
        Result<string> fromNull = (string)null!;

        Assert.That(DefaultOf<Result<string>>(), Is.EqualTo(fromNull));
    }

    [Test]
    public void DefaultResultDoesNotEqualOkOrItsOwnPropagatedError()
    {
        var result = DefaultOf<Result<int>>();

        Assert.That(result, Is.Not.EqualTo(Result<int>.Ok(0)));
        Assert.That(result, Is.Not.EqualTo(result.Select(value => value)));
        Assert.That(result.GetError(), Is.Not.SameAs(result.GetError()));
    }

    [Test]
    public void ArrayElementsAndUninitializedFieldsAreDefaultResults()
    {
        var results = new Result<int>[2];
        var holder = new Holder();

        Assert.That(results.All(result => result.IsError));
        Assert.True(holder.Result.IsError);
        Assert.True(holder.Unit.IsError);
        Assert.That(holder.Result.GetError().Message, Does.Contain(Uninitialized));
    }

    [Test]
    public void KeyOperatorsTreatADefaultResultAsAnError()
    {
        var result = DefaultOf<Result<int>>();
        Result<int>[] results = [1, result];

        Assert.That(result.GetValueOrDefault(7), Is.EqualTo(7));
        Assert.False(result.ToOption().HasValue);
        Assert.True(result.ToLoadable().IsFaulted);
        Assert.That(result.Catch(_ => Result<int>.Ok(3)).GetValue(), Is.EqualTo(3));
        Assert.That(results.Combine().GetError().Message, Does.Contain(Uninitialized));
        Assert.That(results.Errors().Count(), Is.EqualTo(1));
        Assert.That(results.Values(), Is.EqualTo(new[] { 1 }));
    }

    private static T DefaultOf<T>() where T : struct => (new T[1])[0];

    private sealed class Holder
    {
        public Result<int> Result { get; }

        public Result Unit { get; }
    }
}
