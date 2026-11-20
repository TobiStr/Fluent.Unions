using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports a <c>Select</c> whose selector returns a result or an option, which nests it inside another one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NestedSelectAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.NestedSelect);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var types = KnownTypes.Create(start.Compilation);

            if (types is not null)
                start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        var replacement = method.Name switch
        {
            "Select" => "Chain",
            "SelectAwait" => "ChainAwait",
            _ => null
        };

        if (replacement is null || invocation.Type is null || !types.IsLibraryMethod(method))
            return;

        var type = types.UnwrapTask(invocation.Type) ?? invocation.Type;

        if (!IsNested(type, types)|| !types.HasCounterpart(method, replacement))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.NestedSelect,
            AsyncLambdaAnalyzer.OperatorLocation(invocation),
            ImmutableDictionary<string, string?>.Empty.Add(Descriptors.ReplacementProperty, replacement),
            method.Name,
            type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            replacement));
    }

    private static bool IsNested(ITypeSymbol type, KnownTypes types) =>
        type is INamedTypeSymbol { TypeArguments.Length: > 0 } named
        && (types.IsLibraryResult(named) && types.IsLibraryResult(named.TypeArguments[0])
            || KnownTypes.Is(named, types.Option) && KnownTypes.Is(named.TypeArguments[0], types.Option));
}
