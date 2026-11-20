using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class AsyncLambdaAnalyzerTests
{
    [TestCase("void M() { var r = Result.Try(async () => await LoadAsync()); }", "Try", "TryAsync")]
    [TestCase("void M() { var r = Result.Try(async () => { await SaveAsync(); }); }", "Try", "TryAsync")]
    [TestCase("void M() { var r = Result.Try(async delegate { return await LoadAsync(); }); }", "Try", "TryAsync")]
    [TestCase("void M(Result<int> r) { var s = r.Select(async x => await LoadAsync()); }", "Select", "SelectAwait")]
    [TestCase("void M(Result r) { var s = r.Select(async () => await LoadAsync()); }", "Select", "SelectAwait")]
    [TestCase("void M(Option<int> o) { var s = o.Select(async x => await LoadAsync()); }", "Select", "SelectAwait")]
    [TestCase("void M(Task<Result<int>> t) { var s = t.Select(async x => await LoadAsync()); }", "Select", "SelectAwait")]
    [TestCase("void M(Task<Option<int>> t) { var s = t.Select(async x => await LoadAsync()); }", "Select", "SelectAwait")]
    public async Task ReportsAsyncLambdaInSyncOperator(string member, string name, string counterpart)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0002" }));
        Assert.That(diagnostics[0].GetMessage(), Does.StartWith($"'{name}' does not await").And.EndWith($"use '{counterpart}'"));
        Assert.That(diagnostics[0].Properties[Descriptors.ReplacementProperty], Is.EqualTo(counterpart));
    }

    [Test]
    public async Task ReportsOnTheOperatorName()
    {
        const string member = "void M(Result<int> r) { var s = r.Select(async x => await LoadAsync()); }";

        var diagnostics = await Analyze(member);

        var span = diagnostics.Single().Location.SourceSpan;
        Assert.That((AnalyzerTest.Usings + Wrap(member)).Substring(span.Start, span.Length), Is.EqualTo("Select"));
    }

    [TestCase("void M() { var r = Result.TryAsync(async () => await LoadAsync()); }")]
    [TestCase("void M() { var r = Result.TryAsync(async () => { await SaveAsync(); }); }")]
    [TestCase("void M(Result<int> r) { var s = r.SelectAwait(async x => await LoadAsync()); }")]
    [TestCase("void M(Result<int> r) { var s = r.ChainAwait(async x => Result.From(await LoadAsync())); }")]
    [TestCase("void M() { var r = Result.Try(() => 1); }")]
    [TestCase("void M(Result<int> r) { var s = r.Select(x => LoadAsync()); }")]
    [TestCase("void M(Result<int> r) { var s = r.Select(x => x + 1); }")]
    [TestCase("void M(Result<int> r) { var s = r.Chain(x => Result.From(x)); }")]
    [TestCase("void M(Loadable<int> l) { var s = l.Select(async x => await LoadAsync()); }")]
    [TestCase("void M(Result<int, OrderError> r) { var s = r.Select(async x => await LoadAsync()); }")]
    [TestCase("void M() { Func<Task<int>> f = async () => await LoadAsync(); var r = Result.Try(f); }")]
    [TestCase("void M(IEnumerable<int> e) { var s = e.Select(async x => await LoadAsync()); }")]
    public async Task IgnoresAwaitedOrNonAsyncShapes(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task IgnoresOperatorsOfUserCode()
    {
        const string source = """
            static class Ops
            {
                public static Result<Task<int>> Select(this Result<int> r, Func<int, Task<int>> f) => Result.From(f(1));
                public static Result<int> Do(this Result<int> r, Action<int> f) => r;
            }

            class C
            {
                void M(Result<int> r)
                {
                    var s = r.Select(async x => await Task.FromResult(x));
                    r.Do(async x => await Task.Yield());
                }
            }
            """;

        var diagnostics = await AnalyzerTest.Analyze(new AsyncLambdaAnalyzer(), source);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("void M(Result<int> r) { r.Do(async x => await SaveAsync()); }", "Do", "DoAwait")]
    [TestCase("void M(Result<int> r) { r.Do(async x => { await SaveAsync(); }); }", "Do", "DoAwait")]
    [TestCase("void M(Result r) { r.Do(async () => await SaveAsync()); }", "Do", "DoAwait")]
    [TestCase("void M(Option<int> o) { o.Do(async x => await SaveAsync()); }", "Do", "DoAwait")]
    [TestCase("void M(Task<Result<int>> t) { t.Do(async x => await SaveAsync()); }", "Do", "DoAwait")]
    [TestCase("void M(Task<Result> t) { t.Do(async () => await SaveAsync()); }", "Do", "DoAwait")]
    [TestCase("void M(Task<Option<int>> t) { t.Do(async x => await SaveAsync()); }", "Do", "DoAwait")]
    [TestCase("void M(Result<int>? r) { r?.Do(async x => await SaveAsync()); }", "Do", "DoAwait")]
    public async Task ReportsAsyncVoidLambdaWithCounterpart(string member, string name, string counterpart)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0003" }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"The async lambda passed to '{name}' is async void; use '{counterpart}'"));
        Assert.That(diagnostics[0].Properties[Descriptors.ReplacementProperty], Is.EqualTo(counterpart));
    }

    [TestCase("void M(Result<int> r) { r.OnErrorDo(async e => await SaveAsync()); }", "OnErrorDo")]
    [TestCase("void M(Result r) { r.OnErrorDo(async e => await SaveAsync()); }", "OnErrorDo")]
    [TestCase("void M(Result<int> r) { r.Do(async x => await SaveAsync(), e => { }); }", "Do")]
    [TestCase("void M(Result<int> r) { r.Do(x => { }, async e => await SaveAsync()); }", "Do")]
    [TestCase("void M(Option<int> o) { o.Do(x => { }, async () => await SaveAsync()); }", "Do")]
    public async Task ReportsAsyncVoidLambdaWithoutCounterpart(string member, string name)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0003" }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"The async lambda passed to '{name}' is async void; await the work outside the chain"));
        Assert.That(diagnostics[0].Properties, Does.Not.ContainKey(Descriptors.ReplacementProperty));
    }

    [TestCase("void M(Result<int> r) { r.DoAwait(async x => await SaveAsync()); }")]
    [TestCase("void M(Result<int> r) { r.Do(x => { }); }")]
    [TestCase("void M(Result<int> r) { r.OnErrorDo(e => { }); }")]
    [TestCase("void M(Result<int> r) { r.Do(x => SaveAsync()); }")]
    [TestCase("void M(Result<int> r) { Action<int> a = async x => await SaveAsync(); r.Do(a); }")]
    [TestCase("void M(Result<int> r) { r.Do(x => { Func<Task> f = async () => await SaveAsync(); }); }")]
    public async Task IgnoresAwaitedOrSynchronousCallbacks(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        await AnalyzerTest.Analyze(new AsyncLambdaAnalyzer(), Wrap(member));

    private static string Wrap(string member) =>
        $$"""
            record OutOfStock(string Item);

            union OrderError(OutOfStock, Exception) : IFromException<OrderError>
            {
                public static OrderError FromException(Exception exception) => exception;
            }

            class C
            {
                static Task<int> LoadAsync() => Task.FromResult(1);
                static Task SaveAsync() => Task.CompletedTask;
                {{member}}
            }
            """;
}
