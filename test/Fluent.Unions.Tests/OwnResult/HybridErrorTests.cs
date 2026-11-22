using Fluent.Unions.Abstractions;
using Fluent.Unions.Serialization;
using Is = NUnit.Framework.Is;

namespace HybridErrorSample;

public abstract class ShopException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class NotFoundException(string message) : ShopException(message);

public sealed class InvalidInputException(Exception validation) : ShopException("The input is invalid.", validation);

public sealed class UnexpectedShopException(Exception inner) : ShopException("An unexpected error occurred.", inner);

public union ShopError(NotFoundException, InvalidInputException, UnexpectedShopException) : IFromException<ShopError>
{
    public static ShopError FromException(Exception exception) => exception switch
    {
        NotFoundException notFound => notFound,
        InvalidInputException invalid => invalid,
        UnexpectedShopException unexpected => unexpected,
        ValidationError or ValidationErrors => new InvalidInputException(exception),
        _ => new UnexpectedShopException(exception)
    };
}

[FluentResult<ShopError>]
public readonly partial struct Result<T>;

[FluentResult<ShopError>(Capture = ExceptionCapture.Declared)]
public readonly partial struct StrictResult<T>;

public static class Drift
{
    public sealed class NotListedException(string message) : ShopException(message);
}

public class HybridErrorTests
{
    private static readonly Result<int> Missing = new NotFoundException("No order 7.");

    [Test]
    public void CheckRethrowsTheCaseSoACatchOfTheBaseCatchesIt()
    {
        ShopException? caught = null;

        try
        {
            Missing.Check();
        }
        catch (ShopException exception)
        {
            caught = exception;
        }

        Assert.That(caught, Is.TypeOf<NotFoundException>());
        Assert.That(caught!.Message, Is.EqualTo("No order 7."));
    }

    [Test]
    public void TypedCatchOfTheBaseHandlesEveryCase()
    {
        var recovered = Missing.Catch((ShopException exception) => Result<int>.Ok(exception.Message.Length));

        Assert.That(recovered.GetValue(), Is.EqualTo("No order 7.".Length));
    }

    [Test]
    public void SwitchOverTheCasesNeedsNoFallbackArm()
    {
        var status = Missing.GetError() switch
        {
            NotFoundException => 404,
            InvalidInputException => 400,
            UnexpectedShopException => 500
        };

        Assert.That(status, Is.EqualTo(404));
    }

    [Test]
    public void ThrownCaseStaysItself()
    {
        Result<int> one = 1;

        var failed = one.Select(_ => Throw(new NotFoundException("No order 7.")));

        Assert.That(failed.GetError().Value, Is.TypeOf<NotFoundException>());
    }

    [Test]
    public void SubclassThatIsNoCaseBecomesUnexpectedOrEscapesUnderDeclaredCapture()
    {
        Result<int> one = 1;
        StrictResult<int> strict = 1;

        Assert.That(one.Select(_ => Throw(new Drift.NotListedException("Forgotten."))).GetError().Value, Is.TypeOf<UnexpectedShopException>());
        Assert.Throws<Drift.NotListedException>(() => strict.Select(_ => Throw(new Drift.NotListedException("Forgotten."))));
        Assert.Throws<NullReferenceException>(() => strict.Select(_ => Throw(new NullReferenceException())));
    }

    [Test]
    public void EveryShopExceptionIsACaseOfShopError()
    {
        var cases = UnionCaseNames.For(typeof(ShopError)).CaseTypes;
        var exceptions = typeof(ShopException).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ShopException)) && !type.IsAbstract && type.DeclaringType != typeof(Drift));

        Assert.That(exceptions, Is.SubsetOf(cases));
    }

    private static int Throw(Exception exception) => throw exception;
}
