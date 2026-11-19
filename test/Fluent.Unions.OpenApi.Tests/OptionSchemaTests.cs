using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using NUnit.Framework;

namespace Fluent.Unions.OpenApi.Tests;

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
        Node Tree);

    private static Task<JsonElement> GetSchemasAsync(bool addFluentUnions) =>
        GetSchemasAsync(
            addFluentUnions,
            new Product("Mug", new Address("Main Street 1", "Springfield"), Option.None, Option.None, Option.None, new Node("a", Option.None)));

    private static async Task<JsonElement> GetSchemasAsync<T>(bool addFluentUnions, T sample, Action<OpenApiOptions>? configure = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi(options =>
        {
            configure?.Invoke(options);
            if (addFluentUnions)
            {
                options.AddFluentUnions();
            }
        });

        await using var app = builder.Build();
        app.MapOpenApi();
        app.MapGet("/sample", () => sample);
        await app.StartAsync();

        var json = await app.GetTestClient().GetStringAsync("/openapi/v1.json");
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

    private static JsonElement ProductProperty(JsonElement schemas, string name) =>
        Resolve(schemas, schemas.GetProperty("Product").GetProperty("properties").GetProperty(name));

    private static string[] TypesOf(JsonElement schema) =>
        schema.TryGetProperty("type", out var type)
            ? type.ValueKind == JsonValueKind.Array
                ? type.EnumerateArray().Select(item => item.GetString()!).ToArray()
                : [type.GetString()!]
            : [];

    private static bool AllowsNull(JsonElement schemas, JsonElement schema) =>
        TypesOf(schema).Contains("null")
        || (schema.TryGetProperty("oneOf", out var oneOf)
            && oneOf.EnumerateArray().Any(branch => TypesOf(Resolve(schemas, branch)).Contains("null")));

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

    public record Shirt(
        [property: System.ComponentModel.Description("The color of the shirt")] Option<Color> Color,
        Option<object> Extra);

    public record Garment(Option<Size> Size);

    [Test]
    public async Task OptionOfStringEnum_AllowsNullThroughOneOfAndKeepsThePropertyDescription()
    {
        var schemas = await GetSchemasAsync(true, new Shirt(Option.None, Option.None));

        var property = schemas.GetProperty("Shirt").GetProperty("properties").GetProperty("color");
        var option = schemas.GetProperty("OptionOfColor");

        Assert.That(property.GetProperty("description").GetString(), Is.EqualTo("The color of the shirt"));
        Assert.That(TypesOf(option), Is.Empty);
        Assert.That(AllowsNull(schemas, option), Is.True);
        Assert.That(
            option.GetProperty("oneOf").EnumerateArray().Select(branch => Resolve(schemas, branch)).Any(branch => branch.TryGetProperty("enum", out _)),
            Is.True);
    }

    [Test]
    public async Task OptionOfUntypedValue_AllowsNullThroughOneOf()
    {
        var schemas = await GetSchemasAsync(true, new Shirt(Option.None, Option.None));

        var option = schemas.GetProperty("OptionOfObject");

        Assert.That(TypesOf(option), Is.Empty);
        Assert.That(option.TryGetProperty("properties", out _), Is.False);
        Assert.That(AllowsNull(schemas, option), Is.True);
    }

    [Test]
    public async Task OptionOfTypedEnum_KeepsTheMetadataAndAllowsNullInTheEnum()
    {
        var schemas = await GetSchemasAsync(
            true,
            new Garment(Option.None),
            options => options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(Size))
                {
                    schema.Type = JsonSchemaType.String;
                    schema.Enum = [JsonValue.Create("Small")!, JsonValue.Create("Large")!];
                    schema.Description = "How large";
                    schema.Title = "Garment size";
                    schema.Deprecated = true;
                }

                return Task.CompletedTask;
            }));

        var option = schemas.GetProperty("OptionOfSize");

        Assert.That(TypesOf(option), Is.EquivalentTo(new[] { "string", "null" }));
        Assert.That(option.GetProperty("enum").EnumerateArray().Select(item => item.ValueKind), Does.Contain(JsonValueKind.Null));
        Assert.That(option.GetProperty("enum").EnumerateArray().Select(item => item.GetString()), Does.Contain("Small").And.Contain("Large"));
        Assert.That(option.GetProperty("description").GetString(), Is.EqualTo("How large"));
        Assert.That(option.GetProperty("title").GetString(), Is.EqualTo("Garment size"));
        Assert.That(option.GetProperty("deprecated").GetBoolean(), Is.True);
    }

    [Test]
    public async Task OptionOfString_IsStringOrNull()
    {
        var schemas = await GetSchemasAsync(true);

        var types = TypesOf(ProductProperty(schemas, "nickname"));

        Assert.That(types, Is.EquivalentTo(new[] { "string", "null" }));
    }

    [Test]
    public async Task OptionOfInt_IsIntegerOrNull()
    {
        var schemas = await GetSchemasAsync(true);

        var schema = ProductProperty(schemas, "stock");

        Assert.That(TypesOf(schema), Does.Contain("integer").And.Contain("null"));
        Assert.That(schema.GetProperty("format").GetString(), Is.EqualTo("int32"));
    }

    [Test]
    public async Task OptionOfObject_AllowsNullAndKeepsTheProperties()
    {
        var schemas = await GetSchemasAsync(true);

        var schema = ProductProperty(schemas, "shippingAddress");

        Assert.That(AllowsNull(schemas, schema), Is.True);
        var addressSchema = schema.TryGetProperty("oneOf", out var oneOf)
            ? oneOf.EnumerateArray().Select(branch => Resolve(schemas, branch)).Single(branch => TypesOf(branch).Contains("object"))
            : schema;
        Assert.That(addressSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name), Is.EquivalentTo(new[] { "street", "city" }));
    }

    [Test]
    public async Task RecursiveOption_IsDescribedAsNullableObject()
    {
        var schemas = await GetSchemasAsync(true);

        var parent = Resolve(schemas, schemas.GetProperty("Node").GetProperty("properties").GetProperty("parent"));

        Assert.That(TypesOf(parent), Does.Contain("object").And.Contain("null"));
        Assert.That(parent.GetProperty("properties").EnumerateObject().Select(property => property.Name), Is.EquivalentTo(new[] { "name", "parent" }));

        var nested = parent.GetProperty("properties").GetProperty("parent");
        Assert.That(nested.GetProperty("$ref").GetString(), Is.EqualTo("#/components/schemas/OptionOfNode"));
        Assert.That(AllowsNull(schemas, Resolve(schemas, nested)), Is.True);
    }

    [Test]
    public async Task OptionProperties_RemainRequired()
    {
        var schemas = await GetSchemasAsync(true);

        var required = schemas.GetProperty("Product").GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray();

        Assert.That(required, Does.Contain("nickname").And.Contain("stock").And.Contain("shippingAddress"));
    }

    [Test]
    public async Task NonOptionProperties_AreUnchanged()
    {
        var schemas = await GetSchemasAsync(true);

        var name = ProductProperty(schemas, "name");
        var billingAddress = ProductProperty(schemas, "billingAddress");

        Assert.That(TypesOf(name), Is.EqualTo(new[] { "string" }));
        Assert.That(AllowsNull(schemas, billingAddress), Is.False);
    }

    [Test]
    public async Task WithoutAddFluentUnions_OptionIsNotDescribed()
    {
        var schemas = await GetSchemasAsync(false);

        var nickname = ProductProperty(schemas, "nickname");

        Assert.That(TypesOf(nickname), Is.Empty);
        Assert.That(AllowsNull(schemas, nickname), Is.False);
    }
}
