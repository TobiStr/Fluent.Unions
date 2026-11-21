using MessagePack;
using MessagePack.Resolvers;
using Fluent.Unions.MessagePack;
using NUnit.Framework;

namespace Fluent.Unions.MessagePack.Tests;

[MessagePackObject]
public class Customer
{
    [Key(0)]
    public string Name { get; set; } = "";

    [Key(1)]
    [MessagePackFormatter(typeof(OptionFormatter<string>))]
    public Option<string> Nickname { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class Scoreboard
{
    [Key(0)]
    public Option<int>[] Scores { get; set; } = [];

    [Key(1)]
    public Option<string> Title { get; set; }
}

public class UnionsResolverTests
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithFluentUnions();
    private static readonly byte[] Nil = [MessagePackCode.Nil];

    private static T RoundTrip<T>(T value) =>
        MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, Options), Options);

    [Test]
    public void Some_IsWrittenAsItsValue()
    {
        var bytes = MessagePackSerializer.Serialize<Option<int>>(7, Options);

        Assert.That(MessagePackSerializer.Deserialize<int>(bytes), Is.EqualTo(7));
    }

    [Test]
    public void None_IsWrittenAsNil()
    {
        var bytes = MessagePackSerializer.Serialize<Option<int>>(Option.None, Options);

        Assert.That(bytes, Is.EqualTo(Nil));
    }

    [Test]
    public void Nil_ReadsAsNone()
    {
        var option = MessagePackSerializer.Deserialize<Option<string>>(Nil, Options);

        Assert.That(option.HasValue, Is.False);
    }

    [Test]
    public void DefaultOption_IsWrittenAsNil()
    {
        var bytes = MessagePackSerializer.Serialize(default(Option<string>), Options);

        Assert.That(bytes, Is.EqualTo(Nil));
    }

    [Test]
    public void ValueTypeOption_RoundTrips()
    {
        Assert.That(RoundTrip<Option<int>>(42).GetValue(), Is.EqualTo(42));
        Assert.That(RoundTrip<Option<int>>(Option.None).HasValue, Is.False);
    }

    [Test]
    public void ReferenceTypeOption_RoundTrips()
    {
        Assert.That(RoundTrip<Option<string>>("Ada").GetValue(), Is.EqualTo("Ada"));
        Assert.That(RoundTrip<Option<string>>(Option.None).HasValue, Is.False);
    }

    [Test]
    public void OptionOfComplexType_RoundTrips()
    {
        var option = RoundTrip<Option<Customer>>(new Customer { Name = "Ada" });

        Assert.That(option.GetValue().Name, Is.EqualTo("Ada"));
    }

    [Test]
    public void OptionsInArray_RoundTrip()
    {
        Option<int>[] options = [1, Option.None, 3];

        var result = RoundTrip(options);

        Assert.That(result.Select(o => o.HasValue), Is.EqualTo(new[] { true, false, true }));
        Assert.That(result[2].GetValue(), Is.EqualTo(3));
    }

    [Test]
    public void OptionsInDictionary_RoundTrip()
    {
        var map = new Dictionary<string, Option<string>> { ["a"] = "x", ["b"] = Option.None };

        var result = RoundTrip(map);

        Assert.That(result["a"].GetValue(), Is.EqualTo("x"));
        Assert.That(result["b"].HasValue, Is.False);
    }

    [Test]
    public void ObjectWithFormatterAttribute_RoundTrips()
    {
        var result = RoundTrip(new Customer { Name = "Ada", Nickname = "Countess" });

        Assert.That(result.Name, Is.EqualTo("Ada"));
        Assert.That(result.Nickname.GetValue(), Is.EqualTo("Countess"));
        Assert.That(RoundTrip(new Customer { Name = "Ada" }).Nickname.HasValue, Is.False);
    }

    [Test]
    public void ObjectWithSourceGenerationSuppressed_RoundTrips()
    {
        var result = RoundTrip(new Scoreboard { Scores = [1, Option.None], Title = "Top" });

        Assert.That(result.Scores[0].GetValue(), Is.EqualTo(1));
        Assert.That(result.Scores[1].HasValue, Is.False);
        Assert.That(result.Title.GetValue(), Is.EqualTo("Top"));
    }

    [Test]
    public void SourceGeneratedResolver_RoundTripsOptionProperty()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(UnionsResolver.Instance, GeneratedMessagePackResolver.Instance, StandardResolver.Instance));

        var bytes = MessagePackSerializer.Serialize(new Customer { Name = "Ada", Nickname = "Countess" }, options);
        var result = MessagePackSerializer.Deserialize<Customer>(bytes, options);

        Assert.That(result.Nickname.GetValue(), Is.EqualTo("Countess"));
    }

    [Test]
    public void Result_RefusesToSerialize()
    {
        Result<int> error = new InvalidOperationException("Secret");

        var exception = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Serialize(error, Options))!;

        Assert.That(exception.InnerException, Is.TypeOf<NotSupportedException>());
        Assert.That(exception.ToString(), Does.Not.Contain("Secret"));
    }

    [Test]
    public void Result_RefusesToDeserialize()
    {
        Assert.That(() => MessagePackSerializer.Deserialize<Result>(Nil, Options),
            Throws.InstanceOf<MessagePackSerializationException>().With.InnerException.TypeOf<NotSupportedException>());
        Assert.That(() => MessagePackSerializer.Deserialize<Result<int>>(Nil, Options),
            Throws.InstanceOf<MessagePackSerializationException>().With.InnerException.TypeOf<NotSupportedException>());
    }

    [Test]
    public void UntouchedTypes_AreLeftToTheInnerResolver()
    {
        Assert.That(UnionsResolver.Instance.GetFormatter<int>(), Is.Null);
        Assert.That(UnionsResolver.Instance.GetFormatter<List<int>>(), Is.Null);
    }

    [Test]
    public void WithFluentUnions_KeepsTheExistingResolver()
    {
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithFluentUnions();

        var bytes = MessagePackSerializer.Serialize(new { Name = "Ada", Nickname = (Option<string>)"Countess" }, options);
        var map = MessagePackSerializer.Deserialize<Dictionary<string, string>>(bytes);

        Assert.That(map["Nickname"], Is.EqualTo("Countess"));
    }

    [Test]
    public void WithFluentUnions_CalledAgain_ReturnsTheSameOptions()
    {
        var again = Options.WithFluentUnions();

        Assert.That(again, Is.SameAs(Options));
        Assert.That(MessagePackSerializer.Deserialize<Option<int>>(MessagePackSerializer.Serialize<Option<int>>(5, again), again).GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void WithFluentUnions_ThrowsForNullOptions() =>
        Assert.Throws<ArgumentNullException>(() => MessagePackSerializerOptionsExtensions.WithFluentUnions(null!));
}
