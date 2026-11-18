using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResultType.Tests;

public class JsonTests
{
    private record Person(string Name, Option<string> Nickname, Option<int> Age);

    private record Holder(Option<int> Value);

    [Test]
    public void WritesValueOrNull()
    {
        var json = JsonSerializer.Serialize(new Person("Ada", "Countess", Option.None));

        Assert.That(json, Is.EqualTo("""{"Name":"Ada","Nickname":"Countess","Age":null}"""));
        Assert.That(JsonSerializer.Serialize(new Holder(default)), Is.EqualTo("""{"Value":null}"""));
    }

    [Test]
    public void ReadsValueOrNone()
    {
        var person = JsonSerializer.Deserialize<Person>("""{"Name":"Ada","Nickname":null,"Age":36}""")!;

        Assert.That(person.Nickname.Value, Is.TypeOf<None>());
        Assert.That(person.Age.GetValue(), Is.EqualTo(36));
    }

    [Test]
    public void MissingPropertyStaysDefault()
    {
        var holder = JsonSerializer.Deserialize<Holder>("{}")!;

        Assert.False(holder.Value.HasValue);
        Assert.That(holder.Value.Value, Is.Null);
    }

    [Test]
    public void RoundTripsRootValuesAndLists()
    {
        Option<int>[] options = [1, Option.None, 3];

        var json = JsonSerializer.Serialize(options);
        var back = JsonSerializer.Deserialize<Option<int>[]>(json)!;

        Assert.That(json, Is.EqualTo("[1,null,3]"));
        Assert.That(back.Select(o => o.GetValueOrDefault(-1)), Is.EqualTo(new[] { 1, -1, 3 }));
    }

    [Test]
    public void IgnoreConditionsOnlyOmitUninitializedOptions()
    {
        var whenWritingNull = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        var whenWritingDefault = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

        Assert.That(JsonSerializer.Serialize(new Holder(Option.None), whenWritingNull), Is.EqualTo("""{"Value":null}"""));
        Assert.That(JsonSerializer.Serialize(new Holder(Option.None), whenWritingDefault), Is.EqualTo("""{"Value":null}"""));
        Assert.That(JsonSerializer.Serialize(new Holder(default), whenWritingDefault), Is.EqualTo("{}"));
    }

    [Test]
    public void ResultsAreNotSerialized()
    {
        Result<int> error = new InvalidOperationException("Secret");

        var write = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(error));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize((Result<int>)5));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Result.Ok));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Result<int>>("5"));

        Assert.That(write!.Message, Does.Not.Contain("Secret"));
    }
}
