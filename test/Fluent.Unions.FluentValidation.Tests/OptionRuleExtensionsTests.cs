using FluentValidation;
using NUnit.Framework;

namespace Fluent.Unions.FluentValidation.Tests;

public record Profile(Option<string> Nickname);

public record Member(Option<string> Nickname, Option<int> Age, Profile Profile);

public class MemberValidator : AbstractValidator<Member>
{
    public MemberValidator()
    {
        this.RuleForSome(m => m.Nickname)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(5).WithMessage("{PropertyName} is too long.");
        this.RuleForSome(m => m.Age).InclusiveBetween(0, 150);
        this.RuleForSome(m => m.Profile.Nickname).NotEmpty().WithMessage("{PropertyName} is required.");
    }
}

public class OptionRuleExtensionsTests
{
    private static readonly MemberValidator Validator = new();

    private static Member Valid => new(Option.None, Option.None, new Profile(Option.None));

    [Test]
    public void NoneIsValid()
    {
        Assert.That(Validator.Validate(Valid).IsValid, Is.True);
    }

    [Test]
    public void ValidValuesPass()
    {
        var member = new Member("Ada", 36, new Profile("ada"));

        Assert.That(Validator.Validate(member).IsValid, Is.True);
    }

    [Test]
    public void EveryRuleRunsOnTheValue()
    {
        var failures = Validator.Validate(Valid with { Nickname = "" }).Errors;

        Assert.That(failures.Select(f => f.PropertyName), Is.EqualTo(new[] { "Nickname" }));
        Assert.That(failures[0].ErrorMessage, Is.EqualTo("Nickname is required."));
    }

    [Test]
    public void ValidatorsAddedLaterInTheChainAlsoRunOnlyForSome()
    {
        var failures = Validator.Validate(Valid with { Nickname = "Augusta" }).Errors;

        Assert.That(failures.Select(f => f.PropertyName), Is.EqualTo(new[] { "Nickname" }));
        Assert.That(failures[0].ErrorMessage, Is.EqualTo("Nickname is too long."));
    }

    [Test]
    public void ValueTypesWork()
    {
        var failures = Validator.Validate(Valid with { Age = 200 }).Errors;

        Assert.That(failures.Select(f => f.PropertyName), Is.EqualTo(new[] { "Age" }));
    }

    [Test]
    public void NestedPropertiesAreNamedWithTheirPath()
    {
        var failures = Validator.Validate(Valid with { Profile = new Profile("") }).Errors;

        Assert.That(failures.Select(f => f.PropertyName), Is.EqualTo(new[] { "Profile.Nickname" }));
        Assert.That(failures[0].ErrorMessage, Is.EqualTo("Profile Nickname is required."));
    }

    [Test]
    public void FailuresBecomeValidationErrorsInAResult()
    {
        var result = Validator.ValidateToResult(Valid with { Nickname = "", Age = -1 });

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.Errors.Select(e => e.MemberName), Is.EqualTo(new[] { "Nickname", "Age" }));
    }

    [Test]
    public void AnExpressionThatIsNotAPropertyIsRejected()
    {
        var validator = new InlineValidator<Member>();

        Assert.That(() => validator.RuleForSome(m => Nickname(m)), Throws.ArgumentException);
    }

    private static Option<string> Nickname(Member member) => member.Nickname;
}
