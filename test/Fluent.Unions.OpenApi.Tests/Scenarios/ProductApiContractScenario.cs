using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Fluent.Unions.OpenApi.Tests.Scenarios;

public record Dimensions(int Width, int Height);

public record ProductResponse(string Name, Option<string> Nickname, Option<int> Stock, Option<Dimensions> Size);

public record UpdateProductRequest(Option<string> Nickname, Option<int> Stock);

public sealed class Catalog
{
    private readonly Dictionary<int, ProductResponse> products = new()
    {
        [1] = new ProductResponse("Mug", Option.None, Option.None, Option.None),
        [2] = new ProductResponse("Desk", "Standing", 4, new Dimensions(120, 60))
    };

    public Option<ProductResponse> Find(int id)
    {
        if (products.TryGetValue(id, out var product))
        {
            return product;
        }

        return Option.None;
    }

    public Option<ProductResponse> Update(int id, UpdateProductRequest request) =>
        Find(id).Select(product => products[id] = product with
        {
            Nickname = request.Nickname.Or(() => product.Nickname),
            Stock = request.Stock.Or(() => product.Stock)
        });
}

/// <summary>A published contract where Option properties are nullable in the OpenAPI document and null on the wire, so clients and server agree.</summary>
public class ProductApiContractScenario
{
    private static async Task<(HttpClient Client, WebApplication App, JsonElement Schemas)> StartAsync(bool addFluentUnions)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi(options =>
        {
            if (addFluentUnions)
            {
                options.AddFluentUnions();
            }
        });

        var app = builder.Build();
        var catalog = new Catalog();
        app.MapOpenApi();
        app.MapGet("/products/{id:int}", (int id) => catalog.Find(id).Match<ProductResponse, IResult>(Results.Ok, () => Results.NotFound()))
            .Produces<ProductResponse>()
            .Produces(StatusCodes.Status404NotFound);
        app.MapPut("/products/{id:int}", (int id, UpdateProductRequest request) =>
            catalog.Update(id, request).Match<ProductResponse, IResult>(Results.Ok, () => Results.NotFound()))
            .Produces<ProductResponse>()
            .Produces(StatusCodes.Status404NotFound);
        await app.StartAsync();

        var client = app.GetTestClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        return (client, app, document.RootElement.GetProperty("components").GetProperty("schemas").Clone());
    }

    private static string[] TypesOf(JsonElement schemas, string schema, string property)
    {
        var node = schemas.GetProperty(schema).GetProperty("properties").GetProperty(property);
        if (node.TryGetProperty("$ref", out var reference))
        {
            var name = reference.GetString()!;
            node = schemas.GetProperty(name[(name.LastIndexOf('/') + 1)..]);
        }

        if (!node.TryGetProperty("type", out var type))
        {
            return [];
        }

        return type.ValueKind == JsonValueKind.Array ? type.EnumerateArray().Select(t => t.GetString()!).ToArray() : [type.GetString()!];
    }

    [Test]
    public async Task Option_properties_of_responses_and_requests_are_documented_as_nullable()
    {
        var (_, app, schemas) = await StartAsync(addFluentUnions: true);
        await app.StopAsync();

        Assert.That(TypesOf(schemas, "ProductResponse", "nickname"), Is.EquivalentTo(new[] { "null", "string" }));
        Assert.That(TypesOf(schemas, "ProductResponse", "stock"), Does.Contain("null").And.Contain("integer"));
        Assert.That(TypesOf(schemas, "ProductResponse", "size"), Does.Contain("null").And.Contain("object"));
        Assert.That(TypesOf(schemas, "UpdateProductRequest", "nickname"), Is.EquivalentTo(new[] { "null", "string" }));
        Assert.That(schemas.GetProperty("OptionOfDimensions").GetProperty("properties").EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "width", "height" }));
    }

    [Test]
    public async Task Without_the_transformer_an_option_is_an_untyped_value()
    {
        var (_, app, schemas) = await StartAsync(addFluentUnions: false);
        await app.StopAsync();

        Assert.That(TypesOf(schemas, "ProductResponse", "nickname"), Is.Empty);
    }

    [Test]
    public async Task The_wire_format_matches_the_document_with_null_for_none()
    {
        var (client, app, _) = await StartAsync(addFluentUnions: true);

        var mug = JsonDocument.Parse(await client.GetStringAsync("/products/1")).RootElement;
        var desk = JsonDocument.Parse(await client.GetStringAsync("/products/2")).RootElement;
        var missing = await client.GetAsync("/products/99");
        await app.StopAsync();

        Assert.That(mug.GetProperty("nickname").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(mug.GetProperty("size").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(desk.GetProperty("nickname").GetString(), Is.EqualTo("Standing"));
        Assert.That(desk.GetProperty("size").GetProperty("width").GetInt32(), Is.EqualTo(120));
        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task A_partial_update_changes_only_the_fields_that_were_sent()
    {
        var (client, app, _) = await StartAsync(addFluentUnions: true);

        var updated = await client.PutAsync("/products/2", JsonContent.Create(new { stock = 9 }));
        var body = JsonDocument.Parse(await updated.Content.ReadAsStringAsync()).RootElement;
        var unknown = await client.PutAsync("/products/99", JsonContent.Create(new { stock = 1 }));
        await app.StopAsync();

        Assert.That(body.GetProperty("stock").GetInt32(), Is.EqualTo(9));
        Assert.That(body.GetProperty("nickname").GetString(), Is.EqualTo("Standing"));
        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
