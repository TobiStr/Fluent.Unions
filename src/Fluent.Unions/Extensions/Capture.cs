using Fluent.Unions.Abstractions;

namespace Fluent.Unions;

/// <summary>
/// Runs user code and turns a thrown exception into an error result. Cancellation propagates.
/// </summary>
internal static class Capture
{
    internal static Result<T> Run<T>(Func<Result<T>> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ex;
        }
    }

    internal static Result Run(Func<Result> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ex;
        }
    }

    internal static async Task<Result<T>> RunAsync<T>(Func<Task<Result<T>>> body)
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ex;
        }
    }

    internal static async Task<Result> RunAsync(Func<Task<Result>> body)
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ex;
        }
    }

    internal static TResult Run<TResult, TError>(Func<TResult> body)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            return body();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return TResult.FromError(ToError<TResult, TError>(ex));
        }
    }

    internal static async Task<TResult> RunAsync<TResult, TError>(Func<Task<TResult>> body)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return TResult.FromError(ToError<TResult, TError>(ex));
        }
    }

    internal static TError ToError<TResult, TError>(Exception exception)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        if (exception is TError error)
            return error;

        var mapped = TResult.FromException(exception);

        return mapped is null
            ? throw new InvalidOperationException($"{typeof(TResult)}.FromException returned null.", exception)
            : mapped;
    }

    internal static Task<Result<T>> Settle<T>(Task<Result<T>> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return RunAsync(() => task);
    }

    internal static Task<Result> Settle(Task<Result> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return RunAsync(() => task);
    }

    internal static async Task<TOut> Then<TIn, TOut>(Task<TIn> source, Func<TIn, TOut> next) =>
        next(await source.ConfigureAwait(false));

    internal static async Task<TOut> ThenAwait<TIn, TOut>(Task<TIn> source, Func<TIn, Task<TOut>> next) =>
        await next(await source.ConfigureAwait(false)).ConfigureAwait(false);
}
