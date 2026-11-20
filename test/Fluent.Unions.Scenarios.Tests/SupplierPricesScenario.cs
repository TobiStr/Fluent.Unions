namespace Fluent.Unions.Scenarios.Tests.SupplierPrices;

public record Quote(string Supplier, string Sku, decimal Price, bool Stale = false);

public class SupplierUnavailableException(string supplier) : Exception($"{supplier} is unavailable.");

public interface ISupplier
{
    string Name { get; }

    Task<Quote> GetQuoteAsync(string sku, CancellationToken cancellationToken);
}

/// <summary>
/// Asks several suppliers for a price. Shows explicit async failure: a slow, flaky or dead supplier is an error
/// value after a retry and a timeout, a cached quote can replace it, and the other suppliers still count.
/// </summary>
public sealed class PriceAggregator(
    IReadOnlyList<ISupplier> suppliers,
    IReadOnlyDictionary<string, Quote> lastKnown,
    TimeSpan timeout)
{
    public Task<Result<Quote>> QuoteAsync(ISupplier supplier, string sku, CancellationToken cancellationToken = default) =>
        Result.RetryAsync(
                token => Result.TryAsync(() => supplier.GetQuoteAsync(sku, token)).Timeout(timeout),
                maxAttempts: 3,
                delay: TimeSpan.FromMilliseconds(1),
                shouldRetry: error => error is TimeoutException or IOException,
                cancellationToken: cancellationToken)
            .Catch(error => lastKnown
                .GetValueOrNone($"{supplier.Name}/{sku}")
                .Select(cached => cached with { Stale = true })
                .ToResult(() => error));

    public async Task<Result<Quote>> BestQuoteAsync(string sku, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(suppliers.Select(supplier => QuoteAsync(supplier, sku, cancellationToken)));
        var (quotes, failures) = results.Partition();

        return quotes
            .OrderBy(quote => quote.Stale)
            .ThenBy(quote => quote.Price)
            .FirstOrNone()
            .ToResult(() => new AggregateException($"No supplier could quote {sku}.", failures));
    }

    public Task<Result<IReadOnlyList<Quote>>> AllQuotesAsync(string sku, CancellationToken cancellationToken = default) =>
        Result.WhenAll(suppliers.Select(supplier => QuoteAsync(supplier, sku, cancellationToken)));
}

public class SupplierPricesScenarioTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(10);

    private static readonly Dictionary<string, Quote> LastKnown = new()
    {
        ["Dead/TEA"] = new Quote("Dead", "TEA", 1m),
        ["Hung/TEA"] = new Quote("Hung", "TEA", 2m)
    };

    private static PriceAggregator Aggregator(params ISupplier[] suppliers) => new(suppliers, LastKnown, Timeout);

    [Test]
    public async Task TheCheapestLiveQuoteWins()
    {
        var aggregator = Aggregator(new Supplier("North", (_, _) => Quote("North", 4.5m)), new Supplier("South", (_, _) => Quote("South", 4.2m)));

        var best = await aggregator.BestQuoteAsync("TEA");

        Assert.That(best.GetValue(), Is.EqualTo(new Quote("South", "TEA", 4.2m)));
    }

    [Test]
    public async Task AFlakySupplierIsRetriedUntilItAnswers()
    {
        var flaky = new Supplier("Flaky", (attempt, _) => attempt < 3 ? throw new IOException("reset") : Quote("Flaky", 3m));

        var quote = await Aggregator(flaky).QuoteAsync(flaky, "TEA");

        Assert.That(quote.GetValue().Price, Is.EqualTo(3m));
        Assert.That(flaky.Calls, Is.EqualTo(3));
    }

    [Test]
    public async Task ADeadSupplierIsNotRetriedAndFallsBackToTheCachedQuote()
    {
        var dead = new Supplier("Dead", (_, _) => throw new SupplierUnavailableException("Dead"));

        var quote = await Aggregator(dead).QuoteAsync(dead, "TEA");

        Assert.That(quote.GetValue(), Is.EqualTo(new Quote("Dead", "TEA", 1m, Stale: true)));
        Assert.That(dead.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task AHungSupplierTimesOutOnEveryAttempt()
    {
        var hung = new Supplier("Unknown", (_, _) => new TaskCompletionSource<Quote>().Task);

        var quote = await Aggregator(hung).QuoteAsync(hung, "TEA");

        Assert.That(quote.GetError(), Is.TypeOf<TimeoutException>());
        Assert.That(hung.Calls, Is.EqualTo(3));
    }

    [Test]
    public async Task ALiveQuoteBeatsAStaleOneEvenWhenTheStaleOneIsCheaper()
    {
        var aggregator = Aggregator(
            new Supplier("Dead", (_, _) => throw new SupplierUnavailableException("Dead")),
            new Supplier("North", (_, _) => Quote("North", 4.5m)));

        var best = await aggregator.BestQuoteAsync("TEA");

        Assert.That(best.GetValue().Supplier, Is.EqualTo("North"));
    }

    [Test]
    public async Task WithoutAnyQuoteTheFailuresAreCollected()
    {
        var aggregator = Aggregator(
            new Supplier("Gone", (_, _) => throw new SupplierUnavailableException("Gone")),
            new Supplier("Unknown", (_, _) => throw new InvalidOperationException("bad data")));

        var best = await aggregator.BestQuoteAsync("COFFEE");

        var error = (AggregateException)best.GetError();
        Assert.That(error.Message, Does.StartWith("No supplier could quote COFFEE."));
        Assert.That(error.InnerExceptions.Select(e => e.GetType()), Is.EqualTo(new[] { typeof(SupplierUnavailableException), typeof(InvalidOperationException) }));
    }

    [Test]
    public async Task AllQuotesNeedsEverySupplierAndReportsTheFirstError()
    {
        var healthy = new Supplier("North", (_, _) => Quote("North", 4.5m));
        var broken = new Supplier("Gone", (_, _) => throw new SupplierUnavailableException("Gone"));

        var all = await Aggregator(healthy, broken).AllQuotesAsync("COFFEE");
        var covered = await Aggregator(healthy, healthy).AllQuotesAsync("COFFEE");

        Assert.That(all.GetError(), Is.TypeOf<SupplierUnavailableException>());
        Assert.That(covered.GetValue(), Has.Count.EqualTo(2));
    }

    [Test]
    public void CancellationIsNeverTurnedIntoAnError()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var supplier = new Supplier("North", (_, token) => Task.FromCanceled<Quote>(token));

        Assert.CatchAsync<OperationCanceledException>(() => Aggregator(supplier).QuoteAsync(supplier, "TEA", cancellation.Token));
    }

    private static Task<Quote> Quote(string supplier, decimal price) => Task.FromResult(new Quote(supplier, "TEA", price));

    private sealed class Supplier(string name, Func<int, CancellationToken, Task<Quote>> answer) : ISupplier
    {
        public string Name => name;

        public int Calls { get; private set; }

        public Task<Quote> GetQuoteAsync(string sku, CancellationToken cancellationToken) => answer(++Calls, cancellationToken);
    }
}
