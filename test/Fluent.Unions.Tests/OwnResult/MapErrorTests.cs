using Fluent.Unions.Abstractions;
using Is = NUnit.Framework.Is;

namespace OwnResultSample.Tests;

public sealed record BadRequest(string Message);

public sealed record ServerError;

public union ApiError(BadRequest, ServerError) : IFromException<ApiError>
{
    public static ApiError FromException(Exception exception) => new ServerError();
}

[FluentResult<ApiError>]
public readonly partial struct ApiResult<T>;

public class MapErrorTests
{
    private static ApiError ToApi(CheckoutError error) => error switch
    {
        NotFoundException notFound => new BadRequest(notFound.Message),
        OutOfStockException outOfStock => new BadRequest(outOfStock.Message),
        Unexpected => new ServerError()
    };

    [Test]
    public void MapErrorKeepsTheValueAndDoesNotRunTheMap()
    {
        Outcome<int> found = 5;
        var calls = 0;

        var mapped = found.MapError<ApiResult<int>, ApiError>(error => { calls++; return ToApi(error); });

        Assert.That(mapped.GetValue(), Is.EqualTo(5));
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void MapErrorTranslatesTheErrorWithATargetTypedSwitch()
    {
        Outcome<int> missing = new NotFoundException("No order 7.");

        var mapped = missing.MapError<ApiResult<int>, ApiError>(error => error switch
        {
            NotFoundException notFound => new BadRequest(notFound.Message),
            OutOfStockException outOfStock => new BadRequest(outOfStock.Message),
            Unexpected => new ServerError()
        });

        Assert.That(mapped.GetError().Value, Is.EqualTo(new BadRequest("No order 7.")));
    }

    [Test]
    public void MapErrorMovesToAndFromTheLibraryResult()
    {
        Outcome<int> missing = new NotFoundException("No order 7.");
        Fluent.Unions.Result<int> failed = new TimeoutException("Slow.");

        var toLibrary = missing.MapError<Fluent.Unions.Result<int>, Exception>(error => new InvalidOperationException(error.ToString()));
        var fromLibrary = failed.MapError<ApiResult<int>, ApiError>(exception => new BadRequest(exception.Message));

        Assert.That(toLibrary.GetError(), Is.TypeOf<InvalidOperationException>());
        Assert.That(fromLibrary.GetError().Value, Is.EqualTo(new BadRequest("Slow.")));
    }

    [Test]
    public void MapErrorWithinTheSameTypeReplacesTheError()
    {
        Result<int> missing = new NotFoundException("No order 7.");

        var mapped = missing.MapError<Result<int>, DomainException>(error => new OutOfStockException(error.Message));

        Assert.That(mapped.GetError(), Is.TypeOf<OutOfStockException>());
    }

    [Test]
    public void MapErrorCapturesAThrowingMapInTheTargetType()
    {
        Outcome<int> missing = new NotFoundException("No order 7.");

        var mapped = missing.MapError<ApiResult<int>, ApiError>(_ => throw new InvalidOperationException("Broken mapping."));

        Assert.That(mapped.GetError().Value, Is.TypeOf<ServerError>());
    }

    [Test]
    public void MapErrorLetsCancellationThrough()
    {
        Outcome<int> missing = new NotFoundException("No order 7.");

        Assert.Throws<OperationCanceledException>(() => missing.MapError<ApiResult<int>, ApiError>(_ => throw new OperationCanceledException()));
    }

    [Test]
    public void MapErrorRejectsANullMap()
    {
        Outcome<int> found = 5;

        Assert.Throws<ArgumentNullException>(() => found.MapError<ApiResult<int>, ApiError>(null!));
    }

    [Test]
    public void MapErrorOfADefaultResultMapsTheUninitializedError()
    {
        var empty = (new Outcome<int>[1])[0];
        CheckoutError? seen = null;

        var mapped = empty.MapError<ApiResult<int>, ApiError>(error => { seen = error; return ToApi(error); });

        Assert.That(((Unexpected)seen!.Value.Value!).Exception.Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(mapped.GetError().Value, Is.TypeOf<ServerError>());
    }

    [Test]
    public void MapErrorMapsTheCompanionWithoutAValue()
    {
        var failed = Outcome.Error(new OutOfStockException("Sold out."));

        var mapped = failed.MapError<ApiResult, ApiError>(ToApi);
        var succeeded = Outcome.Ok.MapError<ApiResult, ApiError>(ToApi);

        Assert.That(mapped.GetError().Value, Is.EqualTo(new BadRequest("Sold out.")));
        Assert.That(succeeded.IsSuccess, Is.True);
    }
}
