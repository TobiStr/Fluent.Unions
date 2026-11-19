using FluentValidation;
using FluentValidation.Results;

namespace Fluent.Unions.FluentValidation;

/// <summary>
/// Runs a FluentValidation validator and returns the outcome as a <see cref="Result{T}"/>.
/// </summary>
public static class ValidatorExtensions
{
    /// <summary>
    /// Gives the instance when it is valid. Otherwise the error is an <see cref="AggregateException"/>
    /// with one <see cref="ValidationException"/> per failure.
    /// </summary>
    public static Result<T> ValidateToResult<T>(this IValidator<T> validator, T instance)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        return ToResult(instance, validator.Validate(instance));
    }

    /// <summary>
    /// Async form of <see cref="ValidateToResult{T}(IValidator{T}, T)"/>.
    /// </summary>
    public static async Task<Result<T>> ValidateToResultAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        return ToResult(instance, await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Validates the value of a result. Errors and empty results pass through without calling the validator.
    /// </summary>
    public static Result<T> Validate<T>(this Result<T> result, IValidator<T> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        return result.Value is T value && result.Value is not Exception ? validator.ValidateToResult(value) : result;
    }

    /// <summary>
    /// Async form of <see cref="Validate{T}(Result{T}, IValidator{T})"/>.
    /// </summary>
    public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, IValidator<T> validator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);

        return result.Value is T value && result.Value is not Exception
            ? await validator.ValidateToResultAsync(value, cancellationToken).ConfigureAwait(false)
            : result;
    }

    private static Result<T> ToResult<T>(T instance, ValidationResult validation)
    {
        if (validation.IsValid)
            return instance;

        var errors = validation.Errors
            .Select(failure => (Exception)new ValidationException(failure.ErrorMessage, [failure]))
            .ToList();

        return new AggregateException(errors).Flatten();
    }
}
