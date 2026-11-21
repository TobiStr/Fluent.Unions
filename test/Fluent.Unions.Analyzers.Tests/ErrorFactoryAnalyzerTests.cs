using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class ErrorFactoryAnalyzerTests
{
    private const string CleanFromError = "public static Custom FromError(MyError error) => default;";

    private const string CleanFromException = "public static MyError FromException(Exception exception) => new MyError();";

    [TestCase("public static MyError FromException(Exception exception) { throw new InvalidOperationException(); }", "throw")]
    [TestCase("public static MyError FromException(Exception exception) => throw new InvalidOperationException();", "throw")]
    [TestCase("public static MyError FromException(Exception exception) { return exception is null ? throw new InvalidOperationException() : new MyError(); }", "throw")]
    [TestCase("public static MyError FromException(Exception exception) => null!;", "return null")]
    [TestCase("public static MyError FromException(Exception exception) => null;", "return null")]
    [TestCase("public static MyError FromException(Exception exception) { return default!; }", "return null")]
    [TestCase("public static MyError FromException(Exception exception) { if (exception is null) return null!; return new MyError(); }", "return null")]
    [TestCase("static MyError IFromException<MyError>.FromException(Exception exception) => null!;", "return null")]
    public async Task ReportsFactoriesOfAnErrorType(string member, string action)
    {
        var diagnostics = await Analyze($$"""
            public class MyError : IFromException<MyError>
            {
                {{member}}
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { ErrorFactoryAnalyzer.DiagnosticId }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"'FromException' must not {action}"));
    }

    [TestCase("public static Custom FromError(MyError error) { throw new InvalidOperationException(); }", CleanFromException, "FromError")]
    [TestCase("public static Custom FromError(MyError error) => throw new InvalidOperationException();", CleanFromException, "FromError")]
    [TestCase(CleanFromError, "public static MyError FromException(Exception exception) { throw new InvalidOperationException(); }", "FromException")]
    public async Task ReportsFactoriesOfAResultType(string fromError, string fromException, string name)
    {
        var diagnostics = await Analyze($$"""
            public class MyError : IFromException<MyError>
            {
                {{CleanFromException}}
            }

            public struct Custom : IErrorResult<Custom, MyError>
            {
                public object? Value => null;
                public bool IsSuccess => false;
                public bool TryGetError(out MyError error) { error = default!; return true; }
                {{fromError}}
                {{fromException}}
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { ErrorFactoryAnalyzer.DiagnosticId }));
        Assert.That(diagnostics[0].GetMessage(), Does.StartWith($"'{name}'"));
    }

    [Test]
    public async Task ReportsEveryThrowAndNull()
    {
        var diagnostics = await Analyze("""
            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception)
                {
                    if (exception is null)
                        throw new ArgumentNullException(nameof(exception));

                    if (exception is TimeoutException)
                        return null!;

                    return new MyError();
                }
            }
            """);

        Assert.That(diagnostics.Select(d => d.GetMessage()), Is.EqualTo(new[]
        {
            "'FromException' must not throw",
            "'FromException' must not return null"
        }));
    }

    [TestCase("public static MyError FromException(Exception exception) { Func<MyError> f = () => throw new InvalidOperationException(); return f(); }")]
    [TestCase("public static MyError FromException(Exception exception) { MyError Local() { throw new InvalidOperationException(); } return Local(); }")]
    [TestCase("public static MyError FromException(Exception exception) { MyError? Local() { return null; } return Local() ?? new MyError(); }")]
    public async Task IgnoresThrowsAndNullsInNestedFunctions(string implementation)
    {
        var diagnostics = await Analyze($$"""
            public class MyError : IFromException<MyError>
            {
                {{implementation}}
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("public static MyError Create(Exception exception) { throw new InvalidOperationException(); }")]
    [TestCase("public static MyError FromException(Exception exception, int extra) { throw new InvalidOperationException(); }")]
    [TestCase("public MyError FromMessage(string message) { throw new InvalidOperationException(); }")]
    public async Task IgnoresOtherMethods(string member)
    {
        var diagnostics = await Analyze($$"""
            public class MyError : IFromException<MyError>
            {
                {{CleanFromException}}
                {{member}}
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task IgnoresDefaultOfAValueType()
    {
        var diagnostics = await Analyze($$"""
            public class MyError : IFromException<MyError>
            {
                {{CleanFromException}}
            }

            public struct Custom : IErrorResult<Custom, MyError>
            {
                public object? Value => null;
                public bool IsSuccess => false;
                public bool TryGetError(out MyError error) { error = default!; return true; }
                {{CleanFromError}}
                {{CleanFromException}}
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task IgnoresTypesThatDoNotImplementTheInterfaces()
    {
        var diagnostics = await Analyze("""
            public class Plain
            {
                public static Plain FromException(Exception exception) { throw new InvalidOperationException(); }
                public static Plain FromError(string error) => null!;
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string source) =>
        AnalyzerTest.Analyze(new ErrorFactoryAnalyzer(), source);
}
