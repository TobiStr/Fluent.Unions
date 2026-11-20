using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions;

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
        where TError : notnull
        where TOutResult : struct, IErrorResult<TOutResult, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return result.CaseOf() switch
        {
            TError error => TOutResult.FromError(error),
            T value => Capture.Run<TOutResult, TError>(() => next(value)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a result with the same error type.
    /// </summary>
    public static Task<TOutResult> ChainAwait<TSelf, T, TError, TOutResult>(
        this IResult<TSelf, T, TError> result,
        Func<T, Task<TOutResult>> next)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TOutResult : struct, IErrorResult<TOutResult, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return result.CaseOf() switch
        {
            TError error => Task.FromResult(TOutResult.FromError(error)),
            T value => Capture.RunAsync<TOutResult, TError>(() => next(value)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Handles the error. Return a value to recover, or an error to replace it.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<TError, TSelf> handler)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(handler);

        return result.CaseOf() is TError error ? Capture.Run<TSelf, TError>(() => handler(error)) : (TSelf)result;
    }

    /// <summary>
    /// Handles errors of type <typeparamref name="TCase"/> only, also when it is a case of an error union. Other errors pass through.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError, TCase>(
        this IResult<TSelf, T, TError> result,
        Func<TCase, TSelf> handler)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(handler);

        return result.CaseOf() is TError error && TryGetCase<TError, TCase>(error, out var match)
            ? Capture.Run<TSelf, TError>(() => handler(match))
            : (TSelf)result;
    }

    private static bool TryGetCase<TError, TCase>(TError error, [MaybeNullWhen(false)] out TCase match)
    {
        switch (error)
        {
            case TCase direct:
                match = direct;
                return true;
            case IUnion { Value: TCase inner }:
                match = inner;
                return true;
            default:
                match = default;
                return false;
        }
    }

    /// <summary>
    /// Replaces the error with <paramref name="fallback"/>.
    /// </summary>
    public static TSelf Catch<TSelf, T, TError>(this IResult<TSelf, T, TError> result, TSelf fallback)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() is TError ? fallback : (TSelf)result;
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> with the value.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Action<T> onSuccess)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        var self = (TSelf)result;

        return result.CaseOf() switch
        {
            TError => self,
            T value => Capture.Run<TSelf, TError>(() =>
            {
                onSuccess(value);
                return self;
            }),
            _ => self
        };
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> with the value, or <paramref name="onError"/> with the error.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Action<T> onSuccess,
        Action<TError> onError)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        var self = (TSelf)result;

        return result.CaseOf() switch
        {
            TError error => Capture.Run<TSelf, TError>(() =>
            {
                onError(error);
                return self;
            }),
            T value => Capture.Run<TSelf, TError>(() =>
            {
                onSuccess(value);
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
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onError);

        var self = (TSelf)result;

        return result.CaseOf() is TError error
            ? Capture.Run<TSelf, TError>(() =>
            {
                onError(error);
                return self;
            })
            : self;
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every result, value or error.
    /// </summary>
    public static TSelf Finally<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Action action)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
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
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes the error from <paramref name="error"/>.
    /// An error passes through and the predicate does not run.
    /// </summary>
    public static TSelf Ensure<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Func<T, bool> predicate,
        Func<T, TError> error)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        var self = (TSelf)result;

        return result.CaseOf() switch
        {
            TError => self,
            T value => Capture.Run<TSelf, TError>(() => predicate(value) ? self : TSelf.FromError(error(value))),
            _ => self
        };
    }

    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes the error that
    /// <c>FromException</c> makes of a <see cref="ValidationError"/> with <paramref name="message"/>.
    /// </summary>
    public static TSelf Ensure<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Func<T, bool> predicate,
        string message)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(message);

        return result.Ensure(predicate, _ => TSelf.FromException(new ValidationError(message)));
    }

    /// <summary>
    /// Runs every rule on the value. If any fail, the error is what <paramref name="combine"/> makes of all errors.
    /// A rule that throws counts as a failure. Errors pass through.
    /// A rule returns any result type with the same error type, whose own value type is ignored.
    /// </summary>
    public static TSelf Validate<TSelf, T, TError, TRuleResult>(
        this IResult<TSelf, T, TError> result,
        Func<IReadOnlyList<TError>, TError> combine,
        params Func<T, TRuleResult>[] rules)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TRuleResult : struct, IErrorResult<TRuleResult, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(combine);
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
            ArgumentNullException.ThrowIfNull(rule, nameof(rules));

        var self = (TSelf)result;

        if (result.CaseOf() is TError || result.CaseOf() is not T value)
            return self;

        var errors = new List<TError>();

        foreach (var rule in rules)
        {
            switch (Capture.Run<TRuleResult, TError>(() => rule(value)).CaseOf())
            {
                case TError error:
                    errors.Add(error);
                    break;
            }
        }

        return errors.Count == 0
            ? self
            : Capture.Run<TSelf, TError>(() => TSelf.FromError(
                combine(errors) ?? throw new InvalidOperationException("The combine function returned null.")));
    }

    /// <summary>
    /// Captures the value for later steps of the chain and returns the same result.
    /// <paramref name="value"/> is filled only when the result holds a value.
    /// </summary>
    public static TSelf Out<TSelf, T, TError>(this IResult<TSelf, T, TError> result, out Captured<T> value)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        value = new Captured<T>();

        if (result.CaseOf() is not TError && result.CaseOf() is T payload)
            value.SetFrom(Option<T>.Of(payload));

        return (TSelf)result;
    }

    /// <summary>
    /// Runs an asynchronous side effect with the value and returns the same result.
    /// </summary>
    public static Task<TSelf> DoAwait<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<T, Task> onSuccess)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        var self = (TSelf)result;

        return result.CaseOf() switch
        {
            TError => Task.FromResult(self),
            T value => Capture.RunAsync<TSelf, TError>(async () =>
            {
                await onSuccess(value).ConfigureAwait(false);
                return self;
            }),
            _ => Task.FromResult(self)
        };
    }

    /// <summary>
    /// Handles the error asynchronously. Return a value to recover, or an error to replace it.
    /// </summary>
    public static Task<TSelf> CatchAwait<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Func<TError, Task<TSelf>> handler)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(handler);

        return result.CaseOf() is TError error
            ? Capture.RunAsync<TSelf, TError>(() => handler(error))
            : Task.FromResult((TSelf)result);
    }

    /// <summary>
    /// Runs an asynchronous action for every result, value or error.
    /// </summary>
    public static Task<TSelf> FinallyAwait<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<Task> action)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        var self = (TSelf)result;

        return Capture.RunAsync<TSelf, TError>(async () =>
        {
            await action().ConfigureAwait(false);
            return self;
        });
    }

    /// <summary>
    /// Returns the value, or <c>default</c> for an error.
    /// </summary>
    public static T? GetValueOrDefault<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() switch
        {
            TError => default,
            T value => value,
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Returns the value, or <paramref name="fallback"/> for an error.
    /// </summary>
    public static T GetValueOrDefault<TSelf, T, TError>(this IResult<TSelf, T, TError> result, T fallback)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() switch
        {
            TError => fallback,
            T value => value,
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Throws the error and returns the result if it holds a value. An exception is rethrown with its stack trace;
    /// any other error is thrown inside a <see cref="ResultErrorException"/>.
    /// </summary>
    public static TSelf Check<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        switch (result.CaseOf())
        {
            case TError error when error is Exception exception:
                ExceptionDispatchInfo.Throw(exception);
                break;
            case TError error:
                throw new ResultErrorException(error);
        }

        return (TSelf)result;
    }

    /// <summary>
    /// Leaves the result with one callback for the value and one for the error.
    /// </summary>
    public static TOut Match<TSelf, T, TError, TOut>(
        this IResult<TSelf, T, TError> result,
        Func<T, TOut> onSuccess,
        Func<TError, TOut> onError)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.CaseOf() switch
        {
            TError error => onError(error),
            T value => onSuccess(value),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Keeps the value of a successful result. An error gives an option without a value.
    /// </summary>
    public static Option<T> ToOption<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() switch
        {
            TError => Option<T>.NoValue,
            T value => Option<T>.Of(value),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Turns a finished result into a loaded or failed state. An error that is not an exception is wrapped in a
    /// <see cref="ResultErrorException"/>.
    /// </summary>
    public static Loadable<T> ToLoadable<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() switch
        {
            TError error => new Loadable<T>(error as Exception ?? new ResultErrorException(error)),
            T value => new Loadable<T>(value),
            _ => new Loadable<T>(Loadable.NotLoaded)
        };
    }

    /// <summary>
    /// Flattens a result of a result with the same error type.
    /// </summary>
    public static TInner Unwrap<TSelf, TInner, TError>(this IResult<TSelf, TInner, TError> result)
        where TSelf : struct, IResult<TSelf, TInner, TError>
        where TInner : struct, IErrorResult<TInner, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() switch
        {
            TError error => TInner.FromError(error),
            TInner inner => inner,
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Projects the value into another result type. Needs explicit type arguments; wrap it once for your own type.
    /// </summary>
    public static TOutResult Select<TSelf, T, TError, TOutResult, TOut>(
        this IResult<TSelf, T, TError> result,
        Func<T, TOut> selector)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, TOut, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);

        return result.CaseOf() switch
        {
            TError error => TOutResult.FromError(error),
            T value => Capture.Run<TOutResult, TError>(() => TOutResult.FromValue(selector(value))),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Converts the error into another result type with another error type. Needs explicit type arguments.
    /// </summary>
    public static TOutResult MapError<TSelf, T, TError, TOutResult, TOutError>(
        this IResult<TSelf, T, TError> result,
        Func<TError, TOutError> map)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, T, TOutError>
        where TOutError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return result.CaseOf() switch
        {
            TError error => Capture.Run<TOutResult, TOutError>(() => TOutResult.FromError(
                map(error) ?? throw new InvalidOperationException("The error mapping returned null.", error as Exception))),
            T value => TOutResult.FromValue(value),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Projects the value with an asynchronous selector into another result type. Needs explicit type arguments.
    /// </summary>
    public static Task<TOutResult> SelectAwait<TSelf, T, TError, TOutResult, TOut>(
        this IResult<TSelf, T, TError> result,
        Func<T, Task<TOut>> selector)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, TOut, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);

        return result.CaseOf() switch
        {
            TError error => Task.FromResult(TOutResult.FromError(error)),
            T value => Capture.RunAsync<TOutResult, TError>(
                async () => TOutResult.FromValue(await selector(value).ConfigureAwait(false))),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Combines two results with a selector into another result type. The first error wins.
    /// Needs explicit type arguments.
    /// </summary>
    public static TOutResult Zip<TSelf, T, TError, TOtherResult, TOther, TOutResult, TOut>(
        this IResult<TSelf, T, TError> first,
        IResult<TOtherResult, TOther, TError> second,
        Func<T, TOther, TOut> resultSelector)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TOtherResult : struct, IResult<TOtherResult, TOther, TError>
        where TOutResult : struct, IResult<TOutResult, TOut, TError>
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return (first.CaseOf(), second.CaseOf()) switch
        {
            (TError error, _) => TOutResult.FromError(error),
            (_, TError error) => TOutResult.FromError(error),
            (T a, TOther b) => Capture.Run<TOutResult, TError>(() => TOutResult.FromValue(resultSelector(a, b))),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Runs a dependent step and keeps both values as a tuple in another result type. An error passes
    /// through and the selector does not run. Needs explicit type arguments.
    /// </summary>
    public static TOutResult BindAppend<TSelf, T, TError, TNextResult, TNext, TOutResult>(
        this IResult<TSelf, T, TError> result,
        Func<T, TNextResult> selector)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TNextResult : struct, IResult<TNextResult, TNext, TError>
        where TOutResult : struct, IResult<TOutResult, (T, TNext), TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);

        return result.CaseOf() switch
        {
            TError error => TOutResult.FromError(error),
            T value => Capture.Run<TOutResult, TError>(() => selector(value).CaseOf() switch
            {
                TError nextError => TOutResult.FromError(nextError),
                TNext next => TOutResult.FromValue((value, next)),
                _ => throw new UnreachableException()
            }),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Replaces the value of a successful result with <paramref name="value"/> in another result type. An error passes
    /// through. Needs explicit type arguments.
    /// </summary>
    public static TOutResult WithValue<TSelf, T, TError, TOutResult, TOut>(
        this IResult<TSelf, T, TError> result,
        TOut value)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, TOut, TError>
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(value);

        return result.CaseOf() switch
        {
            TError error => TOutResult.FromError(error),
            T => TOutResult.FromValue(value),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Swaps a result of an option into an optional result: no value gives <c>None</c> and an error stays an
    /// error. Needs explicit type arguments.
    /// </summary>
    public static Option<TOutResult> Transpose<TSelf, T, TError, TOutResult>(
        this IResult<TSelf, Option<T>, TError> result)
        where TSelf : struct, IResult<TSelf, Option<T>, TError>
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, T, TError>
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.CaseOf() switch
        {
            TError error => Option<TOutResult>.Of(TOutResult.FromError(error)),
            Option<T> inner => inner.TryGetValue(out var value)
                ? Option<TOutResult>.Of(TOutResult.FromValue(value))
                : Option<TOutResult>.NoValue,
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Gets the error, if the custom result holds one. A <c>default</c> result holds the error that
    /// <c>FromException</c> makes of an <see cref="InvalidOperationException"/>.
    /// </summary>
    public static bool TryGetError<TSelf, TError>(this IErrorResult<TSelf, TError> result, [MaybeNullWhen(false)] out TError error)
        where TSelf : struct, IErrorResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.CaseOf() is TError found)
        {
            error = found;
            return true;
        }

        error = default;
        return false;
    }

    private static object CaseOf<TSelf, TError>(this IErrorResult<TSelf, TError> result)
        where TSelf : struct, IErrorResult<TSelf, TError>
        where TError : notnull =>
        result.Value ?? TSelf.FromException(ResultErrors.Uninitialized(typeof(TSelf)));
}
