using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ResultType.Serialization;

namespace ResultType;

/// <summary>
/// Marks an <see cref="Option{T}"/> without a value.
/// </summary>
public readonly record struct None;

/// <summary>
/// A value that may be missing: holds a <typeparamref name="T"/> or <see cref="None"/>.
/// A <c>default</c> option holds no value either.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
[JsonConverter(typeof(OptionJsonConverter))]
public union Option<T>(T, None)
{
    /// <summary>
    /// Indicates whether the option holds a value.
    /// </summary>
    public bool HasValue => Value is not (null or None);

    /// <summary>
    /// Gets the value, if the option holds one.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (Value is not (null or None) && Value is T payload)
        {
            value = payload;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The option holds no value.</exception>
    public T GetValue() => TryGetValue(out var value)
        ? value
        : throw new InvalidOperationException($"Option<{typeof(T)}> holds no value.");

    /// <inheritdoc />
    public override string ToString() => TryGetValue(out var value) ? value.ToString() ?? "" : "";

    internal static Option<T> NoValue => new(default(None));

    internal static Option<T> Of(T? value) => value is null ? NoValue : new(value);
}

/// <summary>
/// Creates options.
/// </summary>
public static class Option
{
    /// <summary>
    /// An option without a value. Converts to any <see cref="Option{T}"/>.
    /// </summary>
    public static None None => default;

    /// <summary>
    /// An option with a value.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static Option<T> Some<T>(T value) => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal static Option<T> Normalize<T>(this Option<T> option) => option.HasValue ? option : Option<T>.NoValue;
}
