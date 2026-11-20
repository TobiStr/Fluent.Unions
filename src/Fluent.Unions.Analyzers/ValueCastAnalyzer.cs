using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports an explicit cast of a union's <c>Value</c>, which throws when the union holds another case.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueCastAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "FLU0011";

    private static readonly DiagnosticDescriptor Rule = Descriptors.ValueCast;

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
                start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.Conversion);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var conversion = (IConversionOperation)context.Operation;

        if (conversion.IsImplicit || conversion.IsTryCast || conversion.Syntax is not CastExpressionSyntax)
            return;

        if (conversion.Operand is IPropertyReferenceOperation { Property.Name: "Value" } value
            && types.IsLibraryUnion(value.Property.ContainingType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, conversion.Syntax.GetLocation(), value.Syntax.ToString()));
        }
    }
}
