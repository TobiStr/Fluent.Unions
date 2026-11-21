using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using VerifyNUnit;

namespace Fluent.Unions.Api.Tests;

public class PublicApiTests
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    private static readonly SymbolDisplayFormat Format = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
            | SymbolDisplayGenericsOptions.IncludeTypeConstraints
            | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeType
            | SymbolDisplayMemberOptions.IncludeModifiers
            | SymbolDisplayMemberOptions.IncludeAccessibility
            | SymbolDisplayMemberOptions.IncludeExplicitInterface
            | SymbolDisplayMemberOptions.IncludeConstantValue
            | SymbolDisplayMemberOptions.IncludeRef,
        kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword | SymbolDisplayKindOptions.IncludeMemberKeyword,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName
            | SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeParamsRefOut
            | SymbolDisplayParameterOptions.IncludeDefaultValue
            | SymbolDisplayParameterOptions.IncludeExtensionThis
            | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private static readonly string[] TrackedAttributes =
    [
        "System.ComponentModel.EditorBrowsableAttribute",
        "System.ObsoleteAttribute",
        "System.Runtime.CompilerServices.UnionAttribute"
    ];

    private static IEnumerable<string> Packages() =>
        Directory.GetDirectories(Path.Combine(RepositoryRoot(), "src"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => name.StartsWith("Fluent.Unions", StringComparison.Ordinal) && !name.Contains("Analyzers") && !name.Contains("Generators"))
            .Order(StringComparer.Ordinal);

    [TestCaseSource(nameof(Packages))]
    public Task PublicApiIsUnchanged(string package) =>
        Verifier.Verify(Describe(package)).UseParameters(package);

    private static string Describe(string package)
    {
        var folder = Path.Combine(RepositoryRoot(), "src", package, "bin", Configuration, "net11.0");
        var assemblyPath = Path.Combine(folder, package + ".dll");
        Assert.That(File.Exists(assemblyPath), Is.True, $"Build {package} first: {assemblyPath}");

        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = Directory.GetFiles(folder, "*.dll")
            .Concat(Directory.GetFiles(runtime, "*.dll"))
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Where(IsManagedAssembly)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create("PublicApi", references: references);
        var assembly = compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Single(symbol => symbol.Name == package);

        var text = new StringBuilder();
        foreach (var type in PublicTypes(assembly.GlobalNamespace).OrderBy(type => type.ToDisplayString(Format), StringComparer.Ordinal))
        {
            text.Append(Attributes(type)).AppendLine(type.ToDisplayString(Format) + Bases(type));
            foreach (var member in type.GetMembers().Where(IsVisible).Select(member => Attributes(member) + member.ToDisplayString(Format)).Order(StringComparer.Ordinal))
                text.Append("  ").AppendLine(member);
            text.AppendLine();
        }

        return text.ToString();
    }

    private static IEnumerable<INamedTypeSymbol> PublicTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers().SelectMany(Nested).Where(IsVisible))
            yield return type;
        foreach (var child in ns.GetNamespaceMembers())
        foreach (var type in PublicTypes(child))
            yield return type;
    }

    private static IEnumerable<INamedTypeSymbol> Nested(INamedTypeSymbol type) =>
        new[] { type }.Concat(type.GetTypeMembers().SelectMany(Nested));

    private static bool IsVisible(ISymbol symbol) =>
        symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal
        && (symbol.ContainingType is null || IsVisible(symbol.ContainingType))
        && symbol is not INamedTypeSymbol { IsImplicitlyDeclared: true }
        && symbol is not IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove };

    private static string Bases(INamedTypeSymbol type)
    {
        var bases = new List<string>();
        if (type.BaseType is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum) } baseType
            && type.TypeKind == TypeKind.Class)
            bases.Add(baseType.ToDisplayString(Format));
        bases.AddRange(type.Interfaces.Select(i => i.ToDisplayString(Format)).Order(StringComparer.Ordinal));
        return bases.Count == 0 ? "" : " : " + string.Join(", ", bases);
    }

    private static string Attributes(ISymbol symbol) =>
        string.Concat(symbol.GetAttributes()
            .Where(a => TrackedAttributes.Contains(a.AttributeClass?.ToDisplayString()))
            .Select(a => "[" + a.AttributeClass!.Name.Replace("Attribute", "") + "] "));

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            System.Reflection.AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fluent.Unions.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
