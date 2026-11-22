using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Fluent operators that take asynchronous lambdas, named after System.Linq.Async (<c>SelectAwait</c>, ...).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class ResultAwaitCore
{
    /// <summary>
    /// Projects the value with an asynchronous selector.
    /// </summary>
    public static Task<TOutResult> SelectAwait<TSelf, T, TError, TOutResult, TOut>(TSelf result, Func<T, Task<TOut>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result)));

        return Run(selector, value);

        static async Task<TOutResult> Run(Func<T, Task<TOut>> selector, T value)
        {
            try
            {
                return TOutResult.FromValue(await selector(value).ConfigureAwait(false));
            }
            catch (Exception exception) when (Flow.Captured<TOutResult, TError>(exception))
            {
                return Flow.Thrown<TOutResult, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Produces a value with an asynchronous selector after a successful result without a value.
    /// </summary>
    public static Task<TOutResult> SelectAwait<TSelf, TError, TOutResult, TOut>(TSelf result, Func<Task<TOut>> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
            return Task.FromResult(TOutResult.FromError(error));

        return Run(selector);

        static async Task<TOutResult> Run(Func<Task<TOut>> selector)
        {
            try
            {
                return TOutResult.FromValue(await selector().ConfigureAwait(false));
            }
            catch (Exception exception) when (Flow.Captured<TOutResult, TError>(exception))
            {
                return Flow.Thrown<TOutResult, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a result itself.
    /// </summary>
    public static Task<TOutResult> ChainAwait<TSelf, T, TError, TOutResult, TOut>(TSelf result, Func<T, Task<TOutResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result)));

        return Invoke<T, TOutResult, TError>(selector, value);
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a result without a value.
    /// </summary>
    public static Task<TUnit> ChainAwait<TSelf, T, TError, TUnit>(TSelf result, Func<T, Task<TUnit>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(TUnit.FromError(Flow.ErrorOf<TSelf, T, TError>(result)));

        return Invoke<T, TUnit, TError>(selector, value);
    }

    /// <summary>
    /// Chains an asynchronous operation after a successful result without a value.
    /// </summary>
    public static Task<TSelf> ChainAwait<TSelf, TError>(TSelf result, Func<Task<TSelf>> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.TryGetError(out _) ? Task.FromResult(result) : Invoke<TSelf, TError>(selector);
    }

    /// <summary>
    /// Chains an asynchronous operation that produces a value after a successful result without a value.
    /// </summary>
    public static Task<TOutResult> ChainAwait<TSelf, TError, TOutResult, TOut>(TSelf result, Func<Task<TOutResult>> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.TryGetError(out var error)
            ? Task.FromResult(TOutResult.FromError(error))
            : Invoke<TOutResult, TError>(selector);
    }

    /// <summary>
    /// Keeps the value if the asynchronous predicate is satisfied, otherwise the result becomes the error from <paramref name="error"/>.
    /// </summary>
    public static Task<TSelf> EnsureAwait<TSelf, T, TError>(TSelf result, Func<T, Task<bool>> predicate, Func<T, TError> error)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(result);

        return Run(result, value, predicate, error);

        static async Task<TSelf> Run(TSelf result, T value, Func<T, Task<bool>> predicate, Func<T, TError> error)
        {
            try
            {
                return await predicate(value).ConfigureAwait(false) ? result : TSelf.FromError(error(value));
            }
            catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
            {
                return Flow.Thrown<TSelf, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Keeps the value if the asynchronous predicate is satisfied, otherwise the result becomes a
    /// <see cref="ValidationError"/> with <paramref name="message"/>.
    /// </summary>
    public static Task<TSelf> EnsureAwait<TSelf, T, TError>(TSelf result, Func<T, Task<bool>> predicate, string message)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(predicate);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(result);

        return Run(result, value, predicate, message);

        static async Task<TSelf> Run(TSelf result, T value, Func<T, Task<bool>> predicate, string message)
        {
            try
            {
                return await predicate(value).ConfigureAwait(false)
                    ? result
                    : Flow.Thrown<TSelf, TError>(new ValidationError(message));
            }
            catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
            {
                return Flow.Thrown<TSelf, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Runs an asynchronous dependent step and keeps both values as a tuple.
    /// </summary>
    public static Task<TOutResult> ChainAppendAwait<TSelf, T, TError, TNextResult, TNext, TOutResult>(
        TSelf result,
        Func<T, Task<TNextResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(TOutResult.FromError(Flow.ErrorOf<TSelf, T, TError>(result)));

        return Run(selector, value);

        static async Task<TOutResult> Run(Func<T, Task<TNextResult>> selector, T value)
        {
            var outcome = await Invoke<T, TNextResult, TError>(selector, value).ConfigureAwait(false);

            return outcome.TryGetValue(out var next)
                ? TOutResult.FromValue((value, next))
                : TOutResult.FromError(Flow.ErrorOf<TNextResult, TNext, TError>(outcome));
        }
    }

    /// <summary>
    /// Runs an asynchronous dependent step with the tuple elements and appends its value to the tuple.
    /// </summary>
    public static Task<TOutResult> ChainAppendAwait<TSelf, T1, T2, TError, TNextResult, TNext, TOutResult>(
        TSelf result,
        Func<T1, T2, Task<TNextResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(TOutResult.FromError(Flow.ErrorOf<TSelf, (T1, T2), TError>(result)));

        return Run(selector, value);

        static async Task<TOutResult> Run(Func<T1, T2, Task<TNextResult>> selector, (T1, T2) value)
        {
            TNextResult outcome;
            try
            {
                outcome = await selector(value.Item1, value.Item2).ConfigureAwait(false);
            }
            catch (Exception exception) when (Flow.Captured<TOutResult, TError>(exception))
            {
                return Flow.Thrown<TOutResult, TError>(exception);
            }

            return outcome.TryGetValue(out var next)
                ? TOutResult.FromValue((value.Item1, value.Item2, next))
                : TOutResult.FromError(Flow.ErrorOf<TNextResult, TNext, TError>(outcome));
        }
    }

    /// <summary>
    /// Runs an asynchronous dependent step with the tuple elements and appends its value to the tuple.
    /// </summary>
    public static Task<TOutResult> ChainAppendAwait<TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult>(
        TSelf result,
        Func<T1, T2, T3, Task<TNextResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2, T3), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(TOutResult.FromError(Flow.ErrorOf<TSelf, (T1, T2, T3), TError>(result)));

        return Run(selector, value);

        static async Task<TOutResult> Run(Func<T1, T2, T3, Task<TNextResult>> selector, (T1, T2, T3) value)
        {
            TNextResult outcome;
            try
            {
                outcome = await selector(value.Item1, value.Item2, value.Item3).ConfigureAwait(false);
            }
            catch (Exception exception) when (Flow.Captured<TOutResult, TError>(exception))
            {
                return Flow.Thrown<TOutResult, TError>(exception);
            }

            return outcome.TryGetValue(out var next)
                ? TOutResult.FromValue((value.Item1, value.Item2, value.Item3, next))
                : TOutResult.FromError(Flow.ErrorOf<TNextResult, TNext, TError>(outcome));
        }
    }

    /// <summary>
    /// Runs an asynchronous side effect with the value and returns the same result.
    /// </summary>
    public static Task<TSelf> DoAwait<TSelf, T, TError>(TSelf result, Func<T, Task> onSuccess)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (!result.TryGetValue(out var value))
            return Task.FromResult(result);

        return Run(result, value, onSuccess);

        static async Task<TSelf> Run(TSelf result, T value, Func<T, Task> onSuccess)
        {
            try
            {
                await onSuccess(value).ConfigureAwait(false);
                return result;
            }
            catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
            {
                return Flow.Thrown<TSelf, TError>(exception);
            }
        }
    }

    /// <summary>
    /// Runs an asynchronous side effect after a successful result without a value and returns the same result.
    /// </summary>
    public static Task<TSelf> DoAwait<TSelf, TError>(TSelf result, Func<Task> onSuccess)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? RunAction<TSelf, TError>(result, onSuccess) : Task.FromResult(result);
    }

    /// <summary>
    /// Handles the error asynchronously. Return a value to recover, or an error to replace it.
    /// </summary>
    public static Task<TSelf> CatchAwait<TSelf, T, TError>(TSelf result, Func<TError, Task<TSelf>> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.TryGetValue(out _)
            ? Task.FromResult(result)
            : Invoke<TError, TSelf, TError>(handler, Flow.ErrorOf<TSelf, T, TError>(result));
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> asynchronously, also as a case of an error union.
    /// Other errors pass through.
    /// </summary>
    public static Task<TSelf> CatchAwait<TSelf, T, TError, TException>(TSelf result, Func<TException, Task<TSelf>> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return !result.TryGetValue(out _) && Flow.TryGetCase<TError, TException>(Flow.ErrorOf<TSelf, T, TError>(result), out var match)
            ? Invoke<TException, TSelf, TError>(handler, match)
            : Task.FromResult(result);
    }

    /// <summary>
    /// Handles the error asynchronously. Return success to recover, or an error to replace it.
    /// </summary>
    public static Task<TSelf> CatchAwait<TSelf, TError>(TSelf result, Func<TError, Task<TSelf>> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.TryGetError(out var error) ? Invoke<TError, TSelf, TError>(handler, error) : Task.FromResult(result);
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> asynchronously, also as a case of an error union.
    /// Other errors pass through.
    /// </summary>
    [OperatorName("CatchAwait")]
    public static Task<TSelf> CatchCaseAwait<TSelf, TError, TException>(TSelf result, Func<TException, Task<TSelf>> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        return result.TryGetError(out var error) && Flow.TryGetCase<TError, TException>(error, out var match)
            ? Invoke<TException, TSelf, TError>(handler, match)
            : Task.FromResult(result);
    }

    /// <summary>
    /// Runs an asynchronous action for every result, value or error.
    /// </summary>
    public static Task<TSelf> FinallyAwait<TSelf, T, TError>(TSelf result, Func<Task> action)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);

        return RunAction<TSelf, TError>(result, action);
    }

    /// <summary>
    /// Runs an asynchronous action for every result, success or error.
    /// </summary>
    public static Task<TSelf> FinallyAwait<TSelf, TError>(TSelf result, Func<Task> action)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);

        return RunAction<TSelf, TError>(result, action);
    }

    private static async Task<TResult> Invoke<TArgument, TResult, TError>(Func<TArgument, Task<TResult>> step, TArgument argument)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            return await step(argument).ConfigureAwait(false);
        }
        catch (Exception exception) when (Flow.Captured<TResult, TError>(exception))
        {
            return Flow.Thrown<TResult, TError>(exception);
        }
    }

    private static async Task<TResult> Invoke<TResult, TError>(Func<Task<TResult>> step)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            return await step().ConfigureAwait(false);
        }
        catch (Exception exception) when (Flow.Captured<TResult, TError>(exception))
        {
            return Flow.Thrown<TResult, TError>(exception);
        }
    }

    private static async Task<TResult> RunAction<TResult, TError>(TResult result, Func<Task> action)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            await action().ConfigureAwait(false);
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TResult, TError>(exception))
        {
            return Flow.Thrown<TResult, TError>(exception);
        }
    }
}
