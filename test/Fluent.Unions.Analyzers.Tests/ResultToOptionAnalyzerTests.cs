using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class ResultToOptionAnalyzerTests
{
    [TestCase("Option<int> M(Result<int> r) => r.ToOption();")]
    [TestCase("Option<int> M() => Load().ToOption();")]
    [TestCase("Option<int> M(Result<int> r) => OptionConversionExtensions.ToOption(r);")]
    [TestCase("Option<int> M(Result<int> r) => r.Do(v => { }).ToOption();")]
    [TestCase("Option<int> M(Result<int> r) => r.Select(v => v + 1).ToOption();")]
    [TestCase("async Task<Option<int>> M(Task<Result<int>> t) => (await t).ToOption();")]
    [TestCase("async Task<Option<int>> M(Task<Result<int>> t) => (await t.Select(v => v + 1)).ToOption();")]
    public async Task ReportsToOptionOnAResult(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { ResultToOptionAnalyzer.DiagnosticId }));
    }

    [TestCase("Option<int> M(Result<int> r) => r.OnErrorDo(e => { }).ToOption();")]
    [TestCase("Option<int> M(Result<int> r) => r.Do(v => { }, e => { }).ToOption();")]
    [TestCase("Option<int> M(Result<int> r) => r.Catch(e => default(Result<int>)).ToOption();")]
    [TestCase("Option<int> M(Result<int> r) => r.OnErrorDo(e => { }).Select(v => v + 1).ToOption();")]
    [TestCase("Option<int> M(Result<int> r) => r.Catch(e => default(Result<int>)).Do(v => { }).Select(v => v).ToOption();")]
    [TestCase("async Task<Option<int>> M(Task<Result<int>> t) => (await t.OnErrorDo(e => { })).ToOption();")]
    [TestCase("async Task<Option<int>> M(Task<Result<int>> t) => (await t.Select(v => v + 1).OnErrorDo(e => { })).ToOption();")]
    public async Task IgnoresResultsWhoseErrorIsHandled(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("Option<string> M(string? s) => s.ToOption();")]
    [TestCase("Option<int> M(int? i) => i.ToOption();")]
    [TestCase("Option<int> M(Loadable<int> l) => l.ToOption();")]
    public async Task IgnoresOtherReceivers(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task IgnoresAToOptionThatIsNotFromTheLibrary()
    {
        var diagnostics = await AnalyzerTest.Analyze(new ResultToOptionAnalyzer(), """
            public static class MyExtensions
            {
                public static Option<int> ToOption(this Result<int> result) => default;
            }

            public class Usage
            {
                Option<int> M(Result<int> r) => r.ToOption();
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new ResultToOptionAnalyzer(), $$"""
            public class Usage
            {
                Result<int> Load() => default;

                {{member}}
            }
            """);
}
