using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class ValueCastAnalyzerTests
{
    [TestCase("int M(Result<int> r) => (int)r.Value;", "r.Value")]
    [TestCase("Exception M(Result<int> r) => (Exception)r.Value;", "r.Value")]
    [TestCase("Exception M(Result r) => (Exception)r.Value;", "r.Value")]
    [TestCase("string M(Option<string> o) => (string)o.Value;", "o.Value")]
    [TestCase("int M(Loadable<int> l) => (int)l.Value;", "l.Value")]
    [TestCase("int M(Result<int> r) => (int)r.Value + 1;", "r.Value")]
    [TestCase("int M() => (int)Field.Value;", "Field.Value")]
    [TestCase("int M() => (int)Load().Value;", "Load().Value")]
    public async Task ReportsCastsOfValue(string member, string operand)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { ValueCastAnalyzer.DiagnosticId }));
        Assert.That(diagnostics[0].GetMessage(), Does.StartWith($"The cast of '{operand}' throws"));
    }

    [TestCase("int? M(Result<int> r) => r.Value as int?;")]
    [TestCase("Exception? M(Result<int> r) => r.Value as Exception;")]
    [TestCase("object? M(Result<int> r) => r.Value;")]
    [TestCase("bool M(Result<int> r) => r.Value is int;")]
    [TestCase("int M(Result<int> r) => r.Value is int i ? i : 0;")]
    [TestCase("int M(Result<int> r) => r.Value switch { int i => i, _ => 0 };")]
    [TestCase("int M(Mine m) => (int)m.Value;")]
    [TestCase("int M(Mine m) => (int)m.Value + (int)(long)5;")]
    [TestCase("string M(Result<int> r) => (string)r.ToString()!;")]
    public async Task IgnoresOtherShapes(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new ValueCastAnalyzer(), $$"""
            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception) => new MyError();
            }

            public class Mine
            {
                public object Value => 0;
            }

            public class Usage
            {
                Result<int> Field = default;

                Result<int> Load() => default;

                {{member}}
            }
            """);
}
