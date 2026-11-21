using Microsoft.AspNetCore.Http;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Turns results and options into HTTP responses: a value gives 200, an error gives ProblemDetails,
/// and <see cref="None"/> gives 404.
/// </summary>
public static class HttpResultExtensions
{
    /// <summary>
    /// Gives 200 with the value, or ProblemDetails for the error.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result) => result.Match<T, IResult>(
        value => TypedResults.Ok(value),
        error => new ErrorHttpResult(error));

    /// <summary>
    /// Gives 204 for success, or ProblemDetails for the error.
    /// </summary>
    public static IResult ToHttpResult(this Result result) => result.Match<IResult>(
        () => TypedResults.NoContent(),
        error => new ErrorHttpResult(error));

    /// <summary>
    /// Gives 200 with the value, or 404 when the option holds none.
    /// </summary>
    public static IResult ToHttpResult<T>(this Option<T> option) => option.Match<T, IResult>(
        value => TypedResults.Ok(value),
        () => TypedResults.NotFound());

    /// <summary>
    /// Gives 200 with the value, or the response from <paramref name="onError"/> for the error.
    /// </summary>
    public static IResult ToHttpResult<T, TError>(this Result<T, TError> result, Func<TError, IResult> onError)
        where TError : notnull, IFromException<TError>
    {
        ArgumentNullException.ThrowIfNull(onError);

        return result.Match(value => TypedResults.Ok(value), onError);
    }

    /// <summary>
    /// Gives 200 with the value, or the response from <paramref name="onError"/> for the error of a custom result.
    /// </summary>
    public static IResult ToHttpResult<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Func<TError, IResult> onError)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onError);

        return result.Match(value => (IResult)TypedResults.Ok(value), onError);
    }

    /// <summary>
    /// Gives 200 with the value, or ProblemDetails for the exception error of a custom result.
    /// </summary>
    public static IResult ToHttpResult<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception =>
        result.ToHttpResult(error => new ErrorHttpResult(error));

    /// <inheritdoc cref="ToHttpResult{T}(Result{T})"/>
    public static async Task<IResult> ToHttpResult<T>(this Task<Result<T>> task) =>
        (await Settle(task).ConfigureAwait(false)).ToHttpResult();

    /// <inheritdoc cref="ToHttpResult(Result)"/>
    public static async Task<IResult> ToHttpResult(this Task<Result> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return (await Result.TryAsync(() => task).ConfigureAwait(false)).ToHttpResult();
    }

    /// <inheritdoc cref="ToHttpResult{T}(Option{T})"/>
    public static async Task<IResult> ToHttpResult<T>(this Task<Option<T>> task) =>
        (await task.ConfigureAwait(false)).ToHttpResult();

    /// <inheritdoc cref="ToHttpResult{T, TError}(Result{T, TError}, Func{TError, IResult})"/>
    public static async Task<IResult> ToHttpResult<T, TError>(this Task<Result<T, TError>> task, Func<TError, IResult> onError)
        where TError : notnull, IFromException<TError> =>
        (await task.ConfigureAwait(false)).ToHttpResult(onError);

    private static Task<Result<T>> Settle<T>(Task<Result<T>> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return Result.TryAsync(() => task);
    }
}
