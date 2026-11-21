using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Reports type arguments of a union that overlap another case, or that are nullable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeArgumentAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> UnionNames = ImmutableHashSet.Create("Result", "Option", "Loadable");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Descriptors.OverlappingTypeArgument, Descriptors.NullableTypeArgument);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var types = KnownTypes.Create(start.Compilation);

            if (types is not null && start.Compilation is CSharpCompilation compilation)
                start.RegisterSyntaxNodeAction(node => Analyze(node, types, compilation), SyntaxKind.GenericName);
        });
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, KnownTypes types, CSharpCompilation compilation)
    {
        var name = (GenericNameSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol is not INamedTypeSymbol type
            || types.GeneratedErrorType(type) is null && (!UnionNames.Contains(name.Identifier.ValueText) || !types.IsLibraryUnion(type)))
            return;

        if (FindOverlap(type, types, compilation) is var (argument, other))
            Report(context, Descriptors.OverlappingTypeArgument, name, Display(type), Display(argument), Display(other));

        var annotated = context.SemanticModel.GetNullableContext(name.SpanStart).AnnotationsEnabled();

        if (type.TypeArguments.Any(a => IsNullable(a, annotated)))
            Report(context, Descriptors.NullableTypeArgument, name, Display(type));
    }

    private static (ITypeSymbol Argument, ITypeSymbol Other)? FindOverlap(INamedTypeSymbol type, KnownTypes types, CSharpCompilation compilation)
    {
        var arguments = type.TypeArguments;

        if (arguments.Any(a => a.TypeKind is TypeKind.TypeParameter or TypeKind.Error))
            return null;

        var cases = arguments.Concat(ExtraCases(type, types)).ToList();

        for (var i = 0; i < arguments.Length; i++)
        {
            for (var j = i + 1; j < cases.Count; j++)
            {
                if (Overlaps(compilation, cases[i], cases[j]))
                    return (cases[i], cases[j]);
            }
        }

        return null;
    }

    private static IEnumerable<ITypeSymbol> ExtraCases(INamedTypeSymbol type, KnownTypes types)
    {
        if (types.GeneratedErrorType(type) is { } error)
            return new[] { error }.Concat(UnionCases(error));

        IEnumerable<ITypeSymbol?> extras =
            KnownTypes.Is(type, types.ResultOfT) ? [types.Exception]
            : KnownTypes.Is(type, types.Option) ? [types.None]
            : KnownTypes.Is(type, types.Loadable) ? [types.Exception, types.NotLoaded, types.Loading]
            : [];

        return extras.OfType<ITypeSymbol>();
    }

    private static IEnumerable<ITypeSymbol> UnionCases(ITypeSymbol error) =>
        error is INamedTypeSymbol named && named.AllInterfaces.Any(i => i.ToDisplayString() == "System.Runtime.CompilerServices.IUnion")
            ? named.InstanceConstructors
                .Where(constructor => constructor.Parameters.Length == 1 && constructor.DeclaredAccessibility == Accessibility.Public)
                .Select(constructor => constructor.Parameters[0].Type)
                .Where(caseType => !SymbolEqualityComparer.Default.Equals(caseType, named))
            : [];

    private static bool Overlaps(CSharpCompilation compilation, ITypeSymbol first, ITypeSymbol second) =>
        IsReferenceOrBoxing(compilation.ClassifyConversion(first, second)) || IsReferenceOrBoxing(compilation.ClassifyConversion(second, first));

    private static bool IsReferenceOrBoxing(Conversion conversion) =>
        conversion.IsIdentity || conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing);

    private static bool IsNullable(ITypeSymbol argument, bool annotationsEnabled) =>
        argument.TypeKind != TypeKind.TypeParameter
        && (argument.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            || annotationsEnabled && argument.IsReferenceType && argument.NullableAnnotation == NullableAnnotation.Annotated);

    private static string Display(ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    private static void Report(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode node, params object[] arguments) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, node.GetLocation(), arguments));
}
