using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Generators.Tests;

public class GeneratorTests
{
    private const string Domain = """
        using System;
        using Fluent.Unions;
        using Fluent.Unions.Abstractions;

        namespace Shop;

        public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
        {
            public static DomainException FromException(Exception exception) => exception as DomainException ?? new DomainException(exception.Message, exception);
        }

        """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
    ];

    [Test]
    public void ValidDeclarationCompilesWithoutDiagnostics()
    {
        var (output, diagnostics) = Run(Domain + "[FluentResult<DomainException>] public readonly partial struct Result<T>;");

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()), Is.Empty);
    }

    [Test]
    public void GeneratedTypeSupportsTheWholeChain()
    {
        var (output, _) = Run(Domain + """
            [FluentResult<DomainException>] public readonly partial struct Result<T>;

            public static class Use
            {
                public static Result<int> Length(string text) => Result.From(text)
                    .Select(t => t.Trim())
                    .Ensure(t => t.Length > 0, "empty")
                    .Chain(t => Result<int>.Ok(t.Length))
                    .Do(_ => { })
                    .Catch(e => Result<int>.Ok(0));

                public static System.Threading.Tasks.Task<Result<int>> LengthAsync(string text) =>
                    System.Threading.Tasks.Task.FromResult(Result.From(text)).Select(t => t.Length);

                public static Result<(int, string)> Both() => Result.From(1).BindAppend(n => Result.From(n.ToString()));
            }
            """);

        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()), Is.Empty);
    }

    [TestCase("public partial struct Result<T>;", TestName = "Not readonly")]
    [TestCase("public readonly struct Result<T>;", TestName = "Not partial")]
    [TestCase("public readonly partial struct Result<T, TOther>;", TestName = "Two type parameters")]
    [TestCase("public readonly partial struct Result<T> where T : class;", TestName = "Constrained type parameter")]
    [TestCase("public static partial class Outer { [FluentResult<DomainException>] public readonly partial struct Result<T>; }", TestName = "Nested")]
    public void UnsupportedShapeIsReported(string declaration)
    {
        var source = declaration.Contains("Outer") ? declaration : "[FluentResult<DomainException>] " + declaration;

        var (_, diagnostics) = Run(Domain + source);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101" }));
        Assert.That(diagnostics[0].Location.GetLineSpan().StartLinePosition.Line, Is.GreaterThan(9));
    }

    [Test]
    public void MissingExceptionMappingIsReported()
    {
        var (_, diagnostics) = Run("""
            using System;
            using Fluent.Unions;

            public class PlainException(string message) : Exception(message);

            [FluentResult<PlainException>] public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0102" }));
    }

    [Test]
    public void DeclaredExceptionMappingIsUsed()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;

            public class PlainException(string message) : Exception(message);

            [FluentResult<PlainException>]
            public readonly partial struct Result<T>
            {
                public static PlainException FromException(Exception exception) => exception as PlainException ?? new PlainException(exception.Message);
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()), Is.Empty);
    }

    [Test]
    public void InterfaceCaseOfAnErrorUnionGetsNoConversion()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            public interface IMarked;
            public sealed record Missing(int Id);

            public union AppError(Missing, IMarked) : IFromException<AppError>
            {
                public static AppError FromException(Exception exception) => new Missing(0);
            }

            [FluentResult<AppError>] public readonly partial struct Result<T>;

            public static class Use
            {
                public static Result<int> Find() => new Missing(1);
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0103" }));
        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()), Is.Empty);
    }

    [Test]
    public void EveryPublicCoreMethodBecomesAnOperator()
    {
        var compilation = Compilation("");
        var templates = OperatorReader.Read(compilation);

        var coreMethods = typeof(Result).Assembly.GetTypes()
            .Where(type => type is { Namespace: "Fluent.Unions.Core", IsPublic: true, IsAbstract: true, IsSealed: true }
                && type.Name.StartsWith("Result", StringComparison.Ordinal)
                && type.Name != "ResultMemberCore")
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Count();

        Assert.That(templates.Length, Is.EqualTo(coreMethods));
    }

    [Test]
    public void UnrelatedEditKeepsTheGeneratedOutputCached()
    {
        var source = Domain + "[FluentResult<DomainException>] public readonly partial struct Result<T>;";
        var compilation = Compilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new FluentResultGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var edited = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public static class Unrelated { }", ParseOptions));
        driver = driver.RunGenerators(edited);

        var outputs = driver.GetRunResult().Results[0].TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs);
        Assert.That(outputs.Select(output => output.Reason), Is.All.EqualTo(IncrementalStepRunReason.Cached).Or.EqualTo(IncrementalStepRunReason.Unchanged));
    }

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
    {
        var driver = CSharpGeneratorDriver.Create([new FluentResultGenerator().AsSourceGenerator()], parseOptions: ParseOptions);
        driver.RunGeneratorsAndUpdateCompilation(Compilation(source), out var output, out var diagnostics);
        return (output, diagnostics);
    }

    private static CSharpCompilation Compilation(string source) => CSharpCompilation.Create(
        "Consumer",
        [CSharpSyntaxTree.ParseText(source, ParseOptions)],
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
}
