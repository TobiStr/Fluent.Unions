using Fluent.Unions.Abstractions;

namespace OwnResultSample;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new UnexpectedDomainException(exception);
}

public sealed class NotFoundException(string message) : DomainException(message);

public sealed class OutOfStockException(string message) : DomainException(message);

public sealed class UnexpectedDomainException(Exception inner) : DomainException(inner.Message, inner);

[FluentResult<DomainException>]
public readonly partial struct Result<T>;

public sealed record Unexpected(Exception Exception);

public union CheckoutError(NotFoundException, OutOfStockException, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) => exception switch
    {
        NotFoundException notFound => notFound,
        OutOfStockException outOfStock => outOfStock,
        _ => new Unexpected(exception)
    };
}

[FluentResult<CheckoutError>]
public readonly partial struct Outcome<T>;

[FluentResult<string>]
public readonly partial struct TextResult<T>
{
    public static string FromException(Exception exception) => exception.Message;
}
