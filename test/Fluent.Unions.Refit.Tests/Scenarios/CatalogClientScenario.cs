using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Refit;

namespace Fluent.Unions.Refit.Tests.Scenarios;

public sealed record Product(string Sku, decimal Price);

[JsonSerializable(typeof(Product))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class CatalogJsonContext : JsonSerializerContext;

public interface ICatalogApi
{
    [Get("/products/{sku}")]
    Task<IApiResponse<Product>> GetProduct(string sku, CancellationToken cancellationToken = default);

    [Delete("/products/{sku}")]
    Task<IApiResponse> RetireProduct(string sku, CancellationToken cancellationToken = default);
}

internal sealed class RoutedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = respond(request);
        response.RequestMessage = request;

        return Task.FromResult(response);
    }

    public static ICatalogApi Api(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        RestService.ForGenerated<ICatalogApi>(
            new HttpClient(new RoutedHandler(respond)) { BaseAddress = new Uri("http://catalog.test") },
            RefitSettings.ForJsonContext(CatalogJsonContext.Default));

    public static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

public sealed class CatalogGateway(ICatalogApi api)
{
    public async Task<Result<Option<Product>>> FindAsync(string sku, CancellationToken cancellationToken = default) =>
        await api.GetProduct(sku, cancellationToken).ToOptionalResult();

    public async Task<Result<Product>> RequireAsync(string sku, CancellationToken cancellationToken = default)
    {
        var found = await FindAsync(sku, cancellationToken);

        return found.Chain(product => product.ToResult(() => new KeyNotFoundException($"Unknown sku {sku}")));
    }

    public async Task<Result<decimal>> PriceBasketAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        var products = await Result.WhenAll(skus.Select(sku => RequireAsync(sku, cancellationToken)));

        return products.Select(list => list.Sum(product => product.Price));
    }

    public Task<Result> RetireAsync(string sku, CancellationToken cancellationToken = default) =>
        api.RetireProduct(sku, cancellationToken).ToResult();
}

/// <summary>A typed catalog client where a 404 becomes None, other failures become errors, and both flow through the same Result pipeline.</summary>
public class CatalogClientScenario
{
    private static HttpResponseMessage Catalog(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    {
        "/products/KB-1" => RoutedHandler.Json("""{"sku":"KB-1","price":10.5}"""),
        "/products/MS-2" => RoutedHandler.Json("""{"sku":"MS-2","price":4.5}"""),
        "/products/BROKEN" => new HttpResponseMessage(HttpStatusCode.InternalServerError),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
    };

    private static CatalogGateway Gateway(Func<HttpRequestMessage, HttpResponseMessage>? respond = null) =>
        new(RoutedHandler.Api(respond ?? Catalog));

    [Test]
    public async Task A_missing_product_is_none_not_an_error()
    {
        var gateway = Gateway();

        var found = (await gateway.FindAsync("KB-1")).GetValue();
        var missing = (await gateway.FindAsync("GONE")).GetValue();

        Assert.That(found.GetValue(), Is.EqualTo(new Product("KB-1", 10.5m)));
        Assert.That(missing.HasValue, Is.False);
    }

    [Test]
    public async Task A_server_error_is_an_error_that_keeps_the_status_code()
    {
        var result = await Gateway().FindAsync("BROKEN");

        var error = (ApiException)result.GetError();
        Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task A_basket_is_priced_when_every_product_exists()
    {
        var total = await Gateway().PriceBasketAsync(["KB-1", "MS-2"]);

        Assert.That(total.GetValue(), Is.EqualTo(15m));
    }

    [Test]
    public async Task A_basket_with_an_unknown_product_fails_with_a_domain_error()
    {
        var total = await Gateway().PriceBasketAsync(["KB-1", "GONE"]);

        var message = total.Match(sum => $"total {sum}", error => error.Message);
        Assert.That(message, Is.EqualTo("Unknown sku GONE"));
        Assert.That(total.GetError(), Is.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public async Task A_network_failure_is_an_error_holding_the_http_exception()
    {
        var gateway = Gateway(_ => throw new HttpRequestException("no route to host"));

        var result = await gateway.RequireAsync("KB-1");

        Assert.That(result.GetError(), Is.TypeOf<ApiRequestException>());
        Assert.That(result.GetError().InnerException, Is.TypeOf<HttpRequestException>());
    }

    [Test]
    public async Task Retiring_a_product_is_a_plain_result()
    {
        var gateway = Gateway(request => request.RequestUri!.AbsolutePath == "/products/KB-1"
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        var retired = await gateway.RetireAsync("KB-1");
        var missing = await gateway.RetireAsync("GONE");

        Assert.That(retired.IsSuccess, Is.True);
        Assert.That(((ApiException)missing.GetError()).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Cancellation_is_thrown_and_not_turned_into_an_error()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Assert.CatchAsync<OperationCanceledException>(async () => await Gateway().FindAsync("KB-1", cancellation.Token));
    }
}
