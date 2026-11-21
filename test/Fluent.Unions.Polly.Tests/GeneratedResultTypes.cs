using Fluent.Unions;
using Fluent.Unions.Abstractions;

namespace PollyDomain;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new DomainException(exception.Message, exception);
}

public sealed class TransientException(string message) : DomainException(message);

public sealed class PermanentException(string message) : DomainException(message);

[FluentResult<DomainException>]
public readonly partial struct Result<T>;
