using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Abstractions;
using Mediator;

namespace Fluent.Unions.Mediator.Tests;

public static class Calls
{
    public static int Count;
}

public union Outcome<T>(T, Exception) : IResult<Outcome<T>, T, Exception>
{
    public static Outcome<T> FromValue(T value) => value;

    public static Outcome<T> FromError(Exception error) => error;

    public static Exception FromException(Exception exception) => exception;
}

public record Circle(int Radius);

public record Square(int Side);

public union Shape(Circle, Square);

public sealed record GetNumber(int Id) : IRequest<Result<int>>;

public sealed class GetNumberHandler : IRequestHandler<GetNumber, Result<int>>
{
    public ValueTask<Result<int>> Handle(GetNumber request, CancellationToken cancellationToken)
    {
        Calls.Count++;

        return request.Id switch
        {
            < 0 => throw new InvalidOperationException("negative"),
            0 => throw new OperationCanceledException(),
            _ => new ValueTask<Result<int>>(request.Id),
        };
    }
}

public sealed record GetOutcome(int Id) : IRequest<Outcome<int>>;

public sealed class GetOutcomeHandler : IRequestHandler<GetOutcome, Outcome<int>>
{
    public ValueTask<Outcome<int>> Handle(GetOutcome request, CancellationToken cancellationToken)
        => request.Id < 0 ? throw new InvalidOperationException("negative") : new ValueTask<Outcome<int>>(request.Id);
}

public sealed record GetShape(int Id) : IRequest<Shape>;

public sealed class GetShapeHandler : IRequestHandler<GetShape, Shape>
{
    public ValueTask<Shape> Handle(GetShape request, CancellationToken cancellationToken)
        => request.Id < 0 ? throw new InvalidOperationException("negative") : new ValueTask<Shape>(new Circle(request.Id));
}

public sealed record DoWork(int Id) : IRequest<Result>;

public sealed class DoWorkHandler : IRequestHandler<DoWork, Result>
{
    public ValueTask<Result> Handle(DoWork request, CancellationToken cancellationToken)
        => request.Id < 0 ? throw new InvalidOperationException("negative") : new ValueTask<Result>(new Ok());
}

public sealed record CreateUser([property: Required] string Name, [property: Range(1, 120)] int Age) : IRequest<Result<string>>;

public sealed class CreateUserHandler : IRequestHandler<CreateUser, Result<string>>
{
    public ValueTask<Result<string>> Handle(CreateUser request, CancellationToken cancellationToken)
    {
        Calls.Count++;

        return new ValueTask<Result<string>>(request.Name);
    }
}

public sealed record Transfer(string From, string To) : IRequest<Result<string>>, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From == To)
            yield return new ValidationResult("Source and target must differ.");
    }
}

public sealed class TransferHandler : IRequestHandler<Transfer, Result<string>>
{
    public ValueTask<Result<string>> Handle(Transfer request, CancellationToken cancellationToken)
    {
        Calls.Count++;

        return new ValueTask<Result<string>>(request.To);
    }
}

public sealed class ThrowingAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => throw new InvalidOperationException("validator failed");
}

public sealed record Broken([property: Throwing] string Name) : IRequest<Result<string>>;

public sealed class BrokenHandler : IRequestHandler<Broken, Result<string>>
{
    public ValueTask<Result<string>> Handle(Broken request, CancellationToken cancellationToken)
        => new(request.Name);
}

public sealed record Announce(string Text) : INotification;

public sealed class AnnounceHandler : INotificationHandler<Announce>
{
    public ValueTask Handle(Announce notification, CancellationToken cancellationToken)
    {
        Calls.Count++;

        return ValueTask.CompletedTask;
    }
}

public record Failure(string Reason) : IFromException<Failure>
{
    public static Failure FromException(Exception exception) => new(exception.Message);
}

public sealed record GetFailing(int Id) : IRequest<Result<int, Failure>>;

public sealed class GetFailingHandler : IRequestHandler<GetFailing, Result<int, Failure>>
{
    public ValueTask<Result<int, Failure>> Handle(GetFailing request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("negative");
}
