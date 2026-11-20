using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class ThrowInResultMethodAnalyzerTests
{
    [TestCase("Result M() { throw new Exception(\"x\"); }", "M")]
    [TestCase("Result<int> M(int x) { if (x < 0) throw new InvalidOperationException(); return 1; }", "M")]
    [TestCase("Result<int> M(int x) { if (x < 0) throw new MyException(); return 1; }", "M")]
    [TestCase("Result M(Exception e) { throw e; }", "M")]
    [TestCase("async Task<Result<int>> M() { await Task.Yield(); throw new Exception(); }", "M")]
    [TestCase("async Task<Result> M() { await Task.Yield(); throw new System.IO.IOException(); }", "M")]
    [TestCase("async ValueTask<Result<int>> M() { await Task.Yield(); throw new Exception(); }", "M")]
    [TestCase("Result<int> M() { Result<int> Local() { throw new Exception(); } return Local(); }", "Local")]
    [TestCase("Func<Result<int>> M() => () => { throw new Exception(); };", "lambda")]
    [TestCase("Func<Task<Result>> M() => async () => { await Task.Yield(); throw new Exception(); };", "lambda")]
    [TestCase("Result M() { try { return default(Result); } catch (Exception e) { throw new Exception(\"wrapped\", e); } }", "M")]
    [TestCase("Result<int> Prop { get { throw new Exception(); } }", "get_Prop")]
    public async Task ReportsThrowStatements(string member, string function)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { ThrowInResultMethodAnalyzer.DiagnosticId }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"Return the exception instead of throwing it; '{function}' returns a result"));
    }

    [TestCase("Result M(string? s) { throw new ArgumentException(\"x\"); }")]
    [TestCase("Result M(string? s) { throw new ArgumentNullException(nameof(s)); }")]
    [TestCase("Result M(int i) { throw new ArgumentOutOfRangeException(nameof(i)); }")]
    [TestCase("Result M() { throw new ObjectDisposedException(\"x\"); }")]
    [TestCase("Result M() { throw new NotImplementedException(); }")]
    [TestCase("Result M() { throw new NotSupportedException(); }")]
    [TestCase("Result M() { throw new PlatformNotSupportedException(); }")]
    [TestCase("Result M() { throw new System.Diagnostics.UnreachableException(); }")]
    [TestCase("Result M() { throw new OperationCanceledException(); }")]
    [TestCase("Result M() { throw new TaskCanceledException(); }")]
    [TestCase("Result M() { throw new MyArgumentException(); }")]
    public async Task IgnoresGuardAndProgrammingErrors(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("Result<int> M(string? s) { var t = s ?? throw new Exception(); return 1; }")]
    [TestCase("Result<int> M(string? s) => s is null ? throw new Exception() : Result<int>.Ok(1);")]
    [TestCase("Result M() { try { return default(Result); } catch (Exception) { throw; } }")]
    [TestCase("int M() { throw new Exception(); }")]
    [TestCase("void M() { throw new Exception(); }")]
    [TestCase("Option<int> M() { throw new Exception(); }")]
    [TestCase("Result<int, MyError> M() { throw new Exception(); }")]
    [TestCase("Task<Result> M() { throw new Exception(); }")]
    [TestCase("async Task M() { await Task.Yield(); throw new Exception(); }")]
    [TestCase("async Task<int> M() { await Task.Yield(); throw new Exception(); }")]
    [TestCase("Result M() { Action a = () => { throw new Exception(); }; a(); return default(Result); }")]
    [TestCase("Result M() { void Local() { throw new Exception(); } Local(); return default(Result); }")]
    [TestCase("Result M() { Func<int> f = () => { throw new Exception(); }; return default(Result); }")]
    public async Task IgnoresOtherShapes(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new ThrowInResultMethodAnalyzer(), $$"""
            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception) => new MyError();
            }

            public class MyException : Exception;

            public class MyArgumentException : ArgumentException;

            public class Usage
            {
                {{member}}
            }
            """);
}
