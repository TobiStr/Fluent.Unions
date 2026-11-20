using System.Text.Json.Nodes;
using Fluent.Unions.Validation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.HotChocolate.Tests.Scenarios;

public record ShopProduct(string Sku, string Title, int Stock);

public record ShopOrder(string Customer, string Sku, int Quantity);

public sealed class SoldOutException(string sku) : Exception($"{sku} is sold out.")
{
    public string Sku { get; } = sku;
}

public sealed class InvalidOrderError(ValidationErrors errors)
{
    public string Message { get; } = "The order is invalid.";

    public IReadOnlyList<string> Problems { get; } = errors.Errors.Select(e => e.Message).ToList();
}

public sealed class Shop
{
    private readonly Dictionary<string, ShopProduct> products = new()
    {
        ["dune"] = new ShopProduct("dune", "Dune", 2),
        ["emma"] = new ShopProduct("emma", "Emma", 0)
    };

    public Option<ShopProduct> Find(string sku)
    {
        if (products.TryGetValue(sku, out var product))
        {
            return product;
        }

        return Option.None;
    }

    public Result<ShopOrder> Place(string customer, string sku, int quantity) =>
        Result<(string Customer, string Sku, int Quantity)>.Ok((customer, sku, quantity))
            .Validate(c => c.Customer.EnsureNotEmpty(), c => c.Quantity.Ensure(q => q > 0, "Quantity must be positive."))
            .Chain(c => Find(c.Sku)
                .ToResult(() => new KeyNotFoundException($"Unknown sku {c.Sku}"))
                .Ensure(p => p.Stock >= c.Quantity, p => new SoldOutException(p.Sku))
                .Select(p =>
                {
                    products[p.Sku] = p with { Stock = p.Stock - c.Quantity };
                    return new ShopOrder(c.Customer, c.Sku, c.Quantity);
                }));
}

public class ShopQuery(Shop shop)
{
    public Option<ShopProduct> GetProduct(string sku) => shop.Find(sku);
}

public class ShopMutation(Shop shop)
{
    [Error<InvalidOrderError>]
    [Error<SoldOutException>]
    public Result<ShopOrder> PlaceOrder(string customer, string sku, int quantity) => shop.Place(customer, sku, quantity);
}

/// <summary>A GraphQL shop where Option fields resolve to null and Result errors become a typed error union in the mutation payload.</summary>
public class ShopApiScenario
{
    private static async Task<JsonNode> RunAsync(string query)
    {
        var services = new ServiceCollection().AddSingleton<Shop>();
        var executor = await services
            .AddGraphQL()
            .AddQueryType<ShopQuery>()
            .AddMutationType<ShopMutation>()
            .AddMutationConventions(applyToAllMutations: true)
            .AddFluentUnions()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(query);
        return JsonNode.Parse(result.ToJson())!;
    }

    [Test]
    public async Task An_option_field_resolves_to_the_value_or_null()
    {
        var found = await RunAsync("""{ product(sku: "dune") { title stock } }""");
        var missing = await RunAsync("""{ product(sku: "nope") { title } }""");

        Assert.That(found["data"]!["product"]!["stock"]!.GetValue<int>(), Is.EqualTo(2));
        Assert.That(missing["errors"], Is.Null);
        Assert.That(missing["data"]!["product"], Is.Null);
    }

    [Test]
    public async Task A_valid_order_fills_the_payload_and_leaves_errors_empty()
    {
        var json = await RunAsync("""
            mutation { placeOrder(input: { customer: "Ada", sku: "dune", quantity: 2 }) { shopOrder { sku quantity } errors { __typename } } }
            """);

        Assert.That(json["errors"], Is.Null);
        Assert.That(json["data"]!["placeOrder"]!["shopOrder"]!["quantity"]!.GetValue<int>(), Is.EqualTo(2));
        Assert.That(json["data"]!["placeOrder"]!["errors"], Is.Null);
    }

    [Test]
    public async Task Validation_failures_are_returned_as_one_typed_error_with_every_problem()
    {
        var json = await RunAsync("""
            mutation { placeOrder(input: { customer: "", sku: "dune", quantity: 0 }) { shopOrder { sku } errors { __typename ... on InvalidOrderError { problems } } } }
            """);

        var error = json["data"]!["placeOrder"]!["errors"]![0]!;
        Assert.That(json["errors"], Is.Null);
        Assert.That(error["__typename"]!.GetValue<string>(), Is.EqualTo("InvalidOrderError"));
        Assert.That(error["problems"]!.AsArray(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task A_business_failure_is_a_typed_error_in_the_union()
    {
        var json = await RunAsync("""
            mutation { placeOrder(input: { customer: "Ada", sku: "emma", quantity: 1 }) { shopOrder { sku } errors { __typename ... on SoldOutError { message sku } } } }
            """);

        var error = json["data"]!["placeOrder"]!["errors"]![0]!;
        Assert.That(error["__typename"]!.GetValue<string>(), Is.EqualTo("SoldOutError"));
        Assert.That(error["sku"]!.GetValue<string>(), Is.EqualTo("emma"));
    }

    [Test]
    public async Task An_unmapped_failure_stays_a_graphql_error_and_hides_its_message()
    {
        var json = await RunAsync("""
            mutation { placeOrder(input: { customer: "Ada", sku: "ghost", quantity: 1 }) { shopOrder { sku } } }
            """);

        Assert.That(json["errors"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That(json["errors"]![0]!["message"]!.GetValue<string>(), Does.Not.Contain("ghost"));
    }
}
