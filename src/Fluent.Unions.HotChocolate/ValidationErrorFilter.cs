using HotChocolate;
using HotChocolate.Execution;

namespace Fluent.Unions.HotChocolate;

internal sealed class ValidationErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            ValidationErrors validation => new AggregateError(validation.Errors.Select(failure => Describe(error, failure))),
            ValidationError failure => Describe(error, failure),
            _ => error
        };
    }

    private static IError Describe(IError error, ValidationError failure)
    {
        var described = error.WithMessage(failure.Message).WithException(failure);

        return failure.MemberName is null ? described : described.SetExtension("member", failure.MemberName);
    }
}
