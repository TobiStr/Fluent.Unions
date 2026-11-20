using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.AspNetCore.Tests.Scenarios;

public record PlaceOrder(string Customer, int Quantity);

public record Order(int Id, string Customer, int Quantity, string Status);

public record PaymentDeclined(string Reason);

public record PaymentUnavailable(Exception Exception);

public union PaymentError(PaymentDeclined, PaymentUnavailable) : IFromException<PaymentError>
{
    public static PaymentError FromException(Exception exception) => new PaymentUnavailable(exception);
}

public sealed class OrderShippedException(int id) : Exception($"Order {id} has shipped.");

public sealed class DuplicateOrderException(string customer) : Exception($"{customer} already has an open order.");

public sealed class OrderBook
{
    private readonly Dictionary<int, Order> orders = [];

    public Result<Order> Place(PlaceOrder command) =>
        Result<PlaceOrder>.Ok(command)
            .Validate(c => c.Customer.EnsureNotEmpty(), c => c.Quantity.Ensure(q => q > 0, "Quantity must be positive."))
            .Ensure(c => orders.Values.All(o => o.Customer != c.Customer), c => new DuplicateOrderException(c.Customer))
            .Select(c => Save(new Order(orders.Count + 1, c.Customer, c.Quantity, "Open")));

    public Option<Order> Find(int id)
    {
        if (orders.TryGetValue(id, out var order))
        {
            return order;
        }

        return Option.None;
    }

    public Result Cancel(int id)
    {
        Result<Order> loaded = Find(id)
            .ToResult(() => new KeyNotFoundException($"No order {id}"))
            .Ensure(o => o.Status != "Shipped", o => new OrderShippedException(o.Id));

        return loaded.Do(o => orders.Remove(o.Id));
    }

    public Result<Order, PaymentError> Pay(Order order, string card)
    {
        if (card == "declined")
        {
            PaymentError declined = new PaymentDeclined("The card was declined.");
            return declined;
        }

        if (card == "offline")
        {
            return PaymentError.FromException(new TimeoutException("gateway down"));
        }

        return Save(order with { Status = "Paid" });
    }

    public void Ship(int id) => Save(orders[id] with { Status = "Shipped" });

    private Order Save(Order order) => orders[order.Id] = order;
}

/// <summary>An order API where Result, Option and an error union decide the HTTP status, so handlers hold no status codes or try/catch.</summary>
public class OrderApiScenario
{
    private OrderBook book = null!;
    private WebApplication app = null!;
    private HttpClient client = null!;

    [SetUp]
    public async Task SetUp()
    {
        book = new OrderBook();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(book).AddEndpointsApiExplorer().AddProblemDetails();
        builder.Services.Configure<ResultHttpOptions>(o => o
            .Map<OrderShippedException>(StatusCodes.Status409Conflict, exposeMessage: true)
            .Map<DuplicateOrderException>(StatusCodes.Status409Conflict, exposeMessage: true));
        app = builder.Build();

        var orders = app.MapGroup("/orders").WithResultMapping();
        orders.MapPost("/", (PlaceOrder command, OrderBook orderBook) => orderBook.Place(command));
        orders.MapGet("/{id:int}", (int id, OrderBook orderBook) => orderBook.Find(id));
        orders.MapDelete("/{id:int}", (int id, OrderBook orderBook) => orderBook.Cancel(id));
        orders.MapPost("/{id:int}/pay", IResult (int id, string card, OrderBook orderBook) =>
            orderBook.Find(id).Match<Order, IResult>(
                order => orderBook.Pay(order, card).ToHttpResult(error => error switch
                {
                    PaymentDeclined declined => TypedResults.Problem(declined.Reason, statusCode: StatusCodes.Status402PaymentRequired),
                    PaymentUnavailable => TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable)
                }),
                () => TypedResults.NotFound()));

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [TearDown]
    public async Task TearDown()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    [Test]
    public async Task Placing_an_order_returns_the_value_and_invalid_input_returns_every_problem()
    {
        var placed = await client.PostAsJsonAsync("/orders", new PlaceOrder("Ada", 2));
        var invalid = await client.PostAsJsonAsync("/orders", new PlaceOrder("", 0));

        Assert.That(placed.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await placed.Content.ReadFromJsonAsync<Order>())!.Status, Is.EqualTo("Open"));
        Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var errors = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        Assert.That(errors.EnumerateObject().Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task A_duplicate_order_is_a_conflict_with_a_safe_message()
    {
        await client.PostAsJsonAsync("/orders", new PlaceOrder("Ada", 2));

        var duplicate = await client.PostAsJsonAsync("/orders", new PlaceOrder("Ada", 1));

        Assert.That(duplicate.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await duplicate.Content.ReadAsStringAsync(), Does.Contain("Ada already has an open order."));
    }

    [Test]
    public async Task A_missing_option_is_a_404_and_a_found_one_is_a_200()
    {
        await client.PostAsJsonAsync("/orders", new PlaceOrder("Ada", 2));

        Assert.That((await client.GetAsync("/orders/1")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await client.GetAsync("/orders/99")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Cancelling_maps_not_found_shipped_and_success_to_different_statuses()
    {
        await client.PostAsJsonAsync("/orders", new PlaceOrder("Ada", 2));
        await client.PostAsJsonAsync("/orders", new PlaceOrder("Bob", 1));
        book.Ship(2);

        var unknown = await client.DeleteAsync("/orders/99");
        var shipped = await client.DeleteAsync("/orders/2");
        var cancelled = await client.DeleteAsync("/orders/1");

        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await unknown.Content.ReadAsStringAsync(), Does.Not.Contain("No order 99"));
        Assert.That(shipped.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await shipped.Content.ReadAsStringAsync(), Does.Contain("Order 2 has shipped."));
        Assert.That(cancelled.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(book.Find(1).HasValue, Is.False);
    }

    [Test]
    public async Task Payment_errors_are_data_and_each_case_chooses_its_own_response()
    {
        await client.PostAsJsonAsync("/orders", new PlaceOrder("Ada", 2));

        var paid = await client.PostAsync("/orders/1/pay?card=visa", null);
        var declined = await client.PostAsync("/orders/1/pay?card=declined", null);
        var offline = await client.PostAsync("/orders/1/pay?card=offline", null);
        var unknown = await client.PostAsync("/orders/9/pay?card=visa", null);

        Assert.That((await paid.Content.ReadFromJsonAsync<Order>())!.Status, Is.EqualTo("Paid"));
        Assert.That(declined.StatusCode, Is.EqualTo(HttpStatusCode.PaymentRequired));
        Assert.That(offline.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public void The_endpoint_metadata_documents_the_response_types_of_the_handler()
    {
        var responses = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .ToDictionary(d => $"{d.HttpMethod} {d.RelativePath!.TrimEnd('/')}", d => d.SupportedResponseTypes.Select(r => r.StatusCode).Distinct().ToArray());

        Assert.That(responses["POST orders"], Is.EquivalentTo(new[] { 200, 400, 500 }));
        Assert.That(responses["DELETE orders/{id:int}"], Does.Contain(204));
        Assert.That(responses["GET orders/{id:int}"], Does.Contain(404));
    }
}
