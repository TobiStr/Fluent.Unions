using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class UnusedResultAnalyzerTests
{
    [TestCase("void M(Service s) { s.Save(); }", "Save")]
    [TestCase("void M(Service s) { s.Load(); }", "Load")]
    [TestCase("void M(Service s) => s.Save();", "Save")]
    [TestCase("void M(Service s) { Action a = () => s.Save(); }", "Save")]
    [TestCase("void M(Service? s) { s?.Save(); }", "Save")]
    [TestCase("async Task M(Service s) { await s.SaveAsync(); }", "SaveAsync")]
    [TestCase("void M(Service s) { s.SaveAsync(); }", "SaveAsync")]
    [TestCase("void M(Service s) { s.Load().Do(v => { }); }", "Do")]
    [TestCase("void M(Service s) { s.Load().Catch(e => 0); }", "Catch")]
    [TestCase("void M(Service s) { s.Custom(); }", "Custom")]
    [TestCase("void M<TSelf>(Service s) where TSelf : struct, IErrorResult<TSelf, Exception> { s.Generic<TSelf>(); }", "Generic")]
    public async Task ReportsDiscardedResults(string member, string name)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { UnusedResultAnalyzer.DiagnosticId }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain($"'{name}'"));
    }

    [Test]
    public void DescriptorLinksToTheAnalyzerDocumentation()
    {
        var descriptor = new UnusedResultAnalyzer().SupportedDiagnostics.Single(d => d.Id == UnusedResultAnalyzer.DiagnosticId);

        Assert.That(descriptor.HelpLinkUri, Is.EqualTo("https://tobistr.github.io/Fluent.Unions/analyzer/"));
    }

    [TestCase("void M(Service s) { _ = s.Save(); }")]
    [TestCase("void M(Service s) { var r = s.Save(); }")]
    [TestCase("Result M(Service s) { return s.Save(); }")]
    [TestCase("void M(Service s) { s.Save().Check(); }")]
    [TestCase("async Task M(Service s) { await s.SaveAsync().Check(); }")]
    [TestCase("void M(Service s) { s.Load().OnErrorDo(e => { }); }")]
    [TestCase("void M(Service s) { s.Load().Do(v => { }, e => { }); }")]
    [TestCase("async Task M(Service s) { s.SaveAsync(); }")]
    [TestCase("void M(Service s) { Func<Task> f = async () => { s.SaveAsync(); }; }")]
    [TestCase("void M(Service s) { s.Find(); }")]
    [TestCase("void M(Service s) { s.Count(); }")]
    public async Task IgnoresUsedOrHandledResults(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string member)
    {
        var source = $$"""
            using System;
            using System.Threading.Tasks;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            public struct CustomResult : IErrorResult<CustomResult, Exception>
            {
                public object? Value => null;
                public static CustomResult FromError(Exception error) => default;
                public static Exception FromException(Exception exception) => exception;
            }

            public class Service
            {
                public Result Save() => default;
                public Result<int> Load() => default;
                public Task<Result> SaveAsync() => Task.FromResult(default(Result));
                public Option<int> Find() => default;
                public int Count() => 0;
                public CustomResult Custom() => default;
                public TSelf Generic<TSelf>() where TSelf : struct, IErrorResult<TSelf, Exception> => default;
            }

            public class Usage
            {
                {{member}}
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Snippet",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));

        return await compilation
            .WithAnalyzers([new UnusedResultAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}
