using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace Fluent.Unions.MongoDB.Tests;

[TestFixture]
public class OptionSerializerTests
{
    private enum Level
    {
        Low = 1,
        High = 2,
    }

    private class Address
    {
        public string City { get; set; } = "";
    }

    private class Person
    {
        public string Name { get; set; } = "";

        public Option<string> Nickname { get; set; }

        public Option<int> Age { get; set; }

        public Option<Level> Level { get; set; }

        public Option<Address> Address { get; set; }

        public Option<List<int>> Scores { get; set; }
    }

    private record PositionalPerson(string Name, Option<string> Nickname, Option<int> Age);

    private class Wrapper<T>
    {
        public T Value { get; set; } = default!;
    }

    [OneTimeSetUp]
    public void Register() => FluentUnionsBson.Register();

    [Test]
    public void Some_value_is_written_as_the_value()
    {
        var document = new Person { Name = "Ada", Nickname = "Countess", Age = 36 }.ToBsonDocument();

        Assert.That(document["Nickname"].AsString, Is.EqualTo("Countess"));
        Assert.That(document["Age"].AsInt32, Is.EqualTo(36));
    }

    [Test]
    public void None_is_written_as_bson_null()
    {
        var document = new Person { Name = "Ada", Nickname = Option.None }.ToBsonDocument();

        Assert.That(document["Nickname"], Is.EqualTo(BsonNull.Value));
    }

    [Test]
    public void Default_is_written_as_bson_null()
    {
        var document = new Person { Name = "Ada" }.ToBsonDocument();

        Assert.That(document["Age"], Is.EqualTo(BsonNull.Value));
    }

    [Test]
    public void Class_mapped_document_round_trips()
    {
        var person = new Person
        {
            Name = "Ada",
            Nickname = "Countess",
            Age = Option.None,
            Level = Level.High,
            Address = new Address { City = "London" },
            Scores = new List<int> { 1, 2 },
        };

        var back = BsonSerializer.Deserialize<Person>(person.ToBsonDocument());

        Assert.That(back.Name, Is.EqualTo("Ada"));
        Assert.That(back.Nickname.GetValue(), Is.EqualTo("Countess"));
        Assert.That(back.Age.HasValue, Is.False);
        Assert.That(back.Level.GetValue(), Is.EqualTo(Level.High));
        Assert.That(back.Address.GetValue().City, Is.EqualTo("London"));
        Assert.That(back.Scores.GetValue(), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Bson_null_reads_as_none()
    {
        var document = new BsonDocument { ["Name"] = "Ada", ["Nickname"] = BsonNull.Value, ["Age"] = BsonNull.Value };

        var person = BsonSerializer.Deserialize<Person>(document);

        Assert.That(person.Nickname.HasValue, Is.False);
        Assert.That(person.Age.HasValue, Is.False);
    }

    [Test]
    public void Missing_element_reads_as_none()
    {
        var person = BsonSerializer.Deserialize<Person>(new BsonDocument { ["Name"] = "Ada" });

        Assert.That(person.Nickname.HasValue, Is.False);
        Assert.That(person.Age.HasValue, Is.False);
    }

    [Test]
    public void Positional_record_round_trips()
    {
        var document = new PositionalPerson("Ada", "Countess", Option.None).ToBsonDocument();

        var back = BsonSerializer.Deserialize<PositionalPerson>(document);

        Assert.That(back.Name, Is.EqualTo("Ada"));
        Assert.That(back.Nickname.GetValue(), Is.EqualTo("Countess"));
        Assert.That(back.Age.HasValue, Is.False);
    }

    [Test]
    public void Option_round_trips_through_a_generic_wrapper()
    {
        var document = new Wrapper<Option<int>> { Value = 5 }.ToBsonDocument();

        Assert.That(document["Value"].AsInt32, Is.EqualTo(5));
        Assert.That(BsonSerializer.Deserialize<Wrapper<Option<int>>>(document).Value.GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void Options_in_a_list_round_trip()
    {
        var document = new Wrapper<List<Option<int>>> { Value = [1, Option.None, 3] }.ToBsonDocument();

        var back = BsonSerializer.Deserialize<Wrapper<List<Option<int>>>>(document).Value;

        Assert.That(document["Value"].AsBsonArray[1], Is.EqualTo(BsonNull.Value));
        Assert.That(back[0].GetValue(), Is.EqualTo(1));
        Assert.That(back[1].HasValue, Is.False);
        Assert.That(back[2].GetValue(), Is.EqualTo(3));
    }

    [Test]
    public void Explicit_value_serializer_is_used()
    {
        var serializer = new OptionSerializer<Level>(new EnumSerializer<Level>(BsonType.String));
        var document = new BsonDocument();

        using (var writer = new BsonDocumentWriter(document))
        {
            writer.WriteStartDocument();
            writer.WriteName("Value");
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), default, Level.High);
            writer.WriteEndDocument();
        }

        Assert.That(document["Value"].AsString, Is.EqualTo("High"));
    }

    [Test]
    public void Null_value_serializer_is_rejected() =>
        Assert.Throws<ArgumentNullException>(() => new OptionSerializer<int>(null!));

    [Test]
    public void Register_can_be_called_repeatedly()
    {
        Assert.DoesNotThrow(FluentUnionsBson.Register);
        Assert.DoesNotThrow(FluentUnionsBson.Register);
    }
}
