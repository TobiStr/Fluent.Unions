using System.Text.Json.Nodes;
using HotChocolate;
using HotChocolate.Execution;
using NUnit.Framework;

namespace Fluent.Unions.HotChocolate.Tests;

public class QueryFieldTests
{
    private static string[] Lines(string schema) => schema.Split('\n').Select(line => line.Trim()).ToArray();

    [Test]
    public async Task Result_ExposesTheValueType()
    {
        var executor = await Executor.CreateAsync();

        var schema = executor.Schema.ToString();

        Assert.That(schema, Does.Contain("book(id: Int!): Book!"));
        Assert.That(schema, Does.Contain("fetchBook(id: Int!): Book!"));
        Assert.That(schema, Does.Contain("count: Int!"));
        Assert.That(schema, Does.Contain("books: [Book!]!"));
        Assert.That(executor.Schema.Types.Select(type => type.Name), Has.None.StartsWith("Result"));
    }

    [Test]
    public async Task Option_ExposesANullableValueType()
    {
        var executor = await Executor.CreateAsync();

        var schema = executor.Schema.ToString();

        Assert.That(Lines(schema), Does.Contain("findBook(id: Int!): Book"));
        Assert.That(Lines(schema), Does.Contain("findNumber(found: Boolean!): Int"));
        Assert.That(Lines(schema), Does.Contain("defaultOption: String"));
        Assert.That(executor.Schema.Types.Select(type => type.Name), Has.None.StartsWith("Option"));
    }

    [Test]
    public async Task Result_ReturnsTheValue()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ book(id: 1) { id title } }");

        Assert.That(json["data"]!["book"]!["title"]!.GetValue<string>(), Is.EqualTo("Dune"));
        Assert.That(json["errors"], Is.Null);
    }

    [Test]
    public async Task TaskOfResult_ReturnsTheValue()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ fetchBook(id: 1) { id title } }");

        Assert.That(json["data"]!["fetchBook"]!["id"]!.GetValue<int>(), Is.EqualTo(1));
        Assert.That(json["errors"], Is.Null);
    }

    [Test]
    public async Task ValueTaskOfResult_ReturnsTheValue()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ count }");

        Assert.That(json["data"]!["count"]!.GetValue<int>(), Is.EqualTo(42));
    }

    [Test]
    public async Task Result_OfAList_ReturnsTheItems()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ books { title } }");

        Assert.That(json["data"]!["books"]!.AsArray().Select(book => book!["title"]!.GetValue<string>()),
            Is.EqualTo(new[] { "Dune", "Emma" }));
    }

    [Test]
    public async Task ResultError_RethrowsTheOriginalException()
    {
        var filter = new RecordingErrorFilter();
        var executor = await Executor.CreateAsync(filter: filter);

        var json = await executor.RunAsync("{ book(id: 2) { id } }");

        Assert.That(json["data"], Is.Null);
        Assert.That(json["errors"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That(json["errors"]![0]!["path"]![0]!.GetValue<string>(), Is.EqualTo("book"));
        Assert.That(filter.Exceptions.Single(), Is.TypeOf<KeyNotFoundException>().With.Message.EqualTo("Book 2 was not found."));
    }

    [Test]
    public async Task TaskOfResultError_RethrowsTheOriginalException()
    {
        var filter = new RecordingErrorFilter();
        var executor = await Executor.CreateAsync(filter: filter);

        var json = await executor.RunAsync("{ fetchBook(id: 2) { id } }");

        Assert.That(json["errors"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That(filter.Exceptions.Single(), Is.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public async Task DefaultResult_IsAnError()
    {
        var filter = new RecordingErrorFilter();
        var executor = await Executor.CreateAsync(filter: filter);

        var json = await executor.RunAsync("{ defaultResult { id } }");

        Assert.That(json["errors"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That(filter.Exceptions.Single(), Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ValidationErrors_BecomeOneGraphQlErrorPerFailure()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ invalid { id } }");

        var errors = json["errors"]!.AsArray();
        Assert.That(errors, Has.Count.EqualTo(2));
        Assert.That(errors[0]!["message"]!.GetValue<string>(), Is.EqualTo("Title is required."));
        Assert.That(errors[0]!["extensions"]!["member"]!.GetValue<string>(), Is.EqualTo("Title"));
        Assert.That(errors[0]!["path"]![0]!.GetValue<string>(), Is.EqualTo("invalid"));
        Assert.That(errors[1]!["message"]!.GetValue<string>(), Is.EqualTo("The book is not available."));
        Assert.That(errors[1]!["extensions"], Is.Null);
    }

    [Test]
    public async Task Option_WithAValue_ReturnsTheValue()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ findBook(id: 1) { title } findNumber(found: true) }");

        Assert.That(json["data"]!["findBook"]!["title"]!.GetValue<string>(), Is.EqualTo("Dune"));
        Assert.That(json["data"]!["findNumber"]!.GetValue<int>(), Is.EqualTo(7));
        Assert.That(json["errors"], Is.Null);
    }

    [Test]
    public async Task Option_WithoutAValue_ReturnsNull()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ findBook(id: 2) { title } findNumber(found: false) defaultOption }");

        Assert.That(json["data"]!["findBook"], Is.Null);
        Assert.That(json["data"]!["findNumber"], Is.Null);
        Assert.That(json["data"]!["defaultOption"], Is.Null);
        Assert.That(json["errors"], Is.Null);
    }

    [Test]
    public async Task PlainFields_AreUnaffected()
    {
        var executor = await Executor.CreateAsync();

        var json = await executor.RunAsync("{ plain }");

        Assert.That(json["data"]!["plain"]!.GetValue<string>(), Is.EqualTo("plain"));
    }

    [Test]
    public async Task Cancellation_IsNotSwallowed()
    {
        var executor = await Executor.CreateAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var result = await executor.ExecuteAsync(OperationRequest.FromSourceText("{ wait { id } }"), cts.Token);
        var errors = JsonNode.Parse(result.ToJson())!["errors"]!.AsArray();

        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0]!["extensions"]!["code"]!.GetValue<string>(), Is.EqualTo("HC0049"));
        Assert.That(errors[0]!["path"], Is.Null);
    }

    [Test]
    public void AddFluentUnions_RejectsANullBuilder() =>
        Assert.That(() => RequestExecutorBuilderExtensions.AddFluentUnions(null!), Throws.ArgumentNullException);
}
