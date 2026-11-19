using Polly;

namespace Fluent.Unions.Polly;

/// <summary>
/// Lets a Polly strategy handle the error state of a <see cref="Result{T}"/>.
/// </summary>
public static class PredicateBuilderExtensions
{
    /// <summary>
    /// Handles every result that is an error. Ok and empty results are not handled.
    /// </summary>
    public static PredicateBuilder<Result<T>> HandleError<T>(this PredicateBuilder<Result<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.HandleResult(result => result.Value is Exception);
    }

    /// <summary>
    /// Handles results that are an error for which <paramref name="predicate"/> returns true.
    /// </summary>
    public static PredicateBuilder<Result<T>> HandleError<T>(this PredicateBuilder<Result<T>> builder, Func<Exception, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(predicate);

        return builder.HandleResult(result => result.Value is Exception error && predicate(error));
    }
}
