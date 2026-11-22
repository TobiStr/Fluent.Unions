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
    private const string HelpLink = "https://tobistr.github.io/Fluent.Unions/analyzer/#";

    private static readonly string[] GeneratedMembers =
    [
        "Value", "IsSuccess", "IsError", "TryGetValue", "TryGetError", "GetValue", "GetError", "MapError", "Ok", "Error", "Deconstruct", "ToString", "MapException"
    ];

    internal static readonly DiagnosticDescriptor UnsupportedShape = new(
        "FLU0101",
        "Unsupported result declaration",
        "'{0}' is not a supported result declaration: {1}",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "flu0101");

    internal static readonly DiagnosticDescriptor MissingExceptionMapping = new(
        "FLU0102",
        "The error type cannot represent an unexpected exception",
        "The error type of '{0}' must implement IFromException<TError>, or '{0}' must declare an accessible 'public static TError FromException(Exception exception)'",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "flu0102");

    internal static readonly DiagnosticDescriptor SkippedCase = new(
        "FLU0103",
        "Error case without a conversion",
        "The error case '{0}' gets no conversion to the result: {1}",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "flu0103");

    internal static readonly DiagnosticDescriptor IneffectiveCapture = new(
        "FLU0104",
        "Declared capture has no effect",
        "'{0}' captures only declared exceptions, but its error type '{1}' declares every exception",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "flu0104");

    internal static readonly DiagnosticDescriptor InvalidPropagate = new(
        "FLU0105",
        "Propagated type is not an exception",
        "'{0}' in Propagate of '{1}' is not an exception type",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "flu0105");

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var declarations = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (syntax, cancellationToken) => Model(syntax, cancellationToken))
            .Collect()
            .SelectMany(static (models, _) => Resolve(models));

        var operators = declarations.Collect().Combine(context.CompilationProvider).Select(static (pair, _) =>
            pair.Left.Length == 0 ? EquatableArray<OperatorTemplate>.Empty : OperatorCache.Get(pair.Right));

        context.RegisterSourceOutput(declarations.Combine(operators), static (output, pair) =>
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
        var syntax = (TypeDeclarationSyntax)context.TargetNode;
        var compilation = context.SemanticModel.Compilation;
        var attribute = context.Attributes[0];
        var error = attribute.AttributeClass!.TypeArguments[0];
        var locus = Locus.Of(syntax.Identifier.GetLocation());
        var diagnostics = new List<DiagnosticInfo>();
        var isPublic = type.DeclaredAccessibility == Accessibility.Public;
        var typeParameter = type.TypeParameters.FirstOrDefault();

        foreach (var problem in Problems(type, error, compilation))
            diagnostics.Add(DiagnosticInfo.Create(UnsupportedShape, locus, type.Name, problem));

        var mapping = Mapping(type, error);
        if (mapping == ExceptionMapping.Missing)
            diagnostics.Add(DiagnosticInfo.Create(MissingExceptionMapping, locus, type.Name));

        var conversions = Named(attribute, "CaseConversions", fallback: true);
        var cases = new List<string>();
        if (conversions && typeParameter is not null && IsUnion(error))
        {
            foreach (var caseType in CaseTypes((INamedTypeSymbol)error))
            {
                if (SkipReason(caseType, isPublic, compilation) is { } reason)
                    diagnostics.Add(DiagnosticInfo.Create(SkippedCase, locus, caseType.ToDisplayString(), reason));
                else
                    cases.Add(caseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        var captureFilter = CaptureFilter(type, attribute, error, locus, diagnostics);

        cancellationToken.ThrowIfCancellationRequested();

        var declared = type.GetMembers()
            .Where(member => !member.IsImplicitlyDeclared)
            .SelectMany(Declared)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)
            .ToImmutableArray();

        return new ResultTypeModel(
            type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString(),
            type.Name,
            isPublic ? "public" : "internal",
            typeParameter is null ? null : Names.Escape(typeParameter.Name),
            error.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            error.IsValueType,
            mapping,
            Companion: false,
            conversions,
            new EquatableArray<string>(cases.ToImmutableArray()),
            captureFilter,
            new EquatableArray<string>(declared),
            compilation.AssemblyName == "Fluent.Unions",
            HintName(type),
            locus,
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static ImmutableArray<ResultTypeModel> Resolve(ImmutableArray<ResultTypeModel> models)
    {
        var resolved = ImmutableArray.CreateBuilder<ResultTypeModel>(models.Length);
        foreach (var model in models)
        {
            var isGeneric = model.TypeParameter is not null;
            var sibling = models.FirstOrDefault(other =>
                (other.TypeParameter is not null) != isGeneric && other.Namespace == model.Namespace && other.Name == model.Name);

            var problem = (isGeneric, sibling) switch
            {
                (false, null) => $"a result type without a value needs a generic '{model.Name}<T>' next to it that is declared with [FluentResult]",
                (_, { } other) when other.ErrorType != model.ErrorType =>
                    $"it and '{other.Name}{(other.TypeParameter is null ? "" : "<" + other.TypeParameter + ">")}' must have the same error type",
                (_, { } other) when other.CaptureFilter != model.CaptureFilter =>
                    $"it and '{other.Name}{(other.TypeParameter is null ? "" : "<" + other.TypeParameter + ">")}' must have the same Capture and Propagate settings",
                _ => null
            };

            var diagnostics = problem is null
                ? model.Diagnostics
                : new EquatableArray<DiagnosticInfo>(
                    model.Diagnostics.Append(DiagnosticInfo.Create(UnsupportedShape, model.Location, model.Name, problem)).ToImmutableArray());

            resolved.Add(model with { Companion = isGeneric && sibling is null, Diagnostics = diagnostics });
        }

        return resolved.ToImmutable();
    }

    private static List<string> Problems(INamedTypeSymbol type, ITypeSymbol error, Compilation compilation)
    {
        if (type.TypeKind != TypeKind.Struct)
            return ["it must be a struct"];

        var problems = new List<string>();
        var isPartial = type.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
        if (!isPartial)
            problems.Add("it must be 'partial'");
        if (!type.IsReadOnly)
            problems.Add("it must be 'readonly'");
        if (type.IsRecord)
            problems.Add("a record struct is not supported");
        if (type.IsRefLikeType)
            problems.Add("a ref struct is not supported");
        if (type.IsFileLocal)
            problems.Add("a 'file' type is not supported, because the generated part cannot be file-local");
        if (type.ContainingType is not null)
            problems.Add("it must be declared directly in a namespace, not inside another type");
        if (type.TypeParameters.Length > 1)
            problems.Add("it can have at most one type parameter");
        if (type.TypeParameters.FirstOrDefault() is { } typeParameter)
        {
            if (typeParameter is { ConstraintTypes.Length: > 0 } or { HasReferenceTypeConstraint: true } or { HasValueTypeConstraint: true }
                or { HasNotNullConstraint: true } or { HasUnmanagedTypeConstraint: true } or { HasConstructorConstraint: true })
                problems.Add("its type parameter cannot have constraints");
            if (typeParameter.AllowsRefLikeType)
                problems.Add("its type parameter cannot allow ref structs");
        }

        if (GeneratedMembers.Contains(type.Name))
            problems.Add($"'{type.Name}' is the name of a member that the generator adds to the type");
        if (!IsAccessible(error, type.DeclaredAccessibility == Accessibility.Public, compilation))
            problems.Add($"the error type '{error.ToDisplayString()}' must be at least as accessible as the result type");

        return problems;
    }

    private static string? SkipReason(ITypeSymbol caseType, bool isPublic, Compilation compilation) => caseType switch
    {
        { TypeKind: TypeKind.Interface } => "C# has no conversion from an interface",
        { SpecialType: SpecialType.System_Object or SpecialType.System_ValueType } => "C# has no conversion from a base type of the result",
        { TypeKind: not (TypeKind.Class or TypeKind.Struct) } => "only classes and structs convert",
        _ when !IsAccessible(caseType, isPublic, compilation) => "it is less accessible than the result type",
        _ => null
    };

    private static bool IsAccessible(ITypeSymbol type, bool isPublic, Compilation compilation) => type switch
    {
        IArrayTypeSymbol array => IsAccessible(array.ElementType, isPublic, compilation),
        IPointerTypeSymbol pointer => IsAccessible(pointer.PointedAtType, isPublic, compilation),
        INamedTypeSymbol named => (isPublic ? IsPublic(named) : compilation.IsSymbolAccessibleWithin(named.OriginalDefinition, compilation.Assembly))
            && named.TypeArguments.All(argument => IsAccessible(argument, isPublic, compilation)),
        _ => true
    };

    private static bool IsPublic(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }

    private static IEnumerable<string> Declared(ISymbol member)
    {
        if (member is IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters.Length: 1 } constructor)
            yield return "ctor:" + constructor.Parameters[0].Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        else if (member.CanBeReferencedByName)
            yield return member.Name;
    }

    private static string HintName(INamedTypeSymbol type)
    {
        var parts = new List<string> { type.Name + (type.TypeParameters.Length == 0 ? "" : "`" + type.TypeParameters.Length) };
        for (var space = type.ContainingNamespace; space is { IsGlobalNamespace: false }; space = space.ContainingNamespace)
            parts.Insert(0, space.Name);

        return Names.Sanitize(string.Join(".", parts));
    }

    private static ExceptionMapping Mapping(INamedTypeSymbol type, ITypeSymbol error)
    {
        if (type.GetMembers("FromException").OfType<IMethodSymbol>().Any(method => IsExceptionFactory(method, error)))
            return ExceptionMapping.Declared;
        if (error.ToDisplayString() == "System.Exception")
            return ExceptionMapping.Identity;
        return error.TypeKind != TypeKind.Interface && error.AllInterfaces.Any(contract =>
            contract.OriginalDefinition.ToDisplayString() == "Fluent.Unions.Abstractions.IFromException<TSelf>"
            && SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], error))
            ? ExceptionMapping.ErrorType
            : ExceptionMapping.Missing;
    }

    private static bool IsExceptionFactory(IMethodSymbol method, ITypeSymbol error) =>
        method.IsStatic
        && method.TypeParameters.Length == 0
        && method.Parameters.Length == 1
        && method.Parameters[0].RefKind == RefKind.None
        && method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
        && method.Parameters[0].Type.ToDisplayString() == "System.Exception"
        && SymbolEqualityComparer.Default.Equals(method.ReturnType, error);

    private static string? CaptureFilter(INamedTypeSymbol type, AttributeData attribute, ITypeSymbol error, Locus locus, List<DiagnosticInfo> diagnostics)
    {
        var conditions = new List<string>();

        if (attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "Capture").Value.Value is int and not 0)
        {
            if (error.ToDisplayString() == "System.Exception")
                diagnostics.Add(DiagnosticInfo.Create(IneffectiveCapture, locus, type.Name, error.ToDisplayString()));

            var captured = new[] { error }
                .Concat(IsUnion(error) ? CaseTypes((INamedTypeSymbol)error) : ImmutableArray<ITypeSymbol>.Empty)
                .Where(IsException)
                .Select(FullName)
                .Distinct()
                .ToList();
            conditions.Add(captured.Count == 0 ? "false" : $"exception is {string.Join(" or ", captured)}");
        }

        var propagated = new List<string>();
        var propagate = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "Propagate").Value;
        if (propagate.Kind == TypedConstantKind.Array)
        {
            foreach (var entry in propagate.Values)
            {
                if (entry.Value is not ITypeSymbol propagatedType)
                    continue;
                if (IsException(propagatedType))
                    propagated.Add(FullName(propagatedType));
                else
                    diagnostics.Add(DiagnosticInfo.Create(InvalidPropagate, locus, propagatedType.ToDisplayString(), type.Name));
            }
        }

        if (propagated.Count > 0)
            conditions.Add($"exception is not ({string.Join(" or ", propagated.Distinct())})");

        return conditions.Count == 0 ? null : string.Join(" && ", conditions);

        static string FullName(ITypeSymbol exceptionType) =>
            exceptionType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static bool IsException(ITypeSymbol type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Exception")
                return true;
        }

        return false;
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
