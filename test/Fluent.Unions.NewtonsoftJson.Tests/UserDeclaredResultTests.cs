using Fluent.Unions;
using Fluent.Unions.NewtonsoftJson;
using Newtonsoft.Json;
using NUnit.Framework;

namespace NewtonsoftJsonDomain.Tests;

public class UserDeclaredResultTests
{
    private record Response(Result<int> Result);

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings().AddFluentUnions();

    [Test]
    public void UserDeclaredResultsAreNotSerialized()
    {
        Result<int> error = new DomainException("Secret");

        var write = Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(error, Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject((Result<int>)5, Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject((Result<int>?)5, Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(Result.Ok, Settings));

        Assert.That(write!.Message, Does.Not.Contain("Secret"));
        Assert.That(write.Message, Does.Contain("Convert it first"));
    }

    [Test]
    public void UserDeclaredResultsAreNotDeserialized()
    {
        Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Result<int>>("5", Settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Result>("{}", Settings));
    }

    [Test]
    public void UserDeclaredResultInsideAnObjectIsNotSerialized() =>
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(new Response(5), Settings));
}
