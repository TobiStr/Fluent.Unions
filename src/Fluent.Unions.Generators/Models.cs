using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Fluent.Unions.Generators;

/// <summary>
/// One generated operator: a forwarder from a result type to a core method.
/// </summary>
internal sealed record OperatorTemplate(
    string Group,
    string Name,
    bool IsStatic,
    string TypeParameters,
    string Parameters,
    string ReturnType,
    string Constraints,
    string Call,
    string Attributes,
    string DocumentationId,
    bool ReturnsVoid);

/// <summary>
/// A struct marked with <c>[FluentResult]</c>.
/// </summary>
internal sealed record ResultTypeModel(
    string Namespace,
    string Name,
    string Accessibility,
    string? TypeParameter,
    string ErrorType,
    bool ErrorIsValueType,
    ExceptionMapping Mapping,
    bool Companion,
    bool CaseConversions,
    EquatableArray<string> ErrorCases,
    string? CaptureFilter,
    EquatableArray<string> DeclaredMembers,
    bool IsLibrary,
    string HintName,
    Locus Location,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public string Identifier => Names.Escape(Name);

    public string FullName => Namespace.Length == 0 ? $"global::{Identifier}" : $"global::{Namespace}.{Identifier}";
}

/// <summary>
/// How an unexpected exception becomes the error type.
/// </summary>
internal enum ExceptionMapping
{
    Missing,
    Identity,
    ErrorType,
    Declared
}

/// <summary>
/// Where a declaration is, in a form that can be compared for caching.
/// </summary>
internal sealed record Locus(string? FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static Locus Of(Location location) =>
        new(location.SourceTree?.FilePath, location.SourceSpan, location.GetLineSpan().Span);

    public Location ToLocation() => FilePath is null ? Location.None : Location.Create(FilePath, Span, LineSpan);
}

/// <summary>
/// A diagnostic with a location that can be compared for caching.
/// </summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, Locus Locus, EquatableArray<string> Arguments)
{
    public Diagnostic ToDiagnostic() => Diagnostic.Create(
        Descriptor,
        Locus.ToLocation(),
        Arguments.Select(argument => (object)argument).ToArray());

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Locus locus, params string[] arguments) =>
        new(descriptor, locus, new EquatableArray<string>(arguments.ToImmutableArray()));
}

/// <summary>
/// Spelling of the names in generated code.
/// </summary>
internal static class Names
{
    internal static string Escape(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    internal static string Sanitize(string name)
    {
        var text = new StringBuilder(name.Length);
        foreach (var character in name)
            text.Append(char.IsLetterOrDigit(character) || character is '.' or '_' or '`' ? character : '_');

        return text.ToString();
    }
}

/// <summary>
/// An immutable array compared by its elements.
/// </summary>
internal readonly struct EquatableArray<T>(ImmutableArray<T> items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static EquatableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    private ImmutableArray<T> Items => items.IsDefault ? ImmutableArray<T>.Empty : items;

    public int Length => Items.Length;

    public bool Equals(EquatableArray<T> other) => Items == other.Items || Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in Items)
            hash = hash * 31 + (item?.GetHashCode() ?? 0);
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
