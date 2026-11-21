using Grpc.Core;

namespace Fluent.Unions.Grpc;

/// <summary>
/// Converts gRPC client calls to <see cref="Result{T}"/>. An error is an <see cref="RpcException"/> with the status code the call
/// failed with, an empty detail and no trailers, so it can be returned to your own caller without leaking the downstream message.
/// The original is its <c>Status.DebugException</c>. A cancelled call throws <see cref="OperationCanceledException"/>, never an error.
/// </summary>
public static class CallExtensions
{
    /// <summary>
    /// Awaits the unary call and returns its response, or the sanitized <see cref="RpcException"/> as an error.
    /// </summary>
    public static async Task<Result<TResponse>> ToResult<TResponse>(this AsyncUnaryCall<TResponse> call)
    {
        ArgumentNullException.ThrowIfNull(call);

        using (call)
            return await Capture(call.ResponseAsync).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the client-streaming call and returns its response, or the sanitized <see cref="RpcException"/> as an error.
    /// </summary>
    public static async Task<Result<TResponse>> ToResult<TRequest, TResponse>(this AsyncClientStreamingCall<TRequest, TResponse> call)
    {
        ArgumentNullException.ThrowIfNull(call);

        using (call)
            return await Capture(call.ResponseAsync).ConfigureAwait(false);
    }

    private static async Task<Result<TResponse>> Capture<TResponse>(Task<TResponse> response)
    {
        try
        {
            return Result<TResponse>.Ok(await response.ConfigureAwait(false));
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Cancelled)
        {
            throw new OperationCanceledException(exception.Message, exception);
        }
        catch (RpcException exception)
        {
            return Result<TResponse>.Error(new RpcException(new Status(exception.StatusCode, string.Empty, exception)));
        }
    }
}
