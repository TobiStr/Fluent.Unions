namespace Fluent.Unions.Tests;

public class ErrorOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void CatchRecoversWithValue()
    {
        Result<string> result = Failure;

        var recovered = result.Catch(_ => "fallback");

        Assert.That(recovered.GetOk(), Is.EqualTo("fallback"));
    }

    [Test]
    public void CatchTranslatesError()
    {
        Result<string> result = Failure;

        var translated = result.Catch(error => new ApplicationException("Translated", error));

        Assert.That(translated.GetError(), Is.TypeOf<ApplicationException>());
        Assert.That(translated.GetError().InnerException, Is.SameAs(Failure));
    }

    [Test]
    public void CatchDoesNotRunForValueOrEmpty()
    {
        Result<string> ok = "value";
        var calls = 0;

        var okResult = ok.Catch(_ =>
        {
            calls++;
            return "fallback";
        });
        var emptyResult = default(Result<string>).Catch(_ =>
        {
            calls++;
            return "fallback";
        });

        Assert.That(okResult.GetOk(), Is.EqualTo("value"));
        Assert.That(emptyResult.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void CatchCapturesExceptionFromHandler()
    {
        Result<string> result = Failure;
        var thrown = new ApplicationException("Handler failed");

        var handled = result.Catch(_ => throw thrown);

        Assert.That(handled.GetError(), Is.SameAs(thrown));
    }

    [Test]
    public void TypedCatchHandlesMatchingException()
    {
        Result<string> result = new FileNotFoundException("Missing");

        var recovered = result.Catch((IOException _) => "default content");

        Assert.That(recovered.GetOk(), Is.EqualTo("default content"));
    }

    [Test]
    public void TypedCatchPassesOtherExceptionsThrough()
    {
        Result<string> result = Failure;

        var notHandled = result.Catch((IOException _) => "default content");

        Assert.That(notHandled.GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void CatchWithFallbackResult()
    {
        Result<int> result = Failure;
        Result<int> ok = 1;

        Assert.That(result.Catch(2).GetOk(), Is.EqualTo(2));
        Assert.That(ok.Catch(2).GetOk(), Is.EqualTo(1));
    }

    [Test]
    public void CatchOnNonGenericResult()
    {
        var recovered = Result.Error(Failure).Catch(_ => Result.Ok);
        var typed = Result.Error(new FileNotFoundException()).Catch((IOException _) => Result.Ok);
        var notHandled = Result.Error(Failure).Catch((IOException _) => Result.Ok);
        var fallback = Result.Error(Failure).Catch(Result.Ok);

        Assert.True(recovered.Success);
        Assert.True(typed.Success);
        Assert.That(notHandled.GetError(), Is.SameAs(Failure));
        Assert.True(fallback.Success);
    }

    [Test]
    public void CatchAfterSelectHandlesSelectorException()
    {
        Result<string> result = "x";

        var parsed = result
            .Select(int.Parse)
            .Catch((FormatException _) => -1);

        Assert.That(parsed.GetOk(), Is.EqualTo(-1));
    }
}
