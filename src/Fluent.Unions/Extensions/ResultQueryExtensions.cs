using System.ComponentModel;
using System.Diagnostics;

namespace Fluent.Unions;

/// <summary>
/// LINQ-style operators that run only when a result holds a value. Errors pass through.
/// </summary>
public static class ResultQueryExtensions
{
    /// <summary>
    /// Projects the value into a new form. A <c>null</c> projection gives an error.
    /// </summary>
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Case switch
        {
            Exception error => error,
            T value => Capture.Run(() => Result<TOut>.Of(selector(value))),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Produces a value after a successful <see cref="Result"/>.
    /// </summary>
    public static Result<TOut> Select<TOut>(this Result result, Func<TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Case switch
        {
            Exception error => error,
            Ok => Capture.Run(() => Result<TOut>.Of(selector())),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Chains an operation that returns a result itself.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TOut> SelectMany<T, TOut>(this Result<T> result, Func<T, Result<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Case switch
        {
            Exception error => error,
            T value => Capture.Run(() => selector(value)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Chains an operation and combines both values. Enables query syntax with multiple <c>from</c> clauses.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TOut> SelectMany<T, TMid, TOut>(
        this Result<T> result,
        Func<T, Result<TMid>> selector,
        Func<T, TMid, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return result.SelectMany(value => selector(value).Select(mid => resultSelector(value, mid)));
    }

    /// <summary>
    /// Chains an operation that returns a <see cref="Result"/> without a value.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result SelectMany<T>(this Result<T> result, Func<T, Result> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Case switch
        {
            Exception error => error,
            T value => Capture.Run(() => selector(value)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Chains an operation after a successful <see cref="Result"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result SelectMany(this Result result, Func<Result> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Case switch
        {
            Exception error => error,
            Ok => Capture.Run(selector),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Chains an operation that produces a value after a successful <see cref="Result"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TOut> SelectMany<TOut>(this Result result, Func<Result<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Case switch
        {
            Exception error => error,
            Ok => Capture.Run(selector),
            _ => throw new UnreachableException()
        };
    }

    /// <inheritdoc cref="SelectMany{T, TOut}(Result{T}, Func{T, Result{TOut}})"/>
    public static Result<TOut> Chain<T, TOut>(this Result<T> result, Func<T, Result<TOut>> selector) =>
        result.SelectMany(selector);

    /// <inheritdoc cref="SelectMany{T}(Result{T}, Func{T, Result})"/>
    public static Result Chain<T>(this Result<T> result, Func<T, Result> selector) =>
        result.SelectMany(selector);

    /// <inheritdoc cref="SelectMany(Result, Func{Result})"/>
    public static Result Chain(this Result result, Func<Result> selector) =>
        result.SelectMany(selector);

    /// <inheritdoc cref="SelectMany{TOut}(Result, Func{Result{TOut}})"/>
    public static Result<TOut> Chain<TOut>(this Result result, Func<Result<TOut>> selector) =>
        result.SelectMany(selector);

    /// <summary>
    /// Combines two results into a tuple. The first error wins.
    /// </summary>
    public static Result<(T1 First, T2 Second)> Zip<T1, T2>(this Result<T1> first, Result<T2> second) =>
        first.Zip(second, (a, b) => (a, b));

    /// <summary>
    /// Combines two results with a selector. The first error wins.
    /// </summary>
    public static Result<TOut> Zip<T1, T2, TOut>(
        this Result<T1> first,
        Result<T2> second,
        Func<T1, T2, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(resultSelector);

        return (first.Case, second.Case) switch
        {
            (Exception error, _) => error,
            (_, Exception error) => error,
            (T1 a, T2 b) => Capture.Run(() => Result<TOut>.Of(resultSelector(a, b))),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Returns the value, or <c>default</c> for an error.
    /// </summary>
    public static T? GetValueOrDefault<T>(this Result<T> result) => result.Case switch
    {
        Exception => default,
        T value => value,
        _ => throw new UnreachableException()
    };

    /// <summary>
    /// Returns the value, or <paramref name="fallback"/> for an error.
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, T fallback) => result.Case switch
    {
        Exception => fallback,
        T value => value,
        _ => throw new UnreachableException()
    };

    /// <summary>
    /// Returns the value, or the result of <paramref name="defaultValueFactory"/> for an error.
    /// The factory runs only when needed, and its exceptions propagate.
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, Func<T> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        return result.TryGetValue(out var value) ? value : defaultValueFactory();
    }

    /// <summary>
    /// Flattens a result of a result. Like <see cref="TaskExtensions.Unwrap{TResult}(Task{Task{TResult}})"/>.
    /// </summary>
    public static Result<T> Unwrap<T>(this Result<Result<T>> result) => result.Case switch
    {
        Exception error => error,
        Result<T> inner => inner,
        _ => throw new UnreachableException()
    };

    /// <summary>
    /// Flattens a result of a <see cref="Result"/>.
    /// </summary>
    public static Result Unwrap(this Result<Result> result) => result.Case switch
    {
        Exception error => error,
        Result inner => inner,
        _ => throw new UnreachableException()
    };
}
