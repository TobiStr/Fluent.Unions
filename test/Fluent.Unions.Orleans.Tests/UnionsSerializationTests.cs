using Fluent.Unions.Abstractions;
using Fluent.Unions.Orleans;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans;
using Orleans.Serialization;

namespace Fluent.Unions.Orleans.Tests;

[GenerateSerializer]
public sealed record Customer([property: Id(0)] string Name, [property: Id(1)] Option<string> Nickname);

[GenerateSerializer]
public readonly record struct AppError([property: Id(0)] string Message) : IFromException<AppError>
{
    public static AppError FromException(Exception exception) => new(exception.Message);
}

[TestFixture]
public class UnionsSerializationTests
{
    private ServiceProvider _provider = null!;

    [OneTimeSetUp]
    public void SetUp() =>
        _provider = new ServiceCollection().AddSerializer(builder => builder.AddUnions()).BuildServiceProvider();

    [OneTimeTearDown]
    public void TearDown() => _provider.Dispose();

    private T RoundTrip<T>(T value)
    {
        var serializer = _provider.GetRequiredService<Serializer<T>>();
        return serializer.Deserialize(serializer.SerializeToArray(value))!;
    }

    private T Copy<T>(T value) => _provider.GetRequiredService<DeepCopier<T>>().Copy(value)!;

    [Test]
    public void Some_RoundTrips() =>
        Assert.That(RoundTrip<Option<int>>(42).GetValue(), Is.EqualTo(42));

    [Test]
    public void None_RoundTrips() =>
        Assert.That(RoundTrip<Option<int>>(Option.None).HasValue, Is.False);

    [Test]
    public void DefaultOption_RoundTripsAsNone() =>
        Assert.That(RoundTrip(default(Option<string>)).HasValue, Is.False);

    [Test]
    public void ReferenceTypeOption_RoundTrips()
    {
        Assert.That(RoundTrip<Option<string>>("Ada").GetValue(), Is.EqualTo("Ada"));
        Assert.That(RoundTrip<Option<string>>(Option.None).HasValue, Is.False);
    }

    [Test]
    public void OptionOfComplexType_RoundTrips() =>
        Assert.That(RoundTrip<Option<Customer>>(new Customer("Ada", Option.None)).GetValue().Name, Is.EqualTo("Ada"));

    [Test]
    public void OptionsInCollections_RoundTrip()
    {
        var list = RoundTrip(new List<Option<int>> { 1, Option.None, 3 });
        var map = RoundTrip(new Dictionary<string, Option<string>> { ["a"] = "x", ["b"] = Option.None });

        Assert.That(list.Select(o => o.HasValue), Is.EqualTo(new[] { true, false, true }));
        Assert.That(map["a"].GetValue(), Is.EqualTo("x"));
        Assert.That(map["b"].HasValue, Is.False);
    }

    [Test]
    public void GenerateSerializerTypeWithOptionProperty_RoundTrips()
    {
        var some = RoundTrip(new Customer("Ada", "Countess"));
        var none = RoundTrip(new Customer("Ada", Option.None));

        Assert.That(some.Nickname.GetValue(), Is.EqualTo("Countess"));
        Assert.That(none.Nickname.HasValue, Is.False);
    }

    [Test]
    public void DeepCopy_KeepsTheValue()
    {
        Assert.That(Copy<Option<string>>("Ada").GetValue(), Is.EqualTo("Ada"));
        Assert.That(Copy<Option<string>>(Option.None).HasValue, Is.False);
    }

    [Test]
    public void Converter_ConvertsBothWays()
    {
        var converter = new OptionConverter<int>();

        var surrogate = converter.ConvertToSurrogate(7);

        Assert.That(surrogate.HasValue, Is.True);
        Assert.That(surrogate.Value, Is.EqualTo(7));
        Assert.That(converter.ConvertFromSurrogate(surrogate).GetValue(), Is.EqualTo(7));
        Assert.That(converter.ConvertToSurrogate(Option.None).HasValue, Is.False);
        Assert.That(converter.ConvertFromSurrogate(default).HasValue, Is.False);
    }

    [Test]
    public void Result_RefusesToSerialize()
    {
        Result<int> error = new InvalidOperationException("Secret");

        var exception = Assert.Throws<NotSupportedException>(() => _provider.GetRequiredService<Serializer<Result<int>>>().SerializeToArray(error))!;

        Assert.That(exception.Message, Does.Not.Contain("Secret"));
    }

    [Test]
    public void ResultVariants_RefuseToSerialize()
    {
        Assert.Throws<NotSupportedException>(() => _provider.GetRequiredService<Serializer<Result>>().SerializeToArray(Result.Ok));
        Assert.Throws<NotSupportedException>(() => _provider.GetRequiredService<Serializer<Result<int, AppError>>>().SerializeToArray(1));
    }

    [Test]
    public void ResultInsideACollection_RefusesToSerialize()
    {
        var serializer = _provider.GetRequiredService<Serializer<List<Result<int>>>>();

        Assert.Throws<NotSupportedException>(() => serializer.SerializeToArray([Result<int>.Ok(1)]));
    }

    [Test]
    public void WithoutAddUnions_ResultStillFailsWithoutALeak()
    {
        using var provider = new ServiceCollection().AddSerializer().BuildServiceProvider();
        Result<int> error = new InvalidOperationException("Secret");

        var exception = Assert.Throws<CodecNotFoundException>(() => provider.GetRequiredService<Serializer<Result<int>>>().SerializeToArray(error))!;

        Assert.That(exception.Message, Does.Not.Contain("Secret"));
    }

    [Test]
    public void AddUnions_ThrowsForNullBuilder() =>
        Assert.Throws<ArgumentNullException>(() => SerializerBuilderExtensions.AddUnions(null!));
}
