using Orleans;

namespace Fluent.Unions.Orleans;

/// <summary>
/// The serialized form of an <see cref="Option{T}"/>.
/// </summary>
[GenerateSerializer]
public struct OptionSurrogate<T>
{
    /// <summary>
    /// Indicates whether the option holds a value.
    /// </summary>
    [Id(0)]
    public bool HasValue;

    /// <summary>
    /// The value, if the option holds one.
    /// </summary>
    [Id(1)]
    public T? Value;
}

/// <summary>
/// Converts an <see cref="Option{T}"/> to and from its <see cref="OptionSurrogate{T}"/>. Registered with Orleans by <see cref="SerializerBuilderExtensions.AddUnions"/>.
/// </summary>
[RegisterConverter]
public sealed class OptionConverter<T> : IConverter<Option<T>, OptionSurrogate<T>>
{
    /// <inheritdoc />
    public Option<T> ConvertFromSurrogate(in OptionSurrogate<T> surrogate) =>
        surrogate is { HasValue: true, Value: { } value } ? Option.Some(value) : Option.None;

    /// <inheritdoc />
    public OptionSurrogate<T> ConvertToSurrogate(in Option<T> value) =>
        value.TryGetValue(out var payload)
            ? new OptionSurrogate<T> { HasValue = true, Value = payload }
            : default;
}
