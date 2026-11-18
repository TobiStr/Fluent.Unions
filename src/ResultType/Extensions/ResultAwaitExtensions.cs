namespace ResultType.Extensions;

/// <summary>
/// Fluent operators that take asynchronous lambdas, named after System.Linq.Async (<c>SelectAwait</c>, ...).
/// </summary>
public static class ResultAwaitExtensions
{
    /// <summary>
    /// Projects the value with an asynchronous selector.
    /// </summary>
    public static Task<Result<TOut>> SelectAwait<T, TOut>(this Result<T> result, Func<T, Task<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => Task.FromResult<Result<TOut>>(error),
            T value => Capture.RunAsync(async () => new Result<TOut>(await selector(value).ConfigureAwait(false))),
            _ => Task.FromResult(default(Result<TOut>))
        };
    }

    /// <summary>
    /// Produces a value with an asynchronous selector after a successful <see cref="Result"/>.
    /// </summary>
    public static Task<Result<TOut>> SelectAwait<TOut>(this Result result, Func<Task<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => Task.FromResult<Result<TOut>>(error),
            Ok => Capture.RunAsync(async () => new Result<TOut>(await selector().ConfigureAwait(false))),
            _ => Task.FromResult(default(Result<TOut>))
        };
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a result itself.
    /// </summary>
    public static Task<Result<TOut>> SelectManyAwait<T, TOut>(this Result<T> result, Func<T, Task<Result<TOut>>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => Task.FromResult<Result<TOut>>(error),
            T value => Capture.RunAsync(() => selector(value)),
            _ => Task.FromResult(default(Result<TOut>))
        };
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a <see cref="Result"/> without a value.
    /// </summary>
    public static Task<Result> SelectManyAwait<T>(this Result<T> result, Func<T, Task<Result>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => Task.FromResult<Result>(error),
            T value => Capture.RunAsync(() => selector(value)),
            _ => Task.FromResult(default(Result))
        };
    }

    /// <summary>
    /// Chains an asynchronous operation after a successful <see cref="Result"/>.
    /// </summary>
    public static Task<Result> SelectManyAwait(this Result result, Func<Task<Result>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => Task.FromResult<Result>(error),
            Ok => Capture.RunAsync(selector),
            _ => Task.FromResult(default(Result))
        };
    }

    /// <summary>
    /// Chains an asynchronous operation that produces a value after a successful <see cref="Result"/>.
    /// </summary>
    public static Task<Result<TOut>> SelectManyAwait<TOut>(this Result result, Func<Task<Result<TOut>>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => Task.FromResult<Result<TOut>>(error),
            Ok => Capture.RunAsync(selector),
            _ => Task.FromResult(default(Result<TOut>))
        };
    }

    /// <inheritdoc cref="SelectManyAwait{T, TOut}(Result{T}, Func{T, Task{Result{TOut}}})"/>
    public static Task<Result<TOut>> ChainAwait<T, TOut>(this Result<T> result, Func<T, Task<Result<TOut>>> selector) =>
        result.SelectManyAwait(selector);

    /// <inheritdoc cref="SelectManyAwait{T}(Result{T}, Func{T, Task{Result}})"/>
    public static Task<Result> ChainAwait<T>(this Result<T> result, Func<T, Task<Result>> selector) =>
        result.SelectManyAwait(selector);

    /// <inheritdoc cref="SelectManyAwait(Result, Func{Task{Result}})"/>
    public static Task<Result> ChainAwait(this Result result, Func<Task<Result>> selector) =>
        result.SelectManyAwait(selector);

    /// <inheritdoc cref="SelectManyAwait{TOut}(Result, Func{Task{Result{TOut}}})"/>
    public static Task<Result<TOut>> ChainAwait<TOut>(this Result result, Func<Task<Result<TOut>>> selector) =>
        result.SelectManyAwait(selector);

    /// <summary>
    /// Keeps the value if the asynchronous predicate is satisfied, otherwise the result becomes empty.
    /// </summary>
    public static Task<Result<T>> WhereAwait<T>(this Result<T> result, Func<T, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return result.Value switch
        {
            Exception => Task.FromResult(result),
            T value => Capture.RunAsync(async () => await predicate(value).ConfigureAwait(false) ? result : default),
            _ => Task.FromResult(result)
        };
    }

    /// <summary>
    /// Runs an asynchronous side effect with the value and returns the same result.
    /// </summary>
    public static Task<Result<T>> DoAwait<T>(this Result<T> result, Func<T, Task> onNext)
    {
        ArgumentNullException.ThrowIfNull(onNext);

        return result.Value switch
        {
            Exception => Task.FromResult(result),
            T value => Capture.RunAsync(async () =>
            {
                await onNext(value).ConfigureAwait(false);
                return result;
            }),
            _ => Task.FromResult(result)
        };
    }

    /// <summary>
    /// Runs an asynchronous side effect after a successful <see cref="Result"/> and returns the same result.
    /// </summary>
    public static Task<Result> DoAwait(this Result result, Func<Task> onNext)
    {
        ArgumentNullException.ThrowIfNull(onNext);

        return result.Value is Ok
            ? Capture.RunAsync(async () =>
            {
                await onNext().ConfigureAwait(false);
                return result;
            })
            : Task.FromResult(result);
    }

    /// <summary>
    /// Handles the error asynchronously. Return a value to recover, or an exception to replace the error.
    /// </summary>
    public static Task<Result<T>> CatchAwait<T>(this Result<T> result, Func<Exception, Task<Result<T>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is Exception error ? Capture.RunAsync(() => handler(error)) : Task.FromResult(result);
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> asynchronously. Other errors pass through.
    /// </summary>
    public static Task<Result<T>> CatchAwait<T, TException>(this Result<T> result, Func<TException, Task<Result<T>>> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TException error ? Capture.RunAsync(() => handler(error)) : Task.FromResult(result);
    }

    /// <summary>
    /// Handles the error asynchronously. Return <see cref="Result.Ok"/> to recover, or an exception to replace the error.
    /// </summary>
    public static Task<Result> CatchAwait(this Result result, Func<Exception, Task<Result>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is Exception error ? Capture.RunAsync(() => handler(error)) : Task.FromResult(result);
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> asynchronously. Other errors pass through.
    /// </summary>
    public static Task<Result> CatchAwait<TException>(this Result result, Func<TException, Task<Result>> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TException error ? Capture.RunAsync(() => handler(error)) : Task.FromResult(result);
    }

    /// <summary>
    /// Runs an asynchronous action for every result: value, error or empty.
    /// </summary>
    public static Task<Result<T>> FinallyAwait<T>(this Result<T> result, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return Capture.RunAsync(async () =>
        {
            await action().ConfigureAwait(false);
            return result;
        });
    }

    /// <summary>
    /// Runs an asynchronous action for every result: success, error or empty.
    /// </summary>
    public static Task<Result> FinallyAwait(this Result result, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return Capture.RunAsync(async () =>
        {
            await action().ConfigureAwait(false);
            return result;
        });
    }

    /// <inheritdoc cref="SelectAwait{T, TOut}(Result{T}, Func{T, Task{TOut}})"/>
    public static Task<Result<TOut>> SelectAwait<T, TOut>(this Task<Result<T>> task, Func<T, Task<TOut>> selector) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.SelectAwait(selector));

    /// <inheritdoc cref="SelectAwait{TOut}(Result, Func{Task{TOut}})"/>
    public static Task<Result<TOut>> SelectAwait<TOut>(this Task<Result> task, Func<Task<TOut>> selector) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.SelectAwait(selector));

    /// <inheritdoc cref="SelectManyAwait{T, TOut}(Result{T}, Func{T, Task{Result{TOut}}})"/>
    public static Task<Result<TOut>> SelectManyAwait<T, TOut>(this Task<Result<T>> task, Func<T, Task<Result<TOut>>> selector) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.SelectManyAwait(selector));

    /// <inheritdoc cref="SelectManyAwait{T}(Result{T}, Func{T, Task{Result}})"/>
    public static Task<Result> SelectManyAwait<T>(this Task<Result<T>> task, Func<T, Task<Result>> selector) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.SelectManyAwait(selector));

    /// <inheritdoc cref="SelectManyAwait(Result, Func{Task{Result}})"/>
    public static Task<Result> SelectManyAwait(this Task<Result> task, Func<Task<Result>> selector) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.SelectManyAwait(selector));

    /// <inheritdoc cref="SelectManyAwait{TOut}(Result, Func{Task{Result{TOut}}})"/>
    public static Task<Result<TOut>> SelectManyAwait<TOut>(this Task<Result> task, Func<Task<Result<TOut>>> selector) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.SelectManyAwait(selector));

    /// <inheritdoc cref="SelectManyAwait{T, TOut}(Result{T}, Func{T, Task{Result{TOut}}})"/>
    public static Task<Result<TOut>> ChainAwait<T, TOut>(this Task<Result<T>> task, Func<T, Task<Result<TOut>>> selector) =>
        task.SelectManyAwait(selector);

    /// <inheritdoc cref="SelectManyAwait{T}(Result{T}, Func{T, Task{Result}})"/>
    public static Task<Result> ChainAwait<T>(this Task<Result<T>> task, Func<T, Task<Result>> selector) =>
        task.SelectManyAwait(selector);

    /// <inheritdoc cref="SelectManyAwait(Result, Func{Task{Result}})"/>
    public static Task<Result> ChainAwait(this Task<Result> task, Func<Task<Result>> selector) =>
        task.SelectManyAwait(selector);

    /// <inheritdoc cref="SelectManyAwait{TOut}(Result, Func{Task{Result{TOut}}})"/>
    public static Task<Result<TOut>> ChainAwait<TOut>(this Task<Result> task, Func<Task<Result<TOut>>> selector) =>
        task.SelectManyAwait(selector);

    /// <inheritdoc cref="WhereAwait{T}(Result{T}, Func{T, Task{bool}})"/>
    public static Task<Result<T>> WhereAwait<T>(this Task<Result<T>> task, Func<T, Task<bool>> predicate) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.WhereAwait(predicate));

    /// <inheritdoc cref="DoAwait{T}(Result{T}, Func{T, Task})"/>
    public static Task<Result<T>> DoAwait<T>(this Task<Result<T>> task, Func<T, Task> onNext) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.DoAwait(onNext));

    /// <inheritdoc cref="DoAwait(Result, Func{Task})"/>
    public static Task<Result> DoAwait(this Task<Result> task, Func<Task> onNext) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.DoAwait(onNext));

    /// <inheritdoc cref="CatchAwait{T}(Result{T}, Func{Exception, Task{Result{T}}})"/>
    public static Task<Result<T>> CatchAwait<T>(this Task<Result<T>> task, Func<Exception, Task<Result<T>>> handler) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.CatchAwait(handler));

    /// <inheritdoc cref="CatchAwait{T, TException}(Result{T}, Func{TException, Task{Result{T}}})"/>
    public static Task<Result<T>> CatchAwait<T, TException>(this Task<Result<T>> task, Func<TException, Task<Result<T>>> handler)
        where TException : Exception =>
        Capture.ThenAwait(Capture.Settle(task), result => result.CatchAwait(handler));

    /// <inheritdoc cref="CatchAwait(Result, Func{Exception, Task{Result}})"/>
    public static Task<Result> CatchAwait(this Task<Result> task, Func<Exception, Task<Result>> handler) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.CatchAwait(handler));

    /// <inheritdoc cref="CatchAwait{TException}(Result, Func{TException, Task{Result}})"/>
    public static Task<Result> CatchAwait<TException>(this Task<Result> task, Func<TException, Task<Result>> handler)
        where TException : Exception =>
        Capture.ThenAwait(Capture.Settle(task), result => result.CatchAwait(handler));

    /// <inheritdoc cref="FinallyAwait{T}(Result{T}, Func{Task})"/>
    public static Task<Result<T>> FinallyAwait<T>(this Task<Result<T>> task, Func<Task> action) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.FinallyAwait(action));

    /// <inheritdoc cref="FinallyAwait(Result, Func{Task})"/>
    public static Task<Result> FinallyAwait(this Task<Result> task, Func<Task> action) =>
        Capture.ThenAwait(Capture.Settle(task), result => result.FinallyAwait(action));
}
