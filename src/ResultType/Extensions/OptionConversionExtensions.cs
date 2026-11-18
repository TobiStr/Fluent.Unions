namespace ResultType.Extensions;

/// <summary>
/// Conversions between nullable values, options and results.
/// </summary>
public static class OptionConversionExtensions
{
    /// <summary>
    /// Wraps a reference that may be <c>null</c>.
    /// </summary>
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        Option<T>.Of(value);

    /// <summary>
    /// Wraps a nullable value type.
    /// </summary>
    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value.HasValue ? new Option<T>(value.Value) : Option<T>.NoValue;

    /// <summary>
    /// Returns the value as a nullable value type, <c>null</c> when the option holds none.
    /// </summary>
    public static T? ToNullable<T>(this Option<T> option) where T : struct =>
        option.TryGetValue(out var value) ? value : null;

    /// <summary>
    /// Keeps the value of a successful result. Errors and empty results give an option without a value.
    /// </summary>
    public static Option<T> ToOption<T>(this Result<T> result) => result.Value switch
    {
        Exception => Option<T>.NoValue,
        T value => Option<T>.Of(value),
        _ => Option<T>.NoValue
    };

    /// <summary>
    /// Swaps an optional result into a result of an option: no value gives ok with <see cref="None"/>,
    /// an error stays an error, and an empty inner result stays empty.
    /// </summary>
    public static Result<Option<T>> Transpose<T>(this Option<Result<T>> option)
    {
        if (!option.TryGetValue(out var inner))
            return new Result<Option<T>>(Option<T>.NoValue);

        return inner.Value switch
        {
            Exception error => error,
            T value => new Result<Option<T>>(Option<T>.Of(value)),
            _ => default
        };
    }

    /// <summary>
    /// Swaps a result of an option into an optional result: ok with <see cref="None"/> gives no value,
    /// and errors and empty results are kept inside the option. Reverses the other <c>Transpose</c>.
    /// </summary>
    public static Option<Result<T>> Transpose<T>(this Result<Option<T>> result) => result.Value switch
    {
        Exception error => new Option<Result<T>>(new Result<T>(error)),
        Option<T> inner => inner.TryGetValue(out var value)
            ? new Option<Result<T>>(new Result<T>(value))
            : Option<Result<T>>.NoValue,
        _ => new Option<Result<T>>(default(Result<T>))
    };

    /// <summary>
    /// Turns the option into a result, using <paramref name="error"/> when it holds no value.
    /// </summary>
    public static Result<T> ToResult<T>(this Option<T> option, Func<Exception> error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return option.TryGetValue(out var value)
            ? new Result<T>(value)
            : Capture.Run(() => new Result<T>(error() ?? throw new InvalidOperationException("The error factory returned null.")));
    }
}
