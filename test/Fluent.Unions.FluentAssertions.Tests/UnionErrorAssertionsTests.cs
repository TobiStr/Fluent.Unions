#pragma warning disable FLU0004
using FluentAssertions;
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.FluentAssertions;
using NUnit.Framework;

namespace FluentAssertionsDomain.Tests;

[FluentResult<string>]
public readonly partial struct SResult<T>
{
    public static string FromException(Exception exception) => exception.Message;
}

public class UnionErrorAssertionsTests
{
    [Test]
    public void BeOk_OnAResultWhoseValueHasTheErrorType_Passes()
    {
        SResult<string>.Ok("fine").Should().BeOk().Which.Should().Be("fine");
        SResult<string>.Ok("fine").Should().BeOkWithValue("fine");
    }

    [Test]
    public void BeError_OnAResultWhoseValueHasTheErrorType_PassesOnlyForTheError()
    {
        SResult<string>.Error("broken").Should().BeError().Which.Should().Be("broken");
        Assert.Catch<Exception>(() => SResult<string>.Ok("fine").Should().BeError());
        Assert.Catch<Exception>(() => SResult<string>.Error("broken").Should().BeOk());
    }
}
