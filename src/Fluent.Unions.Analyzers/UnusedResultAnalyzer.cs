using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports a call whose result is dropped, like Rust's <c>#[must_use]</c>: an ignored result hides its error.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedResultAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "FLU0001";

    private static readonly DiagnosticDescriptor Rule = Descriptors.UnusedResult;

    private static readonly ImmutableHashSet<string> HandlingOperators = ImmutableHashSet.Create("Check", "OnErrorDo");

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
                start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.ExpressionStatement);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var expression = SkipConditionalAccess(((IExpressionStatementOperation)context.Operation).Operation);

        switch (expression)
        {
            case IAwaitOperation awaited when types.IsMustUse(awaited.Type):
                var awaitedInvocation = SkipConditionalAccess(awaited.Operation) as IInvocationOperation;
                if (awaitedInvocation is null || !IsHandled(awaitedInvocation, types))
                    Report(context, awaited, awaitedInvocation?.TargetMethod.Name ?? awaited.Operation.Syntax.ToString());
                break;

            case IInvocationOperation invocation when types.IsMustUse(invocation.Type):
                if (!IsHandled(invocation, types))
                    Report(context, invocation, invocation.TargetMethod.Name);
                break;

            case IInvocationOperation invocation when types.IsTaskOfMustUse(invocation.Type):
                if (!IsInAsyncFunction(invocation, context.ContainingSymbol) && !IsHandled(invocation, types))
                    Report(context, invocation, invocation.TargetMethod.Name);
                break;
        }
    }

    private static IOperation SkipConditionalAccess(IOperation operation)
    {
        while (operation is IConditionalAccessOperation conditional)
            operation = conditional.WhenNotNull;

        return operation;
    }

    private static bool IsHandled(IInvocationOperation invocation, KnownTypes types)
    {
        var method = invocation.TargetMethod;

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, types.Library))
            return false;

        return HandlingOperators.Contains(method.Name)
            || method.Name == "Do" && method.Parameters.Count(p => p.Type.TypeKind == TypeKind.Delegate) == 2;
    }

    private static bool IsInAsyncFunction(IOperation operation, ISymbol containingSymbol)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case IAnonymousFunctionOperation lambda:
                    return lambda.Symbol.IsAsync;
                case ILocalFunctionOperation localFunction:
                    return localFunction.Symbol.IsAsync;
            }
        }

        return containingSymbol is IMethodSymbol { IsAsync: true };
    }

    private static void Report(OperationAnalysisContext context, IOperation operation, string name) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), name));
}
