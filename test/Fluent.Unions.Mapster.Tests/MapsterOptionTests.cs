using Fluent.Unions.Mapster;
using Mapster;
using NUnit.Framework;

namespace Fluent.Unions.Mapster.Tests;

public sealed class MapsterOptionTests
{
    public sealed class Address
    {
        public string City { get; set; } = "";
    }

    public sealed class AddressDto
    {
        public string City { get; set; } = "";
    }

    public sealed class Person
    {
        public string Name { get; set; } = "";
        public string? Nickname { get; set; }
        public int? Age { get; set; }
        public Address? Home { get; set; }
        public List<string?> Tags { get; set; } = [];
    }

    public sealed class PersonDto
    {
        public string Name { get; set; } = "";
        public Option<string> Nickname { get; set; }
        public Option<int> Age { get; set; }
        public Option<AddressDto> Home { get; set; }
        public List<Option<string>> Tags { get; set; } = [];
    }

    public sealed class PersonOptions
    {
        public Option<string> Nickname { get; set; }
        public Option<int> Age { get; set; }
        public Option<Address> Home { get; set; }
    }

    public sealed class Counts
    {
        public Option<int> Count { get; set; }
    }

    public sealed class LongCounts
    {
        public Option<long> Count { get; set; }
    }

    private static TypeAdapterConfig NewConfig()
    {
        var config = new TypeAdapterConfig();
        config.AddFluentUnions();
        return config;
    }

    [Test]
    public void Compile_validates_all_option_mappings()
    {
        var config = NewConfig();
        config.NewConfig<Person, PersonDto>();
        config.NewConfig<PersonDto, Person>();
        config.NewConfig<PersonOptions, PersonDto>();
        config.NewConfig<Address, AddressDto>();

        Assert.DoesNotThrow(() => config.Compile());
    }

    [Test]
    public void Compile_validates_option_members_with_converted_elements()
    {
        var config = NewConfig();
        config.NewConfig<Counts, LongCounts>();
        config.NewConfig<LongCounts, Counts>();

        Assert.DoesNotThrow(() => config.Compile());
        Assert.That(new Counts { Count = Option.Some(4) }.Adapt<LongCounts>(config).Count.GetValue(), Is.EqualTo(4L));
        Assert.That(new LongCounts().Adapt<Counts>(config).Count.HasValue, Is.False);
    }

    [Test]
    public void Mapping_onto_an_existing_object_replaces_option_members()
    {
        var config = NewConfig();
        var target = new PersonDto { Nickname = Option.Some("old"), Age = Option.Some(1) };

        new Person { Nickname = "new" }.Adapt(target, config);

        Assert.That(target.Nickname.GetValue(), Is.EqualTo("new"));
        Assert.That(target.Age.HasValue, Is.False);
    }

    [Test]
    public void Nullable_members_map_to_options_with_values()
    {
        var config = NewConfig();
        var person = new Person { Name = "Ada", Nickname = "A", Age = 36, Home = new Address { City = "London" } };

        var dto = person.Adapt<PersonDto>(config);

        Assert.That(dto.Nickname.GetValue(), Is.EqualTo("A"));
        Assert.That(dto.Age.GetValue(), Is.EqualTo(36));
        Assert.That(dto.Home.GetValue().City, Is.EqualTo("London"));
    }

    [Test]
    public void Null_members_map_to_none()
    {
        var config = NewConfig();
        var person = new Person { Name = "Ada" };

        var dto = person.Adapt<PersonDto>(config);

        Assert.That(dto.Nickname.HasValue, Is.False);
        Assert.That(dto.Age.HasValue, Is.False);
        Assert.That(dto.Home.HasValue, Is.False);
    }

    [Test]
    public void Option_members_map_to_nullable_members()
    {
        var config = NewConfig();
        var dto = new PersonDto
        {
            Name = "Ada",
            Nickname = Option.Some("A"),
            Age = Option.Some(36),
            Home = Option.Some(new AddressDto { City = "London" }),
        };

        var person = dto.Adapt<Person>(config);

        Assert.That(person.Nickname, Is.EqualTo("A"));
        Assert.That(person.Age, Is.EqualTo(36));
        Assert.That(person.Home!.City, Is.EqualTo("London"));
    }

    [Test]
    public void None_members_map_to_null()
    {
        var config = NewConfig();
        var dto = new PersonDto { Name = "Ada" };

        var person = dto.Adapt<Person>(config);

        Assert.That(person.Nickname, Is.Null);
        Assert.That(person.Age, Is.Null);
        Assert.That(person.Home, Is.Null);
    }

    [Test]
    public void Option_members_map_to_option_members_of_another_type()
    {
        var config = NewConfig();
        var options = new PersonOptions { Nickname = Option.Some("A"), Home = Option.Some(new Address { City = "Paris" }) };

        var dto = options.Adapt<PersonDto>(config);

        Assert.That(dto.Nickname.GetValue(), Is.EqualTo("A"));
        Assert.That(dto.Age.HasValue, Is.False);
        Assert.That(dto.Home.GetValue().City, Is.EqualTo("Paris"));
    }

    [Test]
    public void Element_mapping_uses_the_configured_rule()
    {
        var config = NewConfig();
        config.NewConfig<Address, AddressDto>().Map(dest => dest.City, src => src.City.ToUpperInvariant());
        var options = new PersonOptions { Home = Option.Some(new Address { City = "Paris" }) };

        var dto = options.Adapt<PersonDto>(config);

        Assert.That(dto.Home.GetValue().City, Is.EqualTo("PARIS"));
    }

    [Test]
    public void Collections_of_options_map_in_both_directions()
    {
        var config = NewConfig();
        var person = new Person { Tags = ["a", null, "c"] };

        var dto = person.Adapt<PersonDto>(config);
        var back = dto.Adapt<Person>(config);

        Assert.That(dto.Tags.Select(tag => tag.HasValue), Is.EqualTo(new[] { true, false, true }));
        Assert.That(back.Tags, Is.EqualTo(new[] { "a", null, "c" }));
    }

    [Test]
    public void Nested_options_inside_collections_of_objects_are_mapped()
    {
        var config = NewConfig();
        var people = new[] { new Person { Nickname = "A" }, new Person() };

        var dtos = people.Adapt<List<PersonDto>>(config);

        Assert.That(dtos[0].Nickname.GetValue(), Is.EqualTo("A"));
        Assert.That(dtos[1].Nickname.HasValue, Is.False);
    }

    [Test]
    public void Option_converts_to_a_value_type_member_with_default_for_none()
    {
        var config = NewConfig();

        Assert.That(Option.Some(5).Adapt<int>(config), Is.EqualTo(5));
        Assert.That(default(Option<int>).Adapt<int>(config), Is.EqualTo(0));
    }

    [Test]
    public void Top_level_conversions_map_values_and_none()
    {
        var config = NewConfig();

        Assert.That("x".Adapt<Option<string>>(config).GetValue(), Is.EqualTo("x"));
        Assert.That(((string?)null).Adapt<Option<string>>(config).HasValue, Is.False);
        Assert.That(((int?)7).Adapt<Option<int>>(config).GetValue(), Is.EqualTo(7));
        Assert.That(Option.Some(3).Adapt<int?>(config), Is.EqualTo(3));
        Assert.That(default(Option<int>).Adapt<int?>(config), Is.Null);
        Assert.That(Option.Some(3).Adapt<Option<long>>(config).GetValue(), Is.EqualTo(3L));
    }

    [Test]
    public void AddFluentUnions_rejects_null_config()
    {
        Assert.Throws<ArgumentNullException>(() => ((TypeAdapterConfig)null!).AddFluentUnions());
    }

    [Test]
    public void AddFluentUnions_works_on_global_settings()
    {
        var config = TypeAdapterConfig.GlobalSettings;
        var rules = config.Rules.ToList();
        config.AddFluentUnions();
        try
        {
            var dto = new Person { Nickname = "A" }.Adapt<PersonDto>();
            Assert.That(dto.Nickname.GetValue(), Is.EqualTo("A"));
        }
        finally
        {
            config.Rules.Clear();
            config.Rules.AddRange(rules);
        }
    }
}
