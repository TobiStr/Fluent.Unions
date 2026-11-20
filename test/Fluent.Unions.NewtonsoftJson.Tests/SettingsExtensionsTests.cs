using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson.Tests;

public class SettingsExtensionsTests
{
    [Test]
    public void AddsBothConverters()
    {
        var settings = new JsonSerializerSettings();

        var returned = settings.AddFluentUnions();

        Assert.That(returned, Is.SameAs(settings));
        Assert.That(settings.Converters.OfType<OptionJsonConverter>(), Has.Exactly(1).Items);
        Assert.That(settings.Converters.OfType<ResultJsonConverter>(), Has.Exactly(1).Items);
    }

    [Test]
    public void CallingItTwiceAddsNothing()
    {
        var settings = new JsonSerializerSettings().AddFluentUnions().AddFluentUnions();

        Assert.That(settings.Converters, Has.Count.EqualTo(2));
    }

    [Test]
    public void KeepsExistingConverters()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

        settings.AddFluentUnions();

        Assert.That(settings.Converters, Has.Count.EqualTo(3));
    }

    [Test]
    public void NullSettingsThrow() =>
        Assert.Throws<ArgumentNullException>(() => JsonSerializerSettingsExtensions.AddFluentUnions(null!));

    [Test]
    public void WorksWithAJsonSerializer()
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings().AddFluentUnions());
        var writer = new StringWriter();

        serializer.Serialize(writer, new[] { (Option<int>)1, Option.None });

        Assert.That(writer.ToString(), Is.EqualTo("[1,null]"));
    }
}
