using Fluent.Unions;
using Fluent.Unions.Abstractions;

namespace OrleansDomain;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new DomainException(exception.Message, exception);
}

[FluentResult<DomainException>]
public readonly partial struct Result<T>;
