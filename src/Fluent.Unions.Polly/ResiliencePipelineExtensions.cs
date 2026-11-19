using Polly;

namespace Fluent.Unions.Polly;

/// <summary>
/// Runs Polly resilience pipelines and returns the outcome as a <see cref="Result{T}"/>.
/// </summary>
public static class ResiliencePipelineExtensions
{
    /// <summary>
    /// Runs <paramref name="operation"/> through the pipeline. An exception that escapes the pipeline, including
    /// <see cref="OperationCanceledException"/>, becomes an error.
    /// </summary>
    public static async ValueTask<Result<T>> ExecuteResultAsync<T>(
        this ResiliencePipeline<Result<T>> pipeline,
        Func<CancellationToken, ValueTask<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(operation);

        var context = ResilienceContextPool.Shared.Get(cancellationToken);

        try
        {
            var outcome = await pipeline.ExecuteOutcomeAsync(Run, context, operation).ConfigureAwait(false);
            return outcome.ToResult();
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Runs <paramref name="operation"/> through the pipeline. An exception that escapes the pipeline, including
    /// <see cref="OperationCanceledException"/>, becomes an error.
    /// </summary>
    public static async ValueTask<Result<T>> ExecuteResultAsync<T>(
        this ResiliencePipeline pipeline,
        Func<CancellationToken, ValueTask<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(operation);

        var context = ResilienceContextPool.Shared.Get(cancellationToken);

        try
        {
            var outcome = await pipeline.ExecuteOutcomeAsync(Run, context, operation).ConfigureAwait(false);
            return outcome.ToResult();
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Converts a Polly outcome to a result. An exception in the outcome becomes an error, and an outcome without a value is empty.
    /// </summary>
    public static Result<T> ToResult<T>(this Outcome<Result<T>> outcome)
    {
        return outcome.Exception is { } exception ? Result<T>.Error(exception) : outcome.Result;
    }

    private static async ValueTask<Outcome<Result<T>>> Run<T>(ResilienceContext context, Func<CancellationToken, ValueTask<Result<T>>> operation)
    {
        try
        {
            return Outcome.FromResult(await operation(context.CancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Outcome.FromException<Result<T>>(exception);
        }
    }
}
