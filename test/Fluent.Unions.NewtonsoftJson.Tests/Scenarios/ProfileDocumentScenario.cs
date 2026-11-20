using Fluent.Unions.NewtonsoftJson;
using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson.Tests.Scenarios;

public record ProfileDocument(string Name, Option<string> Nickname, Option<int> Age);

public record NicknameChanged(string Nickname);

public record ProfileDeleted(string Reason);

[JsonConverter(typeof(UnionJsonConverter))]
public union ProfileEvent(NicknameChanged, ProfileDeleted);

public record SaveReply(bool Saved, string? Problem);

public sealed class ProfileStore
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings().AddFluentUnions();

    private readonly Dictionary<string, string> documents = [];

    public void Save(string id, ProfileDocument document) => documents[id] = JsonConvert.SerializeObject(document, Settings);

    public void SeedRaw(string id, string json) => documents[id] = json;

    public string Raw(string id) => documents[id];

    public Option<ProfileDocument> Find(string id)
    {
        if (documents.TryGetValue(id, out var json))
        {
            return JsonConvert.DeserializeObject<ProfileDocument>(json, Settings)!;
        }

        return Option.None;
    }

    public string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);

    public static SaveReply Reply(Result<ProfileDocument> saved) =>
        saved.Match(_ => new SaveReply(true, null), error => new SaveReply(false, error.GetType().Name));
}

/// <summary>A document store where optional fields are Option values written as value or null, and results can never be persisted by accident.</summary>
public class ProfileDocumentScenario
{
    private readonly ProfileStore store = new();

    [Test]
    public void Optional_fields_are_stored_as_value_or_null_and_read_back()
    {
        store.Save("ada", new ProfileDocument("Ada", "Countess", Option.None));

        var loaded = store.Find("ada").GetValue();

        Assert.That(store.Raw("ada"), Is.EqualTo("""{"Name":"Ada","Nickname":"Countess","Age":null}"""));
        Assert.That(loaded.Nickname.GetValue(), Is.EqualTo("Countess"));
        Assert.That(loaded.Age.HasValue, Is.False);
    }

    [Test]
    public void A_document_from_before_a_field_existed_reads_the_field_as_absent()
    {
        store.SeedRaw("old", """{"Name":"Grace","Nickname":"Amazing"}""");

        var loaded = store.Find("old").GetValue();

        Assert.That(loaded.Nickname.GetValue(), Is.EqualTo("Amazing"));
        Assert.That(loaded.Age.HasValue, Is.False);
    }

    [Test]
    public void An_unknown_document_is_none()
    {
        Assert.That(store.Find("missing").HasValue, Is.False);
    }

    [Test]
    public void A_result_refuses_to_serialize_and_never_leaks_its_error()
    {
        Result<ProfileDocument> failed = new InvalidOperationException("db password is hunter2");

        var exception = Assert.Throws<NotSupportedException>(() => store.Serialize(failed));

        Assert.That(exception!.Message, Does.Not.Contain("hunter2"));
    }

    [Test]
    public void A_result_is_converted_to_a_reply_dto_before_it_crosses_the_boundary()
    {
        Result<ProfileDocument> saved = new ProfileDocument("Ada", Option.None, Option.None);
        Result<ProfileDocument> failed = new InvalidOperationException("db password is hunter2");

        Assert.That(store.Serialize(ProfileStore.Reply(saved)), Is.EqualTo("""{"Saved":true,"Problem":null}"""));
        Assert.That(store.Serialize(ProfileStore.Reply(failed)), Is.EqualTo("""{"Saved":false,"Problem":"InvalidOperationException"}"""));
    }

    [Test]
    public void An_event_log_of_a_union_round_trips_each_case()
    {
        List<ProfileEvent> events = [new NicknameChanged("Countess"), new ProfileDeleted("requested")];

        var json = JsonConvert.SerializeObject(events);
        var restored = JsonConvert.DeserializeObject<List<ProfileEvent>>(json)!;

        Assert.That(json, Does.Contain("""{"$type":"NicknameChanged","$value":{"Nickname":"Countess"}}"""));
        var summary = restored.Select(e => e switch
        {
            NicknameChanged changed => $"nickname {changed.Nickname}",
            ProfileDeleted deleted => $"deleted {deleted.Reason}"
        });
        Assert.That(summary, Is.EqualTo(new[] { "nickname Countess", "deleted requested" }));
    }
}
