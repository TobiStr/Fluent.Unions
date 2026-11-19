namespace Fluent.Unions.Tests;

public class ResultTests
{
    [Test]
    public void OkTest()
    {
        var okResult = Result.Ok;

        Assert.True(okResult.Success);
        Assert.That(okResult.Message, Is.EqualTo(""));
    }

    [Test]
    public void GetErrorTest()
    {
        var exception = new Exception("Test");
        var errorResult = Result.Error(exception);
        var okResult = Result.Ok;

        Assert.False(errorResult.Success);
        Assert.That(errorResult.GetError(), Is.SameAs(exception));
        Assert.That(errorResult.Message, Is.EqualTo("Test"));

        var ex = Assert.Throws<InvalidOperationException>(() => okResult.GetError());
        Assert.That(ex!.Message, Is.EqualTo("Error property for this Result not set."));
    }

    [Test]
    public void ImplicitCastTest()
    {
        var exception = new Exception("Test");
        Result errorResult = exception;
        Result okResult = new Ok();

        Assert.False(errorResult.Success);
        Assert.True(okResult.Success);
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

        Assert.False(result.Success);
        Assert.That(result.Message, Is.EqualTo(""));
        Assert.Throws<InvalidOperationException>(() => result.GetError());
    }

    [Test]
    public void GenericGetOkTest()
    {
        var okResult = Result<string>.Ok("Test");
        var errorResult = Result<string>.Error(new Exception("Test"));

        Assert.True(okResult.Success);
        Assert.That(okResult.GetOk(), Is.EqualTo("Test"));

        Assert.False(errorResult.Success);
        Assert.That(errorResult.Message, Is.EqualTo("Test"));
        var ex = Assert.Throws<InvalidOperationException>(() => errorResult.GetOk());
        Assert.That(ex!.Message, Does.StartWith("Operation for Result<System.String> was not successful"));
    }

    [Test]
    public void GenericGetErrorTest()
    {
        var exception = new Exception("Test");
        var errorResult = Result<string>.Error(exception);
        var okResult = Result<string>.Ok("Test");

        Assert.That(errorResult.GetError(), Is.SameAs(exception));
        var ex = Assert.Throws<InvalidOperationException>(() => okResult.GetError());
        Assert.That(ex!.Message, Is.EqualTo("Error property for Result<System.String> not set."));
    }

    [Test]
    public void GenericImplicitCastTest()
    {
        var exception = new Exception("Test");
        Result<string> errorResult = exception;
        Result<string> okResult = "Test";

        Assert.False(errorResult.Success);
        Assert.True(okResult.Success);
        Assert.That(okResult.GetOk(), Is.EqualTo("Test"));
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

        Assert.That(name.GetOk(), Is.EqualTo("Ada"));
        Assert.Throws<ArgumentNullException>(() => Result.From<string>(null!));
    }

    [Test]
    public void ImplicitNullPayloadTest()
    {
        Result<string> result = (string)null!;

        Assert.False(result.Success);
        var ex = Assert.Throws<InvalidOperationException>(() => result.GetOk());
        Assert.That(ex!.Message, Is.EqualTo("Payload for Result<System.String> was not set."));
    }

    [Test]
    public void DefaultGenericResultTest()
    {
        var result = default(Result<string>);

        Assert.False(result.Success);
        Assert.That(result.Message, Is.EqualTo(""));
        Assert.Throws<InvalidOperationException>(() => result.GetOk());
        var ex = Assert.Throws<InvalidOperationException>(() => result.GetError());
        Assert.That(ex!.Message, Is.EqualTo("Error property for Result<System.String> not set."));
    }

    [Test]
    public void ValueTypePayloadTest()
    {
        Result<int> okResult = 5;
        Result<int> errorResult = new Exception("Test");

        Assert.True(okResult.Success);
        Assert.That(okResult.GetOk(), Is.EqualTo(5));
        Assert.False(errorResult.Success);
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
        Assert.That(errorResult.ToString(), Is.EqualTo("System.Exception: Failed"));
        Assert.That(default(Result<string>).ToString(), Is.EqualTo(""));
    }

    [Test]
    public void ConvertToResultTest()
    {
        Result<string> okGeneric = "Test";
        Result<string> errorGeneric = new Exception("Failed");

        Result okResult = okGeneric;
        Result errorResult = errorGeneric;
        Result defaultResult = default(Result<string>);

        Assert.True(okResult.Success);
        Assert.That(okResult.Message, Is.EqualTo(""));
        Assert.False(errorResult.Success);
        Assert.That(errorResult.Message, Is.EqualTo("Failed"));
        Assert.That(errorResult.GetError(), Is.SameAs(errorGeneric.GetError()));
        Assert.False(defaultResult.Success);
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
    public void DefaultGenericGetOkMessageTest()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => default(Result<string>).GetOk());

        Assert.That(ex!.Message, Is.EqualTo("Payload for Result<System.String> was not set."));
    }

    [Test]
    public void ValueTypeDefaultTest()
    {
        var result = default(Result<int>);

        Assert.False(result.Success);
        var ex = Assert.Throws<InvalidOperationException>(() => result.GetOk());
        Assert.That(ex!.Message, Is.EqualTo("Payload for Result<System.Int32> was not set."));
    }

    [Test]
    public void ValueTypeZeroPayloadIsSuccessTest()
    {
        Result<int> result = 0;

        Assert.True(result.Success);
        Assert.That(result.GetOk(), Is.EqualTo(0));
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

        Assert.True(okResult.Success);
        Assert.False(errorResult.Success);
        Assert.That(errorResult.GetError(), Is.SameAs(errorGeneric.GetError()));
    }

    [Test]
    public void ValueTypeCheckTest()
    {
        Result<int> okResult = 5;
        Result<int> errorResult = new ArgumentException("Failed");

        Assert.That(okResult.Check().GetOk(), Is.EqualTo(5));
        Assert.Throws<ArgumentException>(() => errorResult.Check());
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Check());
    }

    [Test]
    public void GenericGetErrorOnDefaultMessageTest()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => default(Result<int>).GetError());

        Assert.That(ex!.Message, Is.EqualTo("Error property for Result<System.Int32> not set."));
    }

    [Test]
    public void CheckReturnsSameResultTest()
    {
        var okResult = Result.Ok;
        Result<string> okGeneric = "Test";

        Assert.True(okResult.Check().Success);
        Assert.That(okGeneric.Check().GetOk(), Is.EqualTo("Test"));
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
