using Grpc.Core;

namespace Fluent.Unions.Grpc;

/// <summary>
/// Converts result errors to <see cref="RpcException"/>.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Converts the error to an <see cref="RpcException"/> with the status code from <paramref name="options"/>.
    /// An <see cref="RpcException"/> is returned as it is. An unmapped error gives <see cref="StatusCode.Internal"/> without details.
    /// </summary>
    public static RpcException ToRpcException(this Exception error, ResultGrpcOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error is RpcException rpcException)
            return rpcException;

        return (options ?? new ResultGrpcOptions()).TryResolve(error, out var mapping)
            ? new RpcException(new Status(mapping.StatusCode, mapping.ExposeMessage ? error.Message : ""))
            : new RpcException(new Status(StatusCode.Internal, ""));
    }
}
