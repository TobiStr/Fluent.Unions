namespace Fluent.Unions;

/// <summary>
/// A failed validation rule, optionally for a named member such as a property.
/// </summary>
public class ValidationError(string message, string? memberName = null) : Exception(message)
{
    /// <summary>
    /// The member that failed, or <c>null</c> when the failure is about the whole value.
    /// </summary>
    public string? MemberName { get; } = memberName;
}

/// <summary>
/// Every failed validation rule of a value.
/// </summary>
public sealed class ValidationErrors : Exception
{
    /// <summary>
    /// Collects at least one failed rule.
    /// </summary>
    public ValidationErrors(IEnumerable<ValidationError> errors)
        : this(errors?.ToArray() ?? throw new ArgumentNullException(nameof(errors)))
    {
    }

    private ValidationErrors(ValidationError[] errors)
        : base(errors.Length == 0
            ? throw new ArgumentException("At least one validation error is required.", nameof(errors))
            : string.Join(" ", errors.Select(error => error.Message)))
    {
        Errors = errors;
    }

    /// <summary>
    /// The failed rules, in the order they ran.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// The messages grouped by member name. Failures without a member use the key <c>""</c>.
    /// </summary>
    public IDictionary<string, string[]> ToDictionary() => Errors
        .GroupBy(error => error.MemberName ?? "")
        .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

    internal static Exception Combine(IReadOnlyCollection<Exception> errors)
    {
        var flattened = errors.SelectMany(Flatten).ToList();

        return flattened.All(error => error is ValidationError)
            ? new ValidationErrors(flattened.Cast<ValidationError>())
            : new AggregateException(flattened);
    }

    private static IEnumerable<Exception> Flatten(Exception error) => error switch
    {
        ValidationErrors validation => validation.Errors,
        AggregateException aggregate => aggregate.Flatten().InnerExceptions.SelectMany(Flatten),
        _ => [error]
    };
}
