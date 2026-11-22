using System.Text.Json;
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;

namespace Fluent.Unions.Tests;

[JsonConverter(typeof(UnionJsonConverter))]
public union Bag(List<int>, string);

[JsonConverter(typeof(UnionJsonConverter))]
public union Counted(Option<int>, string);

public static class FirstNamespace
{
    public record Item(int Id);
}

public static class SecondNamespace
{
    public record Item(int Id);
}

[JsonConverter(typeof(UnionJsonConverter))]
public union Ambiguous(FirstNamespace.Item, SecondNamespace.Item);

public class UnionJsonConverterTests
{
    public record Drawing(string Name, Shape Shape);

    public record Person(string Name, Option<string> Nickname, Option<int> Age);

    [TestCase("""{"$type":"Circle","$value":{"Radius":2}}""", TestName = "v1.0.0 class case")]
    [TestCase("""{"$type":"Int32","$value":7}""", TestName = "v1.0.0 value case")]
    public void ReadsAndWritesTheV1WireFormatOfAUnion(string json) => AssertRoundTrip<Shape>(json);

    [Test]
    public void ReadsAndWritesTheV1WireFormatOfAnEmptyUnion() =>
        AssertRoundTrip<Drawing>("""{"Name":"Sketch","Shape":null}""");

    [Test]
    public void ReadsAndWritesTheV1WireFormatOfAGenericCase() =>
        AssertRoundTrip<Bag>("""{"$type":"List\u00601","$value":[1,2]}""");

    [Test]
    public void ReadsAndWritesTheV1WireFormatOfAnOptionCaseWithoutValue() =>
        AssertRoundTrip<Counted>("""{"$type":"Option\u00601","$value":null}""");

    [Test]
    public void ReadsAndWritesTheV1WireFormatOfOptions()
    {
        AssertRoundTrip<Person>("""{"Name":"Ada","Nickname":"Countess","Age":null}""");
        AssertRoundTrip<Option<int>[]>("[1,null,3]");
    }

    [Test]
    public void ReadsPropertiesInAnyOrder()
    {
        var shape = JsonSerializer.Deserialize<Shape>("""{"$value":7,"$type":"Int32"}""");

        Assert.That(shape.Value, Is.EqualTo(7));
    }

    [Test]
    public void MissingOrUnknownTypeThrows()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$value":7}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$type":null,"$value":7}"""));
        var unknown = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$type":"Triangle","$value":1}"""));

        Assert.That(unknown!.Message, Does.Contain("'Triangle' is not a case of Shape"));
    }

    [Test]
    public void MissingValueThrows()
    {
        var thrown = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$type":"Int32"}"""));

        Assert.That(thrown!.Message, Does.Contain("needs a '$value' property"));
    }

    [Test]
    public void NullValueThatWouldGiveAnEmptyUnionThrows()
    {
        var thrown = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$type":"Circle","$value":null}"""));

        Assert.That(thrown!.Message, Does.Contain("An empty Shape is written as null"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$type":"Int32","$value":null}"""));
    }

    [TestCase("5")]
    [TestCase("[1]")]
    [TestCase("\"Circle\"")]
    public void NonObjectTokenThrows(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>(json));

    [Test]
    public void SameShortNameFromTwoNamespacesThrowsInvalidOperationException()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize((Ambiguous)new FirstNamespace.Item(1)));

        Assert.That(thrown!.Message, Does.Contain("several cases named 'Item'"));
    }
    [Test]
    public void NestedOptionLosesItsLevels()
    {
        Option<Option<int>> someNone = Option.Some<Option<int>>(Option.None);

        var json = JsonSerializer.Serialize(someNone);

        Assert.That(json, Is.EqualTo("null"));
        Assert.That(JsonSerializer.Deserialize<Option<Option<int>>>(json).HasValue, Is.False);
    }

    [Test]
    public void ExplicitOptionConverterWritesTheSameFormat()
    {
        var options = new JsonSerializerOptions { Converters = { new OptionJsonConverter<int>(), new OptionJsonConverter<string>() } };

        var json = JsonSerializer.Serialize(new Person("Ada", Option.None, 36), options);

        Assert.That(json, Is.EqualTo("""{"Name":"Ada","Nickname":null,"Age":36}"""));
        Assert.That(JsonSerializer.Deserialize<Person>(json, options)!.Age.GetValue(), Is.EqualTo(36));
    }

    [Test]
    public void ExplicitUnionConverterWritesTheSameFormat()
    {
        var options = new JsonSerializerOptions { Converters = { new UnionJsonConverter<Shape>() } };

        var json = JsonSerializer.Serialize<Shape>(new Circle(2), options);

        Assert.That(json, Is.EqualTo("""{"$type":"Circle","$value":{"Radius":2}}"""));
        Assert.That(JsonSerializer.Deserialize<Shape>(json, options).Value, Is.EqualTo(new Circle(2)));
    }
    private static void AssertRoundTrip<T>(string json)
    {
        var value = JsonSerializer.Deserialize<T>(json);

        Assert.That(JsonSerializer.Serialize(value), Is.EqualTo(json));
    }
}
