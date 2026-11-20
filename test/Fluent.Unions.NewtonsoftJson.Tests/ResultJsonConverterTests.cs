using Fluent.Unions.Abstractions;
using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson.Tests;

public record Unexpected(Exception Exception);

public union AppError(string, Unexpected) : IFromException<AppError>
{
    public static AppError FromException(Exception exception) => new Unexpected(exception);
}

public class ResultJsonConverterTests
{
    private record Response(Result<int> Result);

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings().AddFluentUnions();

    [Test]
    public void ResultsAreNotSerialized()
    {
        Result<int> error = new InvalidOperationException("Secret");

        var write = Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(error, Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject((Result<int>)5, Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(Result.Ok, Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject((Result<int, AppError>)5, Settings));

        Assert.That(write!.Message, Does.Not.Contain("Secret"));
        Assert.That(write.Message, Does.Contain("Convert it first"));
    }

    [Test]
    public void ResultsAreNotDeserialized()
    {
        Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Result<int>>("5", Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Result>("{}", Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Result<int, AppError>>("5", Settings));
    }

    [Test]
    public void ResultsInsideObjectsAreNotSerialized()
    {
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(new Response(5), Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Response>("""{"Result":5}""", Settings));
    }

    [Test]
    public void MessageMatchesTheCoreWording()
    {
        var exception = Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject((Result<int>)5, Settings));

        Assert.That(exception!.Message, Is.EqualTo(
            $"{typeof(Result<int>)} is not serialized, because an error would expose exception details. " +
            "Convert it first, for example with Match, GetValueOrDefault or ToOption."));
    }

    [Test]
    public void CanConvertOnlyResults()
    {
        var converter = new ResultJsonConverter();

        Assert.That(converter.CanConvert(typeof(Result)), Is.True);
        Assert.That(converter.CanConvert(typeof(Result<int>)), Is.True);
        Assert.That(converter.CanConvert(typeof(Result<int, AppError>)), Is.True);
        Assert.That(converter.CanConvert(typeof(Result<int>?)), Is.True);
        Assert.That(converter.CanConvert(typeof(Option<int>)), Is.False);
        Assert.That(converter.CanConvert(typeof(int)), Is.False);
    }
}
