using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Validation with the attributes of <c>System.ComponentModel.DataAnnotations</c>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultAnnotationCore
{
    /// <summary>
    /// Checks the value with its DataAnnotations attributes and <see cref="IValidatableObject"/>, like
    /// <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/> with every property.
    /// If any fail, the error is a <see cref="ValidationErrors"/> with one <see cref="ValidationError"/> per failure and member.
    /// An attribute that throws gives the exception as the error. Errors pass through.
    /// </summary>
    [RequiresUnreferencedCode(ResultAnnotationExtensions.TrimmingMessage)]
    public static TSelf ValidateAnnotations<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (!result.TryGetValue(out var value) || value is null)
            return result;

        try
        {
            var failures = new List<ValidationResult>();
            return Validator.TryValidateObject(value, new ValidationContext(value), failures, validateAllProperties: true)
                ? result
                : Flow.Thrown<TSelf, TError>(new ValidationErrors(failures.SelectMany(ToErrors)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Flow.Thrown<TSelf, TError>(exception);
        }
    }

    private static IEnumerable<ValidationError> ToErrors(ValidationResult failure)
    {
        var message = failure.ErrorMessage ?? "The value is invalid.";
        var members = failure.MemberNames.Where(member => !string.IsNullOrEmpty(member)).ToList();

        return members.Count == 0
            ? [new ValidationError(message)]
            : members.Select(member => new ValidationError(message, member));
    }
}
