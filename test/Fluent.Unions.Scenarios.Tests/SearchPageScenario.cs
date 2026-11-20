using Rule = Fluent.Unions.Validation.Is;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Scenarios.Tests.SearchPage;

public record Product(string Sku, string Name);

public record Page(string Query, IReadOnlyList<Product> Products);

public interface ICatalog
{
    Task<Result<Page>> SearchAsync(string query, CancellationToken cancellationToken);
}

/// <summary>
/// The view model of a search page. Shows <c>Loadable</c>: one property holds the not-loaded, loading, loaded and
/// failed states, so the screen cannot show a spinner and an error together, and every state needs a text.
/// </summary>
public sealed class SearchViewModel(ICatalog catalog)
{
    public event Action? Changed;

    public Loadable<Page> Results { get; private set; } = Loadable.NotLoaded;

    public string Status => Results.Match(
        () => "Type to search.",
        () => "Searching...",
        page => page.Products.Count == 0 ? $"Nothing found for '{page.Query}'." : $"{page.Products.Count} results for '{page.Query}'.",
        error => $"Search failed: {error.Message}");

    public Option<Product> TopResult => Results.ToOption().Chain(page => page.Products.FirstOrNone());

    public async Task SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var term = query.ToOption().Select(text => text.Trim()).Where(Rule.NotEmpty);

        if (!term.TryGetValue(out var text))
        {
            Update(Loadable.NotLoaded);
            return;
        }

        Update(Loadable.Loading);
        Update(await catalog.SearchAsync(text, cancellationToken).ToLoadable());
    }

    private void Update(Loadable<Page> state)
    {
        Results = state;
        Changed?.Invoke();
    }
}

public class SearchPageScenarioTests
{
    private static readonly Page Tea = new("tea", [new Product("T-1", "Green tea"), new Product("T-2", "Black tea")]);

    [Test]
    public async Task ASearchWalksThroughLoadingToLoaded()
    {
        var viewModel = new SearchViewModel(new StubCatalog(_ => Task.FromResult<Result<Page>>(Tea)));
        List<string> seen = [];
        viewModel.Changed += () => seen.Add(viewModel.Status);

        await viewModel.SearchAsync("  tea ");

        Assert.That(seen, Is.EqualTo(new[] { "Searching...", "2 results for 'tea'." }));
        Assert.That(viewModel.TopResult.GetValue().Sku, Is.EqualTo("T-1"));
    }

    [Test]
    public async Task TheLoadingStateIsVisibleWhileTheCatalogIsBusy()
    {
        var gate = new TaskCompletionSource<Result<Page>>();
        var viewModel = new SearchViewModel(new StubCatalog(_ => gate.Task));

        var pending = viewModel.SearchAsync("tea");

        Assert.That(viewModel.Status, Is.EqualTo("Searching..."));
        Assert.That(viewModel.Results.IsLoading, Is.True);
        gate.SetResult(Tea);
        await pending;
        Assert.That(viewModel.Status, Is.EqualTo("2 results for 'tea'."));
    }

    [Test]
    public async Task ABlankQueryClearsTheResultsAndNeverAsksTheCatalog()
    {
        var catalog = new StubCatalog(_ => Task.FromResult<Result<Page>>(Tea));
        var viewModel = new SearchViewModel(catalog);
        await viewModel.SearchAsync("tea");

        await viewModel.SearchAsync("   ");
        await viewModel.SearchAsync(null);

        Assert.That(viewModel.Status, Is.EqualTo("Type to search."));
        Assert.That(viewModel.TopResult.HasValue, Is.False);
        Assert.That(catalog.Queries, Is.EqualTo(new[] { "tea" }));
    }

    [Test]
    public async Task AnEmptyPageIsLoadedNotFailed()
    {
        var viewModel = new SearchViewModel(new StubCatalog(query => Task.FromResult<Result<Page>>(new Page(query, []))));

        await viewModel.SearchAsync("xyz");

        Assert.That(viewModel.Status, Is.EqualTo("Nothing found for 'xyz'."));
        Assert.That(viewModel.TopResult.HasValue, Is.False);
    }

    [Test]
    public async Task AnErrorResultAndAFaultedTaskBothBecomeTheFailedState()
    {
        var failing = new SearchViewModel(new StubCatalog(_ => Task.FromResult<Result<Page>>(new TimeoutException("The catalog is slow."))));
        var throwing = new SearchViewModel(new StubCatalog(_ => Task.FromException<Result<Page>>(new IOException("Connection lost."))));

        await failing.SearchAsync("tea");
        await throwing.SearchAsync("tea");

        Assert.That(failing.Status, Is.EqualTo("Search failed: The catalog is slow."));
        Assert.That(throwing.Status, Is.EqualTo("Search failed: Connection lost."));
        Assert.That(throwing.Results.IsFaulted, Is.True);
    }

    [Test]
    public void ACancelledSearchThrowsInsteadOfBecomingAFailedState()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var viewModel = new SearchViewModel(new StubCatalog(_ => Task.FromCanceled<Result<Page>>(cancellation.Token)));

        Assert.CatchAsync<OperationCanceledException>(() => viewModel.SearchAsync("tea", cancellation.Token));
    }

    [Test]
    public void ANewViewModelStartsNotLoaded()
    {
        var viewModel = new SearchViewModel(new StubCatalog(_ => Task.FromResult<Result<Page>>(Tea)));

        Assert.That(viewModel.Results.IsNotLoaded, Is.True);
        Assert.That(viewModel.Status, Is.EqualTo("Type to search."));
    }

    private sealed class StubCatalog(Func<string, Task<Result<Page>>> search) : ICatalog
    {
        public List<string> Queries { get; } = [];

        public Task<Result<Page>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            Queries.Add(query);
            return search(query);
        }
    }
}
