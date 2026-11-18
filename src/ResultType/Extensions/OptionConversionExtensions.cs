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
    /// Keeps the value of a successful result. Errors and empty results give an option without a value.
    /// </summary>
    public static Option<T> ToOption<T>(this Result<T> result) => result.Value switch
    {
        Exception => Option<T>.NoValue,
        T value => Option<T>.Of(value),
        _ => Option<T>.NoValue
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
