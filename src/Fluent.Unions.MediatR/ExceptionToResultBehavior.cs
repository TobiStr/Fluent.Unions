using MediatR;

namespace Fluent.Unions.MediatR;

/// <summary>
/// Returns the exception thrown by a handler as an error result instead of throwing. Cancellation propagates.
/// </summary>
public sealed class ExceptionToResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : struct
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException && ExceptionResult<TResponse>.IsResult)
        {
            return ExceptionResult<TResponse>.Create(exception);
        }
    }
}
