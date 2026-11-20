using Polly;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Polly;

/// <summary>
/// Lets a Polly strategy handle the error state of a <see cref="Result{T}"/> or a custom result type.
/// </summary>
public static class PredicateBuilderExtensions
{
    /// <summary>
    /// Handles every result that is an error.
    /// </summary>
    public static PredicateBuilder<Result<T>> HandleError<T>(this PredicateBuilder<Result<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.HandleResult(result => result.IsError);
    }

    /// <summary>
    /// Handles results that are an error for which <paramref name="predicate"/> returns true.
    /// </summary>
    public static PredicateBuilder<Result<T>> HandleError<T>(this PredicateBuilder<Result<T>> builder, Func<Exception, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(predicate);

        return builder.HandleResult(result => result.TryGetError(out var error) && predicate(error));
    }

    /// <summary>
    /// Handles every custom result that is an error. Needs explicit type arguments.
    /// </summary>
    public static PredicateBuilder<TResult> HandleError<TResult, TError>(this PredicateBuilder<TResult> builder)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.HandleResult(result => result.TryGetError(out _));
    }

    /// <summary>
    /// Handles custom results that are an error for which <paramref name="predicate"/> returns true. Needs explicit type arguments.
    /// </summary>
    public static PredicateBuilder<TResult> HandleError<TResult, TError>(this PredicateBuilder<TResult> builder, Func<TError, bool> predicate)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(predicate);

        return builder.HandleResult(result => result.TryGetError(out var error) && predicate(error));
    }
}
