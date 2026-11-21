namespace Fluent.Unions.Tests;

public class ResultTests
{
    [Test]
    public void OkTest()
    {
        var okResult = Result.Ok;

        Assert.True(okResult.IsSuccess);
        Assert.False(okResult.IsError);
        Assert.False(okResult.TryGetError(out _));
    }

    [Test]
    public void GetErrorTest()
    {
        var exception = new Exception("Test");
        var errorResult = Result.Error(exception);
        var okResult = Result.Ok;

        Assert.False(errorResult.IsSuccess);
        Assert.That(errorResult.GetError(), Is.SameAs(exception));
        Assert.That(errorResult.GetError().Message, Is.EqualTo("Test"));

        var ex = Assert.Throws<InvalidOperationException>(() => okResult.GetError());
        Assert.That(ex!.Message, Is.EqualTo("The Result is successful and holds no error."));
    }

    [Test]
    public void ImplicitCastTest()
    {
        var exception = new Exception("Test");
        Result errorResult = exception;
        Result okResult = new Ok();

        Assert.False(errorResult.IsSuccess);
        Assert.True(okResult.IsSuccess);
    }

    [Test]
    public void PatternMatchTest()
    {
        Result okResult = Result.Ok;
        Result errorResult = new InvalidOperationException("Test");

        Assert.That(Describe(okResult), Is.EqualTo("ok"));
        Assert.That(Describe(errorResult), Is.EqualTo("error: Test"));
    }

    [Test]
    public void DefaultResultTest()
    {
        var result = default(Result);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>());
        Assert.That(result.GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void GenericGetValueTest()
    {
        var okResult = Result<string>.Ok("Test");
        var errorResult = Result<string>.Error(new Exception("Test"));

        Assert.True(okResult.IsSuccess);
        Assert.That(okResult.GetValue(), Is.EqualTo("Test"));

        Assert.False(errorResult.IsSuccess);
        Assert.That(errorResult.GetError().Message, Is.EqualTo("Test"));
        var ex = Assert.Throws<InvalidOperationException>(() => errorResult.GetValue());
        Assert.That(ex!.Message, Is.EqualTo("Result<String> holds an error, not a value: Test"));
        Assert.That(ex.InnerException, Is.SameAs(errorResult.GetError()));
    }

    [Test]
    public void GenericGetErrorTest()
    {
        var exception = new Exception("Test");
        var errorResult = Result<string>.Error(exception);
        var okResult = Result<string>.Ok("Test");

        Assert.That(errorResult.GetError(), Is.SameAs(exception));
        var ex = Assert.Throws<InvalidOperationException>(() => okResult.GetError());
        Assert.That(ex!.Message, Is.EqualTo("Result<String> holds a value, not an error."));
    }

    [Test]
    public void GenericImplicitCastTest()
    {
        var exception = new Exception("Test");
        Result<string> errorResult = exception;
        Result<string> okResult = "Test";

        Assert.False(errorResult.IsSuccess);
        Assert.True(okResult.IsSuccess);
        Assert.That(okResult.GetValue(), Is.EqualTo("Test"));
    }

    [Test]
    public void OkNullThrowsTest()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Ok(null!));
    }

    [Test]
    public void FromInfersTheTypeTest()
    {
        Result<string> name = Result.From("Ada");

        Assert.That(name.GetValue(), Is.EqualTo("Ada"));
        Assert.Throws<ArgumentNullException>(() => Result.From<string>(null!));
    }

    [Test]
    public void ImplicitNullPayloadTest()
    {
        Result<string> result = (string)null!;

        Assert.True(result.IsError);
        Assert.That(result.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.Throws<InvalidOperationException>(() => result.GetValue());
    }

    [Test]
    public void DefaultGenericResultTest()
    {
        var result = default(Result<string>);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
        Assert.That(result.GetError().Message, Does.Contain("holds neither a value nor an error"));
        var ex = Assert.Throws<InvalidOperationException>(() => result.GetValue());
        Assert.That(ex!.InnerException!.Message, Is.EqualTo(result.GetError().Message));
    }

    [Test]
    public void ValueTypePayloadTest()
    {
        Result<int> okResult = 5;
        Result<int> errorResult = new Exception("Test");

        Assert.True(okResult.IsSuccess);
        Assert.That(okResult.GetValue(), Is.EqualTo(5));
        Assert.False(errorResult.IsSuccess);
    }

    [Test]
    public void GenericPatternMatchTest()
    {
        Result<string> okResult = "Test";
        Result<string> errorResult = new InvalidOperationException("Failed");

        Assert.That(Describe(okResult), Is.EqualTo("ok: Test"));
        Assert.That(Describe(errorResult), Is.EqualTo("error: Failed"));
    }

    [Test]
    public void ToStringTest()
    {
        Result<string> okResult = "Test";
        Result<int> intResult = 5;
        Result<string> errorResult = new Exception("Failed");

        Assert.That(okResult.ToString(), Is.EqualTo("Test"));
        Assert.That(intResult.ToString(), Is.EqualTo("5"));
        Assert.That(errorResult.ToString(), Is.EqualTo("Exception: Failed"));
        Assert.That(default(Result<string>).ToString(), Does.StartWith("InvalidOperationException: Result<String> holds neither a value nor an error"));
    }

    [Test]
    public void ToStringOfAnErrorLeavesOutStackTraceAndInnerException()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("outer", new FormatException("inner"));
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        var text = Result<int>.Error(thrown).ToString();

        Assert.That(text, Is.EqualTo("InvalidOperationException: outer"));
        Assert.That(Result.Error(thrown).ToString(), Is.EqualTo("InvalidOperationException: outer"));
    }

    [Test]
    public void ToStringOfAResultWithoutAValue()
    {
        Assert.That(Result.Ok.ToString(), Is.EqualTo("Ok"));
        Assert.That(Result.Error(new TimeoutException("too slow")).ToString(), Is.EqualTo("TimeoutException: too slow"));
        Assert.That(default(Result).ToString(), Does.StartWith("InvalidOperationException: Result holds neither a value nor an error"));
    }

    [Test]
    public void ConvertToResultTest()
    {
        Result<string> okGeneric = "Test";
        Result<string> errorGeneric = new Exception("Failed");

        Result okResult = okGeneric;
        Result errorResult = errorGeneric;
        Result defaultResult = default(Result<string>);

        Assert.True(okResult.IsSuccess);
        Assert.False(errorResult.IsSuccess);
        Assert.That(errorResult.GetError().Message, Is.EqualTo("Failed"));
        Assert.That(errorResult.GetError(), Is.SameAs(errorGeneric.GetError()));
        Assert.True(defaultResult.IsError);
        Assert.That(defaultResult.GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void IsValueTypeTest()
    {
        Assert.True(typeof(Result).IsValueType);
        Assert.True(typeof(Result<string>).IsValueType);
        Assert.True(typeof(System.Runtime.CompilerServices.IUnion).IsAssignableFrom(typeof(Result)));
        Assert.True(typeof(System.Runtime.CompilerServices.IUnion).IsAssignableFrom(typeof(Result<string>)));
    }

    [Test]
    public void ThrowTest()
    {
        var okResult = Result.Ok;
        var okResult2 = Result<string>.Ok("Test");

        var exception = new Exception("Test");
        Result errorResult = exception;
        Result<string> errorResult2 = exception;

        Assert.DoesNotThrow(() => okResult.Check());
        Assert.DoesNotThrow(() => okResult2.Check());

        Assert.Throws(exception.GetType(), () => errorResult.Check());
        Assert.Throws(exception.GetType(), () => errorResult2.Check());
    }

    [Test]
    public void CheckDefaultThrowsTest()
    {
        Assert.Throws<InvalidOperationException>(() => default(Result).Check());
        Assert.Throws<InvalidOperationException>(() => default(Result<string>).Check());
    }

    [Test]
    public void DefaultGenericGetValueMessageTest()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => default(Result<string>).GetValue());

        Assert.That(ex!.InnerException!.Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void ValueTypeDefaultTest()
    {
        var result = default(Result<int>);

        Assert.True(result.IsError);
        var ex = Assert.Throws<InvalidOperationException>(() => result.GetValue());
        Assert.That(ex!.InnerException!.Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void ValueTypeZeroPayloadIsSuccessTest()
    {
        Result<int> result = 0;

        Assert.True(result.IsSuccess);
        Assert.That(result.GetValue(), Is.EqualTo(0));
    }

    [Test]
    public void ValueTypePatternMatchTest()
    {
        Result<int> okResult = 7;
        Result<int> errorResult = new InvalidOperationException("Failed");

        Assert.That(Describe(okResult), Is.EqualTo("ok: 7"));
        Assert.That(Describe(errorResult), Is.EqualTo("error: Failed"));
    }

    [Test]
    public void ValueTypeConvertToResultTest()
    {
        Result<int> okGeneric = 5;
        Result<int> errorGeneric = new Exception("Failed");

        Result okResult = okGeneric;
        Result errorResult = errorGeneric;

        Assert.True(okResult.IsSuccess);
        Assert.False(errorResult.IsSuccess);
        Assert.That(errorResult.GetError(), Is.SameAs(errorGeneric.GetError()));
    }

    [Test]
    public void ValueTypeCheckTest()
    {
        Result<int> okResult = 5;
        Result<int> errorResult = new ArgumentException("Failed");

        Assert.That(okResult.Check().GetValue(), Is.EqualTo(5));
        Assert.Throws<ArgumentException>(() => errorResult.Check());
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Check());
    }

    [Test]
    public void GenericGetErrorOnDefaultNamesTheResultTypeTest()
    {
        var error = default(Result<int>).GetError();

        Assert.That(error, Is.TypeOf<InvalidOperationException>());
        Assert.That(error.Message, Does.StartWith("Result<Int32> holds neither a value nor an error"));
    }

    [Test]
    public void CheckReturnsSameResultTest()
    {
        var okResult = Result.Ok;
        Result<string> okGeneric = "Test";

        Assert.True(okResult.Check().IsSuccess);
        Assert.That(okGeneric.Check().GetValue(), Is.EqualTo("Test"));
    }

    [Test]
    public void ErrorOfAGenericTypeWithoutABacktickInItsNameIsReported()
    {
        var result = default(Result<List<int>.Enumerator>).Select(x => 1);

        Assert.That(result.GetError().Message, Does.Contain("Result<Enumerator<Int32>>"));
    }

    [Test]
    public void ResultTypesReportErrorAndValueTypesRepeatedly()
    {
        for (var i = 0; i < 2; i++)
        {
            Assert.That(Fluent.Unions.Abstractions.ResultTypes.ErrorType(typeof(Result<int>)), Is.EqualTo(typeof(Exception)));
            Assert.That(Fluent.Unions.Abstractions.ResultTypes.ValueType(typeof(Result<int>)), Is.EqualTo(typeof(int)));
            Assert.That(Fluent.Unions.Abstractions.ResultTypes.ValueType(typeof(Result)), Is.Null);
            Assert.That(Fluent.Unions.Abstractions.ResultTypes.ErrorType(typeof(Option<int>)), Is.Null);
            Assert.That(Fluent.Unions.Abstractions.ResultTypes.IsResult(typeof(string)), Is.False);
        }
    }

    private static string Describe(Result<int> result) => result switch
    {
        int i => $"ok: {i}",
        Exception e => $"error: {e.Message}"
    };

    private static string Describe(Result result) => result switch
    {
        Ok => "ok",
        Exception e => $"error: {e.Message}"
    };

    private static string Describe(Result<string> result) => result switch
    {
        string s => $"ok: {s}",
        Exception e => $"error: {e.Message}"
    };
}
