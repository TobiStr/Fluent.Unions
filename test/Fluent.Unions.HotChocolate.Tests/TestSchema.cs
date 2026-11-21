using System.Text.Json.Nodes;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Fluent.Unions.HotChocolate.Tests;

public record Book(int Id, string Title);

public class OutOfStockException(string sku) : Exception($"{sku} is out of stock.")
{
    public string Sku { get; } = sku;
}

public class InvalidTitleError(ValidationErrors errors)
{
    public string Message { get; } = errors.Message;

    public string? Member { get; } = errors.Errors[0].MemberName;
}

public class Query
{
    public Result<Book> GetBook(int id) => id == 1
        ? new Book(1, "Dune")
        : Result<Book>.Error(new KeyNotFoundException($"Book {id} was not found."));

    public async Task<Result<Book>> FetchBookAsync(int id)
    {
        await Task.Yield();
        return id == 1 ? new Book(1, "Dune") : Result<Book>.Error(new KeyNotFoundException($"Book {id} was not found."));
    }

    public async ValueTask<Result<int>> GetCountAsync()
    {
        await Task.Yield();
        return 42;
    }

    public Result<List<Book>> GetBooks() => new List<Book> { new(1, "Dune"), new(2, "Emma") };

    public Option<Book> FindBook(int id) => id == 1 ? new Book(1, "Dune") : Option.None;

    public async Task<Option<int>> FindNumberAsync(bool found)
    {
        await Task.Yield();
        return found ? 7 : Option.None;
    }

    public Option<string> DefaultOption() => default;

    public Result<Book> DefaultResult() => default;

    public Result<Book> Invalid() => new ValidationErrors(
    [
        new ValidationError("Title is required.", "Title"),
        new ValidationError("The book is not available.")
    ]);

    public Result<Book> InvalidSingle() => new ValidationError("Title is required.", "Title");

    public async Task<Result<Book>> WaitAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return new Book(0, "never");
    }

    public string Plain() => "plain";
}

public class Mutation
{
    [Error<OutOfStockException>]
    public Result<Book> Buy(string sku) => sku == "dune"
        ? new Book(1, "Dune")
        : Result<Book>.Error(new OutOfStockException(sku));

    [Error<OutOfStockException>]
    public async Task<Result<int>> BuyManyAsync(string sku)
    {
        await Task.Yield();
        return sku == "dune" ? 3 : Result<int>.Error(new OutOfStockException(sku));
    }

    [Error<InvalidTitleError>]
    public Result<Book> Rename(string title) => string.IsNullOrEmpty(title)
        ? new ValidationErrors([new ValidationError("Title is required.", "Title")])
        : new Book(1, title);

    public Result<Book> Unmapped() => Result<Book>.Error(new InvalidOperationException("boom"));
}

public sealed class RecordingErrorFilter : IErrorFilter
{
    public List<Exception?> Exceptions { get; } = [];

    public IError OnError(IError error)
    {
        Exceptions.Add(error.Exception);
        return error;
    }
}

public static class Executor
{
    public static async Task<IRequestExecutor> CreateAsync(
        bool withMutations = false,
        RecordingErrorFilter? filter = null)
    {
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFluentUnions();

        if (withMutations)
        {
            builder.AddMutationType<Mutation>().AddMutationConventions(applyToAllMutations: true);
        }

        if (filter is not null)
        {
            builder.AddErrorFilter(_ => filter);
        }

        return await builder.BuildRequestExecutorAsync();
    }

    public static async Task<JsonNode> RunAsync(this IRequestExecutor executor, string query)
    {
        var result = await executor.ExecuteAsync(query);
        return JsonNode.Parse(result.ToJson())!;
    }
}
