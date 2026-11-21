using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Tests;

public record CustomerNotFound(int Id);

public record CartEmpty;

public record PaymentDeclined(string Reason);

public record Unexpected(Exception Exception);

public union CheckoutError(CustomerNotFound, CartEmpty, PaymentDeclined, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) => new Unexpected(exception);
}

[FluentResult<CheckoutError>]
public readonly partial struct CheckoutResult<T>;
