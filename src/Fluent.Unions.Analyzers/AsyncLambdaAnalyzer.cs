using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports an async lambda passed to an operator that does not await it, or that turns it into <c>async void</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncLambdaAnalyzer : DiagnosticAnalyzer
{
    private const string NoCounterpart = "await the work outside the chain";

    private static readonly ImmutableDictionary<string, string> Counterparts = ImmutableDictionary<string, string>.Empty
        .Add("Try", "TryAsync")
        .Add("Select", "SelectAwait")
        .Add("Chain", "ChainAwait")
        .Add("SelectMany", "ChainAwait")
        .Add("Do", "DoAwait")
        .Add("Tap", "TapAwait");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Descriptors.AsyncLambdaInSyncOperator, Descriptors.AsyncVoidLambda);

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

    internal static Location OperatorLocation(IOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax { Expression: var expression }
            ? expression switch
            {
                MemberAccessExpressionSyntax access => access.Name.GetLocation(),
                MemberBindingExpressionSyntax binding => binding.Name.GetLocation(),
                _ => expression.GetLocation()
            }
            : invocation.Syntax.GetLocation();

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (!types.IsLibraryMethod(invocation.TargetMethod))
            return;

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter is not { Type: INamedTypeSymbol { DelegateInvokeMethod: { } invoke } } parameter
                || !IsAsyncLambda(argument.Value))
                continue;

            if (invoke.ReturnsVoid)
                ReportAsyncVoid(context, types, invocation);
            else if (types.IsTask(invoke.ReturnType) && ReturnsTypeParameter(parameter))
                ReportSyncOperator(context, types, invocation);
        }
    }

    private static void ReportSyncOperator(OperationAnalysisContext context, KnownTypes types, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (!Counterparts.TryGetValue(method.Name, out var counterpart) || !types.HasCounterpart(method, counterpart))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.AsyncLambdaInSyncOperator,
            OperatorLocation(invocation),
            ImmutableDictionary<string, string?>.Empty.Add(Descriptors.ReplacementProperty, counterpart),
            method.Name,
            counterpart));
    }

    private static void ReportAsyncVoid(OperationAnalysisContext context, KnownTypes types, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        var found = Counterparts.TryGetValue(method.Name, out var counterpart) && types.HasCounterpart(method, counterpart);

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.AsyncVoidLambda,
            OperatorLocation(invocation),
            found
                ? ImmutableDictionary<string, string?>.Empty.Add(Descriptors.ReplacementProperty, counterpart)
                : ImmutableDictionary<string, string?>.Empty,
            method.Name,
            found ? $"use '{counterpart}'" : NoCounterpart));
    }

    private static bool ReturnsTypeParameter(IParameterSymbol parameter) =>
        parameter.OriginalDefinition.Type is INamedTypeSymbol { DelegateInvokeMethod.ReturnType: ITypeParameterSymbol };

    private static bool IsAsyncLambda(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    break;
                case IDelegateCreationOperation creation:
                    operation = creation.Target;
                    break;
                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    break;
                case IAnonymousFunctionOperation lambda:
                    return lambda.Symbol.IsAsync;
                default:
                    return false;
            }
        }
    }
}
