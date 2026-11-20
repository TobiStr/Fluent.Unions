using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Fluent.Unions;

/// <summary>
/// Validation with the attributes of <c>System.ComponentModel.DataAnnotations</c>.
/// </summary>
public static class ResultAnnotationExtensions
{
    internal const string TrimmingMessage =
        "DataAnnotations validation reads the attributes of the value's type through reflection.";

    /// <summary>
    /// Checks the value with its DataAnnotations attributes and <see cref="IValidatableObject"/>, like
    /// <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/> with every property.
    /// If any fail, the error is a <see cref="ValidationErrors"/> with one <see cref="ValidationError"/> per failure and member.
    /// An attribute that throws gives the exception as the error. Errors pass through.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessage)]
    public static Result<T> ValidateAnnotations<T>(this Result<T> result)
    {
        if (result.Case is Exception || result.Case is not T value)
            return result;

        try
        {
            var failures = new List<ValidationResult>();

            return Validator.TryValidateObject(value, new ValidationContext(value), failures, validateAllProperties: true)
                ? result
                : new ValidationErrors(failures.SelectMany(ToErrors));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return exception;
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
