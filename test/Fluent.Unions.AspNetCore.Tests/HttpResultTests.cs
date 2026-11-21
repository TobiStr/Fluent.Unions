using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.AspNetCore.Tests;

public record OutOfStock(string Item);

public record Unexpected(Exception Exception);

public union OrderError(OutOfStock, Unexpected) : IFromException<OrderError>
{
    public static OrderError FromException(Exception exception) => new Unexpected(exception);
}

[FluentResult<OrderError>]
public readonly partial struct OrderResult<T>;

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
    public async Task DefaultResultGivesServerErrorNoneGivesNotFoundAndPlainOkGivesNoContent()
    {
        Assert.That((await Execute(default(Result<int>).ToHttpResult())).Status, Is.EqualTo(500));
        Assert.That((await Execute(((Option<int>)Option.None).ToHttpResult())).Status, Is.EqualTo(404));
        Assert.That((await Execute(((Option<string>)"Ada").ToHttpResult())).Body, Is.EqualTo("\"Ada\""));
        Assert.That((await Execute(Result.Ok.ToHttpResult())).Status, Is.EqualTo(204));
    }

    [Test]
    public async Task KeyNotFoundGivesNotFoundWithoutTheMessage()
    {
        var (status, body) = await Execute(((Result<int>)new KeyNotFoundException("No order 7")).ToHttpResult());

        Assert.That(status, Is.EqualTo(404));
        Assert.That(body, Does.Not.Contain("No order 7"));
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
            n => n.Length > 2 ? Result.Ok : new ValidationError("Too short", "length"),
            n => char.IsUpper(n[0]) ? Result.Ok : new ValidationError("Not upper case", "casing"),
            n => n.Length > 3 ? Result.Ok : new ValidationError("Way too short", "length"),
            n => n != "x" ? Result.Ok : new ValidationError("Reserved"));

        var (status, body) = await Execute(invalid.ToHttpResult());

        Assert.That(status, Is.EqualTo(400));
        var errors = Problem(body).GetProperty("errors");
        Assert.That(errors.GetProperty("length").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "Too short", "Way too short" }));
        Assert.That(errors.GetProperty("casing").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "Not upper case" }));
        Assert.That(errors.GetProperty("").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "Reserved" }));
    }

    [Test]
    public async Task SingleValidationErrorGivesBadRequestGroupedByMember()
    {
        Result<int> result = Result<int>.Ok(2).Ensure(value => value > 3, "Too small.");

        var (status, body) = await Execute(result.ToHttpResult());

        Assert.That(status, Is.EqualTo(400));
        Assert.That(Problem(body).GetProperty("errors").GetProperty("").EnumerateArray().Single().GetString(), Is.EqualTo("Too small."));
    }

    [Test]
    public async Task ArgumentExceptionGivesServerErrorWithoutDetail()
    {
        var (status, body) = await Execute(((Result<int>)new ArgumentException("Bad id")).ToHttpResult());

        Assert.That(status, Is.EqualTo(500));
        Assert.That(body, Does.Not.Contain("Bad id"));
    }

    [Test]
    public async Task AggregateExceptionGivesServerError()
    {
        var aggregate = new AggregateException(new InvalidOperationException("Inner secret"));

        var (status, body) = await Execute(((Result<int>)aggregate).ToHttpResult());

        Assert.That(status, Is.EqualTo(500));
        Assert.That(body, Does.Not.Contain("Inner secret"));
    }

    [Test]
    public async Task UnauthorizedAndTimeoutUseTheirDefaultStatusCodes()
    {
        Assert.That((await Execute(((Result<int>)new UnauthorizedAccessException("No")).ToHttpResult())).Status, Is.EqualTo(403));
        Assert.That((await Execute(((Result<int>)new TimeoutException("Slow")).ToHttpResult())).Status, Is.EqualTo(504));
    }

    [Test]
    public async Task StatusCodesAreConfigurableAndHideTheMessageByDefault()
    {
        var (status, body) = await Execute(
            ((Result<int>)new InvalidOperationException("Already shipped")).ToHttpResult(),
            services => services.Configure<ResultHttpOptions>(o => o.Map<InvalidOperationException>(409)));

        Assert.That(status, Is.EqualTo(409));
        Assert.That(body, Does.Not.Contain("Already shipped"));
    }

    [Test]
    public async Task MappingCanExposeTheMessage()
    {
        var (status, body) = await Execute(
            ((Result<int>)new InvalidOperationException("Already shipped")).ToHttpResult(),
            services => services.Configure<ResultHttpOptions>(o => o.Map<InvalidOperationException>(409, exposeMessage: true)));

        Assert.That(status, Is.EqualTo(409));
        Assert.That(Problem(body).GetProperty("detail").GetString(), Is.EqualTo("Already shipped"));
    }

    [Test]
    public async Task ErrorsAsDataUseTheGivenMapping()
    {
        OrderResult<int> outOfStock = (OrderError)new OutOfStock("Tea");

        var (status, _) = await Execute(outOfStock.ToHttpResult(error => error switch
        {
            OutOfStock o => TypedResults.Conflict(o.Item),
            Unexpected => TypedResults.StatusCode(500)
        }));

        Assert.That(status, Is.EqualTo(409));
    }

    [Test]
    public async Task TaskOverloadsConvertResultsAndLetFaultsPropagate()
    {
        Assert.That((await Execute(await Task.FromResult<Result<int>>(5).ToHttpResult())).Status, Is.EqualTo(200));
        Assert.That((await Execute(await Task.FromResult<Option<int>>(Option.None).ToHttpResult())).Status, Is.EqualTo(404));
        Assert.That((await Execute(await Task.FromResult(Result.Ok).ToHttpResult())).Status, Is.EqualTo(204));

        Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromException<Result<int>>(new InvalidOperationException("Boom")).ToHttpResult());
        Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromException<Result>(new InvalidOperationException("Boom")).ToHttpResult());
        Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromException<Option<int>>(new InvalidOperationException("Boom")).ToHttpResult());
    }

    [Test]
    public async Task ServerErrorResponsesAreLoggedWithTheException()
    {
        var logs = new CapturingLoggerProvider();
        var error = new InvalidOperationException("Connection string secret");

        var (_, body) = await Execute(((Result<int>)error).ToHttpResult(), services => services.AddLogging(b => b.AddProvider(logs)));

        var entry = logs.Entries.Single(e => e.Category == "Fluent.Unions.AspNetCore");
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(entry.Exception, Is.SameAs(error));
        Assert.That(entry.Category, Is.EqualTo("Fluent.Unions.AspNetCore"));
        Assert.That(body, Does.Not.Contain("secret"));
    }

    [Test]
    public async Task ClientErrorResponsesAreNotLogged()
    {
        var logs = new CapturingLoggerProvider();

        await Execute(((Result<int>)new KeyNotFoundException("No order 7")).ToHttpResult(), services => services.AddLogging(b => b.AddProvider(logs)));
        await Execute(((Result<int>)new ValidationError("Too short", "Name")).ToHttpResult(), services => services.AddLogging(b => b.AddProvider(logs)));

        Assert.That(logs.Entries.Where(e => e.Category == "Fluent.Unions.AspNetCore"), Is.Empty);
    }

    [Test]
    public async Task ValidationErrorsInsideAnotherErrorStayAValidationProblem()
    {
        var wrapped = new InvalidOperationException("Wrapper message", new ValidationError("Too short", "Name"));
        var wrappedMany = new InvalidOperationException("Wrapper message", new ValidationErrors([new ValidationError("Too short", "Name"), new ValidationError("Required", "Age")]));

        var single = await Execute(((Result<int>)wrapped).ToHttpResult());
        var many = await Execute(((Result<int>)wrappedMany).ToHttpResult());

        Assert.That(single.Status, Is.EqualTo(400));
        Assert.That(Problem(single.Body).GetProperty("errors").GetProperty("Name").EnumerateArray().Single().GetString(), Is.EqualTo("Too short"));
        Assert.That(single.Body, Does.Not.Contain("Wrapper message"));
        Assert.That(many.Status, Is.EqualTo(400));
        Assert.That(Problem(many.Body).GetProperty("errors").EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "Name", "Age" }));
    }

    [Test]
    public async Task AnAggregateExceptionAroundValidationErrorsIsNotAValidationProblem()
    {
        var aggregate = new AggregateException(new ValidationError("Too short", "Name"), new InvalidOperationException("Inner secret"));

        var (status, body) = await Execute(((Result<int>)aggregate).ToHttpResult());

        Assert.That(status, Is.EqualTo(500));
        Assert.That(body, Does.Not.Contain("Inner secret"));
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
        Assert.That(await missing.Content.ReadAsStringAsync(), Does.Not.Contain("No order 0"));
        Assert.That(await asyncFound.Content.ReadAsStringAsync(), Is.EqualTo("\"order 3\""));
        Assert.That(asyncMissing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(plain, Is.EqualTo("untouched"));
    }

    [Test]
    public async Task EndpointReturningADefaultResultGivesServerError()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.MapGroup("/orders").WithResultMapping().MapGet("/default", () => default(Result<int>));

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        var response = await client.GetAsync("/orders/default");
        await app.StopAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task WithResultMappingDescribesTheResponsesOfTheHandlerReturnType()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddEndpointsApiExplorer();
        await using var app = builder.Build();
        var api = app.MapGroup("/orders").WithResultMapping();
        api.MapGet("/value", () => (Result<int>)1);
        api.MapGet("/save", () => Result.Ok);
        api.MapGet("/find", () => (Option<string>)"Ada");

        await app.StartAsync();
        var responses = ResponsesByPath(app);
        await app.StopAsync();

        Assert.That(responses.Keys, Is.EquivalentTo(new[] { "orders/value", "orders/save", "orders/find" }));
        var value = responses["orders/value"];
        Assert.That(value.Select(r => r.StatusCode).Distinct(), Is.EquivalentTo(new[] { 200, 400, 500 }));
        Assert.That(value.First(r => r.StatusCode == 200).Type, Is.EqualTo(typeof(int)));
        Assert.That(value.Single(r => r.StatusCode == 400).Type, Is.EqualTo(typeof(HttpValidationProblemDetails)));
        Assert.That(value.Single(r => r.StatusCode == 500).Type, Is.EqualTo(typeof(ProblemDetails)));
        Assert.That(responses["orders/save"].Select(r => r.StatusCode), Does.Contain(204));
        Assert.That(responses["orders/find"].Select(r => r.StatusCode), Does.Contain(404));
    }

    private static Dictionary<string, IList<ApiResponseType>> ResponsesByPath(WebApplication app) =>
        app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .ToDictionary(description => description.RelativePath!, description => description.SupportedResponseTypes);

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

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<(string Category, LogLevel Level, Exception? Exception)> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, List<(string, LogLevel, Exception?)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Add((category, logLevel, exception));
    }
}
