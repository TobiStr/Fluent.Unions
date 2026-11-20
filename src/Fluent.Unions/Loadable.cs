using System.Diagnostics.CodeAnalysis;

namespace Fluent.Unions;

/// <summary>
/// Marks a <see cref="Loadable{T}"/> that has not started loading.
/// </summary>
public readonly record struct NotLoaded;

/// <summary>
/// Marks a <see cref="Loadable{T}"/> that is loading.
/// </summary>
public readonly record struct Loading;

/// <summary>
/// The state of data loaded asynchronously, for example in a UI: not loaded, loading, loaded, or failed.
/// A <c>default</c> value counts as not loaded.
/// </summary>
/// <typeparam name="T">The type of the loaded value.</typeparam>
public union Loadable<T>(NotLoaded, Loading, T, Exception)
{
    /// <summary>
    /// Indicates whether loading has not started.
    /// </summary>
    public bool IsNotLoaded => Value is null or NotLoaded;

    /// <summary>
    /// Indicates whether the data is loading.
    /// </summary>
    public bool IsLoading => Value is Loading;

    /// <summary>
    /// Indicates whether loading failed.
    /// </summary>
    public bool IsFaulted => Value is Exception;

    /// <summary>
    /// Indicates whether the data is loaded.
    /// </summary>
    public bool IsLoaded => TryGetValue(out _);

    /// <summary>
    /// Gets the loaded value, if there is one.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (Value is not (null or NotLoaded or Loading or Exception) && Value is T payload)
        {
            value = payload;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => TryGetValue(out var value) ? value.ToString() ?? "" : "";
}

/// <summary>
/// Creates <see cref="Loadable{T}"/> states.
/// </summary>
public static class Loadable
{
    /// <summary>
    /// Loading has not started. Converts to any <see cref="Loadable{T}"/>.
    /// </summary>
    public static NotLoaded NotLoaded => default;

    /// <summary>
    /// The data is loading. Converts to any <see cref="Loadable{T}"/>.
    /// </summary>
    public static Loading Loading => default;
}
