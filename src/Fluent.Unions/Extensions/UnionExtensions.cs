using System.Runtime.CompilerServices;

namespace Fluent.Unions.Extensions;

/// <summary>
/// Helpers for any C# union, including your own. A union value type is boxed for the call.
/// </summary>
public static class UnionExtensions
{
    /// <summary>
    /// Indicates whether the union holds a <typeparamref name="TCase"/>.
    /// </summary>
    public static bool Is<TCase>(this IUnion union)
    {
        ArgumentNullException.ThrowIfNull(union);

        return union.Value is TCase;
    }

    /// <summary>
    /// Returns the case as an option: its value when the union holds a <typeparamref name="TCase"/>, otherwise <see cref="None"/>.
    /// </summary>
    public static Option<TCase> As<TCase>(this IUnion union)
    {
        ArgumentNullException.ThrowIfNull(union);

        return union.Value is TCase value ? Option<TCase>.Of(value) : Option<TCase>.NoValue;
    }
}
