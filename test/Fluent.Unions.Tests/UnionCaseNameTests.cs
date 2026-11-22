using System.Text.Json;
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;

namespace Fluent.Unions.Tests;

[JsonConverter(typeof(UnionJsonConverter))]
[UnionCaseName(typeof(Circle), "circle", Aliases = ["Circle"])]
[UnionCaseName(typeof(int), "count")]
public union NamedShape(Circle, Square, int);

[JsonConverter(typeof(UnionJsonConverter))]
[UnionCaseName(typeof(List<int>), "numbers")]
[UnionCaseName(typeof(List<string>), "words")]
public union NamedLists(List<int>, List<string>);

[JsonConverter(typeof(UnionJsonConverter))]
[UnionCaseName(typeof(FirstNamespace.Item), "first")]
[UnionCaseName(typeof(SecondNamespace.Item), "second")]
public union NamedItems(FirstNamespace.Item, SecondNamespace.Item);

[UnionCaseName(typeof(Circle), "Square")]
public union NameCollidesWithAnotherCase(Circle, Square);

[UnionCaseName(typeof(Circle), "circle", Aliases = ["Square"])]
public union AliasCollidesWithAnotherCase(Circle, Square);

[UnionCaseName(typeof(string), "text")]
public union NameForATypeThatIsNoCase(Circle, Square);

[UnionCaseName(typeof(Circle), " ")]
public union EmptyName(Circle, Square);

[UnionCaseName(typeof(Circle), "circle")]
[UnionCaseName(typeof(Circle), "round")]
public union RepeatedName(Circle, Square);

public union TwoLists(List<int>, List<string>);

public class UnionCaseNameTests
{
    [Test]
    public void ConfiguredNameIsWritten()
    {
        Assert.That(JsonSerializer.Serialize((NamedShape)new Circle(2)), Is.EqualTo("""{"$type":"circle","$value":{"Radius":2}}"""));
        Assert.That(JsonSerializer.Serialize((NamedShape)7), Is.EqualTo("""{"$type":"count","$value":7}"""));
    }

    [Test]
    public void UnattributedCasesKeepTheNameOfTheirType() =>
        Assert.That(JsonSerializer.Serialize((NamedShape)new Square(3)), Is.EqualTo("""{"$type":"Square","$value":{"Side":3}}"""));

    [Test]
    public void AliasKeepsReadingTheNameBeforeARename()
    {
        var renamed = JsonSerializer.Deserialize<NamedShape>("""{"$type":"Circle","$value":{"Radius":2}}""");
        var current = JsonSerializer.Deserialize<NamedShape>("""{"$type":"circle","$value":{"Radius":2}}""");

        Assert.That(renamed.Value, Is.EqualTo(new Circle(2)));
        Assert.That(current.Value, Is.EqualTo(new Circle(2)));
    }

    [Test]
    public void ClosedGenericCasesWithNamesRoundTrip()
    {
        var words = JsonSerializer.Serialize((NamedLists)new List<string> { "a" });

        Assert.That(words, Is.EqualTo("""{"$type":"words","$value":["a"]}"""));
        Assert.That(JsonSerializer.Deserialize<NamedLists>(words).Value, Is.EqualTo(new List<string> { "a" }));
        Assert.That(JsonSerializer.Deserialize<NamedLists>("""{"$type":"numbers","$value":[1]}""").Value, Is.EqualTo(new List<int> { 1 }));
    }

    [Test]
    public void SameShortNameFromTwoNamespacesWorksWithNames()
    {
        var json = JsonSerializer.Serialize((NamedItems)new SecondNamespace.Item(4));

        Assert.That(json, Is.EqualTo("""{"$type":"second","$value":{"Id":4}}"""));
        Assert.That(JsonSerializer.Deserialize<NamedItems>(json).Value, Is.TypeOf<SecondNamespace.Item>());
    }

    [Test]
    public void TwoClosedGenericCasesWithoutNamesThrowAndPointAtTheAttribute()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => UnionCaseNames.For(typeof(TwoLists)));

        Assert.That(thrown!.Message, Does.Contain("several cases named 'List`1'").And.Contain("[UnionCaseName]"));
    }

    [TestCase(typeof(NameCollidesWithAnotherCase), "several cases named 'Square'")]
    [TestCase(typeof(AliasCollidesWithAnotherCase), "several cases named 'Square'")]
    [TestCase(typeof(NameForATypeThatIsNoCase), "which is not a case of it")]
    [TestCase(typeof(EmptyName), "empty name or alias")]
    [TestCase(typeof(RepeatedName), "several [UnionCaseName] attributes")]
    public void InvalidNamesThrow(Type unionType, string message)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => UnionCaseNames.For(unionType));

        Assert.That(thrown!.Message, Does.Contain(message));
    }
}
