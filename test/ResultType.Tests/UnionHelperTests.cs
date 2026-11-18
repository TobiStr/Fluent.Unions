using System.Text.Json;
using System.Text.Json.Serialization;
using ResultType.Serialization;

namespace ResultType.Tests;

public record Circle(double Radius);

public record Square(double Side);

[JsonConverter(typeof(UnionJsonConverter))]
public union Shape(Circle, Square, int);

public class UnionHelperTests
{
    private record Drawing(string Name, Shape Shape);

    [Test]
    public void IsAndAsWorkOnAnyUnion()
    {
        Shape circle = new Circle(2);

        Assert.True(circle.Is<Circle>());
        Assert.False(circle.Is<Square>());
        Assert.That(circle.As<Circle>().GetValue(), Is.EqualTo(new Circle(2)));
        Assert.That(circle.As<Square>().Value, Is.TypeOf<None>());
        Assert.That(default(Shape).As<Circle>().Value, Is.TypeOf<None>());
    }

    [Test]
    public void IsAndAsWorkOnLibraryUnions()
    {
        Result<int> error = new InvalidOperationException("Failure");

        Assert.True(error.Is<InvalidOperationException>());
        Assert.That(error.As<Exception>().GetValue().Message, Is.EqualTo("Failure"));
        Assert.That(((Result<int>)5).As<int>().GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void WritesTheCaseName()
    {
        var json = JsonSerializer.Serialize(new Drawing("Ring", new Circle(2)));

        Assert.That(json, Is.EqualTo("""{"Name":"Ring","Shape":{"$type":"Circle","$value":{"Radius":2}}}"""));
    }

    [Test]
    public void RoundTripsEveryCase()
    {
        Shape[] shapes = [new Circle(2), new Square(3), 7, default];

        var json = JsonSerializer.Serialize(shapes);
        var back = JsonSerializer.Deserialize<Shape[]>(json)!;

        Assert.That(json, Is.EqualTo("""[{"$type":"Circle","$value":{"Radius":2}},{"$type":"Square","$value":{"Side":3}},{"$type":"Int32","$value":7},null]"""));
        Assert.That(back.Select(s => s.Value), Is.EqualTo(new object?[] { new Circle(2), new Square(3), 7, null }));
    }

    [Test]
    public void ReadsTheTypeInAnyPosition()
    {
        var shape = JsonSerializer.Deserialize<Shape>("""{"$value":{"Side":4},"$type":"Square"}""");

        Assert.That(shape.Value, Is.EqualTo(new Square(4)));
    }

    [Test]
    public void RejectsUnknownOrMissingCases()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$type":"Triangle","$value":{}}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shape>("""{"$value":{}}"""));
    }
}
