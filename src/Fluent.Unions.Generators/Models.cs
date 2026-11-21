using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
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
    EquatableArray<string> DeclaredMembers,
    bool IsLibrary,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public string FullName => Namespace.Length == 0 ? $"global::{Name}" : $"global::{Namespace}.{Name}";

    public string HintName => (Namespace.Length == 0 ? Name : $"{Namespace}.{Name}") + (TypeParameter is null ? "" : "`1");
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
/// A diagnostic with a location that can be compared for caching.
/// </summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, string? FilePath, TextSpan Span, LinePositionSpan LineSpan, string Argument)
{
    public Diagnostic ToDiagnostic() => Diagnostic.Create(
        Descriptor,
        FilePath is null ? Location.None : Location.Create(FilePath, Span, LineSpan),
        Argument);

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, string argument) =>
        new(descriptor, location?.SourceTree?.FilePath, location?.SourceSpan ?? default, location?.GetLineSpan().Span ?? default, argument);
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

    public bool Equals(EquatableArray<T> other) => Items.SequenceEqual(other.Items);

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
