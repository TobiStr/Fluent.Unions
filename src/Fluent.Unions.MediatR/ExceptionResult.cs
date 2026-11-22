using System.Reflection;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.MediatR;

internal static class ExceptionResult<TResponse> where TResponse : struct
{
    private static readonly Func<Exception, TResponse>? Factory = CreateDelegate<Func<Exception, TResponse>>(nameof(ExceptionResult.Build));

    private static readonly Func<Exception, bool>? Filter = CreateDelegate<Func<Exception, bool>>(nameof(ExceptionResult.Captures));

    internal static bool IsResult => Factory is not null;

    internal static bool Captures(Exception exception) => Filter!(exception);

    internal static TResponse Create(Exception exception) => Factory!(exception);

    private static TDelegate? CreateDelegate<TDelegate>(string name) where TDelegate : Delegate
    {
        if (ResultTypes.ErrorType(typeof(TResponse)) is not { } errorType)
            return null;

        var method = typeof(ExceptionResult).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

        return method.MakeGenericMethod(typeof(TResponse), errorType).CreateDelegate<TDelegate>();
    }
}

internal static class ExceptionResult
{
    internal static TResponse Build<TResponse, TError>(Exception exception)
        where TResponse : struct, IErrorResult<TResponse, TError>
        where TError : notnull =>
        TResponse.FromError(TResponse.FromException(exception));

    internal static bool Captures<TResponse, TError>(Exception exception)
        where TResponse : struct, IErrorResult<TResponse, TError>
        where TError : notnull =>
        TResponse.Captures(exception);
}
