using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports a result created with <c>default</c> or its parameterless constructor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefaultResultAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.DefaultResult);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var types = KnownTypes.Create(start.Compilation);

            if (types is null)
                return;

            start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.DefaultValue);
            start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.ObjectCreation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var operation = context.Operation;

        if (operation.IsImplicit || operation is IObjectCreationOperation { Arguments.IsEmpty: false })
            return;

        if (types.IsLibraryResult(operation.Type))
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.DefaultResult,
                operation.Syntax.GetLocation(),
                operation.Type!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
