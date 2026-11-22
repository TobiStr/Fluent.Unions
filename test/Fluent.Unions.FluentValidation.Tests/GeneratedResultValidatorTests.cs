using FluentValidation;
using FluentValidation.Results;
using Fluent.Unions;
using Fluent.Unions.FluentValidation;
using Fluent.Unions.FluentValidation.Tests;
using NUnit.Framework;

namespace FluentValidationDomain.Tests;

public class GeneratedResultValidatorTests
{
    private static readonly Customer Valid = new("Ada", "ada@example.com");

    private static readonly Customer Invalid = new("", "nope");

    private static DomainException ToError(IReadOnlyList<ValidationFailure> failures) =>
        new InvalidInputException(string.Join(" ", failures.Select(failure => failure.ErrorMessage)));

    [Test]
    public void ValidValueStaysAValue()
    {
        Result<Customer> result = Valid;

        var validated = result.Validate(new CustomerValidator());
        var mapped = result.Validate(new CustomerValidator(), ToError);

        Assert.That(validated.GetValue(), Is.SameAs(Valid));
        Assert.That(mapped.GetValue(), Is.SameAs(Valid));
    }

    [Test]
    public void Failures_BecomeValidationErrorsThroughFromException()
    {
        Result<Customer> result = Invalid;

        var error = result.Validate(new CustomerValidator()).GetError();

        var failures = ((ValidationErrors)error.InnerException!).Errors;
        Assert.That(failures.Select(f => f.MemberName), Is.EqualTo(new[] { "Name", "Email" }));
    }

    [Test]
    public void MapOverload_TurnsFailuresIntoTheMappedError()
    {
        Result<Customer> result = Invalid;

        var error = result.Validate(new CustomerValidator(), ToError).GetError();

        Assert.That(error, Is.TypeOf<InvalidInputException>().With.Message.EqualTo("Name is required. Email is invalid."));
    }

    [Test]
    public void Error_PassesThroughWithoutCallingTheValidator()
    {
        Result<Customer> result = new InvalidInputException("earlier");

        var validated = result.Validate(new CustomerValidator());

        Assert.That(validated.GetError().Message, Is.EqualTo("earlier"));
    }

    [Test]
    public async Task AsyncForms_Validate()
    {
        Result<Customer> valid = Valid;
        Result<Customer> invalid = Invalid;

        var validated = await valid.ValidateAsync(new CustomerValidator());
        var failed = await invalid.ValidateAsync(new CustomerValidator());
        var mapped = await invalid.ValidateAsync(new CustomerValidator(), ToError);

        Assert.That(validated.GetValue(), Is.SameAs(Valid));
        Assert.That(failed.GetError().InnerException, Is.TypeOf<ValidationErrors>());
        Assert.That(mapped.GetError(), Is.TypeOf<InvalidInputException>());
    }

    [Test]
    public void StrictPolicy_ThrowingValidator_Propagates()
    {
        StrictResult<Customer> result = Valid;
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(customer => customer.Name).Must(_ => throw new InvalidOperationException("broken rule"));

        Assert.Throws<InvalidOperationException>(() => result.Validate(validator));
    }
}
