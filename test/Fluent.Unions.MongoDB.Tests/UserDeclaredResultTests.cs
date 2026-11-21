using Fluent.Unions;
using Fluent.Unions.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDBDomain.Tests;

[TestFixture]
public class UserDeclaredResultTests
{
    private class Order
    {
        public Result<int> Outcome { get; set; }
    }

    [OneTimeSetUp]
    public void Register() => FluentUnionsBson.Register();

    [Test]
    public void UserDeclaredResultRefusesToSerializeWithoutExposingTheError()
    {
        Result<int> error = new DomainException("Secret");

        var exception = Assert.Catch(() => new Order { Outcome = error }.ToBsonDocument());

        Assert.That(exception!.GetBaseException(), Is.TypeOf<NotSupportedException>());
        Assert.That(exception.ToString(), Does.Not.Contain("Secret"));
        Assert.That(Assert.Catch(() => new Order { Outcome = 1 }.ToBsonDocument())!.GetBaseException(), Is.TypeOf<NotSupportedException>());
    }

    [Test]
    public void UserDeclaredResultRefusesToDeserialize() =>
        Assert.That(
            Assert.Catch(() => BsonSerializer.Deserialize<Order>(new BsonDocument { ["Outcome"] = 1 }))!.GetBaseException(),
            Is.TypeOf<NotSupportedException>());

    [Test]
    public void EveryUserDeclaredResultHasARefusingSerializer()
    {
        Assert.That(BsonSerializer.LookupSerializer<Result>(), Is.InstanceOf<ResultSerializer<Result>>());
        Assert.That(BsonSerializer.LookupSerializer<Result<string>>(), Is.InstanceOf<ResultSerializer<Result<string>>>());
    }
}
