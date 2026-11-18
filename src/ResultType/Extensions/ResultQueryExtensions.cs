namespace ResultType.Extensions;

/// <summary>
/// LINQ-style operators that run only when a result holds a value. Errors and empty results pass through.
/// </summary>
public static class ResultQueryExtensions
{
    /// <summary>
    /// Projects the value into a new form. A <c>null</c> projection gives an empty result.
    /// </summary>
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => error,
            T value => Capture.Run(() => new Result<TOut>(selector(value))),
            _ => default
        };
    }

    /// <summary>
    /// Produces a value after a successful <see cref="Result"/>.
    /// </summary>
    public static Result<TOut> Select<TOut>(this Result result, Func<TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => error,
            Ok => Capture.Run(() => new Result<TOut>(selector())),
            _ => default
        };
    }

    /// <summary>
    /// Chains an operation that returns a result itself.
    /// </summary>
    public static Result<TOut> SelectMany<T, TOut>(this Result<T> result, Func<T, Result<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => error,
            T value => Capture.Run(() => selector(value)),
            _ => default
        };
    }

    /// <summary>
    /// Chains an operation and combines both values. Enables query syntax with multiple <c>from</c> clauses.
    /// </summary>
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
    public static Result SelectMany<T>(this Result<T> result, Func<T, Result> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => error,
            T value => Capture.Run(() => selector(value)),
            _ => default
        };
    }

    /// <summary>
    /// Chains an operation after a successful <see cref="Result"/>.
    /// </summary>
    public static Result SelectMany(this Result result, Func<Result> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => error,
            Ok => Capture.Run(selector),
            _ => default
        };
    }

    /// <summary>
    /// Chains an operation that produces a value after a successful <see cref="Result"/>.
    /// </summary>
    public static Result<TOut> SelectMany<TOut>(this Result result, Func<Result<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.Value switch
        {
            Exception error => error,
            Ok => Capture.Run(selector),
            _ => default
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
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes empty.
    /// </summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return result.Value switch
        {
            Exception => result,
            T value => Capture.Run(() => predicate(value) ? result : default),
            _ => result
        };
    }

    /// <summary>
    /// Combines two results into a tuple. The first error wins, then an empty side makes the result empty.
    /// </summary>
    public static Result<(T1 First, T2 Second)> Zip<T1, T2>(this Result<T1> first, Result<T2> second) =>
        first.Zip(second, (a, b) => (a, b));

    /// <summary>
    /// Combines two results with a selector. The first error wins, then an empty side makes the result empty.
    /// </summary>
    public static Result<TOut> Zip<T1, T2, TOut>(
        this Result<T1> first,
        Result<T2> second,
        Func<T1, T2, TOut> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(resultSelector);

        return (first.Value, second.Value) switch
        {
            (Exception error, _) => error,
            (_, Exception error) => error,
            (T1 a, T2 b) => Capture.Run(() => new Result<TOut>(resultSelector(a, b))),
            _ => default
        };
    }

    /// <summary>
    /// Returns the value, or <c>default</c> for an error or empty result.
    /// </summary>
    public static T? GetValueOrDefault<T>(this Result<T> result) => result.Value switch
    {
        Exception => default,
        T value => value,
        _ => default
    };

    /// <summary>
    /// Returns the value, or <paramref name="fallback"/> for an error or empty result.
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, T fallback) => result.Value switch
    {
        Exception => fallback,
        T value => value,
        _ => fallback
    };

    /// <summary>
    /// Replaces an empty result with <paramref name="fallback"/>. Values and errors pass through.
    /// </summary>
    public static Result<T> DefaultIfEmpty<T>(this Result<T> result, T fallback) =>
        result.Value is null ? new Result<T>(fallback) : result;
}
