using Fluent.Unions;
using Fluent.Unions.Orleans;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.Serialization;

namespace OrleansDomain.Tests;

[TestFixture]
public class UserDeclaredResultTests
{
    private ServiceProvider _provider = null!;

    [OneTimeSetUp]
    public void SetUp() =>
        _provider = new ServiceCollection().AddSerializer(builder => builder.AddFluentUnions()).BuildServiceProvider();

    [OneTimeTearDown]
    public void TearDown() => _provider.Dispose();

    [Test]
    public void UserDeclaredResultRefusesToSerialize()
    {
        Result<int> error = new DomainException("Secret");

        var exception = Assert.Throws<NotSupportedException>(() => _provider.GetRequiredService<Serializer<Result<int>>>().SerializeToArray(error))!;

        Assert.That(exception.Message, Does.Not.Contain("Secret"));
        Assert.Throws<NotSupportedException>(() => _provider.GetRequiredService<Serializer<Result>>().SerializeToArray(Result.Ok));
    }

    [Test]
    public void UserDeclaredResultDeepCopy_KeepsTheResultLocally()
    {
        var copier = _provider.GetRequiredService<DeepCopier<Result<int>>>();

        Assert.That(copier.Copy((Result<int>)5).GetValue(), Is.EqualTo(5));
        Assert.That(copier.Copy(new DomainException("no order")).GetError().Message, Is.EqualTo("no order"));
    }

    [Test]
    public void UserDeclaredResultInsideACollectionRefusesToSerialize()
    {
        var serializer = _provider.GetRequiredService<Serializer<List<Result<int>>>>();

        Assert.Throws<NotSupportedException>(() => serializer.SerializeToArray([(Result<int>)1]));
    }
}
