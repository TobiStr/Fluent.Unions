using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Fluent.Unions.MongoDB.Tests;

[TestFixture]
public class ResultSerializerTests
{
    private class Order
    {
        public Result<int> Outcome { get; set; }
    }

    private sealed record OrderError(Exception Exception) : Fluent.Unions.Abstractions.IFromException<OrderError>
    {
        public static OrderError FromException(Exception exception) => new(exception);
    }

    [OneTimeSetUp]
    public void Register() => FluentUnionsBson.Register();

    [Test]
    public void Result_refuses_to_serialize_without_exposing_the_error()
    {
        Result<int> error = new InvalidOperationException("Secret");

        var exception = Assert.Catch(() => new Order { Outcome = error }.ToBsonDocument());

        Assert.That(exception!.GetBaseException(), Is.TypeOf<NotSupportedException>());
        Assert.That(exception.ToString(), Does.Not.Contain("Secret"));
    }

    [Test]
    public void Result_of_value_refuses_to_serialize() =>
        Assert.That(Assert.Catch(() => new Order { Outcome = 1 }.ToBsonDocument())!.GetBaseException(), Is.TypeOf<NotSupportedException>());

    [Test]
    public void Result_refuses_to_deserialize() =>
        Assert.That(
            Assert.Catch(() => BsonSerializer.Deserialize<Order>(new BsonDocument { ["Outcome"] = 1 }))!.GetBaseException(),
            Is.TypeOf<NotSupportedException>());

    [Test]
    public void Every_result_type_has_a_refusing_serializer()
    {
        Assert.That(BsonSerializer.LookupSerializer<Result>(), Is.InstanceOf<ResultSerializer<Result>>());
        Assert.That(BsonSerializer.LookupSerializer<Result<string>>(), Is.InstanceOf<ResultSerializer<Result<string>>>());
        Assert.That(BsonSerializer.LookupSerializer<Result<int, OrderError>>(), Is.InstanceOf<ResultSerializer<Result<int, OrderError>>>());
    }
}
