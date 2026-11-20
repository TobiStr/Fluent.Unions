using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports an error factory that throws or returns null, which lets the failure escape the result.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ErrorFactoryAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "FLU0009";

    private static readonly DiagnosticDescriptor Rule = Descriptors.ThrowingErrorFactory;

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

            if (types is not null)
                start.RegisterOperationBlockAction(block => Analyze(block, types));
        });
    }

    private static void Analyze(OperationBlockAnalysisContext context, KnownTypes types)
    {
        if (context.OwningSymbol is not IMethodSymbol { IsStatic: true } method
            || !IsFactoryName(method.Name)
            || !IsErrorFactory(method, types))
            return;

        foreach (var block in context.OperationBlocks)
            Visit(context, block, method);
    }

    private static bool IsFactoryName(string name) =>
        name.EndsWith("FromException", StringComparison.Ordinal) || name.EndsWith("FromError", StringComparison.Ordinal);

    private static bool IsErrorFactory(IMethodSymbol method, KnownTypes types)
    {
        var type = method.ContainingType;

        return type.AllInterfaces
            .Where(i => KnownTypes.Is(i, types.FromException) || KnownTypes.Is(i, types.ErrorResult))
            .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .Any(member => member is { IsStatic: true, IsAbstract: true }
                && SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member), method));
    }

    private static void Visit(OperationBlockAnalysisContext context, IOperation operation, IMethodSymbol method)
    {
        switch (operation)
        {
            case IAnonymousFunctionOperation or ILocalFunctionOperation:
                return;

            case IThrowOperation:
                Report(context, operation, method, "throw");
                break;

            case IReturnOperation { ReturnedValue: { } returned } when method.ReturnType.IsReferenceType && IsNull(returned):
                Report(context, returned, method, "return null");
                break;
        }

        foreach (var child in operation.ChildOperations)
            Visit(context, child, method);
    }

    private static bool IsNull(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;

        return operation is IDefaultValueOperation { Type.IsReferenceType: true }
            || operation.ConstantValue is { HasValue: true, Value: null };
    }

    private static string ShortName(IMethodSymbol method) =>
        method.Name.Substring(method.Name.LastIndexOf('.') + 1);

    private static void Report(OperationBlockAnalysisContext context, IOperation operation, IMethodSymbol method, string action) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), ShortName(method), action));
}
