using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class UncheckedAccessAnalyzerTests
{
    [TestCase("int M(Result<int> r) => r.GetValue();", "GetValue", "is an error")]
    [TestCase("Exception M(Result<int> r) => r.GetError();", "GetError", "has a value")]
    [TestCase("Exception M(Result r) => r.GetError();", "GetError", "has a value")]
    [TestCase("int M() => Load().GetValue();", "GetValue", "is an error")]
    [TestCase("int M() => Field.GetValue();", "GetValue", "is an error")]
    [TestCase("int M() => Prop.GetValue();", "GetValue", "is an error")]
    [TestCase("int M(Result<int> r) { if (r.IsSuccess) return 0; return r.GetValue(); }", "GetValue", "is an error")]
    [TestCase("int M(Result<int> r) { if (r.IsError) return r.GetValue(); return 0; }", "GetValue", "is an error")]
    [TestCase("Exception M(Result<int> r) => r.IsSuccess ? r.GetError() : null!;", "GetError", "has a value")]
    [TestCase("int M(Result<int> a, Result<int> b) => b.IsSuccess ? a.GetValue() : 0;", "GetValue", "is an error")]
    [TestCase("int M() => Field.IsSuccess ? Load().GetValue() : 0;", "GetValue", "is an error")]
    [TestCase("int M(Result<int> r, bool x) { if (x && r.IsError) return 0; return r.GetValue(); }", "GetValue", "is an error")]
    [TestCase("int M(Result<int> r, bool x) { if (x || r.IsSuccess) return r.GetValue(); return 0; }", "GetValue", "is an error")]
    [TestCase("int M(Result<int> r) { if (r.IsError) Console.WriteLine(); return r.GetValue(); }", "GetValue", "is an error")]
    [TestCase("int M(Result<int> r) { var value = r.GetValue(); if (r.IsError) return 0; return value; }", "GetValue", "is an error")]
    public async Task ReportsUncheckedAccess(string member, string name, string description)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { UncheckedAccessAnalyzer.DiagnosticId }));
        Assert.That(diagnostics[0].GetMessage(), Does.StartWith($"'{name}' throws when the result {description}"));
    }

    [TestCase("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => r.IsError ? 0 : r.GetValue();")]
    [TestCase("Exception M(Result<int> r) => r.IsError ? r.GetError() : null!;")]
    [TestCase("Exception M(Result<int> r) => r.IsSuccess ? null! : r.GetError();")]
    [TestCase("int M(Result<int> r) => r.TryGetValue(out _) ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => !r.TryGetError(out _) ? r.GetValue() : 0;")]
    [TestCase("Exception M(Result<int> r) => r.TryGetError(out _) ? r.GetError() : null!;")]
    [TestCase("int M(Result<int> r) => !r.IsSuccess ? 0 : r.GetValue();")]
    [TestCase("int M(Result<int> r) => !!r.IsSuccess ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => r.IsSuccess == true ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => r.IsError == false ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => false != r.IsError ? 0 : r.GetValue();")]
    [TestCase("int M(Result<int> r) => r.IsSuccess is true ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => r.IsError is not true ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) => r.IsError is false ? r.GetValue() : 0;")]
    [TestCase("int M(Result<int> r) { if (r.IsSuccess) { return r.GetValue(); } else { return 0; } }")]
    [TestCase("int M(Result<int> r) { if (r.IsError) { return 0; } else { return r.GetValue(); } }")]
    [TestCase("int M(Result<int> r) { if (r.IsError) return 0; return r.GetValue(); }")]
    [TestCase("int M(Result<int> r) { if (!r.IsSuccess) throw new Exception(); return r.GetValue(); }")]
    [TestCase("int M(Result<int> r) { if (!r.TryGetValue(out _)) return 0; return r.GetValue(); }")]
    [TestCase("int M(Result<int> r) { if (r.IsError) { Console.WriteLine(); return 0; } var value = r.GetValue(); return value; }")]
    [TestCase("int M(Result<int> r) { if (r.IsError) return 0; { return r.GetValue(); } }")]
    [TestCase("Exception M(Result<int> r) { if (r.IsSuccess) return null!; return r.GetError(); }")]
    [TestCase("int M(List<Result<int>> rs) { var sum = 0; foreach (var r in rs) { if (r.IsError) continue; sum += r.GetValue(); } return sum; }")]
    [TestCase("int M(List<Result<int>> rs) { var sum = 0; foreach (var r in rs) { if (r.IsError) break; sum += r.GetValue(); } return sum; }")]
    [TestCase("bool M(Result<int> r) => r.IsSuccess && r.GetValue() > 0;")]
    [TestCase("bool M(Result<int> r) => r.IsError || r.GetValue() > 0;")]
    [TestCase("bool M(Result<int> r, bool x) => x && r.IsSuccess && r.GetValue() > 0;")]
    [TestCase("int M(Result<int> r, bool x) { if (x || r.IsError) return 0; return r.GetValue(); }")]
    [TestCase("int M(Result<int> r, bool x) { if (x) return 0; if (r.IsError) return 1; return r.GetValue(); }")]
    [TestCase("int M(Result<int> r) { if (r.IsError) return 0; Func<int> f = () => r.GetValue(); return f(); }")]
    [TestCase("int M(Result<int> r) => r.IsSuccess ? new Func<int>(() => r.GetValue())() : 0;")]
    [TestCase("int M() => Field.IsSuccess ? Field.GetValue() : 0;")]
    [TestCase("int M() => this.Field.IsSuccess ? Field.GetValue() : 0;")]
    [TestCase("int M() => Prop.IsSuccess ? Prop.GetValue() : 0;")]
    [TestCase("static int M() => StaticField.IsSuccess ? StaticField.GetValue() : 0;")]
    [TestCase("Exception M(Result r) => r.IsError ? r.GetError() : null!;")]
    [TestCase("int M(Option<int> o) => o.GetValue();")]
    [TestCase("int M(Mine m) => m.GetValue();")]
    [TestCase("int M(Result<int> r) => r.GetValueOrDefault();")]
    public async Task IgnoresGuardedOrUnrelatedAccess(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new UncheckedAccessAnalyzer(), $$"""
            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception) => new MyError();
            }

            public class Mine
            {
                public int GetValue() => 0;
            }

            public class Usage
            {
                static Result<int> StaticField = default;
                Result<int> Field = default;
                Result<int> Prop => default;

                Result<int> Load() => default;

                {{member}}
            }
            """);
}
