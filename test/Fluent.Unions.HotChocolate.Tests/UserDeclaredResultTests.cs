using System.Text.Json.Nodes;
using Fluent.Unions;
using Fluent.Unions.HotChocolate;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace HotChocolateDomain.Tests;

public record Book(int Id, string Title);

public class Query
{
    public Result<Book> GetBook(int id) => id == 1 ? new Book(1, "Dune") : new DomainException($"Book {id} was not found.");

    public async Task<Result<int>> GetCountAsync()
    {
        await Task.Yield();
        return 42;
    }

    public Result<Book> DefaultResult() => default;
}

public class UserDeclaredResultTests
{
    private static async Task<IRequestExecutor> CreateAsync(RecordingFilter? filter = null)
    {
        var builder = new ServiceCollection().AddGraphQL().AddQueryType<Query>().AddFluentUnions();

        if (filter is not null)
            builder.AddErrorFilter(_ => filter);

        return await builder.BuildRequestExecutorAsync();
    }

    private static async Task<JsonNode> RunAsync(IRequestExecutor executor, string query) =>
        JsonNode.Parse((await executor.ExecuteAsync(query)).ToJson())!;

    private sealed class RecordingFilter : IErrorFilter
    {
        public List<Exception?> Exceptions { get; } = [];

        public IError OnError(IError error)
        {
            Exceptions.Add(error.Exception);
            return error;
        }
    }

    [Test]
    public async Task ResultExposesTheValueType()
    {
        var executor = await CreateAsync();

        var schema = executor.Schema.ToString();

        Assert.That(schema, Does.Contain("book(id: Int!): Book!"));
        Assert.That(schema, Does.Contain("count: Int!"));
        Assert.That(executor.Schema.Types.Select(type => type.Name), Has.None.StartsWith("Result"));
    }

    [Test]
    public async Task ResultReturnsTheValue()
    {
        var executor = await CreateAsync();

        var book = await RunAsync(executor, "{ book(id: 1) { id title } count }");

        Assert.That(book["data"]!["book"]!["title"]!.GetValue<string>(), Is.EqualTo("Dune"));
        Assert.That(book["data"]!["count"]!.GetValue<int>(), Is.EqualTo(42));
        Assert.That(book["errors"], Is.Null);
    }

    [Test]
    public async Task ResultErrorRethrowsTheOriginalException()
    {
        var filter = new RecordingFilter();
        var executor = await CreateAsync(filter);

        var json = await RunAsync(executor, "{ book(id: 2) { id } }");

        Assert.That(json["errors"]![0]!["message"]!.GetValue<string>(), Is.Not.Empty);
        Assert.That(filter.Exceptions.OfType<DomainException>().Single().Message, Is.EqualTo("Book 2 was not found."));
    }

    [Test]
    public async Task DefaultResultIsAnError()
    {
        var filter = new RecordingFilter();
        var executor = await CreateAsync(filter);

        var json = await RunAsync(executor, "{ defaultResult { id } }");

        Assert.That(json["errors"], Is.Not.Null);
        Assert.That(filter.Exceptions.OfType<InvalidOperationException>(), Is.Not.Empty);
    }
}
