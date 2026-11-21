namespace Fluent.Unions;

/// <summary>
/// The success case of a <see cref="Result"/>.
/// </summary>
public readonly record struct Ok;

/// <summary>
/// The outcome of an operation without a value: <see cref="Ok"/> or an <see cref="Exception"/>.
/// A <c>default</c> result counts as an error.
/// </summary>
[FluentResult<Exception>]
public readonly partial struct Result;

/// <summary>
/// The outcome of an operation: a <typeparamref name="T"/> or an <see cref="Exception"/>.
/// A <c>default</c> result, or one created from <c>null</c>, counts as an error.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
[FluentResult<Exception>(Companion = false)]
public readonly partial struct Result<T>
{
    internal object Case => Value ?? ResultErrors.Uninitialized(typeof(Result<T>));

    internal static Result<T> Of(T? value) => new(value!);
}
