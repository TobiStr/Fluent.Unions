using NUnit.Framework;

namespace Fluent.Unions.HotChocolate.Tests;

public class MutationTests
{
    [Test]
    public async Task Success_FillsThePayload()
    {
        var executor = await Executor.CreateAsync(withMutations: true);

        var json = await executor.RunAsync("""
            mutation { buy(input: { sku: "dune" }) { book { title } errors { __typename } } }
            """);

        Assert.That(json["errors"], Is.Null);
        Assert.That(json["data"]!["buy"]!["book"]!["title"]!.GetValue<string>(), Is.EqualTo("Dune"));
        Assert.That(json["data"]!["buy"]!["errors"], Is.Null);
    }

    [Test]
    public async Task Error_BecomesAnErrorUnion()
    {
        var executor = await Executor.CreateAsync(withMutations: true);

        var json = await executor.RunAsync("""
            mutation { buy(input: { sku: "emma" }) { book { title } errors { __typename ... on OutOfStockError { message sku } } } }
            """);

        Assert.That(json["errors"], Is.Null);
        var payload = json["data"]!["buy"]!;
        Assert.That(payload["book"], Is.Null);
        Assert.That(payload["errors"]![0]!["__typename"]!.GetValue<string>(), Is.EqualTo("OutOfStockError"));
        Assert.That(payload["errors"]![0]!["message"]!.GetValue<string>(), Is.EqualTo("emma is out of stock."));
        Assert.That(payload["errors"]![0]!["sku"]!.GetValue<string>(), Is.EqualTo("emma"));
    }

    [Test]
    public async Task TaskOfResult_Success_FillsThePayload()
    {
        var executor = await Executor.CreateAsync(withMutations: true);

        var json = await executor.RunAsync("""
            mutation { buyMany(input: { sku: "dune" }) { int errors { __typename } } }
            """);

        Assert.That(json["errors"], Is.Null);
        Assert.That(json["data"]!["buyMany"]!["int"]!.GetValue<int>(), Is.EqualTo(3));
    }

    [Test]
    public async Task TaskOfResult_Error_BecomesAnErrorUnion()
    {
        var executor = await Executor.CreateAsync(withMutations: true);

        var json = await executor.RunAsync("""
            mutation { buyMany(input: { sku: "emma" }) { int errors { __typename } } }
            """);

        Assert.That(json["errors"], Is.Null);
        Assert.That(json["data"]!["buyMany"]!["errors"]![0]!["__typename"]!.GetValue<string>(), Is.EqualTo("OutOfStockError"));
    }

    [Test]
    public async Task ValidationErrors_CanBeMappedWithACustomErrorType()
    {
        var executor = await Executor.CreateAsync(withMutations: true);

        var json = await executor.RunAsync("""
            mutation { rename(input: { title: "" }) { book { title } errors { __typename ... on InvalidTitleError { message member } } } }
            """);

        Assert.That(json["errors"], Is.Null);
        Assert.That(json["data"]!["rename"]!["errors"]![0]!["member"]!.GetValue<string>(), Is.EqualTo("Title"));
    }

    [Test]
    public async Task UnmappedError_StaysAGraphQlError()
    {
        var filter = new RecordingErrorFilter();
        var executor = await Executor.CreateAsync(withMutations: true, filter: filter);

        var json = await executor.RunAsync("""
            mutation { unmapped { book { title } } }
            """);

        Assert.That(json["errors"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That(filter.Exceptions.Single(), Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("boom"));
    }
}
