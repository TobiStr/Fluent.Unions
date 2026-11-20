using System.Net;
using System.Runtime.ExceptionServices;
using Refit;

namespace Fluent.Unions.Refit;

/// <summary>
/// Converts Refit API responses to <see cref="Result"/>, <see cref="Result{T}"/> and <c>Result&lt;Option&lt;T&gt;&gt;</c>.
/// An error is the exception Refit reports. Cancellation and timeouts are thrown, never turned into an error.
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Converts the response to a result. A failed request or response becomes an error.
    /// </summary>
    public static Result ToResult(this IApiResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return GetError(response) is { } error ? Result.Error(error) : Result.Ok;
    }

    /// <summary>
    /// Converts the response to a result holding its content. A failed request, a failed response or a missing content becomes an error.
    /// </summary>
    public static Result<T> ToResult<T>(this IApiResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (GetError(response) is { } error)
            return Result<T>.Error(error);

        return response.Content is { } content
            ? Result<T>.Ok(content)
            : Result<T>.Error(new InvalidOperationException("The response has no content."));
    }

    /// <summary>
    /// Converts the response to a result holding an option. A 404 response or a missing content gives <see cref="Option.None"/>;
    /// any other failure becomes an error.
    /// </summary>
    public static Result<Option<T>> ToOptionalResult<T>(this IApiResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (GetError(response) is { } error)
            return response.StatusCode == HttpStatusCode.NotFound && response.Error is ApiException
                ? Result<Option<T>>.Ok(Option.None)
                : Result<Option<T>>.Error(error);

        return Result<Option<T>>.Ok(response.Content is { } content ? Option.Some(content) : Option.None);
    }

    /// <summary>
    /// Awaits the call and converts the response like <see cref="ToResult(IApiResponse)"/>.
    /// </summary>
    public static async Task<Result> ToResult(this Task<IApiResponse> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return (await response.ConfigureAwait(false)).ToResult();
    }

    /// <summary>
    /// Awaits the call and converts the response like <see cref="ToResult{T}(IApiResponse{T})"/>.
    /// </summary>
    public static async Task<Result<T>> ToResult<T>(this Task<IApiResponse<T>> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return (await response.ConfigureAwait(false)).ToResult();
    }

    /// <summary>
    /// Awaits the call and converts the response like <see cref="ToResult{T}(IApiResponse{T})"/>.
    /// </summary>
    public static async Task<Result<T>> ToResult<T>(this Task<ApiResponse<T>> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return (await response.ConfigureAwait(false)).ToResult();
    }

    /// <summary>
    /// Awaits the call and converts the response like <see cref="ToOptionalResult{T}(IApiResponse{T})"/>.
    /// </summary>
    public static async Task<Result<Option<T>>> ToOptionalResult<T>(this Task<IApiResponse<T>> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return (await response.ConfigureAwait(false)).ToOptionalResult();
    }

    /// <summary>
    /// Awaits the call and converts the response like <see cref="ToOptionalResult{T}(IApiResponse{T})"/>.
    /// </summary>
    public static async Task<Result<Option<T>>> ToOptionalResult<T>(this Task<ApiResponse<T>> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return (await response.ConfigureAwait(false)).ToOptionalResult();
    }

    private static Exception? GetError(IApiResponse response)
    {
        var error = response.Error;

        if (error?.InnerException is OperationCanceledException canceled)
            ExceptionDispatchInfo.Throw(canceled);

        return error;
    }
}
