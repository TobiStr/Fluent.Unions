using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Verify.Tests;

public record NotFound(int Id);

public record Declined(string Reason);

public record Unexpected(Exception Exception);

public union AppError(NotFound, Declined, Unexpected) : IFromException<AppError>
{
    public static AppError FromException(Exception exception) => new Unexpected(exception);
}

public record Order(int Id, Option<string> Note, Result<decimal> Price);
