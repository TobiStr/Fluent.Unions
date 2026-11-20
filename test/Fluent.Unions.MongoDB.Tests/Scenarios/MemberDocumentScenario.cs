using Fluent.Unions.Validation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.MongoDB.Tests.Scenarios;

public enum Tier
{
    Basic = 1,
    Premium = 2
}

public class Address
{
    public string City { get; set; } = "";
}

public class Member
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public Option<string> Nickname { get; set; }

    public Option<Address> Address { get; set; }

    public Option<Tier> Tier { get; set; }
}

public class AuditEntry
{
    public Result<Member> Outcome { get; set; }
}

public class SaveReceipt
{
    public bool Saved { get; set; }

    public string? Problem { get; set; }
}

public sealed class MemberStore
{
    private readonly Dictionary<string, BsonDocument> collection = [];

    public Result<Member> Save(Member member) =>
        member.Name.EnsureNotEmpty().Select(_ =>
        {
            collection[member.Id] = member.ToBsonDocument();
            return member;
        });

    public Option<Member> Find(string id)
    {
        if (collection.TryGetValue(id, out var document))
        {
            return BsonSerializer.Deserialize<Member>(document);
        }

        return Option.None;
    }

    public BsonDocument Raw(string id) => collection[id];

    public void Seed(string id, BsonDocument document) => collection[id] = document;

    public static SaveReceipt Receipt(Result<Member> saved) =>
        saved.Match(_ => new SaveReceipt { Saved = true }, error => new SaveReceipt { Problem = error.GetType().Name });
}

/// <summary>Persisting members whose optional fields are Option values, so absent data is explicit in code and in the stored document.</summary>
public class MemberDocumentScenario
{
    private readonly MemberStore store = new();

    [OneTimeSetUp]
    public void Register()
    {
        FluentUnionsBson.Register();
        ConventionRegistry.Register(
            nameof(MemberDocumentScenario),
            new ConventionPack { new OmitNoneConvention() },
            type => type == typeof(Member));
    }

    [Test]
    public void Absent_options_are_left_out_of_the_stored_document()
    {
        store.Save(new Member { Id = "m1", Name = "Ada" }).Check();

        var raw = store.Raw("m1");

        Assert.That(raw.Names, Is.EquivalentTo(new[] { "_id", "Name" }));
        Assert.That(store.Find("m1").GetValue().Nickname.HasValue, Is.False);
    }

    [Test]
    public void Present_options_are_stored_as_plain_values_including_nested_documents_and_enums()
    {
        store.Save(new Member
        {
            Id = "m2",
            Name = "Grace",
            Nickname = "Amazing",
            Address = new Address { City = "Arlington" },
            Tier = Tier.Premium
        }).Check();

        var raw = store.Raw("m2");
        var loaded = store.Find("m2").GetValue();

        Assert.That(raw["Nickname"].AsString, Is.EqualTo("Amazing"));
        Assert.That(raw["Address"]["City"].AsString, Is.EqualTo("Arlington"));
        Assert.That(raw["Tier"].AsInt32, Is.EqualTo(2));
        Assert.That(loaded.Address.GetValue().City, Is.EqualTo("Arlington"));
        Assert.That(loaded.Tier.GetValue(), Is.EqualTo(Tier.Premium));
    }

    [Test]
    public void Documents_with_null_or_missing_elements_both_read_as_no_value()
    {
        store.Seed("legacy", new BsonDocument { ["_id"] = "legacy", ["Name"] = "Linus", ["Nickname"] = BsonNull.Value });

        var loaded = store.Find("legacy").GetValue();

        Assert.That(loaded.Nickname.HasValue, Is.False);
        Assert.That(loaded.Address.HasValue, Is.False);
    }

    [Test]
    public void An_unknown_id_is_none_and_an_invalid_member_is_not_stored()
    {
        var rejected = store.Save(new Member { Id = "m3", Name = "" });

        Assert.That(rejected.GetError(), Is.TypeOf<ValidationError>());
        Assert.That(store.Find("m3").HasValue, Is.False);
    }

    [Test]
    public void A_result_cannot_be_stored_but_its_receipt_can()
    {
        Result<Member> failed = new InvalidOperationException("connection string secret");

        var exception = Assert.Catch(() => new AuditEntry { Outcome = failed }.ToBsonDocument());
        var receipt = MemberStore.Receipt(failed).ToBsonDocument();

        Assert.That(exception!.GetBaseException(), Is.TypeOf<NotSupportedException>());
        Assert.That(exception.ToString(), Does.Not.Contain("secret"));
        Assert.That(receipt["Problem"].AsString, Is.EqualTo("InvalidOperationException"));
    }
}
