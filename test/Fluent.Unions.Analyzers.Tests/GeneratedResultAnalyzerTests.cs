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
}
