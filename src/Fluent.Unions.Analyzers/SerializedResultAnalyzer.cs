using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports a result that is serialized with System.Text.Json, or returned from an unmapped minimal API endpoint.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SerializedResultAnalyzer : DiagnosticAnalyzer
{
    private const int MaxDepth = 5;
    private const int JsonIgnoreAlways = 1;
    private const string MappingOperator = "WithResultMapping";
    private const string ConvertAdvice = "convert it first, for example with Match or GetValueOrDefault";
    private const string MappingAdvice = "add .WithResultMapping() from Fluent.Unions.AspNetCore, or return ToHttpResult()";

    private static readonly ImmutableHashSet<string> SerializeMethods = ImmutableHashSet.Create(
        "Serialize", "SerializeAsync", "SerializeToUtf8Bytes", "SerializeToElement", "SerializeToNode", "SerializeToDocument");

    private static readonly ImmutableHashSet<string> EndpointMethods = ImmutableHashSet.Create(
        "Map", "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapMethods");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.SerializedResult);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var types = KnownTypes.Create(start.Compilation);
            var serializer = start.Compilation.GetTypeByMetadataName("System.Text.Json.JsonSerializer");
            var endpoints = start.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions");
            var group = start.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Routing.RouteGroupBuilder");

            if (types is not null && (serializer is not null || endpoints is not null))
                start.RegisterOperationAction(
                    operation => Analyze(operation, types, serializer, endpoints, group),
                    OperationKind.Invocation);
        });
    }

    private static void Analyze(
        OperationAnalysisContext context,
        KnownTypes types,
        INamedTypeSymbol? serializer,
        INamedTypeSymbol? endpoints,
        INamedTypeSymbol? group)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, serializer) && SerializeMethods.Contains(method.Name))
            AnalyzeSerialization(context, invocation, types);
        else if (SymbolEqualityComparer.Default.Equals(method.ContainingType, endpoints) && EndpointMethods.Contains(method.Name))
            AnalyzeEndpoint(context, invocation, types, group);
    }

    private static void AnalyzeSerialization(OperationAnalysisContext context, IInvocationOperation invocation, KnownTypes types)
    {
        if (SerializedType(invocation) is { } type
            && FindResult(type, types, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), 0) is { } result)
            Report(context, invocation, result, ConvertAdvice);
    }

    private static ITypeSymbol? SerializedType(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (method.IsGenericMethod && method.TypeArguments[0].SpecialType != SpecialType.System_Object)
            return method.TypeArguments[0];

        if (Argument(invocation, "inputType") is ITypeOfOperation typeOf)
            return typeOf.TypeOperand;

        return Argument(invocation, "value")?.Type;
    }

    private static IOperation? Argument(IInvocationOperation invocation, string parameter) =>
        invocation.Arguments.FirstOrDefault(a => a.Parameter?.Name == parameter) is { } argument ? SkipConversions(argument.Value) : null;

    private static ITypeSymbol? FindResult(ITypeSymbol type, KnownTypes types, HashSet<ITypeSymbol> visited, int depth)
    {
        if (depth > MaxDepth || !visited.Add(type))
            return null;

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            return FindResult(nullable.TypeArguments[0], types, visited, depth);

        if (types.IsLibraryResult(type))
            return type;

        if (type is IArrayTypeSymbol array)
            return FindResult(array.ElementType, types, visited, depth + 1);

        if (type is not INamedTypeSymbol named || type.SpecialType != SpecialType.None
            || type.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
            return null;

        if (ElementType(named) is { } element)
            return FindResult(element, types, visited, depth + 1);

        if (IsFrameworkType(named))
            return named.TypeArguments.Select(a => FindResult(a, types, visited, depth + 1)).FirstOrDefault(r => r is not null);

        return SerializedProperties(named)
            .Select(p => FindResult(p.Type, types, visited, depth + 1))
            .FirstOrDefault(r => r is not null);
    }

    private static ITypeSymbol? ElementType(INamedTypeSymbol type) =>
        type.AllInterfaces.Prepend(type)
            .FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            ?.TypeArguments[0];

    private static IEnumerable<IPropertySymbol> SerializedProperties(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null && !IsFrameworkType(current); current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property is { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null }
                    && !IsIgnored(property))
                    yield return property;
            }
        }
    }

    private static bool IsIgnored(IPropertySymbol property) =>
        property.GetAttributes().Any(a => a.AttributeClass?.Name == "JsonIgnoreAttribute"
            && a.NamedArguments.All(n => n.Key != "Condition" || n.Value.Value is JsonIgnoreAlways));

    private static bool IsFrameworkType(INamedTypeSymbol type)
    {
        var name = type.ContainingNamespace?.ToDisplayString();

        return name is not null && (name == "System" || name.StartsWith("System.", StringComparison.Ordinal));
    }

    private static void AnalyzeEndpoint(OperationAnalysisContext context, IInvocationOperation invocation, KnownTypes types, INamedTypeSymbol? group)
    {
        if (HandlerResult(invocation, types) is { } result && !IsMappedOrUnknown(invocation, group))
            Report(context, invocation, result, MappingAdvice);
    }

    private static ITypeSymbol? HandlerResult(IInvocationOperation invocation, KnownTypes types)
    {
        var handler = invocation.Arguments.FirstOrDefault(a => a.Parameter?.Type.SpecialType == SpecialType.System_Delegate);

        if (handler is null)
            return null;

        var value = SkipConversions(handler.Value);
        var returnType = value switch
        {
            IAnonymousFunctionOperation function => function.Symbol.ReturnType,
            IMethodReferenceOperation method => method.Method.ReturnType,
            _ => (value.Type as INamedTypeSymbol)?.DelegateInvokeMethod?.ReturnType
        };

        if (types.IsLibraryResult(returnType))
            return returnType;

        return types.UnwrapTask(returnType) is { } inner && types.IsLibraryResult(inner) ? inner : null;
    }

    private static bool IsMappedOrUnknown(IInvocationOperation invocation, INamedTypeSymbol? group)
    {
        if (IsMappedByChain(invocation))
            return true;

        var receiver = Receiver(invocation);

        return receiver switch
        {
            ILocalReferenceOperation local => IsLocalMappedOrUnknown(local.Local, invocation),
            IParameterReferenceOperation or IFieldReferenceOperation or IPropertyReferenceOperation or IInstanceReferenceOperation => true,
            _ => receiver is null
                || SymbolEqualityComparer.Default.Equals(receiver.Type, group)
                || ReceiverChainMaps(receiver)
        };
    }

    private static bool IsMappedByChain(IInvocationOperation invocation)
    {
        IOperation current = invocation;

        while (Outer(current) is { } outer)
        {
            if (outer.TargetMethod.Name == MappingOperator)
                return true;

            current = outer;
        }

        return false;
    }

    private static bool ReceiverChainMaps(IOperation receiver)
    {
        for (var current = receiver as IInvocationOperation; current is not null; current = Receiver(current) as IInvocationOperation)
        {
            if (current.TargetMethod.Name == MappingOperator)
                return true;
        }

        return false;
    }

    private static bool IsLocalMappedOrUnknown(ILocalSymbol local, IInvocationOperation invocation)
    {
        var declarator = local.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<VariableDeclaratorSyntax>().FirstOrDefault();
        var model = invocation.SemanticModel;

        if (declarator is null || model is null)
            return true;

        if (declarator.Initializer is { } initializer && CallsMapping(initializer.Value.DescendantNodesAndSelf()))
            return true;

        var body = declarator.Ancestors().First(n =>
            n is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or CompilationUnitSyntax);

        return body.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call =>
            CalledName(call) == MappingOperator
            && call.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver }
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, local));
    }

    private static bool CallsMapping(IEnumerable<SyntaxNode> nodes) =>
        nodes.OfType<InvocationExpressionSyntax>().Any(call => CalledName(call) == MappingOperator);

    private static string? CalledName(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => null
    };

    private static IInvocationOperation? Outer(IOperation operation)
    {
        var parent = operation.Parent;

        while (parent is IArgumentOperation or IConversionOperation or IParenthesizedOperation)
            parent = parent.Parent;

        return parent is IInvocationOperation outer && ReferenceEquals(Receiver(outer), operation) ? outer : null;
    }

    private static IOperation? Receiver(IInvocationOperation invocation)
    {
        var receiver = invocation.Instance
            ?? (invocation.TargetMethod.IsExtensionMethod ? invocation.Arguments.FirstOrDefault()?.Value : null);

        return receiver is null ? null : SkipConversions(receiver);
    }

    private static IOperation SkipConversions(IOperation operation)
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
                default:
                    return operation;
            }
        }
    }

    private static void Report(OperationAnalysisContext context, IInvocationOperation invocation, ITypeSymbol result, string advice)
    {
        var location = invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
            ? member.Name.GetLocation()
            : invocation.Syntax.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.SerializedResult,
            location,
            result.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            advice));
    }
}
