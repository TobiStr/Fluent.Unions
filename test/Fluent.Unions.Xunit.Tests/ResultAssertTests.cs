using Xunit;
using Xunit.Sdk;

namespace Fluent.Unions.Xunit.Tests;

public class ResultAssertTests
{
    private static string FailureOf(Action assertion) => Assert.Throws<FailException>(assertion).Message;

    [Fact]
    public void Ok_OnOkResult_Passes()
    {
        ResultAssert.Ok(Result.Ok);
    }

    [Fact]
    public void Ok_OnErrorResult_NamesTheError()
    {
        Result result = new TimeoutException("too slow");

        var message = FailureOf(() => ResultAssert.Ok(result));

        Assert.Equal("Expected an ok result, but it failed with TimeoutException: too slow", message);
    }

    [Fact]
    public void Ok_OnDefaultResult_Fails()
    {
        Assert.Throws<FailException>(() => ResultAssert.Ok(default(Result)));
    }

    [Fact]
    public void OkOfT_OnOkResult_ReturnsTheValue()
    {
        Assert.Equal(42, ResultAssert.Ok(Result<int>.Ok(42)));
    }

    [Fact]
    public void OkOfT_OnErrorResult_NamesTheError()
    {
        var result = Result<int>.Error(new InvalidOperationException("broken"));

        var message = FailureOf(() => ResultAssert.Ok(result));

        Assert.Equal("Expected an ok result, but it failed with InvalidOperationException: broken", message);
    }

    [Fact]
    public void Error_OnErrorResult_ReturnsTheException()
    {
        var error = new TimeoutException("too slow");
        Result result = error;

        Assert.Same(error, ResultAssert.Error(result));
    }

    [Fact]
    public void Error_OnOkResult_Fails()
    {
        var message = FailureOf(() => ResultAssert.Error(Result.Ok));

        Assert.Equal("Expected an error result, but it was ok", message);
    }

    [Fact]
    public void Error_OnDefaultResult_Passes()
    {
        Assert.NotNull(ResultAssert.Error(default(Result)));
    }

    [Fact]
    public void ErrorOfT_OnErrorResult_ReturnsTheException()
    {
        var error = new TimeoutException("too slow");

        Assert.Same(error, ResultAssert.Error(Result<int>.Error(error)));
    }

    [Fact]
    public void ErrorOfT_OnOkResult_NamesTheValue()
    {
        var message = FailureOf(() => ResultAssert.Error(Result<int>.Ok(42)));

        Assert.Equal("Expected an error result, but it was ok with value 42", message);
    }

    [Fact]
    public void ErrorOfType_OnMatchingError_ReturnsTheTypedException()
    {
        Result result = new ArgumentException("bad", "name");

        Assert.Equal("name", ResultAssert.Error<ArgumentException>(result).ParamName);
    }

    [Fact]
    public void ErrorOfType_OnDerivedError_Passes()
    {
        Result result = new ArgumentNullException("name");

        Assert.IsType<ArgumentNullException>(ResultAssert.Error<ArgumentException>(result));
    }

    [Fact]
    public void ErrorOfType_OnResultOfT_ConvertsImplicitly()
    {
        var result = Result<int>.Error(new TimeoutException("too slow"));

        Assert.IsType<TimeoutException>(ResultAssert.Error<TimeoutException>(result));
    }

    [Fact]
    public void ErrorOfType_OnOtherError_NamesTheActualError()
    {
        Result result = new TimeoutException("too slow");

        var message = FailureOf(() => ResultAssert.Error<ArgumentException>(result));

        Assert.Equal("Expected an error of type ArgumentException, but it failed with TimeoutException: too slow", message);
    }

    [Fact]
    public void ErrorOfType_OnOkResult_Fails()
    {
        var message = FailureOf(() => ResultAssert.Error<ArgumentException>(Result.Ok));

        Assert.Equal("Expected an error of type ArgumentException, but the result was ok", message);
    }

    [Fact]
    public void Some_OnOption_ReturnsTheValue()
    {
        Assert.Equal("ada", OptionAssert.Some(Option.Some("ada")));
    }

    [Fact]
    public void Some_OnNone_Fails()
    {
        var message = FailureOf(() => OptionAssert.Some(default(Option<string>)));

        Assert.Equal("Expected an option with a value, but it was none", message);
    }

    [Fact]
    public void None_OnNoneAndDefault_Passes()
    {
        OptionAssert.None(default(Option<string>));
        OptionAssert.None(new Option<string>(Option.None));
    }

    [Fact]
    public void None_OnSome_NamesTheValue()
    {
        var message = FailureOf(() => OptionAssert.None(Option.Some("ada")));

        Assert.Equal("Expected an option without a value, but it was some with value ada", message);
    }
}
