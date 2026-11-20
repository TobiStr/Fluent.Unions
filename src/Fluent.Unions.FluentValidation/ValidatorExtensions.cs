using FluentValidation;
using FluentValidation.Results;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.FluentValidation;

/// <summary>
/// Runs a FluentValidation validator and returns the outcome as a <see cref="Result{T}"/> or a custom result type.
/// A validator that throws gives the exception as the error; cancellation propagates.
/// </summary>
public static class ValidatorExtensions
{
    /// <summary>
    /// Gives the instance when it is valid. Otherwise the error is a <see cref="ValidationErrors"/>
    /// with one <see cref="ValidationError"/> per failure, named after the property.
    /// </summary>
    public static Result<T> ValidateToResult<T>(this IValidator<T> validator, T instance)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        return Result.Try(() => ToResult(instance, validator.Validate(instance)));
    }

    /// <summary>
    /// Async form of <see cref="ValidateToResult{T}(IValidator{T}, T)"/>.
    /// </summary>
    public static Task<Result<T>> ValidateToResultAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        return Result.TryAsync(async () =>
            ToResult(instance, await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false)));
    }

    /// <summary>
    /// Validates the value of a result. An error passes through without calling the validator.
    /// </summary>
    public static Result<T> Validate<T>(this Result<T> result, IValidator<T> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        return result.TryGetValue(out var value) ? validator.ValidateToResult(value) : result;
    }

    /// <summary>
    /// Async form of <see cref="Validate{T}(Result{T}, IValidator{T})"/>.
    /// </summary>
    public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, IValidator<T> validator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);

        return result.TryGetValue(out var value)
            ? await validator.ValidateToResultAsync(value, cancellationToken).ConfigureAwait(false)
            : result;
    }

    /// <summary>
    /// Validates the value of a custom result. A failure becomes the error that <c>FromException</c> makes of a
    /// <see cref="ValidationErrors"/>. An error passes through without calling the validator.
    /// </summary>
    public static TSelf Validate<TSelf, T, TError>(this IResult<TSelf, T, TError> result, IValidator<T> validator)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(validator);

        return result.Validate(validator, failures => TSelf.FromException(ToValidationErrors(failures)));
    }

    /// <summary>
    /// Validates the value of a custom result. A failure becomes the error that <paramref name="map"/> makes of all
    /// failures, for error types that are data. An error passes through without calling the validator.
    /// </summary>
    public static TSelf Validate<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        IValidator<T> validator,
        Func<IReadOnlyList<ValidationFailure>, TError> map)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(map);

        if (result.TryGetError(out _))
            return (TSelf)result;

        var value = (T)result.Value!;

        try
        {
            return ToResult<TSelf, T, TError>(value, validator.Validate(value), map);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return TSelf.FromError(TSelf.FromException(exception));
        }
    }

    /// <summary>
    /// Async form of <see cref="Validate{TSelf, T, TError}(IResult{TSelf, T, TError}, IValidator{T})"/>.
    /// </summary>
    public static Task<TSelf> ValidateAsync<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        IValidator<T> validator,
        CancellationToken cancellationToken = default)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(validator);

        return result.ValidateAsync(validator, failures => TSelf.FromException(ToValidationErrors(failures)), cancellationToken);
    }

    /// <summary>
    /// Async form of <see cref="Validate{TSelf, T, TError}(IResult{TSelf, T, TError}, IValidator{T}, Func{IReadOnlyList{ValidationFailure}, TError})"/>.
    /// </summary>
    public static async Task<TSelf> ValidateAsync<TSelf, T, TError>(
        this IResult<TSelf, T, TError> result,
        IValidator<T> validator,
        Func<IReadOnlyList<ValidationFailure>, TError> map,
        CancellationToken cancellationToken = default)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(map);

        if (result.TryGetError(out _))
            return (TSelf)result;

        var value = (T)result.Value!;

        try
        {
            return ToResult<TSelf, T, TError>(
                value,
                await validator.ValidateAsync(value, cancellationToken).ConfigureAwait(false),
                map);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return TSelf.FromError(TSelf.FromException(exception));
        }
    }

    private static Result<T> ToResult<T>(T instance, ValidationResult validation) =>
        validation.IsValid ? instance : ToValidationErrors(validation.Errors);

    private static TSelf ToResult<TSelf, T, TError>(
        T instance,
        ValidationResult validation,
        Func<IReadOnlyList<ValidationFailure>, TError> map)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        if (validation.IsValid)
            return TSelf.FromValue(instance);

        return TSelf.FromError(map(validation.Errors) ?? throw new InvalidOperationException("The failure mapping returned null."));
    }

    private static ValidationErrors ToValidationErrors(IEnumerable<ValidationFailure> failures) =>
        new(failures.Select(failure => new ValidationError(
            failure.ErrorMessage,
            string.IsNullOrEmpty(failure.PropertyName) ? null : failure.PropertyName)));
}
