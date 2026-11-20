using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports <c>GetValue</c> and <c>GetError</c> calls that no check of the result precedes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UncheckedAccessAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "FLU0010";

    private static readonly DiagnosticDescriptor Rule = Descriptors.UncheckedAccess;

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
        var method = invocation.TargetMethod;

        if (method.Name is not ("GetValue" or "GetError")
            || invocation.Instance is not { } instance
            || !types.IsLibraryMethod(method)
            || !types.IsLibraryResult(method.ContainingType))
            return;

        var wantsSuccess = method.Name == "GetValue";
        var receiver = ReceiverSymbol(instance);

        if (receiver is not null && new GuardSearch(types, receiver, wantsSuccess).IsGuarded(invocation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            method.Name,
            wantsSuccess ? "is an error" : "has a value"));
    }

    private static ISymbol? ReceiverSymbol(IOperation operation) => operation switch
    {
        ILocalReferenceOperation local => local.Local,
        IParameterReferenceOperation parameter => parameter.Parameter,
        IFieldReferenceOperation field when IsThisOrStatic(field.Instance) => field.Field,
        IPropertyReferenceOperation property when IsThisOrStatic(property.Instance) => property.Property,
        _ => null
    };

    private static bool IsThisOrStatic(IOperation? instance) => instance is null or IInstanceReferenceOperation;

    private sealed class GuardSearch(KnownTypes types, ISymbol receiver, bool wantsSuccess)
    {
        public bool IsGuarded(IOperation invocation)
        {
            var child = invocation;

            for (var parent = child.Parent; parent is not null; child = parent, parent = parent.Parent)
            {
                if (IsGuardedBy(parent, child))
                    return true;
            }

            return false;
        }

        private bool IsGuardedBy(IOperation parent, IOperation child) => parent switch
        {
            IConditionalOperation conditional => conditional.WhenTrue == child && Proves(conditional.Condition, true)
                || conditional.WhenFalse == child && Proves(conditional.Condition, false),
            IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } and => and.RightOperand == child && Proves(and.LeftOperand, true),
            IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } or => or.RightOperand == child && Proves(or.LeftOperand, false),
            IBlockOperation block => HasGuardingJump(block, child),
            _ => false
        };

        private bool HasGuardingJump(IBlockOperation block, IOperation child)
        {
            foreach (var statement in block.Operations)
            {
                if (statement == child)
                    return false;

                if (statement is IConditionalOperation conditional && IsJump(conditional.WhenTrue) && Proves(conditional.Condition, false))
                    return true;
            }

            return false;
        }

        private bool Proves(IOperation condition, bool outcome)
        {
            switch (condition)
            {
                case IPropertyReferenceOperation { Property.Name: "IsSuccess" or "IsError", Instance: { } instance } property
                    when IsReceiver(instance):
                    return IsSuccessWhen(property.Property.Name == "IsSuccess", outcome) == wantsSuccess;

                case IInvocationOperation { TargetMethod.Name: "TryGetValue" or "TryGetError", Instance: { } instance } invocation
                    when IsReceiver(instance) && types.IsLibraryMethod(invocation.TargetMethod):
                    return IsSuccessWhen(invocation.TargetMethod.Name == "TryGetValue", outcome) == wantsSuccess;

                case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } not:
                    return Proves(not.Operand, !outcome);

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } and:
                    return outcome && (Proves(and.LeftOperand, true) || Proves(and.RightOperand, true));

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } or:
                    return !outcome && (Proves(or.LeftOperand, false) || Proves(or.RightOperand, false));

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } comparison:
                    return ProvesComparison(comparison, outcome);

                case IIsPatternOperation { Pattern: var pattern } isPattern:
                    return TryGetBoolPattern(pattern, out var expected) && Proves(isPattern.Value, outcome ? expected : !expected);

                default:
                    return false;
            }
        }

        private bool ProvesComparison(IBinaryOperation comparison, bool outcome)
        {
            IOperation operand;

            if (TryGetBool(comparison.RightOperand, out var constant))
                operand = comparison.LeftOperand;
            else if (TryGetBool(comparison.LeftOperand, out constant))
                operand = comparison.RightOperand;
            else
                return false;

            var isEquals = comparison.OperatorKind == BinaryOperatorKind.Equals;

            return Proves(operand, outcome == isEquals ? constant : !constant);
        }

        private bool IsReceiver(IOperation operation) =>
            ReceiverSymbol(operation) is { } symbol && SymbolEqualityComparer.Default.Equals(symbol, receiver);

        private static bool IsSuccessWhen(bool successIndicator, bool outcome) => successIndicator == outcome;

        private static bool TryGetBool(IOperation operation, out bool value)
        {
            if (operation.ConstantValue is { HasValue: true, Value: bool constant })
            {
                value = constant;
                return true;
            }

            value = false;
            return false;
        }

        private static bool TryGetBoolPattern(IPatternOperation pattern, out bool value)
        {
            switch (pattern)
            {
                case IConstantPatternOperation constant:
                    return TryGetBool(constant.Value, out value);

                case INegatedPatternOperation negated when TryGetBoolPattern(negated.Pattern, out var inner):
                    value = !inner;
                    return true;

                default:
                    value = false;
                    return false;
            }
        }

        private static bool IsJump(IOperation operation) => operation switch
        {
            IReturnOperation { Kind: OperationKind.Return } => true,
            IBranchOperation => true,
            IThrowOperation => true,
            IBlockOperation block => block.Operations.LastOrDefault() is { } last && IsJump(last),
            _ => false
        };
    }
}
