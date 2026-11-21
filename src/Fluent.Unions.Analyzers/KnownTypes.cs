using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// The library and framework types the analyzers look for, resolved once per compilation.
/// </summary>
internal sealed class KnownTypes
{
    private KnownTypes(Compilation compilation, INamedTypeSymbol result, INamedTypeSymbol resultOfT)
    {
        Result = result;
        ResultOfT = resultOfT;
        Option = compilation.GetTypeByMetadataName("Fluent.Unions.Option`1");
        Loadable = compilation.GetTypeByMetadataName("Fluent.Unions.Loadable`1");
        Ok = compilation.GetTypeByMetadataName("Fluent.Unions.Ok");
        None = compilation.GetTypeByMetadataName("Fluent.Unions.None");
        NotLoaded = compilation.GetTypeByMetadataName("Fluent.Unions.NotLoaded");
        Loading = compilation.GetTypeByMetadataName("Fluent.Unions.Loading");
        ErrorResult = compilation.GetTypeByMetadataName("Fluent.Unions.Abstractions.IErrorResult`2");
        FromException = compilation.GetTypeByMetadataName("Fluent.Unions.Abstractions.IFromException`1");
        Exception = compilation.GetTypeByMetadataName("System.Exception");
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        FluentResult = compilation.GetTypeByMetadataName("Fluent.Unions.Abstractions.IFluentResult`3");
        FluentUnitResult = compilation.GetTypeByMetadataName("Fluent.Unions.Abstractions.IFluentUnitResult`2");
        ResultOperator = compilation.GetTypeByMetadataName("Fluent.Unions.Core.ResultOperatorAttribute");
    }

    /// <summary>The library assembly; operators are recognized only when they come from it.</summary>
    public IAssemblySymbol Library => Result.ContainingAssembly;

    public INamedTypeSymbol Result { get; }

    public INamedTypeSymbol ResultOfT { get; }

    public INamedTypeSymbol? Option { get; }

    public INamedTypeSymbol? Loadable { get; }

    public INamedTypeSymbol? Ok { get; }

    public INamedTypeSymbol? None { get; }

    public INamedTypeSymbol? NotLoaded { get; }

    public INamedTypeSymbol? Loading { get; }

    public INamedTypeSymbol? ErrorResult { get; }

    public INamedTypeSymbol? FromException { get; }

    public INamedTypeSymbol? Exception { get; }

    public INamedTypeSymbol? Task { get; }

    public INamedTypeSymbol? TaskOfT { get; }

    public INamedTypeSymbol? ValueTask { get; }

    public INamedTypeSymbol? ValueTaskOfT { get; }

    public INamedTypeSymbol? FluentResult { get; }

    public INamedTypeSymbol? FluentUnitResult { get; }

    public INamedTypeSymbol? ResultOperator { get; }

    /// <summary>
    /// Resolves the types, or returns <c>null</c> when the compilation does not reference the library.
    /// </summary>
    public static KnownTypes? Create(Compilation compilation)
    {
        var result = compilation.GetTypeByMetadataName("Fluent.Unions.Result");
        var resultOfT = compilation.GetTypeByMetadataName("Fluent.Unions.Result`1");

        return result is null || resultOfT is null ? null : new KnownTypes(compilation, result, resultOfT);
    }

    /// <summary>
    /// <c>true</c> for <c>Result</c>, <c>Result&lt;T&gt;</c> and every other result type.
    /// </summary>
    public bool IsMustUse(ITypeSymbol? type)
    {
        type = UnwrapNullable(type);

        return type switch
        {
            null => false,
            ITypeParameterSymbol parameter => parameter.ConstraintTypes.Any(IsMustUse),
            _ => Is(type, Result) || Is(type, ResultOfT) || ErrorResult is not null
                && (Is(type, ErrorResult) || type.AllInterfaces.Any(i => Is(i, ErrorResult)))
        };
    }

    /// <summary>
    /// <c>true</c> for <c>Task</c> or <c>ValueTask</c> of a type that <see cref="IsMustUse"/> accepts.
    /// </summary>
    public bool IsTaskOfMustUse(ITypeSymbol? type) =>
        UnwrapTask(type) is { } inner && IsMustUse(inner);

    /// <summary>
    /// <c>true</c> for <c>Result</c> and <c>Result&lt;T&gt;</c> of this library,
    /// and for result types declared with <c>[FluentResult]</c>.
    /// </summary>
    public bool IsLibraryResult(ITypeSymbol? type) =>
        type is not null && (Is(type, Result) || Is(type, ResultOfT) || IsGeneratedResult(type));

    /// <summary>
    /// <c>true</c> for a result type with a value: <c>Result&lt;T&gt;</c> or one declared with <c>[FluentResult]</c>.
    /// </summary>
    public bool IsResultWithValue(ITypeSymbol? type) =>
        type is not null && (Is(type, ResultOfT) || FluentResult is not null && type.AllInterfaces.Any(i => Is(i, FluentResult)));

    /// <summary>
    /// The error type of a result type declared with <c>[FluentResult]</c>, or <c>null</c>.
    /// </summary>
    public ITypeSymbol? GeneratedErrorType(ITypeSymbol? type) =>
        type?.AllInterfaces.FirstOrDefault(i => Is(i, FluentResult)) is { } contract ? contract.TypeArguments[2] : null;

    private bool IsGeneratedResult(ITypeSymbol type) =>
        type.AllInterfaces.Any(i => Is(i, FluentResult) || Is(i, FluentUnitResult));

    /// <summary>
    /// <c>true</c> for every union of this library: the results, <c>Option&lt;T&gt;</c> and <c>Loadable&lt;T&gt;</c>.
    /// </summary>
    public bool IsLibraryUnion(ITypeSymbol? type) =>
        IsLibraryResult(type) || type is not null && (Is(type, Option) || Is(type, Loadable));

    /// <summary>
    /// The <c>T</c> of <c>Task&lt;T&gt;</c> or <c>ValueTask&lt;T&gt;</c>, or <c>null</c> for any other type.
    /// </summary>
    public ITypeSymbol? UnwrapTask(ITypeSymbol? type) =>
        type is INamedTypeSymbol { TypeArguments.Length: 1 } named && (Is(named, TaskOfT) || Is(named, ValueTaskOfT))
            ? named.TypeArguments[0]
            : null;

    /// <summary>
    /// <c>true</c> for <c>Task</c>, <c>ValueTask</c> and their generic forms.
    /// </summary>
    public bool IsTask(ITypeSymbol? type) =>
        type is not null && (Is(type, Task) || Is(type, ValueTask) || Is(type, TaskOfT) || Is(type, ValueTaskOfT));

    /// <summary>
    /// <c>true</c> when the method is declared in the library assembly.
    /// </summary>
    public bool IsLibraryMethod(IMethodSymbol method) =>
        SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, Library)
        || ResultOperator is not null && method.OriginalDefinition.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, ResultOperator));

    /// <summary>
    /// <c>true</c> when the library declares an operator called <paramref name="name"/> that takes the same receiver and the same number of parameters as <paramref name="method"/>.
    /// </summary>
    public bool HasCounterpart(IMethodSymbol method, string name)
    {
        var original = method.OriginalDefinition;

        var candidates = Operators(Library.GlobalNamespace);
        if (!SymbolEqualityComparer.Default.Equals(original.ContainingAssembly, Library) && original.ContainingType?.ContainingNamespace is { } local)
            candidates = candidates.Concat(local.GetTypeMembers().SelectMany(type => type.GetMembers().OfType<IMethodSymbol>()).Where(m => m.IsStatic));

        return candidates.Any(candidate =>
            candidate.Name == name
            && candidate.IsExtensionMethod == original.IsExtensionMethod
            && candidate.Parameters.Length == original.Parameters.Length
            && (original.IsExtensionMethod
                ? SameShape(candidate.Parameters[0].Type, original.Parameters[0].Type)
                : SymbolEqualityComparer.Default.Equals(candidate.ContainingType, original.ContainingType)));
    }

    /// <summary>
    /// <c>true</c> when <paramref name="type"/> is constructed from <paramref name="definition"/>.
    /// </summary>
    public static bool Is(ITypeSymbol type, INamedTypeSymbol? definition) =>
        definition is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, definition);

    private static IEnumerable<IMethodSymbol> Operators(INamespaceSymbol container)
    {
        foreach (var type in container.GetTypeMembers())
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.IsStatic && method.DeclaredAccessibility == Accessibility.Public)
                yield return method;
        }

        foreach (var nested in container.GetNamespaceMembers())
        foreach (var method in Operators(nested))
            yield return method;
    }

    private static bool SameShape(ITypeSymbol left, ITypeSymbol right)
    {
        if (left is ITypeParameterSymbol leftParameter && right is ITypeParameterSymbol rightParameter)
            return leftParameter.TypeParameterKind == rightParameter.TypeParameterKind
                && leftParameter.Ordinal == rightParameter.Ordinal;

        if (left is not INamedTypeSymbol leftNamed || right is not INamedTypeSymbol rightNamed
            || !SymbolEqualityComparer.Default.Equals(leftNamed.OriginalDefinition, rightNamed.OriginalDefinition))
            return false;

        for (var i = 0; i < leftNamed.TypeArguments.Length; i++)
        {
            if (!SameShape(leftNamed.TypeArguments[i], rightNamed.TypeArguments[i]))
                return false;
        }

        return true;
    }

    private static ITypeSymbol? UnwrapNullable(ITypeSymbol? type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type;
}
