using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class NestedSelectAnalyzerTests
{
    [TestCase("void M(Result<int> r) { var s = r.Select(x => Result.From(x)); }", "Select", "Result<Result<int>>", "Chain")]
    [TestCase("void M(Result<int> r) { var s = r.Select(x => Result.Ok); }", "Select", "Result<Result>", "Chain")]
    [TestCase("void M(Result r) { var s = r.Select(() => Result.Ok); }", "Select", "Result<Result>", "Chain")]
    [TestCase("void M(Result r) { var s = r.Select(() => Result.From(1)); }", "Select", "Result<Result<int>>", "Chain")]
    [TestCase("void M(Option<int> o) { var s = o.Select(x => Option.Some(x)); }", "Select", "Option<Option<int>>", "Chain")]
    [TestCase("void M(Task<Result<int>> t) { var s = t.Select(x => Result.From(x)); }", "Select", "Result<Result<int>>", "Chain")]
    [TestCase("void M(Task<Option<int>> t) { var s = t.Select(x => Option.Some(x)); }", "Select", "Option<Option<int>>", "Chain")]
    [TestCase("void M(Result<int> r) { var s = r.SelectAwait(async x => Result.From(await LoadAsync())); }", "SelectAwait", "Result<Result<int>>", "ChainAwait")]
    [TestCase("void M(Option<int> o) { var s = o.SelectAwait(async x => Option.Some(await LoadAsync())); }", "SelectAwait", "Option<Option<int>>", "ChainAwait")]
    [TestCase("void M(Task<Result<int>> t) { var s = t.SelectAwait(async x => Result.From(await LoadAsync())); }", "SelectAwait", "Result<Result<int>>", "ChainAwait")]
    [TestCase("void M(Result<int> r) { var s = from x in r select Result.From(x); }", "Select", "Result<Result<int>>", "Chain")]
    public async Task ReportsNestedSelect(string member, string name, string nested, string replacement)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0006" }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"'{name}' gives a nested '{nested}'; use '{replacement}'"));
        Assert.That(diagnostics[0].Properties[Descriptors.ReplacementProperty], Is.EqualTo(replacement));
    }

    [Test]
    public async Task ReportsOnTheOperatorName()
    {
        const string member = "void M(Result<int> r) { var s = r.Select(x => Result.From(x)); }";

        var diagnostics = await Analyze(member);

        var span = diagnostics.Single().Location.SourceSpan;
        Assert.That((AnalyzerTest.Usings + Wrap(member)).Substring(span.Start, span.Length), Is.EqualTo("Select"));
    }

    [TestCase("void M(Result<int> r) { var s = r.Select(x => Option.Some(x)); }")]
    [TestCase("void M(Option<int> o) { var s = o.Select(x => Result.From(x)); }")]
    [TestCase("void M(Result<int> r) { var s = r.Select(x => x + 1); }")]
    [TestCase("void M(Result<int> r) { var s = r.Chain(x => Result.From(x)); }")]
    [TestCase("void M(Option<int> o) { var s = o.Chain(x => Option.Some(x)); }")]
    [TestCase("void M(Result<int> r) { var s = r.SelectAwait(async x => await LoadAsync()); }")]
    [TestCase("void M(Result<int> r) { var s = r.ChainAwait(async x => Result.From(await LoadAsync())); }")]
    [TestCase("void M(Loadable<int> l) { var s = l.Select(x => l); }")]
    [TestCase("void M(IEnumerable<int> e) { var s = e.Select(x => Result.From(x)); }")]
    public async Task IgnoresFlatSelectAndMixedKinds(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task IgnoresSelectOfUserCode()
    {
        const string source = """
            static class Ops
            {
                public static Result<Result<int>> Select(this Result<int> r, Func<int, Result<int>> f) => Result.From(f(1));
            }

            class C
            {
                void M(Result<int> r)
                {
                    var s = r.Select(x => Result.From(x));
                }
            }
            """;

        var diagnostics = await AnalyzerTest.Analyze(new NestedSelectAnalyzer(), source);

        Assert.That(diagnostics, Is.Empty);
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        await AnalyzerTest.Analyze(new NestedSelectAnalyzer(), Wrap(member));

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
                {{member}}
            }
            """;
}
