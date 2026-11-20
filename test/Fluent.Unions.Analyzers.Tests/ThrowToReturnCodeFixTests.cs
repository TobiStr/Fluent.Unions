using Fluent.Unions.Analyzers.CodeFixes;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class ThrowToReturnCodeFixTests
{
    [Test]
    public async Task ReturnsTheExceptionInsteadOfThrowingIt()
    {
        var fixedSource = await Fix("""
            public class Usage
            {
                public Result<int> Load(int id)
                {
                    if (id < 0)
                        throw new InvalidOperationException("negative");

                    return 1;
                }
            }
            """);

        Assert.That(fixedSource, Is.EqualTo("""
            public class Usage
            {
                public Result<int> Load(int id)
                {
                    if (id < 0)
                        return new InvalidOperationException("negative");

                    return 1;
                }
            }
            """));
    }

    [Test]
    public async Task FixesAnAsyncMethod()
    {
        var fixedSource = await Fix("""
            public class Usage
            {
                public async Task<Result<int>> LoadAsync()
                {
                    await Task.Yield();
                    throw new System.IO.IOException("gone");
                }
            }
            """);

        Assert.That(fixedSource, Does.Contain("return new System.IO.IOException(\"gone\");"));
        Assert.That(fixedSource, Does.Not.Contain("throw"));
    }

    [Test]
    public async Task FixesALambda()
    {
        var fixedSource = await Fix("""
            public class Usage
            {
                public Func<Result> Create() => () => { throw new Exception("x"); };
            }
            """);

        Assert.That(fixedSource, Is.EqualTo("""
            public class Usage
            {
                public Func<Result> Create() => () => { return new Exception("x"); };
            }
            """));
    }

    [Test]
    public async Task KeepsTheComments()
    {
        var fixedSource = await Fix("""
            public class Usage
            {
                public Result Run()
                {
                    // fail fast
                    throw new Exception("x"); // done
                }
            }
            """);

        Assert.That(fixedSource, Is.EqualTo("""
            public class Usage
            {
                public Result Run()
                {
                    // fail fast
                    return new Exception("x"); // done
                }
            }
            """));
    }

    [Test]
    public void ProvidesABatchFixer()
    {
        var provider = new ThrowToReturnCodeFix();

        Assert.That(provider.FixableDiagnosticIds, Is.EqualTo(new[] { ThrowInResultMethodAnalyzer.DiagnosticId }));
        Assert.That(provider.GetFixAllProvider(), Is.Not.Null);
    }

    private static Task<string> Fix(string source) =>
        AnalyzerTest.Fix(new ThrowInResultMethodAnalyzer(), new ThrowToReturnCodeFix(), source);
}
