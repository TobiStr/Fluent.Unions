using Fluent.Unions.Mapster;
using Fluent.Unions.Validation;
using Mapster;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Mapster.Tests.Scenarios;

public sealed class AddressEntity
{
    public string Street { get; set; } = "";

    public string City { get; set; } = "";
}

public sealed class CustomerEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Nickname { get; set; }

    public int? Age { get; set; }

    public AddressEntity? Billing { get; set; }
}

public sealed class AddressDto
{
    public string Line { get; set; } = "";
}

public sealed class CustomerDto
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public Option<string> Nickname { get; set; }

    public Option<int> Age { get; set; }

    public Option<AddressDto> Billing { get; set; }
}

public sealed class NewCustomerDto
{
    public string Name { get; set; } = "";

    public Option<string> Nickname { get; set; }

    public Option<int> Age { get; set; }
}

public static class ScenarioMapping
{
    public static TypeAdapterConfig Config()
    {
        var config = new TypeAdapterConfig();
        config.AddFluentUnions();
        config.NewConfig<AddressEntity, AddressDto>().Map(d => d.Line, s => s.Street + ", " + s.City);
        config.NewConfig<CustomerEntity, CustomerDto>();
        config.NewConfig<NewCustomerDto, CustomerEntity>();
        return config;
    }
}

public sealed class CustomerService(TypeAdapterConfig config)
{
    private readonly Dictionary<int, CustomerEntity> store = [];

    public Result<CustomerDto> Register(NewCustomerDto request) =>
        Result<NewCustomerDto>.Ok(request)
            .Validate(
                r => r.Name.EnsureNotEmpty(),
                r => r.Age.Match(a => a is >= 0 and < 130 ? Result.Ok : new ValidationError("Age is out of range.", "Age"), () => Result.Ok))
            .Select(r => Save(r.Adapt<CustomerEntity>(config)))
            .Select(e => e.Adapt<CustomerDto>(config));

    public Option<CustomerDto> Find(int id) => Entity(id).Select(e => e.Adapt<CustomerDto>(config));

    public Option<CustomerEntity> Entity(int id)
    {
        if (store.TryGetValue(id, out var entity))
        {
            return entity;
        }

        return Option.None;
    }

    public CustomerEntity Save(CustomerEntity entity)
    {
        entity.Id = store.Count + 1;
        return store[entity.Id] = entity;
    }
}

/// <summary>Mapping entities with nullable columns to DTOs with Option properties and back, validated by the core, without null checks in the mapping code.</summary>
public class CustomerMappingScenario
{
    private CustomerService service = null!;

    [SetUp]
    public void SetUp() => service = new CustomerService(ScenarioMapping.Config());

    [Test]
    public void A_registration_maps_options_to_nullable_columns_and_back()
    {
        var created = service.Register(new NewCustomerDto { Name = "Ada", Nickname = "countess" }).GetValue();

        var entity = service.Entity(created.Id).GetValue();
        Assert.That(entity.Nickname, Is.EqualTo("countess"));
        Assert.That(entity.Age, Is.Null);
        Assert.That(created.Nickname.GetValue(), Is.EqualTo("countess"));
        Assert.That(created.Age.HasValue, Is.False);
    }

    [Test]
    public void A_nullable_nested_entity_maps_to_an_option_using_the_element_rule()
    {
        var withBilling = service.Save(new CustomerEntity { Name = "Grace", Billing = new AddressEntity { Street = "Main 1", City = "Arlington" } });
        var without = service.Save(new CustomerEntity { Name = "Linus" });

        Assert.That(service.Find(withBilling.Id).GetValue().Billing.GetValue().Line, Is.EqualTo("Main 1, Arlington"));
        Assert.That(service.Find(without.Id).GetValue().Billing.HasValue, Is.False);
    }

    [Test]
    public void An_unknown_customer_is_none_instead_of_a_null_dto()
    {
        Assert.That(service.Find(99).HasValue, Is.False);
    }

    [Test]
    public void Invalid_input_is_rejected_before_any_mapping_happens()
    {
        var result = service.Register(new NewCustomerDto { Name = "", Age = 200 });

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.Errors, Has.Count.EqualTo(2));
        Assert.That(service.Entity(1).HasValue, Is.False);
    }

    [Test]
    public void The_mapping_configuration_is_valid()
    {
        Assert.DoesNotThrow(() => ScenarioMapping.Config().Compile());
    }
}
