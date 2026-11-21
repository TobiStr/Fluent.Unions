namespace Fluent.Unions;

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
    /// Keeps the value of a successful result. An error gives an option without a value.
    /// </summary>
    public static Option<T> ToOption<T>(this Result<T> result) =>
        global::Fluent.Unions.Core.ResultConversionCore.ToOption<Result<T>, T, Exception>(result);

    /// <summary>
    /// Swaps an optional result into a result of an option: no value gives ok with <see cref="None"/>,
    /// and an error stays an error.
    /// </summary>
    public static Result<Option<T>> Transpose<T>(this Option<Result<T>> option) =>
        global::Fluent.Unions.Core.ResultConversionCore.Transpose<Result<T>, T, Exception, Result<Option<T>>>(option);

    /// <summary>
    /// Swaps a result of an option into an optional result: ok with <see cref="None"/> gives no value,
    /// and an error is kept inside the option. Reverses the other <c>Transpose</c>.
    /// </summary>
    public static Option<Result<T>> Transpose<T>(this Result<Option<T>> result) =>
        global::Fluent.Unions.Core.ResultConversionCore.Transpose<Result<Option<T>>, T, Exception, Result<T>>(result);

    /// <summary>
    /// Turns a reference that may be <c>null</c> into a result, using <paramref name="error"/> for <c>null</c>.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error) where T : class =>
        value.ToOption().ToResult(error);

    /// <summary>
    /// Turns a nullable value type into a result, using <paramref name="error"/> for <c>null</c>.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error) where T : struct =>
        value.ToOption().ToResult(error);

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
