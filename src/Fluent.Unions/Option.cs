using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;

namespace Fluent.Unions;

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
public union Option<T>(T, None) : IEquatable<Option<T>>
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

    /// <summary>
    /// Indicates whether both options hold no value, or hold equal values.
    /// </summary>
    public bool Equals(Option<T> other) => HasValue ? other.HasValue && Equals(Value, other.Value) : !other.HasValue;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HasValue ? Value!.GetHashCode() : 0;

    /// <summary>
    /// Indicates whether both options hold no value, or hold equal values.
    /// </summary>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <summary>
    /// Indicates whether the options differ.
    /// </summary>
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

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

    /// <summary>
    /// Parses <paramref name="s"/> into a value, or gives <see cref="None"/> when it cannot be parsed.
    /// </summary>
    public static Option<T> TryParse<T>(string? s, IFormatProvider? provider = null) where T : IParsable<T> =>
        T.TryParse(s, provider, out var value) ? Option<T>.Of(value) : Option<T>.NoValue;

    /// <summary>
    /// Parses <paramref name="s"/> into a value, or gives <see cref="None"/> when it cannot be parsed.
    /// </summary>
    public static Option<T> TryParse<T>(ReadOnlySpan<char> s, IFormatProvider? provider = null) where T : ISpanParsable<T> =>
        T.TryParse(s, provider, out var value) ? Option<T>.Of(value) : Option<T>.NoValue;

    internal static Option<T> Normalize<T>(this Option<T> option) => option.HasValue ? option : Option<T>.NoValue;
}
