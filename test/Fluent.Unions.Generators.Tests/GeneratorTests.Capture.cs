using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Generators.Tests;

public partial class GeneratorTests
{
    [Test]
    public void DefaultCaptureEmitsNoCapturesMember()
    {
        var (output, diagnostics) = Run(Domain + "[FluentResult<DomainException>] public readonly partial struct Result<T>;");

        Assert.That(diagnostics, Is.Empty);
        Assert.That(GeneratedText(output), Does.Not.Contain(".Captures("));
    }

    [Test]
    public void DeclaredCaptureEmitsTheErrorTypeAndTheExceptionCases()
    {
        var (output, diagnostics) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            namespace Shop;

            public sealed class NotFound(string message) : Exception(message);
            public sealed record Unexpected(Exception Exception);

            public union ShopError(NotFound, Unexpected) : IFromException<ShopError>
            {
                public static ShopError FromException(Exception exception) => new Unexpected(exception);
            }

            [FluentResult<ShopError>(Capture = ExceptionCapture.Declared)] public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(ErrorsOf(output), Is.Empty);
        Assert.That(GeneratedText(output), Does.Contain("Captures(global::System.Exception exception) => exception is global::Shop.NotFound;"));
    }

    [Test]
    public void DeclaredCaptureWithoutExceptionCasesCapturesNothing()
    {
        var (output, _) = Run("""
            using System;
            using Fluent.Unions;
            using Fluent.Unions.Abstractions;

            namespace Shop;

            public sealed record Missing;

            public union ShopError(Missing) : IFromException<ShopError>
            {
                public static ShopError FromException(Exception exception) => new Missing();
            }

            [FluentResult<ShopError>(Capture = ExceptionCapture.Declared)] public readonly partial struct Result<T>;
            """);

        Assert.That(ErrorsOf(output), Is.Empty);
        Assert.That(GeneratedText(output), Does.Contain("Captures(global::System.Exception exception) => false;"));
    }

    [Test]
    public void PropagateEmitsADenyList()
    {
        var (output, diagnostics) = Run(Domain +
            "[FluentResult<DomainException>(Propagate = [typeof(NullReferenceException), typeof(ArgumentException)])] public readonly partial struct Result<T>;");

        Assert.That(diagnostics, Is.Empty);
        Assert.That(ErrorsOf(output), Is.Empty);
        Assert.That(GeneratedText(output), Does.Contain("=> exception is not (global::System.NullReferenceException or global::System.ArgumentException);"));
    }

    [Test]
    public void DeclaredCaptureAndPropagateCombine()
    {
        var (output, _) = Run(Domain +
            "[FluentResult<DomainException>(Capture = ExceptionCapture.Declared, Propagate = [typeof(DomainException)])] public readonly partial struct Result<T>;");

        Assert.That(GeneratedText(output), Does.Contain("=> exception is global::Shop.DomainException && exception is not (global::Shop.DomainException);"));
    }

    [Test]
    public void CompanionGetsTheSamePolicy()
    {
        var (output, _) = Run(Domain +
            "[FluentResult<DomainException>(Propagate = [typeof(NullReferenceException)])] public readonly partial struct Result<T>;");

        var text = GeneratedText(output);
        Assert.That(text, Does.Contain("IErrorResult<Result<T>, global::Shop.DomainException>.Captures("));
        Assert.That(text, Does.Contain("IErrorResult<Result, global::Shop.DomainException>.Captures("));
    }

    [Test]
    public void DeclaredCompanionWithAnotherPolicyIsReported()
    {
        var (_, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>(Propagate = [typeof(NullReferenceException)])] public readonly partial struct Result<T>;
            [FluentResult<DomainException>] public readonly partial struct Result;
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0101", "FLU0101" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("same Capture and Propagate settings"));
    }

    [Test]
    public void DeclaredCaptureOnExceptionIsReported()
    {
        var (_, diagnostics) = Run("""
            using System;
            using Fluent.Unions;

            [FluentResult<Exception>(Capture = ExceptionCapture.Declared)] public readonly partial struct Result<T>;
            """);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0104" }));
    }

    [Test]
    public void PropagatedTypeThatIsNoExceptionIsReported()
    {
        var (_, diagnostics) = Run(Domain + "[FluentResult<DomainException>(Propagate = [typeof(string)])] public readonly partial struct Result<T>;");

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0105" }));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("'string' in Propagate of 'Result'"));
    }

    private static string GeneratedText(Compilation output) =>
        string.Join("\n", output.SyntaxTrees.Skip(1).Select(tree => tree.ToString()));

    private static IEnumerable<string> ErrorsOf(Compilation output) =>
        output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString());
}
