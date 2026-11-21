using System.ComponentModel.DataAnnotations;
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Mediator;

namespace MediatorDomain;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) =>
        exception as DomainException ?? new DomainException(exception.Message, exception);
}

public sealed class MissingException(string message) : DomainException(message);

[FluentResult<DomainException>]
public readonly partial struct Result<T>;

[FluentResult<DomainException>]
public readonly partial struct Result;

public sealed record Find(int Id) : IRequest<Result<int>>;

public sealed class FindHandler : IRequestHandler<Find, Result<int>>
{
    public ValueTask<Result<int>> Handle(Find request, CancellationToken cancellationToken) => request.Id switch
    {
        < 0 => throw new InvalidOperationException("negative"),
        0 => throw new MissingException("missing"),
        > 100 => throw new OperationCanceledException(),
        _ => new ValueTask<Result<int>>(request.Id)
    };
}

public sealed record Run(int Id) : IRequest<Result>;

public sealed class RunHandler : IRequestHandler<Run, Result>
{
    public ValueTask<Result> Handle(Run request, CancellationToken cancellationToken) =>
        request.Id < 0 ? throw new InvalidOperationException("negative") : new ValueTask<Result>(Result.Ok);
}

public sealed record Register([property: Required] string Name) : IRequest<Result<string>>;

public sealed class RegisterHandler : IRequestHandler<Register, Result<string>>
{
    public static int Calls;

    public ValueTask<Result<string>> Handle(Register request, CancellationToken cancellationToken)
    {
        Calls++;

        return new ValueTask<Result<string>>(request.Name);
    }
}
