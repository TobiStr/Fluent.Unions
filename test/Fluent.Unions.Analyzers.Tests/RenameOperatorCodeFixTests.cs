using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class RenameOperatorCodeFixTests
{
    [Test]
    public async Task RenamesTryForAsyncLambda() =>
        await AssertFix(
            new AsyncLambdaAnalyzer(),
            "var t = Result.Try(async () => await LoadAsync());",
            "var t = Result.TryAsync(async () => await LoadAsync());");

    [Test]
    public async Task RenamesSelectForAsyncLambda() =>
        await AssertFix(
            new AsyncLambdaAnalyzer(),
            "var s = r.Select(async x => await LoadAsync());",
            "var s = r.SelectAwait(async x => await LoadAsync());");

    [Test]
    public async Task RenamesDoForAsyncVoidLambda() =>
        await AssertFix(
            new AsyncLambdaAnalyzer(),
            "r.Do(async x => await SaveAsync());",
            "r.DoAwait(async x => await SaveAsync());");

    [Test]
    public async Task KeepsTypeArguments() =>
        await AssertFix(
            new AsyncLambdaAnalyzer(),
            "r.Do<int>(async x => await SaveAsync());",
            "r.DoAwait<int>(async x => await SaveAsync());");

    [Test]
    public async Task RenamesInsideConditionalAccess() =>
        await AssertFix(
            new AsyncLambdaAnalyzer(),
            "Result<int>? n = r; n?.Do(async x => await SaveAsync());",
            "Result<int>? n = r; n?.DoAwait(async x => await SaveAsync());");

    [Test]
    public async Task RenamesSelectForNestedResult() =>
        await AssertFix(
            new NestedSelectAnalyzer(),
            "var s = r.Select(x => Result.From(x));",
            "var s = r.Chain(x => Result.From(x));");

    [Test]
    public async Task RenamesSelectAwaitForNestedResult() =>
        await AssertFix(
            new NestedSelectAnalyzer(),
            "var s = r.SelectAwait(async x => Result.From(await LoadAsync()));",
            "var s = r.ChainAwait(async x => Result.From(await LoadAsync()));");

    [Test]
    public async Task RenamesSelectForNestedOption() =>
        await AssertFix(
            new NestedSelectAnalyzer(),
            "var s = o.Select(x => Option.Some(x));",
            "var s = o.Chain(x => Option.Some(x));");

    [Test]
    public async Task OffersNoFixForQuerySyntax() =>
        Assert.That(await CountFixes(new NestedSelectAnalyzer(), "var s = from x in r select Result.From(x);"), Is.Zero);

    [Test]
    public async Task OffersNoFixWithoutCounterpart() =>
        Assert.That(await CountFixes(new AsyncLambdaAnalyzer(), "r.OnErrorDo(async e => await SaveAsync());"), Is.Zero);

    [Test]
    public void FixesTheRenamingDiagnosticsWithBatchFixAll()
    {
        var provider = new RenameOperatorCodeFix();

        Assert.That(provider.FixableDiagnosticIds, Is.EquivalentTo(new[] { "FLU0002", "FLU0003", "FLU0006" }));
        Assert.That(provider.GetFixAllProvider(), Is.SameAs(WellKnownFixAllProviders.BatchFixer));
    }

    private static async Task AssertFix(DiagnosticAnalyzer analyzer, string statement, string expectedStatement)
    {
        var fixedSource = await Fix(analyzer, statement);

        Assert.That(fixedSource, Is.EqualTo(Source(expectedStatement)));
    }

    private static Task<string> Fix(DiagnosticAnalyzer analyzer, string statement) =>
        AnalyzerTest.Fix(analyzer, new RenameOperatorCodeFix(), Source(statement));

    private static async Task<int> CountFixes(DiagnosticAnalyzer analyzer, string statement)
    {
        using var workspace = new AdhocWorkspace();
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var document = workspace.AddProject("Snippet", LanguageNames.CSharp)
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .WithMetadataReferences(references)
            .AddDocument("Snippet.cs", SourceText.From(AnalyzerTest.Usings + Source(statement)));

        var compilation = (await document.Project.GetCompilationAsync())!;
        var diagnostic = (await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync()).Single();

        var actions = new List<CodeAction>();
        await new RenameOperatorCodeFix().RegisterCodeFixesAsync(
            new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));

        return actions.Count;
    }

    private static string Source(string statement) =>
        $$"""
            class C
            {
                static Task<int> LoadAsync() => Task.FromResult(1);
                static Task SaveAsync() => Task.CompletedTask;

                void M(Result<int> r, Option<int> o)
                {
                    {{statement}}
                }
            }
            """;
}
