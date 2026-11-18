namespace ResultType.Extensions;

/// <summary>
/// Runs user code and turns any thrown exception into an error result.
/// </summary>
internal static class Capture
{
    internal static Result<T> Run<T>(Func<Result<T>> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
        {
            return ex;
        }
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
