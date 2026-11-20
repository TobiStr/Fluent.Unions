using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fluent.Unions.HotChocolate.Tests;

public class LimitationTests
{
    public class NonGenericQuery
    {
        public Result Run() => Result.Ok;
    }

    [Test]
    public void NonGenericResult_FailsLoudlyWhenTheSchemaIsBuilt()
    {
        var builder = new ServiceCollection().AddGraphQL().AddQueryType<NonGenericQuery>().AddFluentUnions();

        Assert.That(async () => await builder.BuildRequestExecutorAsync(), Throws.InstanceOf<SchemaException>());
    }
}
