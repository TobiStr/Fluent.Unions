using System.Runtime.ExceptionServices;
using ResultType.Abstractions;

namespace ResultType.Extensions;

/// <summary>
/// Operators for custom result types that implement <see cref="IResult{TSelf, T, TError}"/>.
/// The library's own <see cref="Result{T}"/> keeps using its concrete operators.
/// </summary>
public static class ResultInterfaceExtensions
{
    /// <summary>
    /// Chains an operation that returns a result with the same error type.
    /// </summary>
    public static TOutResult Chain<TSelf, T, TError, TOutResult>(
        this IResult<TSelf, T, TError> result,
        Func<T, TOutResult> next)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
        where TOutResult : struct, IErrorResult<TOutResult, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return result.Value switch
        {
            TError error => TOutResult.FromError(error),
            T value => Capture.Run<TOutResult, TError>(() => next(value)),
            _ => default
        };
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a result with the same error type.
    /// </summary>
    public static Task<TOutResult> ChainAwait<TSelf, T, TError, TOutResult>(
        this IResult<TSelf, T, TError> result,
        Func<T, Task<TOutResult>> next)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
        where TOutResult : struct, IErrorResult<TOutResult, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return result.Value switch
        {
            TError error => Task.FromResult(TOutResult.FromError(error)),
            T value => Capture.RunAsync<TOutResult, TError>(() => next(value)),
            _ => Task.FromResult(default(TOutResult))
        };
    }

    /// <summary>
    /// Handles the error. Return a value to recover, or an error to replace it.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<TError, TSelf> handler)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TError error ? Capture.Run<TSelf, TError>(() => handler(error)) : (TSelf)result;
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TException"/> only. Other errors pass through.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError, TException>(
        this IResult<TSelf, T, TError> result,
        Func<TException, TSelf> handler)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
        where TException : TError
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TException error ? Capture.Run<TSelf, TError>(() => handler(error)) : (TSelf)result;
    }

    /// <summary>
    /// Replaces the error with <paramref name="fallback"/>.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError>(this IResult<TSelf, T, TError> result, TSelf fallback)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value is TError ? fallback : (TSelf)result;
    }

    /// <summary>
    /// Runs <paramref name="onNext"/> with the value.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Action<T> onNext)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onNext);

        var self = (TSelf)result;

        return result.Value switch
        {
            TError => self,
            T value => Capture.Run<TSelf, TError>(() =>
            {
                onNext(value);
                return self;
            }),
            _ => self
        };
    }

    /// <summary>
    /// Runs <paramref name="onNext"/> with the value, or <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Action<T> onNext,
        Action<TError> onError)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onNext);
        ArgumentNullException.ThrowIfNull(onError);

        var self = (TSelf)result;

        return result.Value switch
        {
            TError error => Capture.Run<TSelf, TError>(() =>
            {
                onError(error);
                return self;
            }),
            T value => Capture.Run<TSelf, TError>(() =>
            {
                onNext(value);
                return self;
            }),
            _ => self
        };
    }

    /// <summary>
    /// Runs <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf OnErrorDo<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Action<TError> onError)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onError);

        var self = (TSelf)result;

        return result.Value is TError error
            ? Capture.Run<TSelf, TError>(() =>
            {
                onError(error);
                return self;
            })
            : self;
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every result: value, error or empty.
    /// </summary>
    public static TSelf Finally<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Action action)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        var self = (TSelf)result;

        return Capture.Run<TSelf, TError>(() =>
        {
            action();
            return self;
        });
    }

    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes empty.
    /// </summary>
    public static TSelf Where<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<T, bool> predicate)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var self = (TSelf)result;

        return result.Value switch
        {
            TError => self,
            T value => Capture.Run<TSelf, TError>(() => predicate(value) ? self : default),
            _ => self
        };
    }

    /// <summary>
    /// Replaces an empty result with <paramref name="fallback"/>. Values and errors pass through.
    /// </summary>
    public static TSelf DefaultIfEmpty<TSelf, T, TError>(this IResult<TSelf, T, TError> result, T fallback)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value is null ? TSelf.FromValue(fallback) : (TSelf)result;
    }

    /// <summary>
    /// Returns the value, or <c>default</c> for an error or empty result.
    /// </summary>
    public static T? GetValueOrDefault<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value switch
        {
            TError => default,
            T value => value,
            _ => default
        };
    }

    /// <summary>
    /// Returns the value, or <paramref name="fallback"/> for an error or empty result.
    /// </summary>
    public static T GetValueOrDefault<TSelf, T, TError>(this IResult<TSelf, T, TError> result, T fallback)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value switch
        {
            TError => fallback,
            T value => value,
            _ => fallback
        };
    }

    /// <summary>
    /// Throws the error, preserving its stack trace, and returns the result if it holds a value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result is empty.</exception>
    public static TSelf Check<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);

        switch (result.Value)
        {
            case TError error:
                ExceptionDispatchInfo.Capture(error).Throw();
                break;
            case T:
                return (TSelf)result;
        }

        throw new InvalidOperationException($"Error property for {typeof(TSelf)} not set.");
    }

    /// <summary>
    /// Projects the value into another result type. Needs explicit type arguments; wrap it once for your own type.
    /// </summary>
    public static TOutResult Select<TSelf, T, TError, TOutResult, TOut>(
        this IResult<TSelf, T, TError> result,
        Func<T, TOut> selector)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
        where TOutResult : struct, IResult<TOutResult, TOut, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            TError error => TOutResult.FromError(error),
            T value => Capture.Run<TOutResult, TError>(() => TOutResult.FromValue(selector(value))),
            _ => default
        };
    }

    /// <summary>
    /// Converts the error into another result type with another error type. Needs explicit type arguments.
    /// </summary>
    public static TOutResult MapError<TSelf, T, TError, TOutResult, TOutError>(
        this IResult<TSelf, T, TError> result,
        Func<TError, TOutError> map)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : Exception
        where TOutResult : struct, IResult<TOutResult, T, TOutError>
        where TOutError : Exception
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return result.Value switch
        {
            TError error => Capture.Run<TOutResult, TOutError>(() => TOutResult.FromError(
                map(error) ?? throw new InvalidOperationException("The error mapping returned null.", error))),
            T value => TOutResult.FromValue(value),
            _ => default
        };
    }
}
