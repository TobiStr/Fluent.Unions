using MessagePack;
using Fluent.Unions.MessagePack;
using Fluent.Unions.Validation;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.MessagePack.Tests.Scenarios;

[MessagePackObject]
public class CachedProfile
{
    [Key(0)]
    public string Name { get; set; } = "";

    [Key(1)]
    [MessagePackFormatter(typeof(OptionFormatter<string>))]
    public Option<string> Nickname { get; set; }

    [Key(2)]
    [MessagePackFormatter(typeof(OptionFormatter<int>))]
    public Option<int> Age { get; set; }

    [IgnoreMember]
    public string DisplayName => Nickname.GetValueOrDefault(Name);

    public static Result<CachedProfile> Create(string? name, Option<string> nickname, Option<int> age) =>
        Result<CachedProfile>.Ok(new CachedProfile { Name = name ?? "", Nickname = nickname, Age = age }).Validate(
            profile => profile.Name.EnsureNotEmpty(),
            profile => profile.Age.Match<int, Result>(
                age => age.EnsureInRange(18, 120, name: "Age"),
                () => Result.Ok));
}

internal sealed class ProfileCache(Func<string, Result<CachedProfile>> load)
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithUnions();

    private readonly Dictionary<string, byte[]> _entries = [];

    public int Loads { get; private set; }

    public bool Contains(string id) => _entries.ContainsKey(id);

    public Result<CachedProfile> Get(string id)
    {
        if (_entries.TryGetValue(id, out var bytes))
            return MessagePackSerializer.Deserialize<CachedProfile>(bytes, Options);

        Loads++;
        return load(id).Do(profile => _entries[id] = MessagePackSerializer.Serialize(profile, Options));
    }
}

/// <summary>
/// Optional profile fields travel through the cache as <see cref="Option{T}"/>, and a failed load stays a result that is never cached.
/// </summary>
public class ProfileCacheScenario
{
    private static readonly Dictionary<string, Result<CachedProfile>> Database = new()
    {
        ["ada"] = CachedProfile.Create("Ada Lovelace", "Ada", 36),
        ["grace"] = CachedProfile.Create("Grace Hopper", Option.None, Option.None),
        ["minor"] = CachedProfile.Create("Tim", Option.None, 9),
        ["gone"] = new KeyNotFoundException("Profile 'gone' does not exist.")
    };

    private static ProfileCache NewCache() =>
        new(id => Database.TryGetValue(id, out var profile) ? profile : new KeyNotFoundException($"Profile '{id}' does not exist."));

    [Test]
    public void SecondRead_IsServedFromTheCacheWithoutLoading()
    {
        var cache = NewCache();

        var first = cache.Get("ada");
        var second = cache.Get("ada");

        Assert.That(cache.Loads, Is.EqualTo(1));
        Assert.That(second.GetValue().Name, Is.EqualTo(first.GetValue().Name));
    }

    [Test]
    public void PresentOptionalFields_SurviveTheCache()
    {
        var cache = NewCache();
        cache.Get("ada");

        var profile = cache.Get("ada").GetValue();

        Assert.That(profile.Nickname.GetValue(), Is.EqualTo("Ada"));
        Assert.That(profile.Age.GetValue(), Is.EqualTo(36));
        Assert.That(profile.DisplayName, Is.EqualTo("Ada"));
    }

    [Test]
    public void MissingOptionalFields_StayNoneAndNotNull()
    {
        var cache = NewCache();
        cache.Get("grace");

        var profile = cache.Get("grace").GetValue();

        Assert.That(profile.Nickname.HasValue, Is.False);
        Assert.That(profile.Age.HasValue, Is.False);
        Assert.That(profile.DisplayName, Is.EqualTo("Grace Hopper"));
    }

    [Test]
    public void FailedLoad_IsReturnedAndNeverCached()
    {
        var cache = NewCache();

        var first = cache.Get("gone");
        var second = cache.Get("gone");

        Assert.That(first.GetError(), Is.TypeOf<KeyNotFoundException>());
        Assert.That(second.IsError, Is.True);
        Assert.That(cache.Contains("gone"), Is.False);
        Assert.That(cache.Loads, Is.EqualTo(2));
    }

    [Test]
    public void InvalidProfile_IsReportedPerMemberAndNeverCached()
    {
        var cache = NewCache();

        var result = cache.Get("minor");
        var errors = (ValidationErrors)result.GetError();

        Assert.That(errors.ToDictionary().Keys, Is.EquivalentTo(new[] { "Age" }));
        Assert.That(cache.Contains("minor"), Is.False);
    }

    [Test]
    public void SerializingTheResultItself_IsRefused()
    {
        var options = MessagePackSerializerOptions.Standard.WithUnions();
        var result = Database["ada"];

        Assert.That(() => MessagePackSerializer.Serialize(result, options),
            Throws.InstanceOf<MessagePackSerializationException>().With.InnerException.TypeOf<NotSupportedException>());
    }
}
