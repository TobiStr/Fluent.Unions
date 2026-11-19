namespace Fluent.Unions.Tests;

public class LoadableTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void StatesAreDistinct()
    {
        Loadable<int> notLoaded = Loadable.NotLoaded;
        Loadable<int> loading = Loadable.Loading;
        Loadable<int> loaded = 5;
        Loadable<int> failed = Failure;

        Assert.That((notLoaded.IsNotLoaded, notLoaded.IsLoading, notLoaded.HasValue, notLoaded.IsFaulted), Is.EqualTo((true, false, false, false)));
        Assert.That((loading.IsNotLoaded, loading.IsLoading, loading.HasValue, loading.IsFaulted), Is.EqualTo((false, true, false, false)));
        Assert.That((loaded.IsNotLoaded, loaded.IsLoading, loaded.HasValue, loaded.IsFaulted), Is.EqualTo((false, false, true, false)));
        Assert.That((failed.IsNotLoaded, failed.IsLoading, failed.HasValue, failed.IsFaulted), Is.EqualTo((false, false, false, true)));
        Assert.True(default(Loadable<int>).IsNotLoaded);
    }

    [Test]
    public void MatchHandlesEveryState()
    {
        static string Render(Loadable<int> loadable) => loadable.Match(
            () => "idle",
            () => "spinner",
            value => $"value {value}",
            error => $"error {error.Message}");

        Assert.That(Render(Loadable.NotLoaded), Is.EqualTo("idle"));
        Assert.That(Render(default), Is.EqualTo("idle"));
        Assert.That(Render(Loadable.Loading), Is.EqualTo("spinner"));
        Assert.That(Render(5), Is.EqualTo("value 5"));
        Assert.That(Render(Failure), Is.EqualTo("error Failure"));
    }

    [Test]
    public void PatternMatchingIsExhaustive()
    {
        Loadable<string> loadable = "Ada";

        var text = loadable switch
        {
            NotLoaded or null => "idle",
            Loading => "spinner",
            string name => name,
            Exception error => error.Message
        };

        Assert.That(text, Is.EqualTo("Ada"));
    }

    [Test]
    public void SelectProjectsOnlyTheLoadedValue()
    {
        Loadable<string> loaded = "Ada";
        Loadable<string> loading = Loadable.Loading;
        Loadable<string> failed = Failure;

        Assert.That(loaded.Select(n => n.Length).GetValueOrDefault(-1), Is.EqualTo(3));
        Assert.True(loading.Select(n => n.Length).IsLoading);
        Assert.That(failed.Select(n => n.Length).Value, Is.SameAs(Failure));
        Assert.True(default(Loadable<string>).Select(n => n.Length).IsNotLoaded);
        Assert.True(loaded.Select(_ => (string?)null).IsNotLoaded);
    }

    [Test]
    public async Task ConvertsFromResults()
    {
        Assert.That(((Result<int>)5).ToLoadable().GetValueOrDefault(-1), Is.EqualTo(5));
        Assert.True(((Result<int>)Failure).ToLoadable().IsFaulted);
        Assert.True(default(Result<int>).ToLoadable().IsNotLoaded);
        Assert.That((await Task.FromResult<Result<int>>(7).ToLoadable()).GetValueOrDefault(-1), Is.EqualTo(7));
        Assert.That((await Task.FromException<Result<int>>(Failure).ToLoadable()).Value, Is.SameAs(Failure));
    }

    [Test]
    public void ToOptionKeepsOnlyTheValue()
    {
        Assert.That(((Loadable<int>)5).ToOption().GetValue(), Is.EqualTo(5));
        Assert.That(((Loadable<int>)Loadable.Loading).ToOption().Value, Is.TypeOf<None>());
        Assert.That(((Loadable<int>)5).ToString(), Is.EqualTo("5"));
    }
}
