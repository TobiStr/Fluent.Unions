using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ResultType.Abstractions;
using ResultType.Extensions;

namespace ResultType.AspNetCore.Tests;

public record OutOfStock(string Item);

public record Unexpected(Exception Exception);

public union OrderError(OutOfStock, Unexpected) : IFromException<OrderError>
{
    public static OrderError FromException(Exception exception) => new Unexpected(exception);
}

public class HttpResultTests
{
    [Test]
    public async Task ValueGivesOk()
    {
        var (status, body) = await Execute(((Result<int>)42).ToHttpResult());

        Assert.That(status, Is.EqualTo(200));
        Assert.That(body, Is.EqualTo("42"));
    }

    [Test]
    public async Task EmptyAndNoneGiveNotFoundAndPlainOkGivesNoContent()
    {
        Assert.That((await Execute(default(Result<int>).ToHttpResult())).Status, Is.EqualTo(404));
        Assert.That((await Execute(((Option<int>)Option.None).ToHttpResult())).Status, Is.EqualTo(404));
        Assert.That((await Execute(((Option<string>)"Ada").ToHttpResult())).Body, Is.EqualTo("\"Ada\""));
        Assert.That((await Execute(Result.Ok.ToHttpResult())).Status, Is.EqualTo(204));
    }

    [Test]
    public async Task ClientErrorsExposeTheirMessage()
    {
        var (status, body) = await Execute(((Result<int>)new KeyNotFoundException("No order 7")).ToHttpResult());

        Assert.That(status, Is.EqualTo(404));
        Assert.That(Problem(body).GetProperty("detail").GetString(), Is.EqualTo("No order 7"));
    }

    [Test]
    public async Task ServerErrorsHideTheirMessage()
    {
        var (status, body) = await Execute(((Result<int>)new InvalidOperationException("Connection string secret")).ToHttpResult());

        Assert.That(status, Is.EqualTo(500));
        Assert.That(body, Does.Not.Contain("secret"));
    }

    [Test]
    public async Task ValidationErrorsAreListed()
    {
        Result<string> name = "x";
        var invalid = name.Validate(
            n => n.Length > 2 ? Result.Ok : new ArgumentException("Too short"),
            n => char.IsUpper(n[0]) ? Result.Ok : new ArgumentException("Not upper case"));

        var (status, body) = await Execute(invalid.ToHttpResult());

        Assert.That(status, Is.EqualTo(400));
        var errors = Problem(body).GetProperty("errors").EnumerateArray().Select(e => e.GetString());
        Assert.That(errors, Is.EqualTo(new[] { "Too short", "Not upper case" }));
    }

    [Test]
    public async Task StatusCodesAreConfigurable()
    {
        var (status, body) = await Execute(
            ((Result<int>)new InvalidOperationException("Already shipped")).ToHttpResult(),
            services => services.Configure<ResultHttpOptions>(o => o.Map<InvalidOperationException>(409)));

        Assert.That(status, Is.EqualTo(409));
        Assert.That(Problem(body).GetProperty("detail").GetString(), Is.EqualTo("Already shipped"));
    }

    [Test]
    public async Task ErrorsAsDataUseTheGivenMapping()
    {
        Result<int, OrderError> outOfStock = (OrderError)new OutOfStock("Tea");

        var (status, _) = await Execute(outOfStock.ToHttpResult(error => error switch
        {
            OutOfStock o => TypedResults.Conflict(o.Item),
            Unexpected => TypedResults.StatusCode(500)
        }));

        Assert.That(status, Is.EqualTo(409));
    }

    [Test]
    public async Task TaskOverloadsTurnFaultsIntoServerErrors()
    {
        Assert.That((await Execute(await Task.FromResult<Result<int>>(5).ToHttpResult())).Status, Is.EqualTo(200));
        Assert.That((await Execute(await Task.FromException<Result<int>>(new Exception("Boom")).ToHttpResult())).Status, Is.EqualTo(500));
        Assert.That((await Execute(await Task.FromResult<Option<int>>(Option.None).ToHttpResult())).Status, Is.EqualTo(404));
        Assert.That((await Execute(await Task.FromResult(Result.Ok).ToHttpResult())).Status, Is.EqualTo(204));
    }

    [Test]
    public async Task EndpointsCanReturnResultsDirectly()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();

        var api = app.MapGroup("/orders").WithResultMapping();
        api.MapGet("/{id:int}", (int id) => id > 0 ? (Result<int>)id : new KeyNotFoundException($"No order {id}"));
        api.MapGet("/async/{id:int}", async (int id) =>
        {
            await Task.Yield();
            return id > 0 ? (Option<string>)$"order {id}" : Option.None;
        });
        api.MapGet("/save", () => Result.Ok);
        api.MapGet("/plain", () => "untouched");

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        var found = await client.GetAsync("/orders/7");
        var missing = await client.GetAsync("/orders/0");
        var asyncFound = await client.GetAsync("/orders/async/3");
        var asyncMissing = await client.GetAsync("/orders/async/0");
        var saved = await client.GetAsync("/orders/save");
        var plain = await client.GetStringAsync("/orders/plain");
        await app.StopAsync();

        Assert.That(found.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await found.Content.ReadAsStringAsync(), Is.EqualTo("7"));
        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Problem(await missing.Content.ReadAsStringAsync()).GetProperty("detail").GetString(), Is.EqualTo("No order 0"));
        Assert.That(await asyncFound.Content.ReadAsStringAsync(), Is.EqualTo("\"order 3\""));
        Assert.That(asyncMissing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(plain, Is.EqualTo("untouched"));
    }

    private static async Task<(int Status, string Body)> Execute(IResult result, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails();
        configure?.Invoke(services);
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    private static JsonElement Problem(string body) => JsonDocument.Parse(body).RootElement;
}
