using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports a throw statement in a function that returns a result and so promises to report failures in it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThrowInResultMethodAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "FLU0012";

    private static readonly DiagnosticDescriptor Rule = Descriptors.ThrowInResultMethod;

    private static readonly string[] GuardExceptions =
    [
        "System.ArgumentException",
        "System.ObjectDisposedException",
        "System.NotImplementedException",
        "System.NotSupportedException",
        "System.Diagnostics.UnreachableException",
        "System.OperationCanceledException"
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var types = KnownTypes.Create(start.Compilation);

            if (types?.Exception is null)
                return;

            var guards = GuardExceptions
                .Select(start.Compilation.GetTypeByMetadataName)
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();

            start.RegisterOperationAction(operation => Analyze(operation, types, guards), OperationKind.Throw);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types, ImmutableArray<INamedTypeSymbol> guards)
    {
        var statement = (IThrowOperation)context.Operation;

        if (statement.Syntax is not ThrowStatementSyntax || statement.Exception is not { } exception)
            return;

        if (!IsReportableException(StripImplicitConversions(exception).Type, types, guards))
            return;

        var (function, name) = EnclosingFunction(statement, context.ContainingSymbol);

        if (function is not null && ReturnsResult(function, types))
            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.Syntax.GetLocation(), name));
    }

    private static IOperation StripImplicitConversions(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;

        return operation;
    }

    private static bool IsReportableException(ITypeSymbol? type, KnownTypes types, ImmutableArray<INamedTypeSymbol> guards)
    {
        var isException = false;

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, types.Exception))
                isException = true;

            if (guards.Any(guard => SymbolEqualityComparer.Default.Equals(current, guard)))
                return false;
        }

        return isException;
    }

    private static (IMethodSymbol? Function, string Name) EnclosingFunction(IOperation operation, ISymbol containingSymbol)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case IAnonymousFunctionOperation lambda:
                    return (lambda.Symbol, "lambda");
                case ILocalFunctionOperation localFunction:
                    return (localFunction.Symbol, localFunction.Symbol.Name);
            }
        }

        return containingSymbol is IMethodSymbol method ? (method, method.Name) : (null, string.Empty);
    }

    private static bool ReturnsResult(IMethodSymbol function, KnownTypes types)
    {
        var returnType = function.IsAsync ? types.UnwrapTask(function.ReturnType) : function.ReturnType;

        return returnType is not null && (KnownTypes.Is(returnType, types.Result) || KnownTypes.Is(returnType, types.ResultOfT));
    }
}
