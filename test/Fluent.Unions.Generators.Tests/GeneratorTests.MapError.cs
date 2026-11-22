using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Generators.Tests;

public partial class GeneratorTests
{
    private const string TwoFamilies = Domain + """
        public class ApiException(string message) : Exception(message), IFromException<ApiException>
        {
            public static ApiException FromException(Exception exception) => new(exception.Message);
        }

        [FluentResult<DomainException>] public readonly partial struct Result<T>;

        [FluentResult<ApiException>] public readonly partial struct ApiResult<T>;

        """;

    [Test]
    public void MapErrorConvertsBetweenDeclaredTypes()
    {
        var (output, diagnostics) = Run(TwoFamilies + """
            public static class Use
            {
                public static ApiResult<int> ToApi(Result<int> result) => result.MapError<ApiResult<int>, ApiException>(e => new ApiException(e.Message));

                public static ApiResult ToApi(Result result) => result.MapError<ApiResult, ApiException>(e => new ApiException(e.Message));
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(ErrorsOf(output), Is.Empty);
    }

    [Test]
    public void MapErrorToAnotherValueTypeDoesNotCompile()
    {
        var (output, _) = Run(TwoFamilies + """
            public static class Use
            {
                public static ApiResult<string> ToApi(Result<int> result) => result.MapError<ApiResult<string>, ApiException>(e => new ApiException(e.Message));
            }
            """);

        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id), Does.Contain("CS0315").Or.Contain("CS0311"));
    }

    [Test]
    public void DeclaredMapErrorMemberIsKept()
    {
        var (output, diagnostics) = Run(Domain + """
            [FluentResult<DomainException>]
            public readonly partial struct Result<T>
            {
                public string MapError(Func<DomainException, string> map) => "own";
            }
            """);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(ErrorsOf(output), Is.Empty);
    }

    [Test]
    public void MapErrorTypeParametersDoNotCollideWithTheResultTypeParameter()
    {
        var (output, diagnostics) = Run(Domain + "[FluentResult<DomainException>] public readonly partial struct Result<TTarget>;");

        Assert.That(diagnostics, Is.Empty);
        Assert.That(ErrorsOf(output), Is.Empty);
        Assert.That(GeneratedText(output), Does.Contain("public TConverted MapError<TConverted, TConvertedError>"));
    }
}
