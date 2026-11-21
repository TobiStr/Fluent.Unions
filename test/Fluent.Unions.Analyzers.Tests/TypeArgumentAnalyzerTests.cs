using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class TypeArgumentAnalyzerTests
{
    private const string Overlap = "FLU0004";
    private const string Nullable = "FLU0005";

    [TestCase("Result<object>", "'object'", "'Exception'")]
    [TestCase("Result<Exception>", "'Exception'", "'Exception'")]
    [TestCase("Result<IOException>", "'IOException'", "'Exception'")]
    [TestCase("Result<ISerializable>", "'ISerializable'", "'Exception'")]
    [TestCase("Option<None>", "'None'", "'None'")]
    [TestCase("Option<object>", "'object'", "'None'")]
    [TestCase("Option<ValueType>", "'ValueType'", "'None'")]
    [TestCase("Loadable<Exception>", "'Exception'", "'Exception'")]
    [TestCase("Loadable<NotLoaded>", "'NotLoaded'", "'NotLoaded'")]
    [TestCase("Loadable<Loading>", "'Loading'", "'Loading'")]
    [TestCase("Loadable<object>", "'object'", "'Exception'")]
    public async Task ReportsOverlappingTypeArguments(string type, string argument, string other)
    {
        var diagnostics = await Analyze($"{type} F;");

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { Overlap }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"'{type}' cannot tell its cases apart, because the type argument {argument} overlaps the case {other}"));
    }

    [Test]
    public async Task ReportsTheTypeNameOfTheUnion()
    {
        var diagnostics = await Analyze("Fluent.Unions.Result<object> F;");

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo("Result<object>"));
    }

    [TestCase("Result<object> M() => default;")]
    [TestCase("void M() { Result<object> r = default; }")]
    [TestCase("void M() { var r = new Result<object>(); }")]
    [TestCase("void M() { var r = Result<object>.Ok(1); }")]
    [TestCase("void M(Result<object> r) { }")]
    [TestCase("Task<Result<object>> M() => default!;")]
    [TestCase("List<Option<object>> F = new();")]
    [TestCase("Fluent.Unions.Result<object> F;")]
    [TestCase("Type F = typeof(Result<object>);")]
    public async Task ReportsEveryShapeAUnionTypeIsWrittenIn(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id).Distinct(), Is.EqualTo(new[] { Overlap }));
    }

    [TestCase("Result<int> F;")]
    [TestCase("Result<string> F;")]
    [TestCase("Option<int> F;")]
    [TestCase("Option<string> F;")]
    [TestCase("Loadable<int> F;")]
    [TestCase("Result<Result<int>> F;")]
    [TestCase("Result<List<object>> F;")]
    [TestCase("Result<int[]> F;")]
    [TestCase("Result F;")]
    [TestCase("class Box<T> { Result<T> F; Option<T> G; }")]
    [TestCase("Custom<object> F;")]
    [TestCase("Type F = typeof(Result<>);")]
    public async Task IgnoresDistinctTypeArguments(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("Result<string?> F;", "Result<string?>")]
    [TestCase("Option<int?> F;", "Option<int?>")]
    [TestCase("Loadable<Order?> F;", "Loadable<Order?>")]
    [TestCase("Result<int?> M() => default;", "Result<int?>")]
    [TestCase("void M() { Option<string?> o = default; }", "Option<string?>")]
    public async Task ReportsNullableTypeArguments(string member, string type)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { Nullable }));
        Assert.That(diagnostics[0].GetMessage(), Does.StartWith($"'{type}' cannot hold null"));
    }

    [Test]
    public async Task ReportsBothRulesForANullableOverlap()
    {
        var diagnostics = await Analyze("Result<object?> F;");

        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { Overlap, Nullable }));
    }

    [TestCase("Result<int> F;")]
    [TestCase("Option<string> F;")]
    [TestCase("class Box<T> where T : class { Option<T?> F; Result<T?> G; }")]
    [TestCase("class Box<T> { Option<T?> F; }")]
    [TestCase("#nullable disable\nResult<string> F;")]
    [TestCase("#nullable disable\nResult<string?> F;")]
    [TestCase("Custom<string?> F;")]
    public async Task IgnoresTypeArgumentsThatAreNotNullable(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new TypeArgumentAnalyzer(), $$"""
            using System.IO;
            using System.Runtime.Serialization;

            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception) => new();
            }

            public class DerivedError : MyError { }

            public struct StructError : IFromException<StructError>, IComparable
            {
                public static StructError FromException(Exception exception) => default;
                public int CompareTo(object? other) => 0;
            }

            public class Order { }

            public class OrderError : IFromException<OrderError>
            {
                public static OrderError FromException(Exception exception) => new();
            }

            public class Custom<T> { }

            public class Usage
            {
                {{member}}
            }
            """);
}
