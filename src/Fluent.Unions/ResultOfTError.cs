using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Serialization;

namespace Fluent.Unions;

/// <summary>
/// A result whose error is data instead of an exception, typically a union of error records:
/// <c>Result&lt;Order, CheckoutError&gt;</c>. Unexpected exceptions become errors through
/// <see cref="IFromException{TSelf}.FromException"/>, so chains still do not throw.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <typeparam name="TError">The type of the error.</typeparam>
[JsonConverter(typeof(ResultJsonConverter))]
public union Result<T, TError>(T, TError) : IResult<Result<T, TError>, T, TError>
    where TError : notnull, IFromException<TError>
{
    static Result<T, TError> IResult<Result<T, TError>, T, TError>.FromValue(T value) => Of(value);

    static Result<T, TError> IErrorResult<Result<T, TError>, TError>.FromError(TError error) => new(error);

    static TError IErrorResult<Result<T, TError>, TError>.FromException(Exception exception) => TError.FromException(exception);

    /// <summary>
    /// Indicates whether the result holds a value.
    /// </summary>
    public bool IsSuccess => Value is not (null or TError);

    /// <summary>
    /// Indicates whether the result holds an error.
    /// </summary>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result holds an error. An error that is an exception becomes the inner exception.</exception>
    public T GetValue() => TryGetValue(out var value)
        ? value
        : throw new InvalidOperationException(
            $"{ResultErrors.Name(typeof(Result<T, TError>))} holds an error, not a value: {Case}", Case as Exception);

    /// <summary>
    /// Gets the value, if the result holds one.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (IsSuccess)
        {
            value = (T)Value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result holds a value.</exception>
    public TError GetError() => TryGetError(out var error)
        ? error
        : throw new InvalidOperationException($"{ResultErrors.Name(typeof(Result<T, TError>))} holds a value, not an error.");

    /// <summary>
    /// Gets the error, if the result holds one.
    /// </summary>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        if (Case is TError found)
        {
            error = found;
            return true;
        }

        error = default;
        return false;
    }

    internal object Case => Value ?? TError.FromException(ResultErrors.Uninitialized(typeof(Result<T, TError>)));

    internal static Result<T, TError> Of(T? value) =>
        value is null ? new(TError.FromException(ResultErrors.Uninitialized(typeof(Result<T, TError>)))) : new(value);

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? "";
}
