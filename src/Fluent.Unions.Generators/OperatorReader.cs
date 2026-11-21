using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fluent.Unions.Generators;

/// <summary>
/// Reads the operator cores of Fluent.Unions and turns every method into a forwarder template.
/// </summary>
internal static class OperatorReader
{
    internal const string ResultToken = "{{R}}";
    internal const string ErrorToken = "{{E}}";

    private const string CoreNamespace = "Fluent.Unions.Core";
    private const string MemberCore = "ResultMemberCore";
    private const string StaticCore = "ResultStaticCore";

    private static readonly string[] CopiedAttributes =
    [
        "System.ComponentModel.EditorBrowsableAttribute",
        "System.ObsoleteAttribute",
        "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute",
        "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute"
    ];

    internal static EquatableArray<OperatorTemplate> Read(Compilation compilation)
    {
        var core = compilation.GetTypeByMetadataName("Fluent.Unions.Core.ResultQueryCore")?.ContainingNamespace;
        var fluentResult = compilation.GetTypeByMetadataName("Fluent.Unions.Abstractions.IFluentResult`3");
        var fluentUnit = compilation.GetTypeByMetadataName("Fluent.Unions.Abstractions.IFluentUnitResult`2");
        if (core is null || fluentResult is null || fluentUnit is null)
            return EquatableArray<OperatorTemplate>.Empty;

        var templates = new List<OperatorTemplate>();
        foreach (var type in core.GetTypeMembers().Where(IsOperatorCore).OrderBy(type => type.Name, System.StringComparer.Ordinal))
        {
            var group = type.Name.Substring("Result".Length, type.Name.Length - "Result".Length - "Core".Length);
            var isStatic = type.Name == StaticCore;
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(IsOperator))
            {
                if (Template(method, group, isStatic, fluentResult, fluentUnit) is { } template)
                    templates.Add(template);
            }
        }

        return new EquatableArray<OperatorTemplate>(templates.ToImmutableArray());
    }

    private static bool IsOperatorCore(INamedTypeSymbol type) =>
        type.DeclaredAccessibility == Accessibility.Public
        && type.IsStatic
        && type.Name.StartsWith("Result", System.StringComparison.Ordinal)
        && type.Name.EndsWith("Core", System.StringComparison.Ordinal)
        && type.Name != MemberCore
        && type.ContainingNamespace.ToDisplayString() == CoreNamespace;

    private static bool IsOperator(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public && method.IsStatic;

    private static OperatorTemplate? Template(
        IMethodSymbol method,
        string group,
        bool isStatic,
        INamedTypeSymbol fluentResult,
        INamedTypeSymbol fluentUnit)
    {
        var roles = new Dictionary<ITypeParameterSymbol, Role>(SymbolEqualityComparer.Default);
        ITypeParameterSymbol? error = null;
        foreach (var parameter in method.TypeParameters)
        {
            foreach (var constraint in parameter.ConstraintTypes.OfType<INamedTypeSymbol>())
            {
                var definition = constraint.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(definition, fluentResult)
                    && SymbolEqualityComparer.Default.Equals(constraint.TypeArguments[0], parameter))
                {
                    roles[parameter] = Role.Result(constraint.TypeArguments[1]);
                    error = constraint.TypeArguments[2] as ITypeParameterSymbol;
                }
                else if (SymbolEqualityComparer.Default.Equals(definition, fluentUnit)
                         && SymbolEqualityComparer.Default.Equals(constraint.TypeArguments[0], parameter))
                {
                    roles[parameter] = Role.Unit;
                    error = constraint.TypeArguments[1] as ITypeParameterSymbol;
                }
            }
        }

        if (error is null || roles.Count == 0)
            return null;

        roles[error] = Role.Error;
        var printer = new TypePrinter(roles);

        var kept = method.TypeParameters.Where(parameter => !roles.ContainsKey(parameter)).ToList();
        var typeParameters = kept.Count == 0 ? "" : "<" + string.Join(", ", kept.Select(parameter => parameter.Name)) + ">";
        var constraints = string.Concat(kept.Select(parameter => Constraint(parameter, printer)));

        var parameters = new StringBuilder();
        var arguments = new StringBuilder();
        for (var index = 0; index < method.Parameters.Length; index++)
        {
            var parameter = method.Parameters[index];
            if (index > 0)
            {
                parameters.Append(", ");
                arguments.Append(", ");
            }

            if (index == 0 && !isStatic)
                parameters.Append("this ");
            if (parameter.IsParams)
                parameters.Append("params ");
            if (parameter.RefKind == RefKind.Out)
            {
                parameters.Append("out ");
                arguments.Append("out ");
            }

            parameters.Append(ParameterAttributes(parameter));
            parameters.Append(printer.Print(parameter.Type)).Append(' ').Append(Identifier(parameter.Name));
            if (parameter.HasExplicitDefaultValue)
                parameters.Append(" = ").Append(DefaultValue(parameter));
            arguments.Append(Identifier(parameter.Name));
        }

        var typeArguments = string.Join(", ", method.TypeParameters.Select(parameter => printer.Print(parameter)));
        var call = $"global::{method.ContainingType.ToDisplayString()}.{method.Name}<{typeArguments}>({arguments})";
        var name = method.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "Fluent.Unions.Core.OperatorNameAttribute")
            ?.ConstructorArguments[0].Value as string ?? method.Name;

        return new OperatorTemplate(
            group,
            name,
            isStatic,
            typeParameters,
            parameters.ToString(),
            printer.Print(method.ReturnType),
            constraints,
            call,
            Attributes(method),
            method.GetDocumentationCommentId() ?? "",
            method.ReturnsVoid);
    }

    private static string Constraint(ITypeParameterSymbol parameter, TypePrinter printer)
    {
        var parts = new List<string>();
        if (parameter.HasReferenceTypeConstraint)
            parts.Add("class");
        if (parameter.HasValueTypeConstraint)
            parts.Add("struct");
        if (parameter.HasNotNullConstraint)
            parts.Add("notnull");
        parts.AddRange(parameter.ConstraintTypes.Select(type => printer.Print(type)));
        if (parameter.HasConstructorConstraint)
            parts.Add("new()");

        return parts.Count == 0 ? "" : $" where {parameter.Name} : {string.Join(", ", parts)}";
    }

    private static string Attributes(IMethodSymbol method)
    {
        var text = new StringBuilder();
        foreach (var attribute in method.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            if (name is null || !CopiedAttributes.Contains(name))
                continue;

            var arguments = attribute.ConstructorArguments.Select(Constant)
                .Concat(attribute.NamedArguments.Select(argument => $"{argument.Key} = {Constant(argument.Value)}"));
            text.Append($"[global::{name}({string.Join(", ", arguments)})]");
        }

        return text.ToString();
    }

    private static string ParameterAttributes(IParameterSymbol parameter) =>
        parameter.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.MaybeNullAttribute")
            ? "[global::System.Diagnostics.CodeAnalysis.MaybeNull] "
            : "";

    private static string Constant(TypedConstant constant) => constant.Kind switch
    {
        TypedConstantKind.Enum => $"(global::{constant.Type!.ToDisplayString()}){System.Convert.ToString(constant.Value, CultureInfo.InvariantCulture)}",
        _ when constant.Value is string text => SymbolDisplay.FormatLiteral(text, quote: true),
        _ when constant.Value is bool flag => flag ? "true" : "false",
        _ when constant.Value is null => "null",
        _ => System.Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ?? "null"
    };

    private static string DefaultValue(IParameterSymbol parameter) => parameter.ExplicitDefaultValue switch
    {
        null => parameter.Type.IsValueType && parameter.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T ? "default" : "null",
        string text => SymbolDisplay.FormatLiteral(text, quote: true),
        bool flag => flag ? "true" : "false",
        var value => System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "default"
    };

    private static string Identifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != Microsoft.CodeAnalysis.CSharp.SyntaxKind.None ? "@" + name : name;
}

/// <summary>
/// What a type parameter of a core method stands for in a generated forwarder.
/// </summary>
internal sealed class Role
{
    private Role(RoleKind kind, ITypeSymbol? value)
    {
        Kind = kind;
        Value = value;
    }

    internal static Role Unit { get; } = new(RoleKind.Unit, null);

    internal static Role Error { get; } = new(RoleKind.Error, null);

    internal RoleKind Kind { get; }

    internal ITypeSymbol? Value { get; }

    internal static Role Result(ITypeSymbol value) => new(RoleKind.Result, value);
}

internal enum RoleKind
{
    Result,
    Unit,
    Error
}

/// <summary>
/// Prints a type of a core method with result and error type parameters replaced by tokens.
/// </summary>
internal sealed class TypePrinter(Dictionary<ITypeParameterSymbol, Role> roles)
{
    internal string Print(ITypeSymbol type)
    {
        var text = PrintCore(type);
        return type.NullableAnnotation == NullableAnnotation.Annotated && !type.IsValueType && !text.EndsWith("?", System.StringComparison.Ordinal)
            ? text + "?"
            : text;
    }

    private string PrintCore(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol parameter when roles.TryGetValue(parameter, out var role):
                return role.Kind switch
                {
                    RoleKind.Result => $"{OperatorReader.ResultToken}<{Print(role.Value!)}>",
                    RoleKind.Unit => OperatorReader.ResultToken,
                    _ => OperatorReader.ErrorToken
                };
            case ITypeParameterSymbol parameter:
                return parameter.Name;
            case IArrayTypeSymbol array:
                return Print(array.ElementType) + "[]";
            case INamedTypeSymbol { IsTupleType: true } tuple:
                return "(" + string.Join(", ", tuple.TupleElements.Select(element =>
                    element.IsExplicitlyNamedTupleElement ? $"{Print(element.Type)} {element.Name}" : Print(element.Type))) + ")";
            case INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable:
                return Print(nullable.TypeArguments[0]) + "?";
            case INamedTypeSymbol named when Keyword(named.SpecialType) is { } keyword:
                return keyword;
            case INamedTypeSymbol named:
                var name = "global::" + named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                    .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
                    .WithGenericsOptions(SymbolDisplayGenericsOptions.None)
                    .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.None));
                return named.TypeArguments.Length == 0
                    ? name
                    : $"{name}<{string.Join(", ", named.TypeArguments.Select(Print))}>";
            default:
                return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
    }

    private static string? Keyword(SpecialType type) => type switch
    {
        SpecialType.System_Object => "object",
        SpecialType.System_Boolean => "bool",
        SpecialType.System_String => "string",
        SpecialType.System_Int32 => "int",
        SpecialType.System_Int64 => "long",
        SpecialType.System_Void => "void",
        SpecialType.System_Byte => "byte",
        SpecialType.System_Double => "double",
        SpecialType.System_Decimal => "decimal",
        _ => null
    };
}
