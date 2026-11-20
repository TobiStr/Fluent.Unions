using HotChocolate;
using HotChocolate.Execution;

namespace Fluent.Unions.HotChocolate;

internal sealed class ValidationErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is not ValidationErrors validation)
        {
            return error;
        }

        return new AggregateError(validation.Errors.Select(failure => Describe(error, failure)));
    }

    private static IError Describe(IError error, ValidationError failure)
    {
        var described = error.WithMessage(failure.Message).WithException(failure);

        return failure.MemberName is null ? described : described.SetExtension("member", failure.MemberName);
    }
}
