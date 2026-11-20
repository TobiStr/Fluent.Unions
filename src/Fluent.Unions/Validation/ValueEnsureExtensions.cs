using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Checks a plain value and returns it as a <see cref="Result{T}"/>. A null value is an error.
/// </summary>
public static class ValueEnsureExtensions
{
    /// <summary>
    /// Returns the value if it satisfies the predicate, otherwise a validation error with <paramref name="message"/>.
    /// The captured argument name becomes the member name of the error.
    /// </summary>
    public static Result<T> Ensure<T>(this T value, Func<T, bool> predicate, string message, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(message);

        return RuleFailure.Lift(value, message, name).Ensure(predicate, _ => RuleFailure.Create(message, message, name));
    }

    /// <summary>
    /// Returns the value if it satisfies the predicate, otherwise the error from <paramref name="error"/>.
    /// </summary>
    public static Result<T> Ensure<T>(this T value, Func<T, bool> predicate, Func<T, Exception> error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        return RuleFailure.Lift(value, null, null).Ensure(predicate, error);
    }
}
