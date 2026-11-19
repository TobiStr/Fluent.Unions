using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Extensions;

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
        where TError : notnull
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
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(handler);

        return result.Value is TError error ? Capture.Run<TSelf, TError>(() => handler(error)) : (TSelf)result;
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

        return result.Value is TError error && TryGetCase<TError, TCase>(error, out var match)
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

        return result.Value is TError ? fallback : (TSelf)result;
    }

    /// <summary>
    /// Runs <paramref name="onNext"/> with the value.
    /// </summary>
    public static TSelf Do<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Action<T> onNext)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
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
        where TError : notnull
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
        where TError : notnull
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
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes empty.
    /// </summary>
    public static TSelf Where<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<T, bool> predicate)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
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
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes the error from <paramref name="error"/>.
    /// An error or empty result passes through and the predicate does not run.
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

        return result.Value switch
        {
            TError => self,
            T value => Capture.Run<TSelf, TError>(() => predicate(value) ? self : TSelf.FromError(error(value))),
            _ => self
        };
    }

    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes the error that
    /// <c>FromException</c> makes of a <see cref="ValidationException"/> with <paramref name="message"/>.
    /// </summary>
    public static TSelf Ensure<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Func<T, bool> predicate,
        string message)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(message);

        return result.Ensure(predicate, _ => TSelf.FromException(new ValidationException(message)));
    }

    /// <summary>
    /// Runs every rule on the value. If any fail, the error is what <paramref name="combine"/> makes of all errors.
    /// A rule that throws or returns an empty result counts as a failure. Errors and empty results pass through.
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

        if (result.Value is TError || result.Value is not T value)
            return self;

        var errors = new List<TError>();

        foreach (var rule in rules)
        {
            switch (Capture.Run<TRuleResult, TError>(() => rule(value)).Value)
            {
                case TError error:
                    errors.Add(error);
                    break;
                case null:
                    errors.Add(TSelf.FromException(new InvalidOperationException("A validation rule returned an empty result.")));
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

        if (result.Value is not TError && result.Value is T payload)
            value.SetFrom(Option<T>.Of(payload));

        return (TSelf)result;
    }

    /// <summary>
    /// Runs an asynchronous side effect with the value and returns the same result.
    /// </summary>
    public static Task<TSelf> DoAwait<TSelf, T, TError>(this IResult<TSelf, T, TError> result, Func<T, Task> onNext)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onNext);

        var self = (TSelf)result;

        return result.Value switch
        {
            TError => Task.FromResult(self),
            T value => Capture.RunAsync<TSelf, TError>(async () =>
            {
                await onNext(value).ConfigureAwait(false);
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

        return result.Value is TError error
            ? Capture.RunAsync<TSelf, TError>(() => handler(error))
            : Task.FromResult((TSelf)result);
    }

    /// <summary>
    /// Runs an asynchronous action for every result: value, error or empty.
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
    /// Keeps the value if the asynchronous predicate is satisfied, otherwise the result becomes empty.
    /// </summary>
    public static Task<TSelf> WhereAwait<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        Func<T, Task<bool>> predicate)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var self = (TSelf)result;

        return result.Value switch
        {
            TError => Task.FromResult(self),
            T value => Capture.RunAsync<TSelf, TError>(async () => await predicate(value).ConfigureAwait(false) ? self : default),
            _ => Task.FromResult(self)
        };
    }

    /// <summary>
    /// Replaces an empty result with <paramref name="fallback"/>. Values and errors pass through.
    /// </summary>
    public static TSelf DefaultIfEmpty<TSelf, T, TError>(this IResult<TSelf, T, TError> result, T fallback)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value is null ? TSelf.FromValue(fallback) : (TSelf)result;
    }

    /// <summary>
    /// Returns the value, or <c>default</c> for an error or empty result.
    /// </summary>
    public static T? GetValueOrDefault<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
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
        where TError : notnull
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
    /// Throws the error and returns the result if it holds a value. An exception is rethrown with its stack trace;
    /// any other error is thrown inside a <see cref="ResultErrorException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result is empty.</exception>
    public static TSelf Check<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        switch (result.Value)
        {
            case TError error when error is Exception exception:
                ExceptionDispatchInfo.Capture(exception).Throw();
                break;
            case TError error:
                throw new ResultErrorException(error);
            case T:
                return (TSelf)result;
        }

        throw new InvalidOperationException($"Error property for {typeof(TSelf)} not set.");
    }

    /// <summary>
    /// Leaves the result with a value: one callback for each state, so none of them is forgotten.
    /// </summary>
    public static TOut Match<TSelf, T, TError, TOut>(
        this IResult<TSelf, T, TError> result,
        Func<T, TOut> onNext,
        Func<TError, TOut> onError,
        Func<TOut> onEmpty)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onNext);
        ArgumentNullException.ThrowIfNull(onError);
        ArgumentNullException.ThrowIfNull(onEmpty);

        return result.Value switch
        {
            TError error => onError(error),
            T value => onNext(value),
            _ => onEmpty()
        };
    }

    /// <summary>
    /// Keeps the value of a successful result. Errors and empty results give an option without a value.
    /// </summary>
    public static Option<T> ToOption<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value switch
        {
            TError => Option<T>.NoValue,
            T value => Option<T>.Of(value),
            _ => Option<T>.NoValue
        };
    }

    /// <summary>
    /// Turns a finished result into a loaded or failed state. An error that is not an exception is wrapped in a
    /// <see cref="ResultErrorException"/>. An empty result counts as not loaded.
    /// </summary>
    public static Loadable<T> ToLoadable<TSelf, T, TError>(this IResult<TSelf, T, TError> result)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value switch
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

        return result.Value switch
        {
            TError error => TInner.FromError(error),
            TInner inner => inner,
            _ => default
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
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, T, TOutError>
        where TOutError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return result.Value switch
        {
            TError error => Capture.Run<TOutResult, TOutError>(() => TOutResult.FromError(
                map(error) ?? throw new InvalidOperationException("The error mapping returned null.", error as Exception))),
            T value => TOutResult.FromValue(value),
            _ => default
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

        return result.Value switch
        {
            TError error => Task.FromResult(TOutResult.FromError(error)),
            T value => Capture.RunAsync<TOutResult, TError>(
                async () => TOutResult.FromValue(await selector(value).ConfigureAwait(false))),
            _ => Task.FromResult(default(TOutResult))
        };
    }

    /// <summary>
    /// Combines two results with a selector into another result type. The first error wins, then an empty side makes
    /// the result empty. Needs explicit type arguments.
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

        return (first.Value, second.Value) switch
        {
            (TError error, _) => TOutResult.FromError(error),
            (_, TError error) => TOutResult.FromError(error),
            (T a, TOther b) => Capture.Run<TOutResult, TError>(() => TOutResult.FromValue(resultSelector(a, b))),
            _ => default
        };
    }

    /// <summary>
    /// Runs a dependent step and keeps both values as a tuple in another result type. An error or empty result passes
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

        return result.Value switch
        {
            TError error => TOutResult.FromError(error),
            T value => Capture.Run<TOutResult, TError>(() => selector(value).Value switch
            {
                TError nextError => TOutResult.FromError(nextError),
                TNext next => TOutResult.FromValue((value, next)),
                _ => default
            }),
            _ => default
        };
    }

    /// <summary>
    /// Replaces the value of a successful result with <paramref name="value"/> in another result type. An error passes
    /// through and an empty result stays empty. Needs explicit type arguments.
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

        return result.Value switch
        {
            TError error => TOutResult.FromError(error),
            T => TOutResult.FromValue(value),
            _ => default
        };
    }

    /// <summary>
    /// Swaps a result of an option into an optional result: no value gives <c>None</c>, an error stays an error and an
    /// empty result stays empty. Needs explicit type arguments.
    /// </summary>
    public static Option<TOutResult> Transpose<TSelf, T, TError, TOutResult>(
        this IResult<TSelf, Option<T>, TError> result)
        where TSelf : struct, IResult<TSelf, Option<T>, TError>
        where TError : notnull
        where TOutResult : struct, IResult<TOutResult, T, TError>
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Value switch
        {
            TError error => Option<TOutResult>.Of(TOutResult.FromError(error)),
            Option<T> inner => inner.TryGetValue(out var value)
                ? Option<TOutResult>.Of(TOutResult.FromValue(value))
                : Option<TOutResult>.NoValue,
            _ => Option<TOutResult>.Of(default(TOutResult))
        };
    }
}
