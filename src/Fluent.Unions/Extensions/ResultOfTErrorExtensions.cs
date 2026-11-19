using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Extensions;

/// <summary>
/// Operators for <see cref="Result{T, TError}"/> that change the value or error type. The other operators come from
/// <see cref="ResultInterfaceExtensions"/>.
/// </summary>
public static class ResultOfTErrorExtensions
{
    /// <summary>
    /// Projects the value into a new form. A <c>null</c> projection gives an empty result.
    /// </summary>
    public static Result<TOut, TError> Select<T, TError, TOut>(this Result<T, TError> result, Func<T, TOut> selector)
        where TError : notnull, IFromException<TError> =>
        result.Select<Result<T, TError>, T, TError, Result<TOut, TError>, TOut>(selector);

    /// <summary>
    /// Chains an operation that returns a result with the same error type.
    /// </summary>
    public static Result<TOut, TError> SelectMany<T, TError, TOut>(
        this Result<T, TError> result,
        Func<T, Result<TOut, TError>> selector)
        where TError : notnull, IFromException<TError> =>
        result.Chain(selector);

    /// <summary>
    /// Chains an operation and combines both values. Enables query syntax with multiple <c>from</c> clauses.
    /// </summary>
    public static Result<TOut, TError> SelectMany<T, TError, TMid, TOut>(
        this Result<T, TError> result,
        Func<T, Result<TMid, TError>> selector,
        Func<T, TMid, TOut> resultSelector)
        where TError : notnull, IFromException<TError>
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return result.Chain(value => selector(value).Select(mid => resultSelector(value, mid)));
    }

    /// <summary>
    /// Converts the error into another error type.
    /// </summary>
    public static Result<T, TOutError> MapError<T, TError, TOutError>(this Result<T, TError> result, Func<TError, TOutError> map)
        where TError : notnull, IFromException<TError>
        where TOutError : notnull, IFromException<TOutError> =>
        result.MapError<Result<T, TError>, T, TError, Result<T, TOutError>, TOutError>(map);
}
