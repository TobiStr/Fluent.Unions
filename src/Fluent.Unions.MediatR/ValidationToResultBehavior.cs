using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Abstractions;
using MediatR;

namespace Fluent.Unions.MediatR;

/// <summary>
/// Validates the request with its <see cref="System.ComponentModel.DataAnnotations"/> attributes and returns
/// <see cref="ValidationErrors"/> as an error result when it is invalid. The handler does not run then.
/// </summary>
public sealed class ValidationToResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : struct, IErrorResult<TResponse, Exception>
{
    /// <inheritdoc />
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();

        return Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true)
            ? next(cancellationToken)
            : Task.FromResult(TResponse.FromError(new ValidationErrors(ToErrors(results))));
    }

    private static IEnumerable<ValidationError> ToErrors(List<ValidationResult> results)
    {
        foreach (var result in results)
        {
            var message = result.ErrorMessage ?? "The request is invalid.";
            var members = result.MemberNames.ToArray();

            if (members.Length == 0)
                yield return new ValidationError(message);

            foreach (var member in members)
                yield return new ValidationError(message, member);
        }
    }
}
