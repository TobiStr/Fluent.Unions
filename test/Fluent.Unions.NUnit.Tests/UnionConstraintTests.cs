using NUnit.Framework;

namespace Fluent.Unions.NUnit.Tests;

public class UnionConstraintTests
{
    private static string FailureOf(Action assertion) => Assert.Throws<AssertionException>(assertion)!.Message;

    [Test]
    public void Ok_OnOkResults_Passes()
    {
        Assert.That(Result.Ok, IsResult.Ok);
        Assert.That(Result<int>.Ok(42), IsResult.Ok);
    }

    [Test]
    public void Ok_OnErrorResult_NamesTheError()
    {
        Result result = new TimeoutException("too slow");

        var message = FailureOf(() => Assert.That(result, IsResult.Ok));

        Assert.That(message, Does.Contain("Expected: an ok result").And.Contain("But was:  failed with TimeoutException: too slow"));
    }

    [Test]
    public void Ok_OnDefaultResult_Fails()
    {
        var message = FailureOf(() => Assert.That(default(Result<int>), IsResult.Ok));

        Assert.That(message, Does.Contain("a default result, which is an error"));
    }

    [Test]
    public void Error_OnErrorResults_Passes()
    {
        Assert.That(new Result(new TimeoutException()), IsResult.Error);
        Assert.That(Result<int>.Error(new TimeoutException()), IsResult.Error);
        Assert.That(default(Result), IsResult.Error);
    }

    [Test]
    public void Error_OnOkResult_NamesTheValue()
    {
        var message = FailureOf(() => Assert.That(Result<int>.Ok(42), IsResult.Error));

        Assert.That(message, Does.Contain("Expected: an error result").And.Contain("But was:  ok with value 42"));
    }

    [Test]
    public void Error_OnOkResultWithoutValue_SaysOk()
    {
        var message = FailureOf(() => Assert.That(Result.Ok, IsResult.Error));

        Assert.That(message, Does.Contain("But was:  ok").And.Not.Contain("with value"));
    }

    [Test]
    public void OkWith_OnMatchingValue_Passes()
    {
        Assert.That(Result<int>.Ok(42), IsResult.OkWith(Is.EqualTo(42)));
        Assert.That(Result<string>.Ok("ada"), IsResult.OkWith(Does.StartWith("a")));
    }

    [Test]
    public void OkWith_OnOtherValue_NamesTheValue()
    {
        var message = FailureOf(() => Assert.That(Result<int>.Ok(41), IsResult.OkWith(Is.EqualTo(42))));

        Assert.That(message, Does.Contain("Expected: an ok result with value 42").And.Contain("But was:  ok with value 41"));
    }

    [Test]
    public void OkWith_OnErrorResult_NamesTheError()
    {
        var message = FailureOf(() => Assert.That(Result<int>.Error(new TimeoutException("too slow")), IsResult.OkWith(Is.EqualTo(42))));

        Assert.That(message, Does.Contain("failed with TimeoutException: too slow"));
    }

    [Test]
    public void ErrorWith_OnMatchingException_Passes()
    {
        Assert.That(Result<int>.Error(new TimeoutException("too slow")), IsResult.ErrorWith(Has.Message.Contains("slow")));
    }

    [Test]
    public void ErrorWith_OnOtherException_Fails()
    {
        var message = FailureOf(() => Assert.That(Result<int>.Error(new TimeoutException("too slow")), IsResult.ErrorWith(Has.Message.EqualTo("other"))));

        Assert.That(message, Does.Contain("an error result with exception").And.Contain("failed with TimeoutException: too slow"));
    }

    [Test]
    public void ErrorOfType_OnMatchingAndDerivedErrors_Passes()
    {
        Assert.That(Result<int>.Error(new ArgumentNullException("name")), IsResult.ErrorOfType<ArgumentNullException>());
        Assert.That(Result<int>.Error(new ArgumentNullException("name")), IsResult.ErrorOfType<ArgumentException>());
        Assert.That(new Result(new TimeoutException()), IsResult.ErrorOfType<TimeoutException>());
    }

    [Test]
    public void ErrorOfType_OnOtherError_NamesTheActualError()
    {
        var message = FailureOf(() => Assert.That(Result<int>.Error(new TimeoutException("too slow")), IsResult.ErrorOfType<ArgumentException>()));

        Assert.That(message, Does.Contain("Expected: an error result of type ArgumentException").And.Contain("But was:  failed with TimeoutException: too slow"));
    }

    [Test]
    public void ErrorOfType_OnOkResult_Fails()
    {
        var message = FailureOf(() => Assert.That(Result.Ok, IsResult.ErrorOfType<ArgumentException>()));

        Assert.That(message, Does.Contain("But was:  ok"));
    }

    [Test]
    public void Some_OnOption_Passes()
    {
        Assert.That(Option.Some("ada"), IsOption.Some);
    }

    [Test]
    public void Some_OnNone_Fails()
    {
        var message = FailureOf(() => Assert.That(default(Option<string>), IsOption.Some));

        Assert.That(message, Does.Contain("Expected: an option with a value").And.Contain("But was:  none"));
    }

    [Test]
    public void SomeWith_OnMatchingValue_Passes()
    {
        Assert.That(Option.Some("ada"), IsOption.SomeWith(Is.EqualTo("ada")));
    }

    [Test]
    public void SomeWith_OnOtherValueAndNone_Fails()
    {
        var other = FailureOf(() => Assert.That(Option.Some("ada"), IsOption.SomeWith(Is.EqualTo("grace"))));
        var none = FailureOf(() => Assert.That(default(Option<string>), IsOption.SomeWith(Is.EqualTo("grace"))));

        Assert.That(other, Does.Contain("But was:  some with value \"ada\""));
        Assert.That(none, Does.Contain("But was:  none"));
    }

    [Test]
    public void None_OnNoneAndDefault_Passes()
    {
        Assert.That(default(Option<string>), IsOption.None);
        Assert.That(new Option<string>(Option.None), IsOption.None);
    }

    [Test]
    public void None_OnSome_NamesTheValue()
    {
        var message = FailureOf(() => Assert.That(Option.Some("ada"), IsOption.None));

        Assert.That(message, Does.Contain("Expected: an option without a value").And.Contain("But was:  some with value \"ada\""));
    }

    [Test]
    public void Constraints_OnOtherTypes_FailWithTheType()
    {
        var message = FailureOf(() => Assert.That("text", IsResult.Ok));

        Assert.That(message, Does.Contain("\"text\", which is not a result or an option"));
    }

    [Test]
    public void Constraints_OnTheOtherUnion_Fail()
    {
        Assert.That(() => Assert.That(Option.Some(1), IsResult.Ok), Throws.TypeOf<AssertionException>());
        Assert.That(() => Assert.That(Result.Ok, IsOption.Some), Throws.TypeOf<AssertionException>());
    }

    [Test]
    public void Constraints_ComposeWithNunitOperators()
    {
        Assert.That(Result<int>.Ok(42), IsResult.Ok.And.Not.Null);
        Assert.That(Result<int>.Ok(42), IsResult.OkWith(Is.GreaterThan(40)) & Is.Not.Null);
    }

    [Test]
    public void OkWith_WithNullConstraint_Throws()
    {
        Assert.That(() => IsResult.OkWith(null!), Throws.ArgumentNullException);
        Assert.That(() => IsResult.ErrorWith(null!), Throws.ArgumentNullException);
        Assert.That(() => IsOption.SomeWith(null!), Throws.ArgumentNullException);
    }
}
