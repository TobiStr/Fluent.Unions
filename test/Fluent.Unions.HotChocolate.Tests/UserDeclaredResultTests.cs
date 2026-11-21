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

    public StockResult<Book> GetStocked(int id) => id == 1 ? new Book(1, "Dune") : new OutOfStock("secret-sku");

    public Result<Option<Book>> FindBook(int id)
    {
        Option<Book> found = id == 1 ? new Book(1, "Dune") : Option.None;
        return found;
    }
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
    public async Task ResultWithADataErrorRaisesAFieldErrorWithoutExposingTheError()
    {
        var filter = new RecordingFilter();
        var executor = await CreateAsync(filter);

        var found = await RunAsync(executor, "{ stocked(id: 1) { title } }");
        var json = await RunAsync(executor, "{ stocked(id: 2) { title } }");

        Assert.That(executor.Schema.ToString(), Does.Contain("stocked(id: Int!): Book!"));
        Assert.That(found["data"]!["stocked"]!["title"]!.GetValue<string>(), Is.EqualTo("Dune"));
        Assert.That(json["errors"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That(json["errors"]![0]!["message"]!.GetValue<string>(), Does.Not.Contain("secret-sku"));
        var exception = filter.Exceptions.OfType<ResultErrorException>().Single();
        Assert.That(exception.Error, Is.EqualTo(new OutOfStock("secret-sku")));
    }

    [Test]
    public async Task ResultOfOptionBecomesANullableValueType()
    {
        var executor = await CreateAsync();

        var found = await RunAsync(executor, "{ findBook(id: 1) { title } }");
        var missing = await RunAsync(executor, "{ findBook(id: 2) { title } }");

        Assert.That(executor.Schema.ToString().Split("\n").Select(line => line.Trim()), Does.Contain("findBook(id: Int!): Book"));
        Assert.That(executor.Schema.Types.Select(type => type.Name), Has.None.StartsWith("Option"));
        Assert.That(found["data"]!["findBook"]!["title"]!.GetValue<string>(), Is.EqualTo("Dune"));
        Assert.That(missing["data"]!["findBook"], Is.Null);
        Assert.That(missing["errors"], Is.Null);
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
