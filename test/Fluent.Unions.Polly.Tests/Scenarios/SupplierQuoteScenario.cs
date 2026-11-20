using Fluent.Unions.Polly;
using NUnit.Framework;
using Polly;
using Polly.Fallback;
using Polly.Retry;

namespace Fluent.Unions.Polly.Tests.Scenarios;

public record Quote(string Sku, decimal Price, string Supplier);

public interface ISupplier
{
    ValueTask<Result<Quote>> QuoteAsync(string sku, CancellationToken cancellationToken);
}

public sealed class ScriptedSupplier(Func<int, string, Result<Quote>> script) : ISupplier
{
    public int Calls { get; private set; }

    public ValueTask<Result<Quote>> QuoteAsync(string sku, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;

        return ValueTask.FromResult(script(Calls, sku));
    }

    public static ScriptedSupplier Healthy(string name, decimal price) => new((_, sku) => new Quote(sku, price, name));

    public static ScriptedSupplier FlakyFor(string name, int failures, decimal price) =>
        new((call, sku) => call <= failures ? new TimeoutException($"{name} timed out") : new Quote(sku, price, name));
}

public sealed class PricingService(ISupplier primary, ISupplier backup)
{
    private static bool IsTransient(Exception error) => error is TimeoutException or HttpRequestException;

    public async Task<Result<Quote>> QuoteAsync(string sku, CancellationToken cancellationToken = default)
    {
        var pipeline = new ResiliencePipelineBuilder<Result<Quote>>()
            .AddFallback(new FallbackStrategyOptions<Result<Quote>>
            {
                ShouldHandle = new PredicateBuilder<Result<Quote>>().HandleError(IsTransient),
                FallbackAction = async args => Outcome.FromResult(await backup.QuoteAsync(sku, args.Context.CancellationToken))
            })
            .AddRetry(new RetryStrategyOptions<Result<Quote>>
            {
                ShouldHandle = new PredicateBuilder<Result<Quote>>().HandleError(IsTransient),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero
            })
            .Build();

        var quote = await pipeline.ExecuteResultAsync(token => primary.QuoteAsync(sku, token), cancellationToken);

        return quote.Ensure(q => q.Price > 0, q => new InvalidOperationException($"{q.Supplier} returned a non-positive price for {q.Sku}"));
    }
}

/// <summary>Quoting a flaky supplier where only transient errors are retried or replaced by a backup, and every other failure stays data.</summary>
public class SupplierQuoteScenario
{
    [Test]
    public async Task A_flaky_supplier_is_retried_until_it_answers()
    {
        var primary = ScriptedSupplier.FlakyFor("acme", failures: 2, price: 10m);
        var backup = ScriptedSupplier.Healthy("globex", 12m);

        var quote = await new PricingService(primary, backup).QuoteAsync("KB-1");

        Assert.That(quote.GetValue().Supplier, Is.EqualTo("acme"));
        Assert.That(primary.Calls, Is.EqualTo(3));
        Assert.That(backup.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task A_supplier_that_stays_down_is_replaced_by_the_backup()
    {
        var primary = ScriptedSupplier.FlakyFor("acme", failures: 10, price: 10m);
        var backup = ScriptedSupplier.Healthy("globex", 12m);

        var quote = await new PricingService(primary, backup).QuoteAsync("KB-1");

        Assert.That(quote.GetValue(), Is.EqualTo(new Quote("KB-1", 12m, "globex")));
        Assert.That(primary.Calls, Is.EqualTo(3));
        Assert.That(backup.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task When_both_suppliers_fail_the_last_error_is_returned_as_data()
    {
        var primary = ScriptedSupplier.FlakyFor("acme", failures: 10, price: 10m);
        var backup = ScriptedSupplier.FlakyFor("globex", failures: 10, price: 12m);

        var quote = await new PricingService(primary, backup).QuoteAsync("KB-1");

        var message = quote.Match(q => $"quoted {q.Price}", error => error.Message);
        Assert.That(message, Is.EqualTo("globex timed out"));
    }

    [Test]
    public async Task A_business_error_is_neither_retried_nor_replaced()
    {
        var primary = new ScriptedSupplier((_, sku) => new KeyNotFoundException($"Unknown sku {sku}"));
        var backup = ScriptedSupplier.Healthy("globex", 12m);

        var quote = await new PricingService(primary, backup).QuoteAsync("NOPE");

        Assert.That(quote.GetError(), Is.InstanceOf<KeyNotFoundException>());
        Assert.That(primary.Calls, Is.EqualTo(1));
        Assert.That(backup.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task Bad_supplier_data_is_rejected_after_the_pipeline()
    {
        var primary = ScriptedSupplier.Healthy("acme", 0m);

        var quote = await new PricingService(primary, ScriptedSupplier.Healthy("globex", 12m)).QuoteAsync("KB-1");

        Assert.That(quote.GetError().Message, Is.EqualTo("acme returned a non-positive price for KB-1"));
        Assert.That(primary.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Cancellation_is_thrown_and_never_turned_into_an_error()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = new PricingService(ScriptedSupplier.Healthy("acme", 10m), ScriptedSupplier.Healthy("globex", 12m));

        Assert.CatchAsync<OperationCanceledException>(async () => await service.QuoteAsync("KB-1", cancellation.Token));
    }
}
