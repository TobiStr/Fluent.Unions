using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

public static partial class ResultAwaitCore
{
    /// <inheritdoc cref="SelectAwait{TSelf, T, TError, TOutResult, TOut}(TSelf, Func{T, Task{TOut}})"/>
    public static Task<TOutResult> SelectAwait<TSelf, T, TError, TOutResult, TOut>(Task<TSelf> task, Func<T, Task<TOut>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, Task<TOut>> selector) =>
            await ResultAwaitCore.SelectAwait<TSelf, T, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="SelectAwait{TSelf, TError, TOutResult, TOut}(TSelf, Func{Task{TOut}})"/>
    public static Task<TOutResult> SelectAwait<TSelf, TError, TOutResult, TOut>(Task<TSelf> task, Func<Task<TOut>> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<Task<TOut>> selector) =>
            await ResultAwaitCore.SelectAwait<TSelf, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAwait{TSelf, T, TError, TOutResult, TOut}(TSelf, Func{T, Task{TOutResult}})"/>
    public static Task<TOutResult> ChainAwait<TSelf, T, TError, TOutResult, TOut>(Task<TSelf> task, Func<T, Task<TOutResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, Task<TOutResult>> selector) =>
            await ResultAwaitCore.ChainAwait<TSelf, T, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAwait{TSelf, T, TError, TUnit}(TSelf, Func{T, Task{TUnit}})"/>
    public static Task<TUnit> ChainAwait<TSelf, T, TError, TUnit>(Task<TSelf> task, Func<T, Task<TUnit>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TUnit : struct, IFluentUnitResult<TUnit, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TUnit> Run(Task<TSelf> task, Func<T, Task<TUnit>> selector) =>
            await ResultAwaitCore.ChainAwait<TSelf, T, TError, TUnit>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAwait{TSelf, TError}(TSelf, Func{Task{TSelf}})"/>
    public static Task<TSelf> ChainAwait<TSelf, TError>(Task<TSelf> task, Func<Task<TSelf>> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TSelf> Run(Task<TSelf> task, Func<Task<TSelf>> selector) =>
            await ResultAwaitCore.ChainAwait<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAwait{TSelf, TError, TOutResult, TOut}(TSelf, Func{Task{TOutResult}})"/>
    public static Task<TOutResult> ChainAwait<TSelf, TError, TOutResult, TOut>(Task<TSelf> task, Func<Task<TOutResult>> selector)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TOutResult : struct, IFluentResult<TOutResult, TOut, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<Task<TOutResult>> selector) =>
            await ResultAwaitCore.ChainAwait<TSelf, TError, TOutResult, TOut>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="EnsureAwait{TSelf, T, TError}(TSelf, Func{T, Task{bool}}, Func{T, TError})"/>
    public static Task<TSelf> EnsureAwait<TSelf, T, TError>(Task<TSelf> task, Func<T, Task<bool>> predicate, Func<T, TError> error)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, predicate, error);

        static async Task<TSelf> Run(Task<TSelf> task, Func<T, Task<bool>> predicate, Func<T, TError> error) =>
            await ResultAwaitCore.EnsureAwait<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), predicate, error).ConfigureAwait(false);
    }

    /// <inheritdoc cref="EnsureAwait{TSelf, T, TError}(TSelf, Func{T, Task{bool}}, string)"/>
    public static Task<TSelf> EnsureAwait<TSelf, T, TError>(Task<TSelf> task, Func<T, Task<bool>> predicate, string message)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, predicate, message);

        static async Task<TSelf> Run(Task<TSelf> task, Func<T, Task<bool>> predicate, string message) =>
            await ResultAwaitCore.EnsureAwait<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), predicate, message).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAppendAwait{TSelf, T, TError, TNextResult, TNext, TOutResult}(TSelf, Func{T, Task{TNextResult}})"/>
    public static Task<TOutResult> ChainAppendAwait<TSelf, T, TError, TNextResult, TNext, TOutResult>(Task<TSelf> task, Func<T, Task<TNextResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T, Task<TNextResult>> selector) =>
            await ResultAwaitCore.ChainAppendAwait<TSelf, T, TError, TNextResult, TNext, TOutResult>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAppendAwait{TSelf, T1, T2, TError, TNextResult, TNext, TOutResult}(TSelf, Func{T1, T2, Task{TNextResult}})"/>
    public static Task<TOutResult> ChainAppendAwait<TSelf, T1, T2, TError, TNextResult, TNext, TOutResult>(Task<TSelf> task, Func<T1, T2, Task<TNextResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T1, T2, Task<TNextResult>> selector) =>
            await ResultAwaitCore.ChainAppendAwait<TSelf, T1, T2, TError, TNextResult, TNext, TOutResult>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ChainAppendAwait{TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult}(TSelf, Func{T1, T2, T3, Task{TNextResult}})"/>
    public static Task<TOutResult> ChainAppendAwait<TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult>(Task<TSelf> task, Func<T1, T2, T3, Task<TNextResult>> selector)
        where TSelf : struct, IFluentResult<TSelf, (T1, T2, T3), TError>
        where TNextResult : struct, IFluentResult<TNextResult, TNext, TError>
        where TOutResult : struct, IFluentResult<TOutResult, (T1, T2, T3, TNext), TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, selector);

        static async Task<TOutResult> Run(Task<TSelf> task, Func<T1, T2, T3, Task<TNextResult>> selector) =>
            await ResultAwaitCore.ChainAppendAwait<TSelf, T1, T2, T3, TError, TNextResult, TNext, TOutResult>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), selector).ConfigureAwait(false);
    }

    /// <inheritdoc cref="DoAwait{TSelf, T, TError}(TSelf, Func{T, Task})"/>
    public static Task<TSelf> DoAwait<TSelf, T, TError>(Task<TSelf> task, Func<T, Task> onSuccess)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess);

        static async Task<TSelf> Run(Task<TSelf> task, Func<T, Task> onSuccess) =>
            await ResultAwaitCore.DoAwait<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess).ConfigureAwait(false);
    }

    /// <inheritdoc cref="DoAwait{TSelf, TError}(TSelf, Func{Task})"/>
    public static Task<TSelf> DoAwait<TSelf, TError>(Task<TSelf> task, Func<Task> onSuccess)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, onSuccess);

        static async Task<TSelf> Run(Task<TSelf> task, Func<Task> onSuccess) =>
            await ResultAwaitCore.DoAwait<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), onSuccess).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CatchAwait{TSelf, T, TError}(TSelf, Func{TError, Task{TSelf}})"/>
    public static Task<TSelf> CatchAwait<TSelf, T, TError>(Task<TSelf> task, Func<TError, Task<TSelf>> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TError, Task<TSelf>> handler) =>
            await ResultAwaitCore.CatchAwait<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CatchAwait{TSelf, T, TError, TException}(TSelf, Func{TException, Task{TSelf}})"/>
    public static Task<TSelf> CatchAwait<TSelf, T, TError, TException>(Task<TSelf> task, Func<TException, Task<TSelf>> handler)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TException, Task<TSelf>> handler) =>
            await ResultAwaitCore.CatchAwait<TSelf, T, TError, TException>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CatchAwait{TSelf, TError}(TSelf, Func{TError, Task{TSelf}})"/>
    public static Task<TSelf> CatchAwait<TSelf, TError>(Task<TSelf> task, Func<TError, Task<TSelf>> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TError, Task<TSelf>> handler) =>
            await ResultAwaitCore.CatchAwait<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CatchCaseAwait{TSelf, TError, TException}(TSelf, Func{TException, Task{TSelf}})"/>
    [OperatorName("CatchAwait")]
    public static Task<TSelf> CatchCaseAwait<TSelf, TError, TException>(Task<TSelf> task, Func<TException, Task<TSelf>> handler)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, handler);

        static async Task<TSelf> Run(Task<TSelf> task, Func<TException, Task<TSelf>> handler) =>
            await ResultAwaitCore.CatchCaseAwait<TSelf, TError, TException>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), handler).ConfigureAwait(false);
    }

    /// <inheritdoc cref="FinallyAwait{TSelf, T, TError}(TSelf, Func{Task})"/>
    public static Task<TSelf> FinallyAwait<TSelf, T, TError>(Task<TSelf> task, Func<Task> action)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, action);

        static async Task<TSelf> Run(Task<TSelf> task, Func<Task> action) =>
            await ResultAwaitCore.FinallyAwait<TSelf, T, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), action).ConfigureAwait(false);
    }

    /// <inheritdoc cref="FinallyAwait{TSelf, TError}(TSelf, Func{Task})"/>
    public static Task<TSelf> FinallyAwait<TSelf, TError>(Task<TSelf> task, Func<Task> action)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(task);

        return Run(task, action);

        static async Task<TSelf> Run(Task<TSelf> task, Func<Task> action) =>
            await ResultAwaitCore.FinallyAwait<TSelf, TError>(await Flow.Settle<TSelf, TError>(task).ConfigureAwait(false), action).ConfigureAwait(false);
    }
}
