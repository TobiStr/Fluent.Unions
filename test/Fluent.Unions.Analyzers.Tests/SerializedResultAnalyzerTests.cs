using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Fluent.Unions.Analyzers.Tests;

public class SerializedResultAnalyzerTests
{
    private const string ConvertAdvice = "convert it first, for example with Match or GetValueOrDefault";
    private const string MappingAdvice = "add .WithResultMapping() from Fluent.Unions.AspNetCore, or return ToHttpResult()";

    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize(r);", "Result<int>")]
    [TestCase("string M(Result r) => JsonSerializer.Serialize(r);", "Result")]
    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize<Result<int>>(r);", "Result<int>")]
    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize(r, new JsonSerializerOptions());", "Result<int>")]
    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize((object)r);", "Result<int>")]
    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize<object>(r);", "Result<int>")]
    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize(r, typeof(Result<int>));", "Result<int>")]
    [TestCase("string M(object r) => JsonSerializer.Serialize(r, typeof(Result<int>));", "Result<int>")]
    [TestCase("string M(Result<int>? r) => JsonSerializer.Serialize(r);", "Result<int>")]
    [TestCase("Task M(System.IO.Stream s, Result<int> r) => JsonSerializer.SerializeAsync(s, r);", "Result<int>")]
    [TestCase("byte[] M(Result<int> r) => JsonSerializer.SerializeToUtf8Bytes(r);", "Result<int>")]
    [TestCase("JsonElement M(Result<int> r) => JsonSerializer.SerializeToElement(r);", "Result<int>")]
    [TestCase("System.Text.Json.Nodes.JsonNode? M(Result<int> r) => JsonSerializer.SerializeToNode(r);", "Result<int>")]
    [TestCase("JsonDocument M(Result<int> r) => JsonSerializer.SerializeToDocument(r);", "Result<int>")]
    public async Task ReportsSerializedResults(string member, string type)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0008" }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"'{type}' throws NotSupportedException when it is serialized; {ConvertAdvice}"));
    }

    [TestCase("string M(Order o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Order[] o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(List<Order> o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(IEnumerable<Order> o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Dictionary<string, Order> o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Dictionary<string, Result<int>> o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Wrapper o) => JsonSerializer.Serialize(o);", "Result")]
    [TestCase("string M(Derived o) => JsonSerializer.Serialize(o);", "Result")]
    [TestCase("string M(Cyclic o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Order? o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Level1 o) => JsonSerializer.Serialize(o);", "Result<int>")]
    [TestCase("string M(Pair o) => JsonSerializer.Serialize(o);", "Result<int>")]
    public async Task ReportsResultsInsideTheSerializedType(string member, string type)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0008" }));
        Assert.That(diagnostics[0].GetMessage(), Does.StartWith($"'{type}' throws"));
    }

    [TestCase("string M() => JsonSerializer.Serialize(1);")]
    [TestCase("string M() => JsonSerializer.Serialize(\"text\");")]
    [TestCase("string M(Option<int> o) => JsonSerializer.Serialize(o);")]
    [TestCase("string M(Loadable<int> l) => JsonSerializer.Serialize(l);")]
    [TestCase("string M(Clean c) => JsonSerializer.Serialize(c);")]
    [TestCase("string M(Hidden h) => JsonSerializer.Serialize(h);")]
    [TestCase("string M(Level0 o) => JsonSerializer.Serialize(o);")]
    [TestCase("string M(List<int> l) => JsonSerializer.Serialize(l);")]
    [TestCase("string M(DateTime d) => JsonSerializer.Serialize(d);")]
    [TestCase("string M(object o) => JsonSerializer.Serialize(o);")]
    [TestCase("Result<int>? M(string json) => JsonSerializer.Deserialize<Result<int>?>(json);")]
    [TestCase("string M(Result<int> r) => r.Match(v => JsonSerializer.Serialize(v), e => e.Message);")]
    [TestCase("string M(Result<int> r) => JsonSerializer.Serialize(r.GetValueOrDefault());")]
    [TestCase("string M(Result<Clean> r) => JsonSerializer.Serialize(r.GetValueOrDefault());")]
    [TestCase("string M(Result<int> r) => Custom.Serialize(r);")]
    public async Task IgnoresValuesWithoutResults(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("app.MapGet(\"/a\", () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", Result () => Result.Ok);", "Result")]
    [TestCase("app.MapPost(\"/a\", () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.MapPut(\"/a\", () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.MapDelete(\"/a\", () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.MapPatch(\"/a\", () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.Map(\"/a\", () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.MapMethods(\"/a\", new[] { \"GET\" }, () => Result<int>.Ok(1));", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", async () => await Task.FromResult(Result<int>.Ok(1)));", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", () => Task.FromResult(Result<int>.Ok(1)));", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", () => new ValueTask<Result<int>>(Result<int>.Ok(1)));", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", Handler);", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", AsyncHandler);", "Result<int>")]
    [TestCase("Func<Result<int>> handler = Handler; app.MapGet(\"/a\", handler);", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", Handler).WithName(\"a\");", "Result<int>")]
    [TestCase("app.MapGet(\"/a\", Handler).WithName(\"a\").WithTags(\"b\");", "Result<int>")]
    [TestCase("var group = app.MapGroup(\"/api\"); group.MapGet(\"/a\", Handler);", "Result<int>")]
    [TestCase("var other = app.MapGroup(\"/other\").WithResultMapping(); app.MapGet(\"/a\", Handler);", "Result<int>")]
    [TestCase("var other = app.MapGroup(\"/other\"); other.WithResultMapping(); app.MapGet(\"/a\", Handler);", "Result<int>")]
    [TestCase("WebApplication.Create().MapGet(\"/a\", Handler);", "Result<int>")]
    public async Task ReportsUnmappedEndpoints(string body, string type)
    {
        var diagnostics = await Analyze($"void M() {{ var app = WebApplication.Create(); {body} }}");

        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "FLU0008" }));
        Assert.That(diagnostics[0].GetMessage(), Is.EqualTo($"'{type}' throws NotSupportedException when it is serialized; {MappingAdvice}"));
    }

    [TestCase("app.MapGet(\"/a\", Handler).WithResultMapping();")]
    [TestCase("app.MapGet(\"/a\", Handler).WithName(\"a\").WithResultMapping();")]
    [TestCase("app.MapGet(\"/a\", Handler).WithResultMapping().WithName(\"a\");")]
    [TestCase("app.MapGet(\"/a\", () => Result<int>.Ok(1)).WithResultMapping();")]
    [TestCase("app.MapPost(\"/a\", AsyncHandler).WithResultMapping();")]
    [TestCase("app.MapMethods(\"/a\", new[] { \"GET\" }, Handler).WithResultMapping();")]
    [TestCase("var group = app.MapGroup(\"/api\").WithResultMapping(); group.MapGet(\"/a\", Handler);")]
    [TestCase("var group = app.MapGroup(\"/api\"); group.WithResultMapping(); group.MapGet(\"/a\", Handler);")]
    [TestCase("var group = app.MapGroup(\"/api\"); group.MapGet(\"/a\", Handler); group.WithResultMapping();")]
    [TestCase("app.MapGroup(\"/api\").WithResultMapping().MapGet(\"/a\", Handler);")]
    [TestCase("app.MapGroup(\"/api\").MapGet(\"/a\", Handler);")]
    [TestCase("app.MapGet(\"/a\", () => Result<int>.Ok(1).ToHttpResult());")]
    [TestCase("app.MapGet(\"/a\", () => Results.Ok(1));")]
    [TestCase("app.MapGet(\"/a\", () => \"text\");")]
    [TestCase("app.MapGet(\"/a\", () => Task.FromResult(1));")]
    [TestCase("app.MapGet(\"/a\", () => Option.Some(1));")]
    [TestCase("app.MapGet(\"/a\", async context => await context.Response.WriteAsync(\"text\"));")]
    [TestCase("app.MapGet(\"/a\", Void);")]
    [TestCase("app.MapCustom(\"/a\", Handler);")]
    [TestCase("Custom.MapGet(app, \"/a\", Handler);")]
    public async Task IgnoresMappedOrNonResultEndpoints(string body)
    {
        var diagnostics = await Analyze($"void M() {{ var app = WebApplication.Create(); {body} }}");

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("void M(WebApplication app) { app.MapGet(\"/a\", Handler); }")]
    [TestCase("void M(RouteGroupBuilder group) { group.MapGet(\"/a\", Handler); }")]
    [TestCase("void M(IEndpointRouteBuilder routes) { routes.MapGet(\"/a\", Handler); }")]
    [TestCase("WebApplication App = WebApplication.Create(); void M() { App.MapGet(\"/a\", Handler); }")]
    [TestCase("WebApplication App => WebApplication.Create(); void M() { App.MapGet(\"/a\", Handler); }")]
    [TestCase("void M(WebApplication app) { app.MapGet(\"/a\", () => Result<int>.Ok(1)); }")]
    public async Task IgnoresReceiversItCannotInspect(string member)
    {
        var diagnostics = await Analyze(member);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task ReportsEachUnmappedEndpoint()
    {
        var diagnostics = await Analyze("""
            void M()
            {
                var app = WebApplication.Create();
                app.MapGet("/a", Handler);
                app.MapGet("/b", Handler).WithResultMapping();
                app.MapPost("/c", AsyncHandler);
            }
            """);

        Assert.That(diagnostics, Has.Length.EqualTo(2));
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string member) =>
        AnalyzerTest.Analyze(new SerializedResultAnalyzer(), $$"""
            using System.Text.Json;
            using System.Text.Json.Serialization;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using Fluent.Unions.AspNetCore;

            public class MyError : IFromException<MyError>
            {
                public static MyError FromException(Exception exception) => new();
            }

            public class Order
            {
                public string Name { get; set; } = "";
                public Result<int> Status { get; set; }
            }

            public class Wrapper
            {
                public List<Result> Results { get; set; } = new();
            }

            public class Base
            {
                public Result Inherited { get; set; }
            }

            public class Derived : Base
            {
                public int Id { get; set; }
            }

            public class Cyclic
            {
                public Cyclic? Parent { get; set; }
                public Result<int> Status { get; set; }
            }

            public class Pair
            {
                public Dictionary<string, (int Count, Order Order)> Entries { get; set; } = new();
            }

            public class Clean
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public Option<int> Maybe { get; set; }
                public Loadable<int> Loading { get; set; }
                public Clean? Parent { get; set; }
            }

            public class Hidden
            {
                [JsonIgnore]
                public Result<int> Status { get; set; }
            }

            public class Level0 { public Level1 Next { get; set; } = new(); }
            public class Level1 { public Level2 Next { get; set; } = new(); }
            public class Level2 { public Level3 Next { get; set; } = new(); }
            public class Level3 { public Level4 Next { get; set; } = new(); }
            public class Level4 { public Level5 Next { get; set; } = new(); }
            public class Level5 { public Result<int> Status { get; set; } }

            public static class Custom
            {
                public static string Serialize(object value) => "";
                public static void MapGet(WebApplication app, string pattern, Delegate handler) { }
            }

            public static class CustomExtensions
            {
                public static void MapCustom(this WebApplication app, string pattern, Delegate handler) { }
            }

            public class Usage
            {
                static Result<int> Handler() => Result<int>.Ok(1);
                static Task<Result<int>> AsyncHandler() => Task.FromResult(Result<int>.Ok(1));
                static void Void() { }

                {{member}}
            }
            """);
}
