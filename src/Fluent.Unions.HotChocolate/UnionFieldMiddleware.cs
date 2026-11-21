using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Fluent.Unions.Abstractions;
using HotChocolate.Resolvers;

namespace Fluent.Unions.HotChocolate;

internal static class UnionFieldMiddleware
{
    public static FieldMiddleware UnwrapResult => next => async context =>
    {
        await next(context);

        if (context.Result is IUnion { Value: var value } result)
        {
            context.Result = value switch
            {
                Exception error => Throw(error),
                null => throw new InvalidOperationException(
                    "The resolver returned a default Result<T>, which holds neither a value nor an error."),
                _ when ResultTypes.ErrorType(result.GetType())!.IsInstanceOfType(value) => Throw(new ResultErrorException(value)),
                _ => value
            };
        }
    };

    public static FieldMiddleware UnwrapOption => next => async context =>
    {
        await next(context);

        if (context.Result is IUnion { Value: var value })
        {
            context.Result = value is None ? null : value;
        }
    };

    private static object Throw(Exception error)
    {
        ExceptionDispatchInfo.Capture(error).Throw();
        return error;
    }
}
