using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace Fluent.Unions.Grpc;

/// <summary>
/// Turns an exception thrown by a service method, such as the error of <c>result.Check()</c>, into an <see cref="RpcException"/>
/// with the status code from <see cref="ResultGrpcOptions"/>, configured with <c>services.Configure&lt;ResultGrpcOptions&gt;(...)</c>.
/// An exception without a mapping is rethrown, so gRPC logs and reports it as usual. An <see cref="RpcException"/> is left as it is.
/// An <see cref="OperationCanceledException"/> after the client cancelled the call becomes
/// <see cref="StatusCode.Cancelled"/>, which <c>ToResult()</c> turns back into an <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class ResultServerInterceptor(IOptions<ResultGrpcOptions>? options = null) : Interceptor
{
    private readonly ResultGrpcOptions options = options?.Value ?? new ResultGrpcOptions();

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context).ConfigureAwait(false);
        }
        catch (Exception exception) when (Translate(exception, context) is { } translated)
        {
            throw translated;
        }
    }

    /// <inheritdoc />
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context).ConfigureAwait(false);
        }
        catch (Exception exception) when (Translate(exception, context) is { } translated)
        {
            throw translated;
        }
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context).ConfigureAwait(false);
        }
        catch (Exception exception) when (Translate(exception, context) is { } translated)
        {
            throw translated;
        }
    }

    /// <inheritdoc />
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context).ConfigureAwait(false);
        }
        catch (Exception exception) when (Translate(exception, context) is { } translated)
        {
            throw translated;
        }
    }

    private RpcException? Translate(Exception exception, ServerCallContext context) => exception switch
    {
        OperationCanceledException when context.CancellationToken.IsCancellationRequested =>
            new RpcException(new Status(StatusCode.Cancelled, "The call was cancelled.")),
        RpcException or OperationCanceledException => null,
        _ when options.TryResolve(exception, out _) => exception.ToRpcException(options),
        _ => null
    };
}
