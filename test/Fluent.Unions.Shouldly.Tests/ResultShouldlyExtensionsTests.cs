using NUnit.Framework;
using Shouldly;

namespace Fluent.Unions.Shouldly.Tests;

public class ResultShouldlyExtensionsTests
{
    [Test]
    public void ShouldBeOk_OnOkResult_ReturnsTheResult()
    {
        Result result = Result.Ok;

        result.ShouldBeOk().IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldBeOk_OnErrorResult_NamesTheError()
    {
        Result result = new TimeoutException("too slow");

        var exception = Should.Throw<ShouldAssertException>(() => result.ShouldBeOk());

        exception.Message.ShouldContain("Expected an ok result, but it failed with TimeoutException: too slow");
    }

    [Test]
    public void ShouldBeOk_OnDefaultResult_Fails()
    {
        Should.Throw<ShouldAssertException>(() => default(Result).ShouldBeOk());
    }

    [Test]
    public void ShouldBeOk_WithCustomMessage_AppendsIt()
    {
        Result result = new TimeoutException("too slow");

        var exception = Should.Throw<ShouldAssertException>(() => result.ShouldBeOk("order must be saved"));

        exception.Message.ShouldContain("order must be saved");
    }

    [Test]
    public void ShouldBeOkOfT_OnOkResult_ReturnsTheValue()
    {
        Result<int>.Ok(42).ShouldBeOk().ShouldBe(42);
    }

    [Test]
    public void ShouldBeOkOfT_OnErrorResult_NamesTheError()
    {
        var result = Result<int>.Error(new InvalidOperationException("broken"));

        var exception = Should.Throw<ShouldAssertException>(() => result.ShouldBeOk());

        exception.Message.ShouldContain("Expected an ok result, but it failed with InvalidOperationException: broken");
    }

    [Test]
    public void ShouldBeError_OnErrorResult_ReturnsTheException()
    {
        var error = new TimeoutException("too slow");
        Result result = error;

        result.ShouldBeError().ShouldBeSameAs(error);
    }

    [Test]
    public void ShouldBeError_OnOkResult_Fails()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Result.Ok.ShouldBeError());

        exception.Message.ShouldContain("Expected an error result, but it was ok");
    }

    [Test]
    public void ShouldBeError_OnDefaultResult_Passes()
    {
        default(Result).ShouldBeError();
    }

    [Test]
    public void ShouldBeErrorOfT_OnErrorResult_ReturnsTheException()
    {
        var error = new TimeoutException("too slow");

        Result<int>.Error(error).ShouldBeError().ShouldBeSameAs(error);
    }

    [Test]
    public void ShouldBeErrorOfT_ChainsIntoShouldlyTypeChecks()
    {
        Result<int>.Error(new ArgumentNullException("name")).ShouldBeError().ShouldBeAssignableTo<ArgumentException>();
        Result<int>.Error(new ArgumentNullException("name")).ShouldBeError().ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("name");
    }

    [Test]
    public void ShouldBeErrorOfT_OnOkResult_NamesTheValue()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Result<int>.Ok(42).ShouldBeError());

        exception.Message.ShouldContain("Expected an error result, but it was ok with value 42");
    }

    [Test]
    public void ShouldBeErrorOfType_OnMatchingError_ReturnsTheTypedException()
    {
        Result result = new ArgumentException("bad", "name");

        result.ShouldBeError<ArgumentException>().ParamName.ShouldBe("name");
    }

    [Test]
    public void ShouldBeErrorOfType_OnDerivedError_Passes()
    {
        Result result = new ArgumentNullException("name");

        result.ShouldBeError<ArgumentException>();
    }

    [Test]
    public void ShouldBeErrorOfType_OnOtherError_NamesTheActualError()
    {
        Result result = new TimeoutException("too slow");

        var exception = Should.Throw<ShouldAssertException>(() => result.ShouldBeError<ArgumentException>());

        exception.Message.ShouldContain("Expected an error of type ArgumentException, but it failed with TimeoutException: too slow");
    }

    [Test]
    public void ShouldBeErrorOfType_OnOkResult_Fails()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Result.Ok.ShouldBeError<ArgumentException>());

        exception.Message.ShouldContain("Expected an error of type ArgumentException, but the result was ok");
    }

    [Test]
    public void ShouldBeSome_OnOption_ReturnsTheValue()
    {
        Option.Some("ada").ShouldBeSome().ShouldBe("ada");
    }

    [Test]
    public void ShouldBeSome_OnNone_Fails()
    {
        var exception = Should.Throw<ShouldAssertException>(() => default(Option<string>).ShouldBeSome());

        exception.Message.ShouldContain("Expected an option with a value, but it was none");
    }

    [Test]
    public void ShouldBeNone_OnNoneAndDefault_Passes()
    {
        default(Option<string>).ShouldBeNone();
        new Option<string>(Option.None).ShouldBeNone();
    }

    [Test]
    public void ShouldBeNone_OnSome_NamesTheValue()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Option.Some("ada").ShouldBeNone());

        exception.Message.ShouldContain("Expected an option without a value, but it was some with value ada");
    }

    [Test]
    public void ShouldBeNone_WithCustomMessage_AppendsIt()
    {
        var exception = Should.Throw<ShouldAssertException>(() => Option.Some("ada").ShouldBeNone("nickname must be unset"));

        exception.Message.ShouldContain("nickname must be unset");
    }
}
