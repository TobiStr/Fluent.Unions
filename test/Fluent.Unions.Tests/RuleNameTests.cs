using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class RuleNameTests
{
    private sealed record Address(string Zip);

    private sealed record Customer(string Name, Address Address);

    private static string GetName() => "";

    private static IEnumerable<string> Members(Result<string> result) =>
        ((ValidationException)result.GetError()).ValidationResult.MemberNames;

    [Test]
    public void MemberAccessKeepsTheLastSegment()
    {
        var c = new Customer("", new Address(""));

        Result<string> result = c.Name.EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Name must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Name" }));
    }

    [Test]
    public void NestedMemberAccessKeepsTheLastSegment()
    {
        var customer = new Customer("Jane", new Address(""));

        Result<string> result = customer.Address.Zip.EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Zip must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Zip" }));
    }

    [Test]
    public void TrailingNullForgivingOperatorIsDropped()
    {
        var c = new Customer("", new Address(""));

        Result<string> result = c.Name!.EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Name must not be empty."));
    }

    [Test]
    public void LocalVariableKeepsItsName()
    {
        var age = "";

        Result<string> result = age.EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("age must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "age" }));
    }

    [Test]
    public void MethodCallFallsBackToValueWithoutMemberNames()
    {
        Result<string> result = GetName().EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void LiteralFallsBackToValue()
    {
        Result<string> result = "".EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must not be empty."));
    }

    [Test]
    public void ResultFormDefaultsToValue()
    {
        Result<string> result = Result<string>.Ok("").EnsureNotEmpty();

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.Empty);
    }

    [Test]
    public void ResultFormUsesTheExplicitName()
    {
        Result<string> result = Result<string>.Ok("").EnsureNotEmpty(name: "Zip");

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Zip must not be empty."));
        Assert.That(Members(result), NUnit.Framework.Is.EqualTo(new[] { "Zip" }));
    }

    [Test]
    public void ExplicitNameOnTheValueFormReplacesTheCapturedOne()
    {
        var c = new Customer("", new Address(""));

        Result<string> result = c.Name.EnsureNotEmpty(name: "Surname");

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Surname must not be empty."));
    }

    [Test]
    public void ExplicitNameThatIsNotAnIdentifierFallsBackToValue()
    {
        Result<string> result = Result<string>.Ok("").EnsureNotEmpty(name: "Order total");

        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must not be empty."));
    }
}
