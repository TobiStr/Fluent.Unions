using System.ComponentModel;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Validation that collects every failure instead of stopping at the first one.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultValidationCore
{
    /// <summary>
    /// Runs every rule on the value. If any fail, the error is a <see cref="ValidationErrors"/> with all failures,
    /// or an <see cref="AggregateException"/> when a failure is not a <see cref="ValidationError"/>.
    /// A rule that throws counts as a failure. Errors pass through.
    /// </summary>
    public static TSelf Validate<TSelf, T, TError, TRule>(TSelf result, params Func<T, TRule>[] rules)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TRule : struct, IFluentUnitResult<TRule, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var rule in rules)
            ArgumentNullException.ThrowIfNull(rule, nameof(rules));

        if (!result.TryGetValue(out var value))
            return result;

        List<TError>? errors = null;
        foreach (var rule in rules)
        {
            TRule outcome;
            try
            {
                outcome = rule(value);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                outcome = Flow.Thrown<TRule, TError>(exception);
            }

            if (outcome.TryGetError(out var error))
                (errors ??= []).Add(error);
        }

        return errors is null ? result : TSelf.FromError(Flow.Combine<TSelf, TError>(errors));
    }
}
