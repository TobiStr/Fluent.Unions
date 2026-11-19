using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Fluent.Unions.Abstractions;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Fluent.Unions.AspNetCore.Tests;

public union OrderResult<T>(T, OrderError) : IResult<OrderResult<T>, T, OrderError>
{
    public static OrderResult<T> FromValue(T value) => value;

    public static OrderResult<T> FromError(OrderError error) => error;

    public static OrderError FromException(Exception exception) => new Unexpected(exception);
}

public union FailureResult<T>(T, Exception) : IResult<FailureResult<T>, T, Exception>
{
    public static FailureResult<T> FromValue(T value) => value;

    public static FailureResult<T> FromError(Exception error) => error;

    public static Exception FromException(Exception exception) => exception;
}

public class CustomResultHttpTests
{
    private static OrderResult<int> Order(int id) => id;

    private static OrderResult<int> OutOfStockFor(string item) => new OrderError(new OutOfStock(item));

    private static FailureResult<int> Failure(Exception error) => error;

    [Test]
    public async Task ToHttpResultGivesOkForAValue()
    {
        var (status, body) = await Execute(Order(7).ToHttpResult(_ => TypedResults.StatusCode(500)));

        Assert.That(status, Is.EqualTo(200));
        Assert.That(body, Is.EqualTo("7"));
    }

    [Test]
    public async Task ToHttpResultUsesTheGivenMappingForADataError()
    {
        var (status, _) = await Execute(OutOfStockFor("Tea").ToHttpResult(error => error switch
        {
            OutOfStock o => TypedResults.Conflict(o.Item),
            Unexpected => TypedResults.StatusCode(500)
        }));

        Assert.That(status, Is.EqualTo(409));
    }

    [Test]
    public async Task ToHttpResultGivesNotFoundForEmptyAndDoesNotCallTheMapping()
    {
        var calls = 0;

        var (status, _) = await Execute(default(OrderResult<int>).ToHttpResult(_ =>
        {
            calls++;
            return TypedResults.StatusCode(500);
        }));

        Assert.That(status, Is.EqualTo(404));
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public async Task ToHttpResultWorksForAnExceptionError()
    {
        var (status, _) = await Execute(Failure(new KeyNotFoundException("No order")).ToHttpResult(error => TypedResults.Problem(error.Message, statusCode: 410)));

        Assert.That(status, Is.EqualTo(410));
    }

    [Test]
    public async Task ToHttpResultKeepsTheConcreteResultOfTErrorOverload()
    {
        Result<int, OrderError> outOfStock = (OrderError)new OutOfStock("Tea");

        var (status, _) = await Execute(outOfStock.ToHttpResult(_ => TypedResults.StatusCode(418)));

        Assert.That(status, Is.EqualTo(418));
    }

    [Test]
    public async Task FilterGivesOkForAValue()
    {
        await using var host = await Host(api => api.MapGet("/value", () => Order(7)));

        var response = await host.Client.GetAsync("/value");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("7"));
    }

    [Test]
    public async Task FilterGivesNotFoundForEmpty()
    {
        await using var host = await Host(api => api.MapGet("/empty", () => default(OrderResult<int>)));

        var response = await host.Client.GetAsync("/empty");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task FilterGivesServerErrorForADataErrorByDefaultAndHidesIt()
    {
        await using var host = await Host(api => api.MapGet("/data", () => OutOfStockFor("Secret tea")));

        var response = await host.Client.GetAsync("/data");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("Secret tea"));
    }

    [Test]
    public async Task FilterMapsADataErrorThroughResultErrorException()
    {
        await using var host = await Host(
            api => api.MapGet("/data", () => OutOfStockFor("Tea")),
            services => services.Configure<ResultHttpOptions>(o => o.Map<ResultErrorException>(StatusCodes.Status409Conflict)));

        var response = await host.Client.GetAsync("/data");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("detail").GetString(),
            Does.StartWith("The result failed with"));
    }

    [Test]
    public async Task FilterUsesTheOptionsMappingForAnExceptionError()
    {
        await using var host = await Host(
            api =>
            {
                api.MapGet("/argument", () => Failure(new ArgumentException("Bad id")));
                api.MapGet("/conflict", () => Failure(new InvalidOperationException("Already shipped")));
                api.MapGet("/unmapped", () => Failure(new NotSupportedException("Secret")));
            },
            services => services.Configure<ResultHttpOptions>(o => o.Map<InvalidOperationException>(StatusCodes.Status409Conflict)));

        var argument = await host.Client.GetAsync("/argument");
        var conflict = await host.Client.GetAsync("/conflict");
        var unmapped = await host.Client.GetAsync("/unmapped");

        Assert.That(argument.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(
            JsonDocument.Parse(await argument.Content.ReadAsStringAsync()).RootElement.GetProperty("detail").GetString(),
            Is.EqualTo("Bad id"));
        Assert.That(conflict.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(unmapped.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(await unmapped.Content.ReadAsStringAsync(), Does.Not.Contain("Secret"));
    }

    [Test]
    public async Task FilterAlsoMapsResultOfTError()
    {
        await using var host = await Host(api =>
        {
            api.MapGet("/value", () => (Result<int, OrderError>)5);
            api.MapGet("/data", () => (Result<int, OrderError>)(OrderError)new OutOfStock("Tea"));
            api.MapGet("/empty", () => default(Result<int, OrderError>));
        });

        var value = await host.Client.GetAsync("/value");
        var data = await host.Client.GetAsync("/data");
        var empty = await host.Client.GetAsync("/empty");

        Assert.That(value.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await value.Content.ReadAsStringAsync(), Is.EqualTo("5"));
        Assert.That(data.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task FilterAwaitsAsyncHandlersAndLeavesOtherValuesAlone()
    {
        await using var host = await Host(api =>
        {
            api.MapGet("/async", async () =>
            {
                await Task.Yield();
                return Order(3);
            });
            api.MapGet("/plain", () => "untouched");
            api.MapGet("/http", () => (HttpResult)TypedResults.Accepted("/x"));
        });

        var async = await host.Client.GetAsync("/async");
        var plain = await host.Client.GetStringAsync("/plain");
        var http = await host.Client.GetAsync("/http");

        Assert.That(await async.Content.ReadAsStringAsync(), Is.EqualTo("3"));
        Assert.That(plain, Is.EqualTo("untouched"));
        Assert.That(http.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
    }

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
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
