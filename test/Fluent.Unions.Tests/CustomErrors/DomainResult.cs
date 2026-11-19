using Fluent.Unions.Abstractions;

namespace CustomErrorSample;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner);

public class NotFoundException(string message) : DomainException(message);

public class UnexpectedDomainException(Exception inner) : DomainException(inner.Message, inner);

public union Result<T>(T, DomainException) : IResult<Result<T>, T, DomainException>
{
    public static Result<T> FromValue(T value) => value;

    public static Result<T> FromError(DomainException error) => error;

    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new UnexpectedDomainException(exception);
}

public static class DomainResultExtensions
{
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector) =>
        result.Select<Result<T>, T, DomainException, Result<TOut>, TOut>(selector);

    public static async Task<Result<TOut>> Chain<T, TOut>(this Task<Result<T>> task, Func<T, Result<TOut>> next) =>
        (await task).Chain(next);

    public static Result<T> ToDomain<T>(this Fluent.Unions.Result<T> result) =>
        result.MapError<Fluent.Unions.Result<T>, T, Exception, Result<T>, DomainException>(Result<T>.FromException);
}

public union NullHookResult<T>(T, DomainException) : IResult<NullHookResult<T>, T, DomainException>
{
    public static NullHookResult<T> FromValue(T value) => value;

    public static NullHookResult<T> FromError(DomainException error) => error;

    public static DomainException FromException(Exception exception) => null!;
}

public union ThrowingHookResult<T>(T, DomainException) : IResult<ThrowingHookResult<T>, T, DomainException>
{
    public static ThrowingHookResult<T> FromValue(T value) => value;

    public static ThrowingHookResult<T> FromError(DomainException error) => error;

    public static DomainException FromException(Exception exception) => throw new NotSupportedException("hook");
}
