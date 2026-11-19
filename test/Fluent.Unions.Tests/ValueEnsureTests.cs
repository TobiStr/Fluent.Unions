using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class ValueEnsureTests
{
    [Test]
    public void PassingPredicateReturnsTheValue()
    {
        var quantity = 5;

        Result<int> result = quantity.Ensure(q => q > 3, "Too small.");

        Assert.That(result.GetOk(), NUnit.Framework.Is.EqualTo(5));
    }

    [Test]
    public void FailingPredicateUsesTheMessageVerbatim()
    {
        var quantity = 2;

        Result<int> result = quantity.Ensure(q => q > 3, "{Name} is wrong.");

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("{Name} is wrong."));
    }

    [Test]
    public void CapturedNameFillsTheMemberNames()
    {
        var quantity = 2;

        Result<int> result = quantity.Ensure(q => q > 3, "Too small.");

        var error = (ValidationException)result.GetError();
        Assert.That(error.ValidationResult.MemberNames, NUnit.Framework.Is.EqualTo(new[] { "quantity" }));
    }

    [Test]
    public void ExplicitNameFillsTheMemberNames()
    {
        Result<int> result = 2.Ensure(q => q > 3, "Too small.", "quantity");

        var error = (ValidationException)result.GetError();
        Assert.That(error.ValidationResult.MemberNames, NUnit.Framework.Is.EqualTo(new[] { "quantity" }));
    }

    [Test]
    public void FailingPredicateGivesTheSuppliedException()
    {
        var quantity = 2;

        Result<int> result = quantity.Ensure(q => q > 3, q => new ArgumentOutOfRangeException("quantity", q, "Too small."));

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NullValueIsAnErrorNamedAfterTheArgument()
    {
        string? nickname = null;

        Result<string?> result = nickname.Ensure(n => n!.Length > 0, "Nickname is empty.");

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Nickname is empty."));
    }

    [Test]
    public void NullValueWithTheExceptionFormIsAnError()
    {
        string? nickname = null;
        var calls = 0;

        Result<string?> result = nickname.Ensure(n => { calls++; return true; }, n => new InvalidOperationException());

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<ValidationException>());
        Assert.That(result.Message, NUnit.Framework.Is.EqualTo("Value must not be null."));
        Assert.That(calls, NUnit.Framework.Is.EqualTo(0));
    }

    [Test]
    public void ThrowingPredicateBecomesAnError()
    {
        var quantity = 2;

        Result<int> result = quantity.Ensure(q => throw new InvalidOperationException("Boom."), "Too small.");

        Assert.That(result.GetError(), NUnit.Framework.Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ResultReceiverStillBindsToTheResultOverload()
    {
        Result<int> result = Result<int>.Ok(2).Ensure(_ => false, "Too small.");

        var error = (ValidationException)result.GetError();
        Assert.That(error.ValidationResult.MemberNames, NUnit.Framework.Is.Empty);
    }

    [Test]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => 1.Ensure(null!, "message"));
        Assert.Throws<ArgumentNullException>(() => 1.Ensure(_ => true, (string)null!));
        Assert.Throws<ArgumentNullException>(() => 1.Ensure(null!, _ => new Exception()));
        Assert.Throws<ArgumentNullException>(() => 1.Ensure(_ => true, (Func<int, Exception>)null!));
    }
}
