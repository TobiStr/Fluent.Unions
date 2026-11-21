using System.Net;
using Fluent.Unions;
using Fluent.Unions.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace AspNetCoreDomain.Tests;

public class GeneratedResultHttpTests
{
    private static Result<int> Found(int id) => id;

    private static Result<int> Missing() => new MissingException("No order");

    [Test]
    public async Task ToHttpResult_GivesOkForAValueWithoutAMapping()
    {
        var (status, body) = await Execute(Found(7).ToHttpResult());

        Assert.That(status, Is.EqualTo(200));
        Assert.That(body, Is.EqualTo("7"));
    }

    [Test]
    public async Task ToHttpResult_GivesProblemDetailsForTheExceptionError()
    {
        var (status, _) = await Execute(Missing().ToHttpResult());

        Assert.That(status, Is.EqualTo(500));
    }

    [Test]
    public async Task Filter_GivesOkForAValue()
    {
        await using var host = await Host(api => api.MapGet("/value", () => Found(7)));

        var response = await host.Client.GetAsync("/value");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("7"));
    }

    [Test]
    public async Task Filter_MapsTheErrorThroughTheOptions()
    {
        await using var host = await Host(
            api => api.MapGet("/missing", () => Missing()),
            services => services.Configure<ResultHttpOptions>(o => o.Map<MissingException>(StatusCodes.Status404NotFound)));

        var response = await host.Client.GetAsync("/missing");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Filter_GivesServerErrorForADefaultResult()
    {
        await using var host = await Host(api => api.MapGet("/empty", () => default(Result<int>)));

        var response = await host.Client.GetAsync("/empty");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task Filter_GivesNoContentForASuccessWithoutAValue()
    {
        await using var host = await Host(api => api.MapGet("/ok", () => Result.Ok));

        var response = await host.Client.GetAsync("/ok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Filter_MapsTheErrorOfAResultWithoutAValue()
    {
        await using var host = await Host(
            api => api.MapGet("/missing", () => Result.Error(new MissingException("gone"))),
            services => services.Configure<ResultHttpOptions>(o => o.Map<MissingException>(StatusCodes.Status404NotFound)));

        var response = await host.Client.GetAsync("/missing");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Filter_AwaitsAsyncHandlers()
    {
        await using var host = await Host(api => api.MapGet("/async", async () =>
        {
            await Task.Yield();
            return Found(3);
        }));

        var response = await host.Client.GetAsync("/async");

        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("3"));
    }

    [Test]
    public async Task Filter_DescribesTheResponsesInTheEndpointMetadata()
    {
        await using var host = await Host(api =>
        {
            api.MapGet("/value", () => Found(1));
            api.MapGet("/ok", () => Result.Ok);
        });

        Assert.That(StatusCodesOf(host, "/value"), Is.SupersetOf(new[] { 200, 400, 500 }));
        Assert.That(TypeOf(host, "/value", 200), Is.EqualTo(typeof(int)));
        Assert.That(StatusCodesOf(host, "/ok"), Is.SupersetOf(new[] { 204, 400, 500 }));
    }

    private static IEnumerable<int> StatusCodesOf(TestHost host, string pattern) => Metadata(host, pattern).Select(m => m.StatusCode);

    private static Type? TypeOf(TestHost host, string pattern, int status) => Metadata(host, pattern).First(m => m.StatusCode == status).Type;

    private static IEnumerable<IProducesResponseTypeMetadata> Metadata(TestHost host, string pattern) =>
        host.App.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == pattern)
            .Metadata.OfType<IProducesResponseTypeMetadata>();

    private static async Task<(int Status, string Body)> Execute(HttpResult result)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails();
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    private static async Task<TestHost> Host(Action<RouteGroupBuilder> map, Action<IServiceCollection>? configure = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddProblemDetails();
        configure?.Invoke(builder.Services);
        var app = builder.Build();
        map(app.MapGroup("/").WithResultMapping());

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

        return new TestHost(app, new HttpClient { BaseAddress = new Uri(address) });
    }

    private sealed class TestHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;

        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }
}
