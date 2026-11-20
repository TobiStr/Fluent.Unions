using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fluent.Unions.MediatR.Tests.Scenarios;

public sealed class OutOfStockException(string sku, int left) : Exception($"Only {left} of {sku} left")
{
    public int Left { get; } = left;
}

public record Reservation(string Sku, int Quantity, int Remaining);

public sealed class Warehouse
{
    private readonly Dictionary<string, int> stock = new() { ["KB-1"] = 5, ["MS-2"] = 0 };

    public Option<int> Find(string sku)
    {
        if (stock.TryGetValue(sku, out var left))
        {
            return left;
        }

        return Option.None;
    }

    public Result<Reservation> Reserve(string sku, int quantity) =>
        Find(sku)
            .ToResult(() => new KeyNotFoundException($"Unknown sku {sku}"))
            .Ensure(left => left >= quantity, left => new OutOfStockException(sku, left))
            .Select(left =>
            {
                stock[sku] = left - quantity;
                return new Reservation(sku, quantity, left - quantity);
            });
}

public sealed record ReserveStock([property: Required] string Sku, [property: System.ComponentModel.DataAnnotations.Range(1, 10)] int Quantity) : IRequest<Result<Reservation>>;

public sealed class ReserveStockHandler(IServiceProvider services) : IRequestHandler<ReserveStock, Result<Reservation>>
{
    public Task<Result<Reservation>> Handle(ReserveStock request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return request.Sku == "FLAKY"
            ? throw new TimeoutException("warehouse offline")
            : Task.FromResult(services.GetRequiredService<Warehouse>().Reserve(request.Sku, request.Quantity));
    }
}

public record UnknownSku(string Sku);

public record StockUnavailable(string Reason);

public union StockProblem(UnknownSku, StockUnavailable) : IFromException<StockProblem>
{
    public static StockProblem FromException(Exception exception) => new StockUnavailable(exception.Message);
}

public sealed record CheckStock(string Sku) : IRequest<Result<int, StockProblem>>;

public sealed class CheckStockHandler(IServiceProvider services) : IRequestHandler<CheckStock, Result<int, StockProblem>>
{
    public Task<Result<int, StockProblem>> Handle(CheckStock request, CancellationToken cancellationToken)
    {
        var warehouse = services.GetRequiredService<Warehouse>();
        Result<int, StockProblem> result;

        if (request.Sku == "FLAKY")
        {
            result = (StockProblem)new StockUnavailable("warehouse offline");
        }
        else if (warehouse.Find(request.Sku).TryGetValue(out var left))
        {
            result = left;
        }
        else
        {
            result = (StockProblem)new UnknownSku(request.Sku);
        }

        return Task.FromResult(result);
    }
}

/// <summary>A command pipeline where handlers return Result, and the behaviors turn validation failures and thrown exceptions into error values.</summary>
public class StockReservationScenario
{
    private ServiceProvider provider = null!;
    private Warehouse warehouse = null!;

    private IMediator Mediator => provider.GetRequiredService<IMediator>();

    [SetUp]
    public void SetUp()
    {
        warehouse = new Warehouse();
        var services = new ServiceCollection();
        services.AddSingleton(warehouse);
        services.AddLogging();
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<Warehouse>();
            configuration.AddOpenBehavior(typeof(ExceptionToResultBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationToResultBehavior<,>));
        });
        provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => provider.Dispose();

    [Test]
    public async Task A_valid_command_reserves_stock()
    {
        var result = await Mediator.Send(new ReserveStock("KB-1", 2));

        Assert.That(result.GetValue(), Is.EqualTo(new Reservation("KB-1", 2, 3)));
        Assert.That(warehouse.Find("KB-1").GetValue(), Is.EqualTo(3));
    }

    [Test]
    public async Task An_invalid_command_returns_every_validation_error_and_skips_the_handler()
    {
        var result = await Mediator.Send(new ReserveStock("", 0));

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.ToDictionary().Keys, Is.EquivalentTo(new[] { "Sku", "Quantity" }));
        Assert.That(warehouse.Find("KB-1").GetValue(), Is.EqualTo(5));
    }

    [Test]
    public async Task Business_failures_stay_typed_errors()
    {
        var outOfStock = await Mediator.Send(new ReserveStock("KB-1", 6));
        var unknown = await Mediator.Send(new ReserveStock("NOPE", 1));

        Assert.That(((OutOfStockException)outOfStock.GetError()).Left, Is.EqualTo(5));
        Assert.That(unknown.GetError(), Is.TypeOf<KeyNotFoundException>());
        Assert.That(warehouse.Find("KB-1").GetValue(), Is.EqualTo(5));
    }

    [Test]
    public async Task An_exception_thrown_by_the_handler_is_returned_as_an_error()
    {
        var result = await Mediator.Send(new ReserveStock("FLAKY", 1));

        Assert.That(result.GetError(), Is.TypeOf<TimeoutException>().With.Message.EqualTo("warehouse offline"));
    }

    [Test]
    public void Cancellation_is_never_turned_into_an_error()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(async () => await Mediator.Send(new ReserveStock("KB-1", 1), cancellation.Token));
    }

    [Test]
    public async Task Errors_as_data_are_matched_exhaustively()
    {
        var known = await Mediator.Send(new CheckStock("KB-1"));
        var unknown = await Mediator.Send(new CheckStock("NOPE"));
        var offline = await Mediator.Send(new CheckStock("FLAKY"));

        string Describe(Result<int, StockProblem> result) => result.Match(
            left => $"{left} left",
            problem => problem switch
            {
                UnknownSku missing => $"unknown {missing.Sku}",
                StockUnavailable unavailable => $"unavailable: {unavailable.Reason}"
            });

        Assert.That(Describe(known), Is.EqualTo("5 left"));
        Assert.That(Describe(unknown), Is.EqualTo("unknown NOPE"));
        Assert.That(Describe(offline), Is.EqualTo("unavailable: warehouse offline"));
    }
}
