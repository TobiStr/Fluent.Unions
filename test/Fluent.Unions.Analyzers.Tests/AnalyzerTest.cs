using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

/// <summary>
/// Compiles a snippet against the library, runs an analyzer on it, and applies code fixes.
/// </summary>
internal static class AnalyzerTest
{
    /// <summary>
    /// The usings every snippet starts with.
    /// </summary>
    public const string Usings = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;
        using Fluent.Unions;
        using Fluent.Unions.Abstractions;

        """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    private static readonly CSharpCompilationOptions CompilationOptions =
        new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
    ];

    /// <summary>
    /// Runs the analyzer on <see cref="Usings"/> plus <paramref name="source"/>, after checking that it compiles.
    /// </summary>
    public static async Task<ImmutableArray<Diagnostic>> Analyze(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = Compile(Usings + source);

        var diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();

        return [.. diagnostics.OrderBy(d => d.Location.SourceSpan.Start)];
    }

    /// <summary>
    /// Applies the first fix the provider offers for the first diagnostic, and returns the fixed source without <see cref="Usings"/>.
    /// The fixed source must compile.
    /// </summary>
    public static async Task<string> Fix(DiagnosticAnalyzer analyzer, CodeFixProvider provider, string source)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Snippet", LanguageNames.CSharp)
            .WithParseOptions(ParseOptions)
            .WithCompilationOptions(CompilationOptions)
            .WithMetadataReferences(References);
        var document = project.AddDocument("Snippet.cs", SourceText.From(Usings + source));

        var compilation = (await document.Project.GetCompilationAsync())!;
        CSharpGeneratorDriver.Create([new Generators.FluentResultGenerator().AsSourceGenerator()], parseOptions: ParseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
        var diagnostic =(await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync())
            .Where(d => provider.FixableDiagnosticIds.Contains(d.Id))
            .OrderBy(d => d.Location.SourceSpan.Start)
            .First();

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);
        Assert.That(actions, Is.Not.Empty, $"No fix for {diagnostic}");

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var text = (await solution.GetDocument(document.Id)!.GetTextAsync()).ToString();

        Compile(text);

        return text[Usings.Length..];
    }

    private static CSharpCompilation Compile(string source)
    {
        var snippet = CSharpCompilation.Create(
            "Snippet",
            [CSharpSyntaxTree.ParseText(source, ParseOptions)],
            References,
            CompilationOptions);
        CSharpGeneratorDriver.Create([new Generators.FluentResultGenerator().AsSourceGenerator()], parseOptions: ParseOptions)
            .RunGeneratorsAndUpdateCompilation(snippet, out var generated, out _);
        var compilation = (CSharpCompilation)generated;

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));

        return compilation;
    }
}
