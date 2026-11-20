
namespace Fluent.Unions;

/// <summary>
/// Checks the value of a result and turns a failed check into an error.
/// </summary>
public static class ResultEnsureExtensions
{
    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes the error from <paramref name="error"/>.
    /// An error passes through and the predicate does not run.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Func<T, Exception> error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        return result.Case switch
        {
            Exception => result,
            T value => Capture.Run(() => predicate(value) ? result : error(value)),
            _ => result
        };
    }

    /// <summary>
    /// Keeps the value if it satisfies the predicate, otherwise the result becomes a <see cref="ValidationError"/>
    /// with <paramref name="message"/>.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return result.Ensure(predicate, _ => new ValidationError(message));
    }
}
