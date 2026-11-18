namespace ResultType.Extensions;

/// <summary>
/// Validation that collects every failure instead of stopping at the first one.
/// </summary>
public static class ResultValidationExtensions
{
    /// <summary>
    /// Runs every rule on the value. If any fail, the error is an <see cref="AggregateException"/> with all failures.
    /// A rule that throws or returns an empty result counts as a failure. Errors and empty results pass through.
    /// </summary>
    public static Result<T> Validate<T>(this Result<T> result, params Func<T, Result>[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
            ArgumentNullException.ThrowIfNull(rule, nameof(rules));

        if (result.Value is Exception || result.Value is not T value)
            return result;

        var errors = new List<Exception>();

        foreach (var rule in rules)
        {
            switch (Capture.Run(() => rule(value)).Value)
            {
                case Ok:
                    break;
                case Exception error:
                    errors.Add(error);
                    break;
                default:
                    errors.Add(new InvalidOperationException("A validation rule returned an empty result."));
                    break;
            }
        }

        return errors.Count == 0 ? result : new AggregateException(errors).Flatten();
    }
}
