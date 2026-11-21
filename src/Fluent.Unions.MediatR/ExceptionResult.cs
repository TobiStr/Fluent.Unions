using System.Reflection;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.MediatR;

internal static class ExceptionResult<TResponse> where TResponse : struct
{
    private static readonly Func<Exception, TResponse>? Factory = CreateFactory();

    internal static bool IsResult => Factory is not null;

    internal static TResponse Create(Exception exception) => Factory!(exception);

    private static Func<Exception, TResponse>? CreateFactory()
    {
        if (ResultTypes.ErrorType(typeof(TResponse)) is not { } errorType)
            return null;

        var method = typeof(ExceptionResult).GetMethod(nameof(ExceptionResult.Build), BindingFlags.Static | BindingFlags.NonPublic)!;

        return method.MakeGenericMethod(typeof(TResponse), errorType).CreateDelegate<Func<Exception, TResponse>>();
    }
}

internal static class ExceptionResult
{
    internal static TResponse Build<TResponse, TError>(Exception exception)
        where TResponse : struct, IErrorResult<TResponse, TError>
        where TError : notnull =>
        TResponse.FromError(TResponse.FromException(exception));
}
