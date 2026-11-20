using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;
using NSwag.Generation.AspNetCore;
using NUnit.Framework;

namespace Fluent.Unions.NSwag.Tests;

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
        Option<Dictionary<string, int>> Map,
        Node Tree);

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Color
    {
        Red,
        Green,
    }

    public record Shirt(Option<Color> Color, Option<object> Extra);

    private static Task<JsonElement> GetSchemasAsync(
        Action<AspNetCoreOpenApiDocumentGeneratorSettings>? configure = null,
        bool swagger2 = false) =>
        GetSchemasAsync(
            new Product(
                "Mug",
                new Address("Main Street 1", "Springfield"),
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                Option.None,
                new Node("a", Option.None)),
            configure ?? (settings => settings.AddFluentUnions()),
            swagger2);

    private static async Task<JsonElement> GetSchemasAsync<T>(
        T sample,
        Action<AspNetCoreOpenApiDocumentGeneratorSettings> configure,
        bool swagger2 = false)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddEndpointsApiExplorer();
        if (swagger2)
        {
            builder.Services.AddSwaggerDocument(configure);
        }
        else
        {
            builder.Services.AddOpenApiDocument(configure);
        }

        await using var app = builder.Build();
        app.UseOpenApi();
        app.MapGet("/sample", () => sample);
        await app.StartAsync();

        var json = await app.GetTestClient().GetStringAsync("/swagger/v1/swagger.json");
        await app.StopAsync();

        using var document = JsonDocument.Parse(json);
        return swagger2
            ? document.RootElement.GetProperty("definitions").Clone()
            : document.RootElement.GetProperty("components").GetProperty("schemas").Clone();
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

    private static string? TypeOf(JsonElement schema) =>
        schema.TryGetProperty("type", out var type) ? type.GetString() : null;

    private static bool HasNullFlag(JsonElement schema) =>
        (schema.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean())
        || (schema.TryGetProperty("x-nullable", out var extension) && extension.GetBoolean());

    private static bool IsNullBranch(JsonElement schema) =>
        HasNullFlag(schema)
        && schema.TryGetProperty("enum", out var items)
        && items.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Null);

    private static bool AllowsNull(JsonElement schemas, JsonElement schema) =>
        HasNullFlag(schema)
        || (schema.TryGetProperty("oneOf", out var oneOf)
            && oneOf.EnumerateArray().Any(branch => IsNullBranch(Resolve(schemas, branch))));

    private static JsonElement NonNullBranch(JsonElement schemas, JsonElement option) =>
        option.GetProperty("oneOf").EnumerateArray()
            .Select(branch => Resolve(schemas, branch))
            .Single(branch => !IsNullBranch(branch));

    [Test]
    public async Task OptionOfString_IsNullableString()
    {
        var schemas = await GetSchemasAsync();

        var schema = ProductProperty(schemas, "nickname");

        Assert.That(TypeOf(schema), Is.EqualTo("string"));
        Assert.That(AllowsNull(schemas, schema), Is.True);
    }

    [Test]
    public async Task OptionOfInt_IsNullableIntegerWithFormat()
    {
        var schemas = await GetSchemasAsync();

        var schema = ProductProperty(schemas, "stock");

        Assert.That(TypeOf(schema), Is.EqualTo("integer"));
        Assert.That(schema.GetProperty("format").GetString(), Is.EqualTo("int32"));
        Assert.That(AllowsNull(schemas, schema), Is.True);
    }

    [Test]
    public async Task OptionOfRecord_AllowsNullAndKeepsTheProperties()
    {
        var schemas = await GetSchemasAsync();

        var schema = ProductProperty(schemas, "shippingAddress");

        Assert.That(AllowsNull(schemas, schema), Is.True);
        Assert.That(schema.TryGetProperty("properties", out _), Is.False);
        var address = NonNullBranch(schemas, schema);
        Assert.That(
            address.GetProperty("properties").EnumerateObject().Select(property => property.Name),
            Is.EquivalentTo(new[] { "street", "city" }));
    }

    [Test]
    public async Task OptionOfCollection_IsNullableArray()
    {
        var schemas = await GetSchemasAsync();

        var tags = ProductProperty(schemas, "tags");
        var history = ProductProperty(schemas, "history");
        var map = ProductProperty(schemas, "map");

        Assert.That(TypeOf(tags), Is.EqualTo("array"));
        Assert.That(tags.GetProperty("items").GetProperty("type").GetString(), Is.EqualTo("string"));
        Assert.That(AllowsNull(schemas, tags), Is.True);
        Assert.That(TypeOf(history), Is.EqualTo("array"));
        Assert.That(history.GetProperty("items").GetProperty("$ref").GetString(), Does.EndWith("/Address"));
        Assert.That(AllowsNull(schemas, history), Is.True);
        Assert.That(TypeOf(map), Is.EqualTo("object"));
        Assert.That(map.GetProperty("additionalProperties").GetProperty("type").GetString(), Is.EqualTo("integer"));
        Assert.That(AllowsNull(schemas, map), Is.True);
    }

    [Test]
    public async Task NestedOption_IsNullableOptionOfTheInnerOption()
    {
        var schemas = await GetSchemasAsync();

        var nested = ProductProperty(schemas, "nested");

        Assert.That(AllowsNull(schemas, nested), Is.True);
        var inner = NonNullBranch(schemas, nested);
        Assert.That(TypeOf(inner), Is.EqualTo("integer"));
        Assert.That(AllowsNull(schemas, inner), Is.True);
    }

    [Test]
    public async Task RecursiveOption_PointsBackToTheNodeSchema()
    {
        var schemas = await GetSchemasAsync();

        var parent = Property(schemas, "Node", "parent");

        Assert.That(AllowsNull(schemas, parent), Is.True);
        Assert.That(
            parent.GetProperty("oneOf").EnumerateArray().Select(branch => branch.TryGetProperty("$ref", out var reference) ? reference.GetString() : null),
            Has.Some.EndsWith("/Node"));
    }

    [Test]
    public async Task OptionOfEnum_AllowsNullThroughOneOfAndKeepsTheEnum()
    {
        var schemas = await GetSchemasAsync(
            new Shirt(Option.None, Option.None),
            settings => settings.AddFluentUnions());

        var option = Property(schemas, "Shirt", "color");

        Assert.That(AllowsNull(schemas, option), Is.True);
        var values = NonNullBranch(schemas, option).GetProperty("enum").EnumerateArray().Select(item => item.GetString());
        Assert.That(values, Is.EquivalentTo(new[] { "Red", "Green" }));
    }

    [Test]
    public async Task OptionOfUntypedValue_AllowsNull()
    {
        var schemas = await GetSchemasAsync(
            new Shirt(Option.None, Option.None),
            settings => settings.AddFluentUnions());

        var option = Property(schemas, "Shirt", "extra");

        Assert.That(option.TryGetProperty("properties", out _), Is.False);
        Assert.That(AllowsNull(schemas, option), Is.True);
    }

    [Test]
    public async Task OptionProperties_AreNotRequired()
    {
        var schemas = await GetSchemasAsync();

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
        var schemas = await GetSchemasAsync();

        var name = ProductProperty(schemas, "name");
        var billingAddress = ProductProperty(schemas, "billingAddress");

        Assert.That(TypeOf(name), Is.EqualTo("string"));
        Assert.That(AllowsNull(schemas, name), Is.False);
        Assert.That(TypeOf(billingAddress), Is.EqualTo("object"));
        Assert.That(AllowsNull(schemas, billingAddress), Is.False);
    }

    [Test]
    public async Task NewtonsoftSchemaSettings_DescribeTheOptionToo()
    {
        var schemas = await GetSchemasAsync(
            settings =>
            {
                settings.SchemaSettings = new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 };
                settings.AddFluentUnions();
            });

        Assert.That(TypeOf(ProductProperty(schemas, "Nickname")), Is.EqualTo("string"));
        Assert.That(AllowsNull(schemas, ProductProperty(schemas, "Nickname")), Is.True);
        Assert.That(AllowsNull(schemas, ProductProperty(schemas, "ShippingAddress")), Is.True);
    }

    [Test]
    public void Swagger2Document_ThrowsInsteadOfDescribingTheOptionWrongly()
    {
        var exception = Assert.ThrowsAsync<NotSupportedException>(() => GetSchemasAsync(swagger2: true));

        Assert.That(exception!.Message, Does.Contain("Swagger 2"));
    }

    [Test]
    public async Task WithoutAddFluentUnions_OptionIsNotDescribed()
    {
        var schemas = await GetSchemasAsync(_ => { });

        var nickname = ProductProperty(schemas, "nickname");

        Assert.That(TypeOf(nickname), Is.Not.EqualTo("string"));
        Assert.That(AllowsNull(schemas, nickname), Is.False);
    }
}
