using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using NUnit.Framework;

namespace Fluent.Unions.Swashbuckle.Tests;

public class OptionSchemaTests
{
    public record Address(string Street, string City);

    public record Node(string Name, Option<Node> Parent);

    public record Product(
        string Name,
        Address BillingAddress,
        Option<string> Nickname,
        Option<int> Stock,
        Option<Address> ShippingAddress,
        Option<List<string>> Tags,
        Option<Option<int>> Nested,
        Option<Address[]> History,
        Node Tree);

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Color
    {
        Red,
        Green,
    }

    public enum Size
    {
        Small,
        Large,
    }

    public record Shirt(Option<Color> Color, Option<Size> Size, Option<object> Extra);

    private static Task<JsonElement> GetSchemasAsync(bool addFluentUnions, bool openApi31 = false) =>
        GetSchemasAsync(
            addFluentUnions,
            new Product(
                "Mug",
                new Address("Main Street 1", "Springfield"),
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                new Node("a", Option.None)),
            openApi31);

    private static async Task<JsonElement> GetSchemasAsync<T>(bool addFluentUnions, T sample, bool openApi31 = false)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            if (addFluentUnions)
            {
                options.AddFluentUnions();
            }
        });

        await using var app = builder.Build();
        app.UseSwagger(options => options.OpenApiVersion = openApi31 ? OpenApiSpecVersion.OpenApi3_1 : OpenApiSpecVersion.OpenApi3_0);
        app.MapGet("/sample", () => sample);
        await app.StartAsync();

        var json = await app.GetTestClient().GetStringAsync("/swagger/v1/swagger.json");
        await app.StopAsync();

        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("components").GetProperty("schemas").Clone();
    }

    private static JsonElement Resolve(JsonElement schemas, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var name = reference.GetString()!;
        return schemas.GetProperty(name[(name.LastIndexOf('/') + 1)..]);
    }

    private static JsonElement Property(JsonElement schemas, string schema, string name) =>
        Resolve(schemas, schemas.GetProperty(schema).GetProperty("properties").GetProperty(name));

    private static JsonElement ProductProperty(JsonElement schemas, string name) =>
        Property(schemas, "Product", name);

    private static string[] TypesOf(JsonElement schema) =>
        schema.TryGetProperty("type", out var type)
            ? type.ValueKind == JsonValueKind.Array
                ? type.EnumerateArray().Select(item => item.GetString()!).ToArray()
                : [type.GetString()!]
            : [];

    private static bool IsNullBranch(JsonElement schema) =>
        TypesOf(schema).Contains("null")
        || (schema.TryGetProperty("enum", out var items)
            && items.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Null));

    private static bool AllowsNull(JsonElement schemas, JsonElement schema) =>
        TypesOf(schema).Contains("null")
        || (schema.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean())
        || (schema.TryGetProperty("oneOf", out var oneOf)
            && oneOf.EnumerateArray().Any(branch => IsNullBranch(Resolve(schemas, branch))));

    [Test]
    public async Task OptionOfString_IsNullableString()
    {
        var schemas = await GetSchemasAsync(true);

        var schema = ProductProperty(schemas, "nickname");

        Assert.That(TypesOf(schema), Is.EqualTo(new[] { "string" }));
        Assert.That(AllowsNull(schemas, schema), Is.True);
    }

    [Test]
    public async Task OptionOfInt_IsNullableIntegerWithFormat()
    {
        var schemas = await GetSchemasAsync(true);

        var schema = ProductProperty(schemas, "stock");

        Assert.That(TypesOf(schema), Is.EqualTo(new[] { "integer" }));
        Assert.That(schema.GetProperty("format").GetString(), Is.EqualTo("int32"));
        Assert.That(AllowsNull(schemas, schema), Is.True);
    }

    [Test]
    public async Task OptionOfRecord_AllowsNullAndKeepsTheProperties()
    {
        var schemas = await GetSchemasAsync(true);

        var schema = ProductProperty(schemas, "shippingAddress");

        Assert.That(AllowsNull(schemas, schema), Is.True);
        Assert.That(schema.TryGetProperty("properties", out _), Is.False);
        var address = schema.GetProperty("oneOf").EnumerateArray()
            .Select(branch => Resolve(schemas, branch))
            .Single(branch => TypesOf(branch).Contains("object"));
        Assert.That(
            address.GetProperty("properties").EnumerateObject().Select(property => property.Name),
            Is.EquivalentTo(new[] { "street", "city" }));
    }

    [Test]
    public async Task OptionOfCollection_IsNullableArray()
    {
        var schemas = await GetSchemasAsync(true);

        var tags = ProductProperty(schemas, "tags");
        var history = ProductProperty(schemas, "history");

        Assert.That(TypesOf(tags), Is.EqualTo(new[] { "array" }));
        Assert.That(tags.GetProperty("items").GetProperty("type").GetString(), Is.EqualTo("string"));
        Assert.That(AllowsNull(schemas, tags), Is.True);
        Assert.That(TypesOf(history), Is.EqualTo(new[] { "array" }));
        Assert.That(history.GetProperty("items").GetProperty("$ref").GetString(), Is.EqualTo("#/components/schemas/Address"));
        Assert.That(AllowsNull(schemas, history), Is.True);
    }

    [Test]
    public async Task NestedOption_IsNullableOptionOfTheInnerOption()
    {
        var schemas = await GetSchemasAsync(true);

        var nested = ProductProperty(schemas, "nested");

        Assert.That(AllowsNull(schemas, nested), Is.True);
        var inner = nested.GetProperty("oneOf").EnumerateArray()
            .Select(branch => Resolve(schemas, branch))
            .Single(branch => TypesOf(branch).Contains("integer"));
        Assert.That(AllowsNull(schemas, inner), Is.True);
    }

    [Test]
    public async Task RecursiveOption_PointsBackToTheNodeSchema()
    {
        var schemas = await GetSchemasAsync(true);

        var parent = Property(schemas, "Node", "parent");

        Assert.That(AllowsNull(schemas, parent), Is.True);
        Assert.That(
            parent.GetProperty("oneOf").EnumerateArray().Select(branch => branch.TryGetProperty("$ref", out var reference) ? reference.GetString() : null),
            Does.Contain("#/components/schemas/Node"));
    }

    [Test]
    public async Task OptionOfEnum_AllowsNullThroughOneOfAndKeepsTheEnum()
    {
        var schemas = await GetSchemasAsync(true, new Shirt(Option.None, Option.None, Option.None));

        foreach (var (property, count) in new[] { ("color", 2), ("size", 2) })
        {
            var option = Property(schemas, "Shirt", property);

            Assert.That(AllowsNull(schemas, option), Is.True, property);
            var values = option.GetProperty("oneOf").EnumerateArray()
                .Select(branch => Resolve(schemas, branch))
                .Single(branch => !IsNullBranch(branch))
                .GetProperty("enum").EnumerateArray();
            Assert.That(values.Count(), Is.EqualTo(count), property);
        }
    }

    [Test]
    public async Task OptionOfUntypedValue_AllowsNull()
    {
        var schemas = await GetSchemasAsync(true, new Shirt(Option.None, Option.None, Option.None));

        var option = Property(schemas, "Shirt", "extra");

        Assert.That(option.TryGetProperty("properties", out _), Is.False);
        Assert.That(AllowsNull(schemas, option), Is.True);
    }

    [Test]
    public async Task OptionProperties_AreNotRequired()
    {
        var schemas = await GetSchemasAsync(true);

        var product = schemas.GetProperty("Product");

        Assert.That(
            product.TryGetProperty("required", out var required)
                ? required.EnumerateArray().Select(item => item.GetString()).ToArray()
                : [],
            Does.Not.Contain("nickname").And.Not.Contain("stock").And.Not.Contain("shippingAddress"));
    }

    [Test]
    public async Task NonOptionProperties_AreUnchanged()
    {
        var schemas = await GetSchemasAsync(true);

        var name = ProductProperty(schemas, "name");
        var billingAddress = ProductProperty(schemas, "billingAddress");

        Assert.That(TypesOf(name), Is.EqualTo(new[] { "string" }));
        Assert.That(TypesOf(billingAddress), Is.EqualTo(new[] { "object" }));
        Assert.That(
            billingAddress.GetProperty("properties").EnumerateObject().Select(property => property.Name),
            Is.EquivalentTo(new[] { "street", "city" }));
    }

    [Test]
    public async Task OpenApi31_DescribesTheOptionWithATypeArray()
    {
        var schemas = await GetSchemasAsync(true, openApi31: true);

        Assert.That(TypesOf(ProductProperty(schemas, "nickname")), Is.EquivalentTo(new[] { "string", "null" }));
        Assert.That(TypesOf(ProductProperty(schemas, "stock")), Is.EquivalentTo(new[] { "integer", "null" }));
        Assert.That(AllowsNull(schemas, ProductProperty(schemas, "shippingAddress")), Is.True);
    }

    [Test]
    public async Task WithoutAddFluentUnions_OptionIsNotDescribed()
    {
        var schemas = await GetSchemasAsync(false);

        var nickname = ProductProperty(schemas, "nickname");

        Assert.That(TypesOf(nickname), Is.Not.EqualTo(new[] { "string" }));
        Assert.That(AllowsNull(schemas, nickname), Is.False);
    }
}
