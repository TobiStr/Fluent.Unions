using Fluent.Unions.Validation;

namespace Fluent.Unions.Tests;

public class ValidationErrorTests
{
    private sealed record Customer(string Name, string Email, int Age);

    [Test]
    public void ValidateCollectsValidationErrorsWithTheirMemberNames()
    {
        var customer = new Customer("", "nope", 3);

        Result<Customer> result = Result<Customer>.Ok(customer).Validate(
            c => c.Name.EnsureNotEmpty(),
            c => c.Email.EnsureMatchesEmail(),
            c => c.Age.EnsureInRange(18, 120));

        var error = (ValidationErrors)result.GetError();
        Assert.That(error.Errors.Select(e => e.MemberName), NUnit.Framework.Is.EqualTo(new[] { "Name", "Email", "Age" }));
        Assert.That(error.ToDictionary().Keys, NUnit.Framework.Is.EquivalentTo(new[] { "Name", "Email", "Age" }));
    }

    [Test]
    public void ValidateWithAFailureThatIsNotAValidationErrorGivesAnAggregate()
    {
        var other = new InvalidOperationException("Database down");

        Result<Customer> result = Result<Customer>.Ok(new Customer("", "ada@example.com", 30)).Validate(
            c => c.Name.EnsureNotEmpty(),
            _ => Result.Error(other));

        var error = (AggregateException)result.GetError();
        Assert.That(error.InnerExceptions, Has.Count.EqualTo(2));
        Assert.That(error.InnerExceptions[0], NUnit.Framework.Is.TypeOf<ValidationError>());
        Assert.That(error.InnerExceptions[1], NUnit.Framework.Is.SameAs(other));
    }

    [Test]
    public void CombineAllGivesValidationErrorsWhenEveryFailureIsAValidationError()
    {
        Result<string> name = Result<string>.Ok("").Ensure(value => value.Length > 0, "Name is required.");
        Result<int> age = Result<int>.Ok(3).Ensure(value => value > 17, "Too young.");

        var combined = Result.CombineAll(name, age);

        var error = (ValidationErrors)combined.GetError();
        Assert.That(error.Errors.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[] { "Name is required.", "Too young." }));
    }

    [Test]
    public void CombineAllGivesAnAggregateWhenAFailureIsNotAValidationError()
    {
        Result<string> name = Result<string>.Ok("").Ensure(value => value.Length > 0, "Name is required.");
        Result<int> age = new InvalidOperationException("Age service is down.");

        var combined = Result.CombineAll(name, age);

        var error = (AggregateException)combined.GetError();
        Assert.That(error.InnerExceptions.Select(e => e.Message), NUnit.Framework.Is.EqualTo(new[] { "Name is required.", "Age service is down." }));
    }

    [Test]
    public void EnsureWithAMessageGivesAValidationErrorWithoutAMember()
    {
        Result<int> result = Result<int>.Ok(2).Ensure(value => value > 3, "Too small.");

        var error = (ValidationError)result.GetError();
        Assert.That(error.Message, NUnit.Framework.Is.EqualTo("Too small."));
        Assert.That(error.MemberName, NUnit.Framework.Is.Null);
    }

    [Test]
    public void ToDictionaryGroupsByMemberAndUsesAnEmptyKeyForNoMember()
    {
        var errors = new ValidationErrors([
            new ValidationError("Too short", "Name"),
            new ValidationError("Not upper case", "Name"),
            new ValidationError("Rejected")
        ]);

        var dictionary = errors.ToDictionary();

        Assert.That(dictionary["Name"], NUnit.Framework.Is.EqualTo(new[] { "Too short", "Not upper case" }));
        Assert.That(dictionary[""], NUnit.Framework.Is.EqualTo(new[] { "Rejected" }));
    }
}
