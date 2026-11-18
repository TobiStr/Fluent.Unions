using System.Text.Json.Serialization;
using ResultType.Abstractions;
using ResultType.Serialization;

namespace ResultType;

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
    static Result<T, TError> IResult<Result<T, TError>, T, TError>.FromValue(T value) => new(value);

    static Result<T, TError> IErrorResult<Result<T, TError>, TError>.FromError(TError error) => new(error);

    static TError IErrorResult<Result<T, TError>, TError>.FromException(Exception exception) => TError.FromException(exception);

    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success => Value is not TError && Value is T;

    /// <summary>
    /// Gets the value of the successful result.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result failed or is empty.</exception>
    public T GetOk() => Value switch
    {
        TError => throw new InvalidOperationException($"Operation for Result<{typeof(T)}, {typeof(TError)}> was not successful."),
        T value => value,
        _ => throw new InvalidOperationException($"Payload for Result<{typeof(T)}, {typeof(TError)}> was not set.")
    };

    /// <summary>
    /// Gets the error of the failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result did not fail.</exception>
    public TError GetError() => Value is TError error
        ? error
        : throw new InvalidOperationException($"Error property for Result<{typeof(T)}, {typeof(TError)}> not set.");

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? "";
}
