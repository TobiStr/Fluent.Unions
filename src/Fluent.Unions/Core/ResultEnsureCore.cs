using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Checks the value of a result and turns a failed check into an error.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultEnsureCore
{
    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes the error from <paramref name="error"/>.
    /// An error passes through and the predicate does not run.
    /// </summary>
    public static TSelf Ensure<TSelf, T, TError>(TSelf result, Func<T, bool> predicate, Func<T, TError> error)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        if (!result.TryGetValue(out var value))
            return result;

        try
        {
            return predicate(value) ? result : TSelf.FromError(error(value));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes a <see cref="ValidationError"/>
    /// with <paramref name="message"/>. An error type that is not an exception gets it through <c>FromException</c>.
    /// </summary>
    public static TSelf Ensure<TSelf, T, TError>(TSelf result, Func<T, bool> predicate, string message)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(predicate);

        if (!result.TryGetValue(out var value))
            return result;

        try
        {
            return predicate(value) ? result : Flow.Thrown<TSelf, TError>(new ValidationError(message));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }
}
