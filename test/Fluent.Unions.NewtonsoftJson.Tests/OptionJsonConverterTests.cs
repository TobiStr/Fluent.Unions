using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson.Tests;

public class OptionJsonConverterTests
{
    private record Person(string Name, Option<string> Nickname, Option<int> Age);

    private record Holder(Option<int> Value);

    private record NullableHolder(Option<int>? Value);

    private record Address(string City);

    private record Contact(Option<Address> Address, Option<Option<int>> Nested, Option<int?> Count);

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings().AddFluentUnions();

    [Test]
    public void WritesValueOrNull()
    {
        var json = JsonConvert.SerializeObject(new Person("Ada", "Countess", Option.None), Settings);

        Assert.That(json, Is.EqualTo("""{"Name":"Ada","Nickname":"Countess","Age":null}"""));
        Assert.That(JsonConvert.SerializeObject(new Holder(default), Settings), Is.EqualTo("""{"Value":null}"""));
    }

    [Test]
    public void ReadsValueOrNone()
    {
        var person = JsonConvert.DeserializeObject<Person>("""{"Name":"Ada","Nickname":null,"Age":36}""", Settings)!;

        Assert.That(person.Nickname.Value, Is.TypeOf<None>());
        Assert.That(person.Age.GetValue(), Is.EqualTo(36));
    }

    [Test]
    public void MissingPropertyStaysDefault()
    {
        var holder = JsonConvert.DeserializeObject<Holder>("{}", Settings)!;

        Assert.False(holder.Value.HasValue);
        Assert.That(holder.Value.Value, Is.Null);
    }

    [Test]
    public void RoundTripsRootValuesAndArrays()
    {
        Option<int>[] options = [1, Option.None, 3];

        var json = JsonConvert.SerializeObject(options, Settings);
        var back = JsonConvert.DeserializeObject<Option<int>[]>(json, Settings)!;

        Assert.That(json, Is.EqualTo("[1,null,3]"));
        Assert.That(back.Select(o => o.GetValueOrDefault(-1)), Is.EqualTo(new[] { 1, -1, 3 }));
        Assert.That(JsonConvert.SerializeObject((Option<string>)"a", Settings), Is.EqualTo("\"a\""));
        Assert.That(JsonConvert.DeserializeObject<Option<string>>("null", Settings).HasValue, Is.False);
    }

    [Test]
    public void RoundTripsLists()
    {
        List<Option<string>> options = ["a", Option.None];

        var json = JsonConvert.SerializeObject(options, Settings);
        var back = JsonConvert.DeserializeObject<List<Option<string>>>(json, Settings)!;

        Assert.That(json, Is.EqualTo("""["a",null]"""));
        Assert.That(back.Select(o => o.GetValueOrDefault("none")), Is.EqualTo(new[] { "a", "none" }));
    }

    [Test]
    public void RoundTripsDictionaryValues()
    {
        var options = new Dictionary<string, Option<int>> { ["one"] = 1, ["none"] = Option.None };

        var json = JsonConvert.SerializeObject(options, Settings);
        var back = JsonConvert.DeserializeObject<Dictionary<string, Option<int>>>(json, Settings)!;

        Assert.That(json, Is.EqualTo("""{"one":1,"none":null}"""));
        Assert.That(back["one"].GetValue(), Is.EqualTo(1));
        Assert.That(back["none"].HasValue, Is.False);
    }

    [Test]
    public void RoundTripsComplexValues()
    {
        var contact = new Contact(new Address("London"), Option.None, 7);

        var json = JsonConvert.SerializeObject(contact, Settings);
        var back = JsonConvert.DeserializeObject<Contact>(json, Settings)!;

        Assert.That(json, Is.EqualTo("""{"Address":{"City":"London"},"Nested":null,"Count":7}"""));
        Assert.That(back.Address.GetValue(), Is.EqualTo(new Address("London")));
        Assert.That(back.Nested.HasValue, Is.False);
        Assert.That(back.Count.GetValue(), Is.EqualTo(7));
    }

    [Test]
    public void OptionOfNullableValueTypeReadsNullAsNone()
    {
        var json = JsonConvert.SerializeObject(new Contact(Option.None, Option.None, Option.None), Settings);
        var back = JsonConvert.DeserializeObject<Contact>(json, Settings)!;

        Assert.That(json, Is.EqualTo("""{"Address":null,"Nested":null,"Count":null}"""));
        Assert.That(back.Count.HasValue, Is.False);
    }

    [Test]
    public void NestedOptionLosesItsLevels()
    {
        Option<Option<int>> nested = new Option<int>(5);
        Option<Option<int>> inner = new Option<int>(Option.None);

        Assert.That(JsonConvert.SerializeObject(nested, Settings), Is.EqualTo("5"));
        Assert.That(JsonConvert.SerializeObject(inner, Settings), Is.EqualTo("null"));
        Assert.That(JsonConvert.DeserializeObject<Option<Option<int>>>("5", Settings).GetValue().GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void NullableOptionReadsNullAsNull()
    {
        var some = JsonConvert.DeserializeObject<NullableHolder>("""{"Value":4}""", Settings)!;
        var none = JsonConvert.DeserializeObject<NullableHolder>("""{"Value":null}""", Settings)!;

        Assert.That(some.Value!.Value.GetValue(), Is.EqualTo(4));
        Assert.That(none.Value, Is.Null);
        Assert.That(JsonConvert.SerializeObject(new NullableHolder(Option.None), Settings), Is.EqualTo("""{"Value":null}"""));
        Assert.That(JsonConvert.SerializeObject(new NullableHolder(2), Settings), Is.EqualTo("""{"Value":2}"""));
    }

    [Test]
    public void IgnoreHandlingOnlyOmitsUninitializedOptions()
    {
        var nullIgnore = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }.AddFluentUnions();
        var defaultIgnore = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }.AddFluentUnions();

        Assert.That(JsonConvert.SerializeObject(new Holder(Option.None), nullIgnore), Is.EqualTo("""{"Value":null}"""));
        Assert.That(JsonConvert.SerializeObject(new Holder(Option.None), defaultIgnore), Is.EqualTo("""{"Value":null}"""));
        Assert.That(JsonConvert.SerializeObject(new Holder(default), defaultIgnore), Is.EqualTo("{}"));
    }

    [Test]
    public void ConverterCanBeAppliedByAttribute()
    {
        var converter = new OptionJsonConverter();

        Assert.That(converter.CanConvert(typeof(Option<int>)), Is.True);
        Assert.That(converter.CanConvert(typeof(Option<int>?)), Is.True);
        Assert.That(converter.CanConvert(typeof(int)), Is.False);
        Assert.That(converter.CanConvert(typeof(Result<int>)), Is.False);
    }
}
