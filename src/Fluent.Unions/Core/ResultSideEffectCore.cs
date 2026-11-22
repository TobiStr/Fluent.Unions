using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Rx-style operators that run side effects and return the same result, unless the side effect throws.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultSideEffectCore
{
    /// <summary>
    /// Runs <paramref name="onSuccess"/> with the value.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(TSelf result, Action<T> onSuccess)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (!result.TryGetValue(out var value))
            return result;

        try
        {
            onSuccess(value);
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> with the value, or <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(TSelf result, Action<T> onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        try
        {
            if (result.TryGetValue(out var value))
                onSuccess(value);
            else
                onError(Flow.ErrorOf<TSelf, T, TError>(result));

            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> after a successful result without a value.
    /// </summary>
    public static TSelf Do<TSelf, TError>(TSelf result, Action onSuccess)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (!result.IsSuccess)
            return result;

        try
        {
            onSuccess();
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> after success, or <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf Do<TSelf, TError>(TSelf result, Action onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        try
        {
            if (result.TryGetError(out var error))
                onError(error);
            else
                onSuccess();

            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf OnErrorDo<TSelf, T, TError>(TSelf result, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);

        if (result.TryGetValue(out _))
            return result;

        try
        {
            onError(Flow.ErrorOf<TSelf, T, TError>(result));
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf OnErrorDo<TSelf, TError>(TSelf result, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);

        if (!result.TryGetError(out var error))
            return result;

        try
        {
            onError(error);
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Observes the value and returns the same result. Unlike <c>Do</c>, an exception thrown by
    /// <paramref name="onSuccess"/> is not captured: it propagates and never replaces the result.
    /// </summary>
    public static TSelf Tap<TSelf, T, TError>(TSelf result, Action<T> onSuccess)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.TryGetValue(out var value))
            onSuccess(value);

        return result;
    }

    /// <summary>
    /// Observes the value or the error and returns the same result. An exception thrown by a callback propagates.
    /// </summary>
    public static TSelf Tap<TSelf, T, TError>(TSelf result, Action<T> onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        if (result.TryGetValue(out var value))
            onSuccess(value);
        else
            onError(Flow.ErrorOf<TSelf, T, TError>(result));

        return result;
    }

    /// <summary>
    /// Observes the error and returns the same result, so the original error is kept. An exception thrown by
    /// <paramref name="onError"/> propagates.
    /// </summary>
    public static TSelf TapError<TSelf, T, TError>(TSelf result, Action<TError> onError)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);

        if (!result.TryGetValue(out _))
            onError(Flow.ErrorOf<TSelf, T, TError>(result));

        return result;
    }

    /// <summary>
    /// Observes a successful result without a value and returns the same result. An exception thrown by
    /// <paramref name="onSuccess"/> propagates.
    /// </summary>
    public static TSelf Tap<TSelf, TError>(TSelf result, Action onSuccess)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsSuccess)
            onSuccess();

        return result;
    }

    /// <summary>
    /// Observes success or the error and returns the same result. An exception thrown by a callback propagates.
    /// </summary>
    public static TSelf Tap<TSelf, TError>(TSelf result, Action onSuccess, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        if (result.TryGetError(out var error))
            onError(error);
        else
            onSuccess();

        return result;
    }

    /// <summary>
    /// Observes the error and returns the same result. An exception thrown by <paramref name="onError"/> propagates.
    /// </summary>
    public static TSelf TapError<TSelf, TError>(TSelf result, Action<TError> onError)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(onError);

        if (result.TryGetError(out var error))
            onError(error);

        return result;
    }

    /// <summary>
    /// Captures the value for later steps of the chain and returns the same result.
    /// <paramref name="value"/> is filled only when the result holds a value.
    /// </summary>
    public static TSelf Out<TSelf, T, TError>(TSelf result, out Captured<T> value)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        value = new Captured<T>();
        if (result.TryGetValue(out var payload))
            value.Set(payload);

        return result;
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every result, value or error.
    /// </summary>
    public static TSelf Finally<TSelf, T, TError>(TSelf result, Action action)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every result, success or error.
    /// </summary>
    public static TSelf Finally<TSelf, TError>(TSelf result, Action action)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
            return result;
        }
        catch (Exception exception) when (Flow.Captured<TSelf, TError>(exception))
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }
}
