using AutoMapper;
using AutoMapper.Internal;
using Fluent.Unions.AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Fluent.Unions.AutoMapper.Tests;

public sealed class AutoMapperOptionTests
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

    private static IMappingExpression<PersonOptions, PersonDto> IgnoreUnmapped(IMappingExpression<PersonOptions, PersonDto> map) => map
        .ForMember(dest => dest.Name, opt => opt.Ignore())
        .ForMember(dest => dest.Tags, opt => opt.Ignore());

    private static MapperConfiguration NewConfiguration(Action<IMapperConfigurationExpression>? configure = null) =>
        new(
            cfg =>
            {
                cfg.AddFluentUnions();
                cfg.CreateMap<Address, AddressDto>().ReverseMap();
                cfg.CreateMap<Person, PersonDto>().ReverseMap();
                IgnoreUnmapped(cfg.CreateMap<PersonOptions, PersonDto>());
                cfg.CreateMap<Counts, LongCounts>().ReverseMap();
                configure?.Invoke(cfg);
            },
            NullLoggerFactory.Instance);

    private static IMapper NewMapper(Action<IMapperConfigurationExpression>? configure = null) =>
        NewConfiguration(configure).CreateMapper();

    [Test]
    public void Configuration_is_valid()
    {
        Assert.DoesNotThrow(() => NewConfiguration().AssertConfigurationIsValid());
    }

    [Test]
    public void Configuration_is_invalid_when_the_element_map_is_missing()
    {
        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddFluentUnions();
                IgnoreUnmapped(cfg.CreateMap<PersonOptions, PersonDto>());
            },
            NullLoggerFactory.Instance);

        var exception = Assert.Throws<AutoMapperConfigurationException>(() => configuration.AssertConfigurationIsValid());

        Assert.That(exception!.ToString(), Does.Contain("Home"));
    }

    [Test]
    public void Nullable_members_map_to_options_with_values()
    {
        var person = new Person { Name = "Ada", Nickname = "A", Age = 36, Home = new Address { City = "London" } };

        var dto = NewMapper().Map<PersonDto>(person);

        Assert.That(dto.Nickname.GetValue(), Is.EqualTo("A"));
        Assert.That(dto.Age.GetValue(), Is.EqualTo(36));
        Assert.That(dto.Home.GetValue().City, Is.EqualTo("London"));
    }

    [Test]
    public void Null_members_map_to_none()
    {
        var dto = NewMapper().Map<PersonDto>(new Person { Name = "Ada" });

        Assert.That(dto.Nickname.HasValue, Is.False);
        Assert.That(dto.Age.HasValue, Is.False);
        Assert.That(dto.Home.HasValue, Is.False);
    }

    [Test]
    public void Option_members_map_to_nullable_members()
    {
        var dto = new PersonDto
        {
            Name = "Ada",
            Nickname = Option.Some("A"),
            Age = Option.Some(36),
            Home = Option.Some(new AddressDto { City = "London" }),
        };

        var person = NewMapper().Map<Person>(dto);

        Assert.That(person.Nickname, Is.EqualTo("A"));
        Assert.That(person.Age, Is.EqualTo(36));
        Assert.That(person.Home!.City, Is.EqualTo("London"));
    }

    [Test]
    public void None_members_map_to_null()
    {
        var person = NewMapper().Map<Person>(new PersonDto { Name = "Ada" });

        Assert.That(person.Nickname, Is.Null);
        Assert.That(person.Age, Is.Null);
        Assert.That(person.Home, Is.Null);
    }

    [Test]
    public void Option_members_map_to_option_members_of_another_type()
    {
        var options = new PersonOptions { Nickname = Option.Some("A"), Home = Option.Some(new Address { City = "Paris" }) };

        var dto = NewMapper().Map<PersonDto>(options);

        Assert.That(dto.Nickname.GetValue(), Is.EqualTo("A"));
        Assert.That(dto.Age.HasValue, Is.False);
        Assert.That(dto.Home.GetValue().City, Is.EqualTo("Paris"));
    }

    [Test]
    public void Element_mapping_uses_the_configured_map()
    {
        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddFluentUnions();
                cfg.CreateMap<Address, AddressDto>()
                    .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.ToUpperInvariant()));
                IgnoreUnmapped(cfg.CreateMap<PersonOptions, PersonDto>());
            },
            NullLoggerFactory.Instance);
        var options = new PersonOptions { Home = Option.Some(new Address { City = "Paris" }) };

        var dto = configuration.CreateMapper().Map<PersonDto>(options);

        Assert.That(dto.Home.GetValue().City, Is.EqualTo("PARIS"));
    }

    [Test]
    public void Option_members_with_converted_elements_are_mapped_and_validated()
    {
        var configuration = NewConfiguration();
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();

        Assert.That(mapper.Map<LongCounts>(new Counts { Count = Option.Some(4) }).Count.GetValue(), Is.EqualTo(4L));
        Assert.That(mapper.Map<Counts>(new LongCounts()).Count.HasValue, Is.False);
    }

    [Test]
    public void Collections_of_options_map_in_both_directions()
    {
        var mapper = NewMapper();

        var dto = mapper.Map<PersonDto>(new Person { Tags = ["a", null, "c"] });
        var back = mapper.Map<Person>(dto);

        Assert.That(dto.Tags.Select(tag => tag.HasValue), Is.EqualTo(new[] { true, false, true }));
        Assert.That(back.Tags, Is.EqualTo(new[] { "a", null, "c" }));
    }

    [Test]
    public void Nested_options_inside_collections_of_objects_are_mapped()
    {
        var people = new[] { new Person { Nickname = "A" }, new Person() };

        var dtos = NewMapper().Map<List<PersonDto>>(people);

        Assert.That(dtos[0].Nickname.GetValue(), Is.EqualTo("A"));
        Assert.That(dtos[1].Nickname.HasValue, Is.False);
    }

    [Test]
    public void Mapping_onto_an_existing_object_replaces_option_members()
    {
        var target = new PersonDto { Nickname = Option.Some("old"), Age = Option.Some(1) };

        NewMapper().Map(new Person { Nickname = "new" }, target);

        Assert.That(target.Nickname.GetValue(), Is.EqualTo("new"));
        Assert.That(target.Age.HasValue, Is.False);
    }

    [Test]
    public void Top_level_conversions_map_values_and_none()
    {
        var mapper = NewMapper();

        Assert.That(mapper.Map<Option<string>>("x").GetValue(), Is.EqualTo("x"));
        Assert.That(mapper.Map<Option<int>>((int?)7).GetValue(), Is.EqualTo(7));
        Assert.That(mapper.Map<int?>(Option.Some(3)), Is.EqualTo(3));
        Assert.That(mapper.Map<int?>(default(Option<int>)), Is.Null);
        Assert.That(mapper.Map<int>(Option.Some(5)), Is.EqualTo(5));
        Assert.That(mapper.Map<int>(default(Option<int>)), Is.EqualTo(0));
        Assert.That(mapper.Map<Option<long>>(Option.Some(3)).GetValue(), Is.EqualTo(3L));
    }

    [Test]
    public void AddFluentUnions_twice_still_maps_options()
    {
        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddFluentUnions();
                cfg.AddFluentUnions();
            },
            NullLoggerFactory.Instance);

        Assert.That(configuration.CreateMapper().Map<Option<string>>("x").GetValue(), Is.EqualTo("x"));
    }

    [Test]
    public void AddFluentUnions_adds_one_mapper_when_called_again()
    {
        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddFluentUnions();
                cfg.AddFluentUnions();
                Assert.That(cfg.Internal().Mappers.Count(mapper => mapper.GetType().Name == "OptionMapper"), Is.EqualTo(1));
            },
            NullLoggerFactory.Instance);

        Assert.That(configuration, Is.Not.Null);
    }

    [Test]
    public void AddFluentUnions_rejects_null_configuration()
    {
        Assert.Throws<ArgumentNullException>(() => ((IMapperConfigurationExpression)null!).AddFluentUnions());
    }
}
