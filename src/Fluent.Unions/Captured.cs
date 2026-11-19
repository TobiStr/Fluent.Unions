namespace Fluent.Unions;

/// <summary>
/// A value taken out of a chain with <c>Out(out var value)</c>. It is filled when the chain reaches that step successfully,
/// so later steps of the same chain can use it. Converts implicitly to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the captured value.</typeparam>
public sealed class Captured<T>
{
    private T? value;

    /// <summary>
    /// Indicates whether the chain reached this step with a value.
    /// </summary>
    public bool HasValue { get; private set; }

    /// <summary>
    /// Gets the captured value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The chain failed or was empty before this step.</exception>
    public T Value => HasValue
        ? value!
        : throw new InvalidOperationException(
            $"No {typeof(T).Name} was captured, because the chain did not reach this step with a value.");

    /// <summary>
    /// Gets the captured value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The chain failed or was empty before this step.</exception>
    public static implicit operator T(Captured<T> captured)
    {
        ArgumentNullException.ThrowIfNull(captured);

        return captured.Value;
    }

    /// <inheritdoc />
    public override string ToString() => HasValue ? value?.ToString() ?? "" : "";

    internal void SetFrom(Result<T> result)
    {
        if (result.Value is not Exception && result.Value is T payload)
            Set(payload);
    }

    internal void SetFrom(Option<T> option)
    {
        if (option.TryGetValue(out var payload))
            Set(payload);
    }

    private void Set(T payload)
    {
        value = payload;
        HasValue = true;
    }
}
