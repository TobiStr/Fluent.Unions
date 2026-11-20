using NUnit.Framework;
using VerifyNUnit;

namespace Fluent.Unions.Verify.Tests;

[TestFixture]
public class UnionSnapshotTests
{
    private static Exception Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Test]
    public Task ResultOk() => Verifier.Verify(Result.Ok);

    [Test]
    public Task ResultError() => Verifier.Verify(Result.Error(Thrown("disk full")));

    [Test]
    public Task ResultOfTOk() => Verifier.Verify(Result<int>.Ok(42));

    [Test]
    public Task ResultOfTOkObject() => Verifier.Verify(Result.From(new { Name = "Ada", Age = 36 }));

    [Test]
    public Task ResultOfTError() => Verifier.Verify(Result<int>.Error(Thrown("not a number")));

    [Test]
    public Task ResultOfTDefault() => Verifier.Verify(default(Result<int>));

    [Test]
    public Task ResultOfTErrorIsDataOk() => Verifier.Verify(new Result<string, AppError>("paid"));

    [Test]
    public Task ResultOfTErrorIsDataError() => Verifier.Verify(new Result<string, AppError>(new AppError(new Declined("insufficient funds"))));

    [Test]
    public Task ResultOfTErrorIsDataFromException() => Verifier.Verify(new Result<string, AppError>(AppError.FromException(Thrown("timeout"))));

    [Test]
    public Task ResultOfTErrorIsDataDefault() => Verifier.Verify(default(Result<string, AppError>));

    [Test]
    public Task ResultValidationError() => Verifier.Verify(Result<int>.Error(new ValidationError("Name is required.", "Name")));

    [Test]
    public Task ResultValidationErrors() => Verifier.Verify(Result<int>.Error(new ValidationErrors(
    [
        new ValidationError("Name is required.", "Name"),
        new ValidationError("Age must be positive.", "Age"),
        new ValidationError("The order is invalid."),
    ])));

    [Test]
    public Task OptionSome() => Verifier.Verify(Option.Some(7));

    [Test]
    public Task OptionNone() => Verifier.Verify(new Option<int>(Option.None));

    [Test]
    public Task OptionOfObject() => Verifier.Verify(Option.Some(new NotFound(3)));

    [Test]
    public Task LoadableNotLoaded() => Verifier.Verify(new Loadable<int>(Loadable.NotLoaded));

    [Test]
    public Task LoadableLoading() => Verifier.Verify(new Loadable<int>(Loadable.Loading));

    [Test]
    public Task LoadableLoaded() => Verifier.Verify(new Loadable<string>("ready"));

    [Test]
    public Task LoadableFaulted() => Verifier.Verify(new Loadable<string>(Thrown("offline")));

    [Test]
    public Task NestedInObject() => Verifier.Verify(new Order(1, Option.Some("gift"), Result.From(9.5m)));

    [Test]
    public Task NestedInObjectWithErrorAndNone() => Verifier.Verify(new Order(2, new Option<string>(Option.None), Result<decimal>.Error(Thrown("no price"))));

    [Test]
    public Task NestedInCollections() => Verifier.Verify(new object[]
    {
        Result.Ok,
        Result<int>.Ok(1),
        Result<int>.Error(Thrown("bad")),
        Option.Some("x"),
        new Option<string>(Option.None),
        new Loadable<int>(Loadable.Loading),
        new Dictionary<string, Result<int>> { ["first"] = 1, ["second"] = Thrown("second failed") },
    });

    [Test]
    public Task ResultInsideResult() => Verifier.Verify(Result.From(Option.Some(5)));

    [Test]
    public async Task NoStackTrace()
    {
        var verified = await Verifier.Verify(Result<int>.Error(Thrown("trace me")));
        var text = verified.Text;

        Assert.That(text, Does.Not.Contain("StackTrace"));
        Assert.That(text, Does.Not.Contain(" at "));
    }

    [Test]
    public async Task NoStackTraceInErrorData()
    {
        var verified = await Verifier.Verify(new Result<string, AppError>(AppError.FromException(Thrown("trace me"))));

        Assert.That(verified.Text, Does.Not.Contain("StackTrace"));
    }

    [Test]
    public void InitializeTwiceDoesNothing() => Assert.DoesNotThrow(VerifyFluentUnions.Initialize);
}
