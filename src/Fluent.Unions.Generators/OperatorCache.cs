using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Fluent.Unions.Generators;

/// <summary>
/// Reads the operator templates once per Fluent.Unions assembly instead of once per compilation.
/// </summary>
internal static class OperatorCache
{
    private const string LibraryName = "Fluent.Unions";

    private static readonly ConditionalWeakTable<IAssemblySymbol, Templates> Cache = new();

    internal static EquatableArray<OperatorTemplate> Get(Compilation compilation) =>
        Library(compilation) is { } library
            ? Cache.GetValue(library, _ => new Templates(OperatorReader.Read(compilation))).Items
            : EquatableArray<OperatorTemplate>.Empty;

    private static IAssemblySymbol? Library(Compilation compilation)
    {
        if (compilation.Assembly.Name == LibraryName)
            return compilation.Assembly;

        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (reference.Name == LibraryName)
                return reference;
        }

        return null;
    }

    private sealed class Templates(EquatableArray<OperatorTemplate> items)
    {
        internal EquatableArray<OperatorTemplate> Items { get; } = items;
    }
}
