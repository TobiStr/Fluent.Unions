using Fluent.Unions;
using NUnit.Framework;
using VerifyNUnit;

namespace VerifyDomain.Tests;

[TestFixture]
public class UserDeclaredResultTests
{
    [Test]
    public Task ResultOk() => Verifier.Verify(Result.Ok);

    [Test]
    public Task ResultError() => Verifier.Verify((Result)new DomainException("disk full"));

    [Test]
    public Task ResultOfTOk() => Verifier.Verify((Result<int>)42);

    [Test]
    public Task ResultOfTOkObject() => Verifier.Verify((Result<object>)new { Name = "Ada", Age = 36 });

    [Test]
    public Task ResultOfTError() => Verifier.Verify((Result<int>)new DomainException("not a number"));

    [Test]
    public Task ResultOfTDefault() => Verifier.Verify(default(Result<int>));
}
