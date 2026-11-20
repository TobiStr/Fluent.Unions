using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class DefaultResultAnalyzerTests
{
    [TestCase("Result M() => default(Result);", "Result")]
    [TestCase("Result<int> M() => default(Result<int>);", "Result<int>")]
    [TestCase("Result<int, MyError> M() => default(Result<int, MyError>);", "Result<int, MyError>")]
    [TestCase("void M() { Result<int> r = default; }", "Result<int>")]
    [TestCase("void M() { Result r = default; }", "Result")]
    [TestCase("Result<int> M() { return default; }", "Result<int>")]
    [TestCase("Result<int> M() => default;", "Result<int>")]
    [TestCase("Result<int> F = default;", "Result<int>")]
    [TestCase("Result<int> P { get; } = default;", "Result<int>")]
    [TestCase("Result<string> M(bool b) => b ? default : Result<string>.Ok(\"x\");", "Result<string>")]
    [TestCase("Func<Result<int>> F = () => default;", "Result<int>")]
    [TestCase("Task<Result<int>> M() => Task.FromResult<Result<int>>(default);", "Result<int>")]
    [TestCase("void M() { var r = new Result<int>(); }", "Result<int>")]
    [TestCase("void M() { var r = new Result(); }", "Result")]
    [TestCase("void M() { Result<int> r = new(); }", "Result<int>")]
    [TestCase("void M() { var r = new Result<int, MyError>(); }", "Result<int, MyError>")]
    [TestCase("void M(Result r = default) { }", "Result")]
    [TestCase("void M(Result<int> r = default) { }", "Result<int>")]
    public async Task ReportsDefaultResults(string member, string type)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0007" }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"A default '{type}' is an error; create it with a value or an error"));
    }

    [Test]
    public async Task ReportsEveryDefaultOnItsOwn()
    {
        var diagnostics = await Analyze("Result<int> M() { Result<int> a = default; return default(Result<int>); }");

        Assert.That(diagnostics, Has.Length.EqualTo(2));
    }

    [Test]
    public async Task DoesNotReportTheOmittedArgumentOfAnOptionalParameter()
    {
        var diagnostics = await Analyze("void M(Result r = default) { } void Caller() { M(); M(default); }");

        Assert.That(diagnostics, Has.Length.EqualTo(2));
    }

    [TestCase("Option<int> M() => default;")]
    [TestCase("Option<int> M() => default(Option<int>);")]
    [TestCase("Loadable<int> M() => default;")]
    [TestCase("void M() { var o = new Option<int>(); }")]
    [TestCase("void M() { var l = new Loadable<int>(); }")]
    [TestCase("int M() => default;")]
    [TestCase("string? M() => default;")]
    [TestCase("T M<T>() => default!;")]
    [TestCase("Result<T> M<T>() => Result<T>.Ok(default!);")]
    [TestCase("Result<int>? M() => default;")]
    [TestCase("Result<int> M() => Result<int>.Ok(1);")]
    [TestCase("Result<int> M() => Result.From(1);")]
    [TestCase("Result<int> M() => new Result<int>(1);")]
    [TestCase("Result<int> M() => new Result<int>(new Exception());")]
    [TestCase("Result M() => Result.Ok;")]
    [TestCase("CustomResult M() => default;")]
    [TestCase("void M(Result r) { }")]
    [TestCase("void M(Result? r = null) { }")]
    [TestCase("void M(int x = default) { }")]
    [TestCase("object M() => new Order();")]
    public async Task IgnoresResultsThatAreNotDefault(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new DefaultResultAnalyzer(), $$"""
            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception) => new();
            }

            public class Order { }

            public struct CustomResult : IErrorResult<CustomResult, Exception>
            {
                public object? Value => null;
                public static CustomResult FromError(Exception error) => default;
                public static Exception FromException(Exception exception) => exception;
            }

            public class Usage
            {
                {{member}}
            }
            """);
}
