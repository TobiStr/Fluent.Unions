using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace Fluent.Unions.MongoDB.Tests;

[TestFixture]
public class OmitNoneConventionTests
{
    private class Profile
    {
        public string Name { get; set; } = "";

        public Option<string> Nickname { get; set; }

        public Option<int> Age { get; set; }

        public int Visits { get; set; }
    }

    [OneTimeSetUp]
    public void Register()
    {
        FluentUnionsBson.Register();
        ConventionRegistry.Register(
            nameof(OmitNoneConventionTests),
            new ConventionPack { new OmitNoneConvention() },
            type => type == typeof(Profile));
    }

    [Test]
    public void None_and_default_are_left_out()
    {
        var document = new Profile { Name = "Ada", Nickname = Option.None }.ToBsonDocument();

        Assert.That(document.Contains("Nickname"), Is.False);
        Assert.That(document.Contains("Age"), Is.False);
        Assert.That(document["Visits"].AsInt32, Is.EqualTo(0));
    }

    [Test]
    public void Some_value_is_written()
    {
        var document = new Profile { Name = "Ada", Nickname = "Countess", Age = 36 }.ToBsonDocument();

        Assert.That(document["Nickname"].AsString, Is.EqualTo("Countess"));
        Assert.That(document["Age"].AsInt32, Is.EqualTo(36));
    }

    [Test]
    public void Left_out_element_reads_as_none()
    {
        var back = BsonSerializer.Deserialize<Profile>(new Profile { Name = "Ada" }.ToBsonDocument());

        Assert.That(back.Nickname.HasValue, Is.False);
        Assert.That(back.Age.HasValue, Is.False);
    }
}
