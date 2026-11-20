using Fluent.Unions.Abstractions;
using Mediator;

namespace Fluent.Unions.Mediator;

/// <summary>
/// Returns the exception thrown by a handler as an error result instead of throwing. Cancellation propagates.
/// </summary>
public sealed class ExceptionToResultBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : struct, IErrorResult<TResponse, Exception>
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return TResponse.FromError(exception);
        }
    }
}
