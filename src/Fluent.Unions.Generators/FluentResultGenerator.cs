using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fluent.Unions.Generators;

/// <summary>
/// Generates the union layout and every operator of a struct marked with <c>[FluentResult&lt;TError&gt;]</c>.
/// The operators are thin forwarders to the cores in Fluent.Unions, so behaviour lives in one place.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class FluentResultGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Fluent.Unions.FluentResultAttribute`1";

    internal static readonly DiagnosticDescriptor UnsupportedShape = new(
        "FLU0101",
        "Unsupported result declaration",
        "'{0}' must be a top-level 'readonly partial struct' with at most one type parameter and no constraints",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MissingExceptionMapping = new(
        "FLU0102",
        "The error type cannot represent an unexpected exception",
        "The error type of '{0}' must implement IFromException<TError>, or '{0}' must declare 'public static TError FromException(Exception exception)'",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor SkippedCase = new(
        "FLU0103",
        "Error case without a conversion",
        "The error case '{0}' is an interface and gets no conversion to the result",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var operators = context.CompilationProvider.Select(static (compilation, _) => OperatorReader.Read(compilation));

        var types = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is StructDeclarationSyntax,
            static (syntax, cancellationToken) => Model(syntax, cancellationToken));

        context.RegisterSourceOutput(types.Combine(operators), static (output, pair) =>
        {
            var (model, templates) = pair;
            foreach (var diagnostic in model.Diagnostics)
                output.ReportDiagnostic(diagnostic.ToDiagnostic());

            if (model.Diagnostics.Any(diagnostic => diagnostic.Descriptor.DefaultSeverity == DiagnosticSeverity.Error))
                return;

            output.AddSource(model.HintName + ".g.cs", Emitter.Emit(model, templates));
        });
    }

    private static ResultTypeModel Model(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var syntax = (StructDeclarationSyntax)context.TargetNode;
        var attribute = context.Attributes[0];
        var error = attribute.AttributeClass!.TypeArguments[0];
        var diagnostics = new List<DiagnosticInfo>();

        var isPartial = syntax.Modifiers.Any(SyntaxKind.PartialKeyword);
        var isReadOnly = syntax.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
        var typeParameter = type.TypeParameters.FirstOrDefault();
        if (!isPartial || !isReadOnly || type.ContainingType is not null || type.TypeParameters.Length > 1
            || typeParameter is { ConstraintTypes.Length: > 0 } or { HasReferenceTypeConstraint: true } or { HasValueTypeConstraint: true }
                or { HasNotNullConstraint: true } or { HasUnmanagedTypeConstraint: true } or { HasConstructorConstraint: true })
            diagnostics.Add(DiagnosticInfo.Create(UnsupportedShape, syntax.Identifier.GetLocation(), type.Name));

        var mapping = Mapping(type, error);
        if (mapping == ExceptionMapping.Missing)
            diagnostics.Add(DiagnosticInfo.Create(MissingExceptionMapping, syntax.Identifier.GetLocation(), type.Name));

        var companion = Named(attribute, "Companion", fallback: true);
        var conversions = Named(attribute, "CaseConversions", fallback: true);
        var cases = ImmutableArray<string>.Empty;
        if (conversions && typeParameter is not null && IsUnion(error))
        {
            var caseTypes = CaseTypes((INamedTypeSymbol)error);
            foreach (var skipped in caseTypes.Where(caseType => caseType.TypeKind == TypeKind.Interface))
                diagnostics.Add(DiagnosticInfo.Create(SkippedCase, syntax.Identifier.GetLocation(), skipped.ToDisplayString()));
            cases = caseTypes
                .Where(caseType => caseType.TypeKind is TypeKind.Class or TypeKind.Struct)
                .Select(caseType => caseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .ToImmutableArray();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var declared = type.GetMembers()
            .Where(member => !member.IsImplicitlyDeclared && member.CanBeReferencedByName)
            .Select(member => member.Name)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)
            .ToImmutableArray();

        return new ResultTypeModel(
            type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString(),
            type.Name,
            type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal",
            typeParameter?.Name,
            error.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            error.IsValueType,
            mapping,
            companion,
            conversions,
            new EquatableArray<string>(cases),
            new EquatableArray<string>(declared),
            context.SemanticModel.Compilation.AssemblyName == "Fluent.Unions",
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static ExceptionMapping Mapping(INamedTypeSymbol type, ITypeSymbol error)
    {
        if (type.GetMembers("FromException").OfType<IMethodSymbol>().Any(method => method.IsStatic && method.Parameters.Length == 1))
            return ExceptionMapping.Declared;
        if (error.ToDisplayString() == "System.Exception")
            return ExceptionMapping.Identity;
        return error.AllInterfaces.Any(contract => contract.OriginalDefinition.ToDisplayString() == "Fluent.Unions.Abstractions.IFromException<TSelf>")
            ? ExceptionMapping.ErrorType
            : ExceptionMapping.Missing;
    }

    private static bool Named(AttributeData attribute, string name, bool fallback) =>
        attribute.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value as bool? ?? fallback;

    private static bool IsUnion(ITypeSymbol type) =>
        type.AllInterfaces.Any(contract => contract.ToDisplayString() == "System.Runtime.CompilerServices.IUnion");

    private static ImmutableArray<ITypeSymbol> CaseTypes(INamedTypeSymbol union) =>
        union.InstanceConstructors
            .Where(constructor => constructor.Parameters.Length == 1 && constructor.DeclaredAccessibility == Accessibility.Public)
            .Select(constructor => constructor.Parameters[0].Type)
            .Where(caseType => !SymbolEqualityComparer.Default.Equals(caseType, union))
            .ToImmutableArray();
}
