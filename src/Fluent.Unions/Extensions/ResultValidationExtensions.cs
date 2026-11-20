namespace Fluent.Unions;

/// <summary>
/// Validation that collects every failure instead of stopping at the first one.
/// </summary>
public static class ResultValidationExtensions
{
    /// <summary>
    /// Runs every rule on the value. If any fail, the error is a <see cref="ValidationErrors"/> with all failures,
    /// or an <see cref="AggregateException"/> when a failure is not a <see cref="ValidationError"/>.
    /// A rule that throws counts as a failure. Errors pass through.
    /// </summary>
    public static Result<T> Validate<T>(this Result<T> result, params Func<T, Result>[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
            ArgumentNullException.ThrowIfNull(rule, nameof(rules));

        if (result.Case is Exception || result.Case is not T value)
            return result;

        var errors = new List<Exception>();

        foreach (var rule in rules)
        {
            switch (Capture.Run(() => rule(value)).Case)
            {
                case Ok:
                    break;
                case Exception error:
                    errors.Add(error);
                    break;
            }
        }

        return errors.Count == 0 ? result : ValidationErrors.Combine(errors);
    }
}
