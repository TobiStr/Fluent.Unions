using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson.Tests;

public record Circle(double Radius);

public record Square(double Side);

public record Moment(DateTime At, decimal Amount);

[JsonConverter(typeof(UnionJsonConverter))]
public union Shape(Circle, Square, int);

[JsonConverter(typeof(UnionJsonConverter))]
public union Event(Moment, string);

public class First
{
    public record Item(int Id);
}

public class Second
{
    public record Item(int Id);
}

[JsonConverter(typeof(UnionJsonConverter))]
public union Ambiguous(First.Item, Second.Item);

public class UnionJsonConverterTests
{
    private record Drawing(string Name, Shape Shape, Shape? Maybe);

    [Test]
    public void WritesCaseNameAndValue()
    {
        Assert.That(JsonConvert.SerializeObject((Shape)new Circle(2)), Is.EqualTo("""{"$type":"Circle","$value":{"Radius":2.0}}"""));
        Assert.That(JsonConvert.SerializeObject((Shape)7), Is.EqualTo("""{"$type":"Int32","$value":7}"""));
    }

    [Test]
    public void RoundTripsEveryCase()
    {
        Shape[] shapes = [new Circle(2), new Square(3), 7];

        var back = JsonConvert.DeserializeObject<Shape[]>(JsonConvert.SerializeObject(shapes))!;

        Assert.That(back[0].As<Circle>().GetValue(), Is.EqualTo(new Circle(2)));
        Assert.That(back[1].As<Square>().GetValue(), Is.EqualTo(new Square(3)));
        Assert.That(back[2].As<int>().GetValue(), Is.EqualTo(7));
    }

    [Test]
    public void RoundTripsInsideObjects()
    {
        var drawing = new Drawing("Sketch", new Square(1), new Circle(4));

        var back = JsonConvert.DeserializeObject<Drawing>(JsonConvert.SerializeObject(drawing))!;

        Assert.That(back.Name, Is.EqualTo("Sketch"));
        Assert.That(back.Shape.Is<Square>(), Is.True);
        Assert.That(back.Maybe!.Value.Is<Circle>(), Is.True);
    }

    [Test]
    public void EmptyUnionIsNull()
    {
        var json = JsonConvert.SerializeObject(default(Shape));
        var back = JsonConvert.DeserializeObject<Shape>("null");

        Assert.That(json, Is.EqualTo("null"));
        Assert.That(back.Value, Is.Null);
    }

    [Test]
    public void NullableUnionReadsNullAsNull()
    {
        var back = JsonConvert.DeserializeObject<Drawing>("""{"Name":"x","Shape":null,"Maybe":null}""")!;

        Assert.That(back.Maybe, Is.Null);
        Assert.That(back.Shape.Value, Is.Null);
    }

    [Test]
    public void ReadsPropertiesInAnyOrder()
    {
        var shape = JsonConvert.DeserializeObject<Shape>("""{"$value":{"Side":3},"$type":"Square"}""");

        Assert.That(shape.As<Square>().GetValue(), Is.EqualTo(new Square(3)));
    }

    [Test]
    public void OutOfOrderValueKeepsDatesAndDecimals()
    {
        var moment = new Moment(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), 12.5m);
        var json = JsonConvert.SerializeObject((Event)moment);
        var reordered = """{"$value":""" + JsonConvert.SerializeObject(moment) + ""","$type":"Moment"}""";

        Assert.That(JsonConvert.DeserializeObject<Event>(json).As<Moment>().GetValue(), Is.EqualTo(moment));
        Assert.That(JsonConvert.DeserializeObject<Event>(reordered).As<Moment>().GetValue(), Is.EqualTo(moment));
    }

    [Test]
    public void IgnoresUnknownProperties()
    {
        var shape = JsonConvert.DeserializeObject<Shape>("""{"extra":{"a":[1,2]},"$type":"Int32","$value":5}""");

        Assert.That(shape.As<int>().GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void MissingOrUnknownTypeThrows()
    {
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Shape>("""{"$value":{"Side":3}}"""));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Shape>("{}"));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Shape>("""{"$type":null}"""));
        var unknown = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Shape>("""{"$type":"Triangle","$value":1}"""));

        Assert.That(unknown!.Message, Does.Contain("'Triangle' is not a case of Shape"));
    }

    [Test]
    public void NonObjectTokenThrows() =>
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Shape>("5"));

    [Test]
    public void DuplicateCaseNamesThrow()
    {
        Assert.Throws<InvalidOperationException>(() => JsonConvert.SerializeObject((Ambiguous)new First.Item(1)));
        Assert.Throws<InvalidOperationException>(() => JsonConvert.DeserializeObject<Ambiguous>("""{"$type":"Item","$value":{"Id":1}}"""));
    }

    [Test]
    public void CanConvertOnlyUnionStructs()
    {
        var converter = new UnionJsonConverter();

        Assert.That(converter.CanConvert(typeof(Shape)), Is.True);
        Assert.That(converter.CanConvert(typeof(Shape?)), Is.True);
        Assert.That(converter.CanConvert(typeof(Circle)), Is.False);
        Assert.That(converter.CanConvert(typeof(int)), Is.False);
    }

    [Test]
    public void CanNotConvertResultsOrOptions()
    {
        var converter = new UnionJsonConverter();

        Assert.That(converter.CanConvert(typeof(Result)), Is.False);
        Assert.That(converter.CanConvert(typeof(Result<int>)), Is.False);
        Assert.That(converter.CanConvert(typeof(Result<int>?)), Is.False);
        Assert.That(converter.CanConvert(typeof(Option<int>)), Is.False);
        Assert.That(converter.CanConvert(typeof(Option<int>?)), Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ResultsStayRefusedWhateverTheRegistrationOrder(bool unionConverterFirst)
    {
        var settings = new JsonSerializerSettings();

        if (unionConverterFirst)
        {
            settings.Converters.Add(new UnionJsonConverter());
            settings.AddFluentUnions();
        }
        else
        {
            settings.AddFluentUnions();
            settings.Converters.Add(new UnionJsonConverter());
        }

        Result<int> error = new InvalidOperationException("Secret");
        List<Result<int>> results = [5, error];

        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(error, settings));
        Assert.Throws<NotSupportedException>(() => JsonConvert.SerializeObject(results, settings));
        Assert.That(JsonConvert.SerializeObject(new List<Option<int>> { Option.Some(5), Option.None }, settings), Is.EqualTo("[5,null]"));
    }

    [Test]
    public void PropertyNamesAreExposed()
    {
        Assert.That(UnionJsonConverter.TypeProperty, Is.EqualTo("$type"));
        Assert.That(UnionJsonConverter.ValueProperty, Is.EqualTo("$value"));
    }
}
