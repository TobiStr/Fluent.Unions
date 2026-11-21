using Fluent.Unions.Analyzers.CodeFixes;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class GeneratedResultAnalyzerTests
{
    private const string Domain = """
        public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
        {
            public static DomainException FromException(Exception exception) => exception as DomainException ?? new DomainException(exception.Message, exception);
        }

        public sealed class NotFound(string message) : DomainException(message);

        public sealed record Unexpected(Exception Exception);

        public union AppError(NotFound, Unexpected) : IFromException<AppError>
        {
            public static AppError FromException(Exception exception) => new Unexpected(exception);
        }

        [FluentResult<DomainException>] public readonly partial struct Outcome<T>;

        [FluentResult<AppError>] public readonly partial struct Checked<T>;

        """;

    [Test]
    public async Task UnusedGeneratedResultIsReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new UnusedResultAnalyzer(), Domain + """
            public static class Use
            {
                public static void M(Outcome<int> outcome) { outcome.Select(x => x + 1); }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0001" }));
    }

    [Test]
    public async Task UncheckedAccessOnAGeneratedResultIsReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new UncheckedAccessAnalyzer(), Domain + """
            public static class Use
            {
                public static int M(Outcome<int> outcome) => outcome.GetValue();
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { UncheckedAccessAnalyzer.DiagnosticId }));
    }

    [Test]
    public async Task CheckedAccessOnAGeneratedResultIsNotReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new UncheckedAccessAnalyzer(), Domain + """
            public static class Use
            {
                public static int M(Outcome<int> outcome) => outcome.IsSuccess ? outcome.GetValue() : 0;
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task ValueTypeThatIsACaseOfTheErrorUnionIsReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new TypeArgumentAnalyzer(), Domain + """
            public static class Use
            {
                public static Checked<NotFound> M() => default;
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0004" }));
    }

    [Test]
    public async Task ValueTypeThatIsTheErrorTypeIsReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new TypeArgumentAnalyzer(), Domain + """
            public static class Use
            {
                public static Outcome<NotFound> M() => default;
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0004" }));
    }

    [Test]
    public async Task AsyncLambdaInAGeneratedOperatorIsReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new AsyncLambdaAnalyzer(), Domain + """
            public static class Use
            {
                public static Outcome<Task<int>> M(Outcome<int> outcome) => outcome.Select(async x => { await Task.Yield(); return x; });
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("FLU0002"));
    }

    [TestCase("public static void M(Outcome<int> outcome) { outcome.OnErrorDo(e => { }); }")]
    [TestCase("public static void M(Outcome<int> outcome) { outcome.Do(x => { }, e => { }); }")]
    [TestCase("public static async Task M(Task<Outcome<int>> task) { await task.OnErrorDo(e => { }); }")]
    public async Task HandlingOperatorOnAGeneratedResultIsNotReportedAsUnused(string member)
    {
        var diagnostics = await AnalyzerTest.Analyze(new UnusedResultAnalyzer(), Domain + $$"""
            public static class Use
            {
                {{member}}
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task HandlingOperatorsOfALibraryResultAndAGeneratedOneBehaveTheSame()
    {
        var diagnostics = await AnalyzerTest.Analyze(new UnusedResultAnalyzer(), Domain + """
            public static class Use
            {
                public static void M(Outcome<int> outcome, Result<int> result)
                {
                    outcome.OnErrorDo(e => { });
                    result.OnErrorDo(e => { });
                    outcome.Do(x => { }, e => { });
                    result.Do(x => { }, e => { });
                    outcome.Do(x => { });
                    result.Do(x => { });
                }
            }
            """);

        Assert.That(diagnostics.Select(d => d.GetMessage()), Is.EqualTo(new[]
        {
            "The result of 'Do' is not used",
            "The result of 'Do' is not used"
        }));
    }

    [Test]
    public async Task ThrowOfAnExceptionThatTheResultCannotHoldIsNotReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new ThrowInResultMethodAnalyzer(), Domain + """
            public static class Use
            {
                public static Outcome<int> M(int x) { if (x < 0) throw new InvalidOperationException("negative"); return x; }
                public static async Task<Outcome<int>> N(int x) { await Task.Yield(); throw new InvalidOperationException("negative"); }
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task ThrowOfAnExceptionThatTheResultCanHoldIsReported()
    {
        var diagnostics = await AnalyzerTest.Analyze(new ThrowInResultMethodAnalyzer(), Domain + """
            public static class Use
            {
                public static Outcome<int> M(int x) { if (x < 0) throw new DomainException("negative"); return x; }
                public static Checked<int> N(int x) { if (x < 0) throw new NotFound("negative"); return x; }
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0012", "FLU0012" }));
    }

    [Test]
    public async Task ThrowToReturnFixOfAGeneratedResultCompiles()
    {
        var fixedSource = await AnalyzerTest.Fix(new ThrowInResultMethodAnalyzer(), new ThrowToReturnCodeFix(), Domain + """
            public static class Use
            {
                public static Outcome<int> M(int x)
                {
                    if (x < 0)
                        throw new DomainException("negative");

                    return x;
                }
            }
            """);

        Assert.That(fixedSource, Does.Contain("return new DomainException(\"negative\");"));
    }
}
