using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Generators.Tests;

public partial class GeneratorTests
{
    [TestCase("public readonly partial record struct Result<T>;", TestName = "Record struct")]
    [TestCase("public readonly ref partial struct Result<T>;", TestName = "Ref struct")]
    [TestCase("public readonly partial struct Result<T> where T : allows ref struct;", TestName = "Allows ref struct")]
    [TestCase("file readonly partial struct Result<T>;", TestName = "File type")]
    public void UnsupportedShapeOfAModernStructIsReported(string declaration)
    {
        var (output, diagnostics) = Run(Domain + "[FluentResult<DomainException>] " + declaration);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101" }));
        Assert.That(Errors(output).Where(error => error.Contains("g.cs")), Is.Empty);
    }

    [TestCase("public partial struct Result<T>;", "readonly")]
    [TestCase("public readonly struct Result<T>;", "partial")]
    [TestCase("public readonly partial record struct Result<T>;", "record struct")]
    [TestCase("public readonly ref partial struct Result<T>;", "ref struct")]
    [TestCase("public readonly partial struct Result<T> where T : allows ref struct;", "ref structs")]
    [TestCase("file readonly partial struct Result<T>;", "file")]
    [TestCase("public readonly partial struct Result<T, TOther>;", "one type parameter")]
    [TestCase("public readonly partial struct Result<T> where T : class;", "constraints")]
    public void UnsupportedShapeNamesTheReason(string declaration, string reason)
    {
        var (_, diagnostics) = Run(Domain + "[FluentResult<DomainException>] " + declaration);

        Assert.That(diagnostics.Single().GetMessage(), Does.Contain(reason));
        Assert.That(diagnostics.Single().Descriptor.HelpLinkUri, Does.EndWith("#flu0101"));
    }

    [Test]
    public void ReadOnlyOnAnotherPartIsEnough()
    {
        var (output, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>] public partial struct Result<T>;
            public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void CompanionIsGeneratedWhenNoneIsDeclared()
    {
        var (output, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>] public readonly partial struct Result<T>;

            public static class Use
            {
                public static Result<int> Number() => Result.From(1);
                public static Result Done() => Result.Ok;
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void DeclaredCompanionIsUsedInsteadOfAGeneratedOne()
    {
        var (output, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>] public readonly partial struct Result<T>;
            [FluentResult<DomainException>] public readonly partial struct Result;

            public static class Use
            {
                public static Result<int> Number() => Result.From(1);
                public static Result Done() => Result.Ok.Chain(() => Result.Ok);
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void DeclaredCompanionWithAnotherErrorTypeIsReported()
    {
        var (_, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>] public readonly partial struct Result<T>;
            [FluentResult<System.Exception>] public readonly partial struct Result;
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101", "FLU0101" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("same error type"));
    }

    [Test]
    public void ResultWithoutAValueNeedsAGenericResultNextToIt()
    {
        var (output, diagnostics) = Run(Domain + "[FluentResult<DomainException>] public readonly partial struct Result;");

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("Result<T>"));
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void LessAccessibleErrorTypeIsReported()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            internal class Hidden(string message) : Exception(message), IFromException<Hidden>
            {
                public static Hidden FromException(Exception exception) => new(exception.Message);
            }

            [FluentResult<Hidden>] public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("at least as accessible"));
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void InternalResultMayUseAnInternalErrorType()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            internal class Hidden(string message) : Exception(message), IFromException<Hidden>
            {
                public static Hidden FromException(Exception exception) => new(exception.Message);
            }

            [FluentResult<Hidden>] internal readonly partial struct Result<T>;

            internal static class Use
            {
                public static Result<int> Length(string text) => Result.From(text).Select(t => t.Length);
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void ErrorCaseLessAccessibleThanTheResultGetsNoConversion()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            internal sealed record Hidden;
            public sealed record Shown;

            public union AppError(Shown, Hidden) : IFromException<AppError>
            {
                public static AppError FromException(Exception exception) => new Shown();
            }

            [FluentResult<AppError>] public readonly partial struct Result<T>;

            public static class Use
            {
                public static Result<int> Find() => new Shown();
            }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0103" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("less accessible"));
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void ObjectCaseOfAnErrorUnionGetsNoConversion()
    {
        var (output, diagnostics) = Run(ObjectCaseSource);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0103" }));
        Assert.That(diagnostics[0].Descriptor.HelpLinkUri, Does.EndWith("#flu0103"));
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void KeywordIdentifiersAreEscaped()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            namespace Shop.@event;

            public class DomainException(string message) : Exception(message), IFromException<DomainException>
            {
                public static DomainException FromException(Exception exception) => new(exception.Message);
            }

            [FluentResult<DomainException>] public readonly partial struct @class<T>;

            public static class Use
            {
                public static @class<int> Length(string text) => @class<string>.Ok(text).Select(t => t.Length);
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void GeneratedOperatorClassesDoNotCollideWithUserClasses()
    {
        var (output, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>] public readonly partial struct Result<T>;

            public static class ResultExtensions { public static int Twice(this int value) => value * 2; }
            public static partial class ResultTaskExtensions { public static int Thrice(this int value) => value * 3; }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
        Assert.That(output.GetTypeByMetadataName("Shop.ResultOperators"), Is.Not.Null);
        Assert.That(output.GetTypeByMetadataName("Shop.ResultTaskOperators"), Is.Not.Null);
        Assert.That(output.GetTypeByMetadataName("Shop.ResultQueryOperators"), Is.Not.Null);
    }

    [TestCase("public static Plain FromException(string text) => new(text);", TestName = "Wrong parameter")]
    [TestCase("private static Plain FromException(Exception exception) => new(exception.Message);", TestName = "Private")]
    [TestCase("public static Exception FromException(Exception exception) => exception;", TestName = "Wrong return type")]
    [TestCase("public Plain FromException(Exception exception) => new(exception.Message);", TestName = "Instance method")]
    public void UnusableFromExceptionIsReported(string method)
    {
        var (output, diagnostics) = Run($$"""
            using System;
            using Fluent.Unions;

            public class Plain(string message) : Exception(message);

            [FluentResult<Plain>] public readonly partial struct Result<T> { {{method}} }
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0102" }));
        Assert.That(diagnostics[0].Descriptor.HelpLinkUri, Does.EndWith("#flu0102"));
        Assert.That(Errors(output).Where(error => error.Contains("g.cs")), Is.Empty);
    }

    [Test]
    public void ErrorTypeThatImplementsTheContractForAnotherTypeIsReported()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            public class Other(string message) : Exception(message), IFromException<Other>
            {
                public static Other FromException(Exception exception) => new(exception.Message);
            }

            public class Mine(string message) : Exception(message), IFromException<Other>
            {
                public static Other FromException(Exception exception) => new(exception.Message);
            }

            [FluentResult<Mine>] public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0102" }));
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void InternalFromExceptionIsUsed()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;

            public class Plain(string message) : Exception(message);

            [FluentResult<Plain>]
            public readonly partial struct Result<T>
            {
                internal static Plain FromException(Exception exception) => new(exception.Message);
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void ExplicitInterfaceImplementationOfFromExceptionIsUsed()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            public class Plain(string message) : Exception(message), IFromException<Plain>
            {
                static Plain IFromException<Plain>.FromException(Exception exception) => new(exception.Message);
            }

            [FluentResult<Plain>] public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void UserDeclaredConstructorIsKept()
    {
        var (output, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>]
            public readonly partial struct Result<T>
            {
                public Result(T value)
                {
                    _value = value;
                    _error = default!;
                    _state = value is null ? (byte)0 : (byte)1;
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void ResultNamedLikeAGeneratedMemberIsReported()
    {
        var (output, diagnostics) = Run(Domain + "[FluentResult<DomainException>] public readonly partial struct Ok<T>;");

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("'Ok'"));
        Assert.That(Errors(output), Is.Empty);
    }

    [Test]
    public void OperatorTemplatesAreTheSameForEveryCompilationOfTheLibrary()
    {
        var compilation = Compilation("");
        var edited = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Other;", ParseOptions));

        Assert.That(OperatorCache.Get(edited), Is.EqualTo(OperatorCache.Get(compilation)));
        Assert.That(OperatorCache.Get(compilation).Length, Is.GreaterThan(0));
    }

    [Test]
    public void ProjectWithoutADeclarationGeneratesNothing()
    {
        var driver = CSharpGeneratorDriver.Create([new FluentResultGenerator().AsSourceGenerator()], parseOptions: ParseOptions);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(Compilation("public class Unrelated;"));

        Assert.That(driver.GetRunResult().GeneratedTrees, Is.Empty);
    }

    private const string ObjectCaseSource = """
        using System;
        using Fluent.Unions;
        using Fluent.Unions.Abstractions;

        public union AppError(object, int) : IFromException<AppError>
        {
            public static AppError FromException(Exception exception) => new AppError((object)exception);
        }

        [FluentResult<AppError>] public readonly partial struct Result<T>;
        """;

    private static IEnumerable<string> Errors(Compilation output) =>
        output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString());
}
