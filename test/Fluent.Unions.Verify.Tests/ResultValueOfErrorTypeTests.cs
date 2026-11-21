#pragma warning disable FLU0004
using NUnit.Framework;
using VerifyNUnit;

namespace Fluent.Unions.Verify.Tests;

[FluentResult<string>]
public readonly partial struct SResult<T>
{
    public static string FromException(Exception exception) => exception.Message;
}

[TestFixture]
public class ResultValueOfErrorTypeTests
{
    [Test]
    public Task Ok() => Verifier.Verify(SResult<string>.Ok("fine"));

    [Test]
    public Task Error() => Verifier.Verify(SResult<string>.Error("broken"));
}
