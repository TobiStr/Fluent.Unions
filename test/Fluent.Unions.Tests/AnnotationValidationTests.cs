using System.ComponentModel.DataAnnotations;

namespace Fluent.Unions.Tests;

public class AnnotationValidationTests
{
    private sealed class SignUp
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is invalid.")]
        public string? Email { get; init; }

        [System.ComponentModel.DataAnnotations.Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
        public int Age { get; init; }
    }

    private sealed class DateRange : IValidatableObject
    {
        public DateOnly From { get; init; }

        public DateOnly To { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (To < From)
                yield return new ValidationResult("To must not be before From.", [nameof(From), nameof(To)]);
        }
    }

    private sealed class WholeObject : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult("The object is invalid.");
        }
    }

    private sealed class ThrowingAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => throw new InvalidOperationException("Boom");
    }

    private sealed class Throwing
    {
        [Throwing]
        public string Name { get; init; } = "";
    }

    private sealed class CancelingAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => throw new OperationCanceledException();
    }

    private sealed class Canceling
    {
        [Canceling]
        public string Name { get; init; } = "";
    }

    [Test]
    public void AValidValuePassesThrough()
    {
        var signUp = new SignUp { Email = "ada@example.com", Age = 36 };

        var result = Result.From(signUp).ValidateAnnotations();

        Assert.That(result.GetValue(), Is.SameAs(signUp));
    }

    [Test]
    public void EveryFailureBecomesAValidationErrorNamedAfterItsMember()
    {
        var result = Result.From(new SignUp { Email = "not-an-email", Age = 7 }).ValidateAnnotations();

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.Errors.Select(e => (e.MemberName, e.Message)), Is.EqualTo(new[]
        {
            ((string?)"Email", "Email is invalid."),
            ("Age", "Age must be between 18 and 120.")
        }));
    }

    [Test]
    public void RequiredStopsTheOtherAttributesOfThatProperty()
    {
        var result = Result.From(new SignUp { Age = 36 }).ValidateAnnotations();

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.ToDictionary()["Email"], Is.EqualTo(new[] { "Email is required." }));
    }

    [Test]
    public void ValidatableObjectsAreCheckedWithOneErrorPerMember()
    {
        var range = new DateRange { From = new DateOnly(2026, 2, 1), To = new DateOnly(2026, 1, 1) };

        var errors = (ValidationErrors)Result.From(range).ValidateAnnotations().GetError();

        Assert.That(errors.Errors.Select(e => e.MemberName), Is.EqualTo(new[] { "From", "To" }));
        Assert.That(errors.Errors.Select(e => e.Message).Distinct(), Is.EqualTo(new[] { "To must not be before From." }));
    }

    [Test]
    public void AFailureWithoutMemberHasNoMemberName()
    {
        var errors = (ValidationErrors)Result.From(new WholeObject()).ValidateAnnotations().GetError();

        Assert.That(errors.Errors.Single().MemberName, Is.Null);
    }

    [Test]
    public void AnErrorPassesThrough()
    {
        var failure = new TimeoutException();

        var result = ((Result<SignUp>)failure).ValidateAnnotations();

        Assert.That(result.GetError(), Is.SameAs(failure));
    }

    [Test]
    public void AThrowingAttributeGivesTheException()
    {
        var result = Result.From(new Throwing()).ValidateAnnotations();

        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("Boom"));
    }

    [Test]
    public void CancellationPropagates()
    {
        Assert.That(() => Result.From(new Canceling()).ValidateAnnotations(), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task TheTaskOverloadValidatesTheAwaitedResult()
    {
        var result = await Task.FromResult(Result.From(new SignUp { Email = "ada@example.com", Age = 3 })).ValidateAnnotations();

        Assert.That(((ValidationErrors)result.GetError()).Errors.Single().MemberName, Is.EqualTo("Age"));
    }
}
