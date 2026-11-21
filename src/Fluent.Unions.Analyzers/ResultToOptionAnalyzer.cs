using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports <c>ToOption</c> on a result, which turns the error into <c>None</c> without a trace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultToOptionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "FLU0013";

    private static readonly DiagnosticDescriptor Rule = Descriptors.ResultToOption;

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
                start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != "ToOption"
            || !types.IsLibraryMethod(invocation.TargetMethod)
            || Receiver(invocation) is not { } receiver
            || !IsResultOfT(receiver.Type, types)
            || IsHandled(receiver, types))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
    }

    private static bool IsResultOfT(ITypeSymbol? type, KnownTypes types) =>
        type is not null && types.IsResultWithValue(types.UnwrapTask(type) ?? type);

    private static IOperation? Receiver(IInvocationOperation invocation)
    {
        var receiver = invocation.Instance
            ?? (invocation.TargetMethod.IsExtensionMethod ? invocation.Arguments.FirstOrDefault()?.Value : null);

        while (receiver is IConversionOperation { IsImplicit: true } conversion)
            receiver = conversion.Operand;

        return receiver;
    }

    private static bool IsHandled(IOperation receiver, KnownTypes types)
    {
        for (var current = receiver; current is not null;)
        {
            switch (current)
            {
                case IAwaitOperation awaited:
                    current = awaited.Operation;
                    break;
                case IInvocationOperation invocation:
                    if (IsHandlingOperator(invocation, types))
                        return true;
                    current = Receiver(invocation);
                    break;
                default:
                    return false;
            }
        }

        return false;
    }

    private static bool IsHandlingOperator(IInvocationOperation invocation, KnownTypes types)
    {
        var method = invocation.TargetMethod;

        if (!types.IsLibraryMethod(method))
            return false;

        return method.Name is "OnErrorDo" or "Catch"
            || method.Name == "Do" && method.Parameters.Count(p => p.Type.TypeKind == TypeKind.Delegate) == 2;
    }
}
