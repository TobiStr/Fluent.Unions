using Fluent.Unions;
using Fluent.Unions.Abstractions;

namespace AspNetCoreDomain;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new DomainException(exception.Message, exception);
}

public sealed class MissingException(string message) : DomainException(message);

[FluentResult<DomainException>(Companion = false)]
public readonly partial struct Result<T>;

[FluentResult<DomainException>]
public readonly partial struct Result;
