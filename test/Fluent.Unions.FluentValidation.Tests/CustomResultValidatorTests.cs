using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework;
using Fluent.Unions.Abstractions;
using Fluent.Unions.FluentValidation;

namespace Fluent.Unions.FluentValidation.Tests;

public record ValidationFailed(IReadOnlyList<string> Messages);

public record Unexpected(Exception Exception);

public union CheckoutError(ValidationFailed, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) => new Unexpected(exception);
}

public union CheckoutResult<T>(T, CheckoutError) : IResult<CheckoutResult<T>, T, CheckoutError>
{
    public static CheckoutResult<T> FromValue(T value) => value;

    public static CheckoutResult<T> FromError(CheckoutError error) => error;

    public static CheckoutError FromException(Exception exception) => new Unexpected(exception);
}

public union ExceptionResult<T>(T, InvalidOperationException) : IResult<ExceptionResult<T>, T, InvalidOperationException>
{
    public static ExceptionResult<T> FromValue(T value) => value;

    public static ExceptionResult<T> FromError(InvalidOperationException error) => error;

    public static InvalidOperationException FromException(Exception exception) => new(exception.Message, exception);
}

public class CustomResultValidatorTests
{
    private static readonly Customer Valid = new("Ada", "ada@example.com");

    private static readonly Customer Invalid = new("", "nope");

    private static CheckoutError ToError(IReadOnlyList<ValidationFailure> failures) =>
        new ValidationFailed(failures.Select(f => f.ErrorMessage).ToList());

    private static CheckoutResult<Customer> Wrap(Customer customer) => customer;

    private static CheckoutResult<Customer> Failed(string message) =>
        CheckoutResult<Customer>.FromError(CheckoutResult<Customer>.FromException(new InvalidOperationException(message)));

    [Test]
    public void ValidValueStaysAValue()
    {
        var validated = Wrap(Valid).Validate(new CustomerValidator());
        var mapped = Wrap(Valid).Validate(new CustomerValidator(), ToError);

        Assert.That(validated.Value, Is.SameAs(Valid));
        Assert.That(mapped.Value, Is.SameAs(Valid));
    }

    [Test]
    public void FailuresBecomeValidationErrorsThroughFromException()
    {
        var validated = Wrap(Invalid).Validate(new CustomerValidator());

        var error = (CheckoutError)validated.Value!;
        var cause = ((Unexpected)error.Value!).Exception;
        var failures = ((ValidationErrors)cause).Errors;
        Assert.That(failures.Select(f => f.Message), Is.EqualTo(new[] { "Name is required.", "Email is invalid." }));
        Assert.That(failures.Select(f => f.MemberName), Is.EqualTo(new[] { "Name", "Email" }));
    }

    [Test]
    public void TheValidationErrorsMatchTheResultOfTVersion()
    {
        var custom = Wrap(Invalid).Validate(new CustomerValidator());
        Result<Customer> concrete = Invalid;

        var customCause = (ValidationErrors)((Unexpected)((CheckoutError)custom.Value!).Value!).Exception;
        var concreteCause = (ValidationErrors)concrete.Validate(new CustomerValidator()).GetError();

        Assert.That(
            customCause.Errors.Select(e => e.Message),
            Is.EqualTo(concreteCause.Errors.Select(e => e.Message)));
        Assert.That(
            customCause.Errors.Select(e => e.MemberName),
            Is.EqualTo(concreteCause.Errors.Select(e => e.MemberName)));
    }

    [Test]
    public void MapOverloadTurnsFailuresIntoADataError()
    {
        var validated = Wrap(Invalid).Validate(new CustomerValidator(), ToError);

        var error = (CheckoutError)validated.Value!;
        Assert.That(((ValidationFailed)error.Value!).Messages, Is.EqualTo(new[] { "Name is required.", "Email is invalid." }));
    }

    [Test]
    public void MapOverloadCapturesANullError()
    {
        ExceptionResult<Customer> result = Invalid;

        var validated = result.Validate(new CustomerValidator(), _ => null!);

        Assert.That(validated.Value, Is.TypeOf<InvalidOperationException>());
        Assert.That(((Exception)validated.Value!).Message, Does.Contain("returned null"));
    }

    [Test]
    public void ErrorAndEmptyPassThroughWithoutCallingTheValidator()
    {
        var validator = new CustomerValidator();
        var error = Failed("Boom");

        var passedError = error.Validate(validator);
        var passedMappedError = error.Validate(validator, ToError);
        var passedEmpty = default(CheckoutResult<Customer>).Validate(validator);
        var passedMappedEmpty = default(CheckoutResult<Customer>).Validate(validator, ToError);

        Assert.That(passedError.Value, Is.EqualTo(error.Value));
        Assert.That(passedMappedError.Value, Is.EqualTo(error.Value));
        Assert.That(passedEmpty.Value, Is.Null);
        Assert.That(passedMappedEmpty.Value, Is.Null);
        Assert.That(validator.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task AsyncFormsValidate()
    {
        var valid = await Wrap(Valid).ValidateAsync(new CustomerValidator());
        var validMapped = await Wrap(Valid).ValidateAsync(new CustomerValidator(), ToError);
        var invalid = await Wrap(Invalid).ValidateAsync(new CustomerValidator());
        var invalidMapped = await Wrap(Invalid).ValidateAsync(new CustomerValidator(), ToError);

        Assert.That(valid.Value, Is.SameAs(Valid));
        Assert.That(validMapped.Value, Is.SameAs(Valid));
        Assert.That(((Unexpected)((CheckoutError)invalid.Value!).Value!).Exception, Is.TypeOf<ValidationErrors>());
        Assert.That(((ValidationFailed)((CheckoutError)invalidMapped.Value!).Value!).Messages, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task AsyncFormsPassTheTokenOn()
    {
        using var source = new CancellationTokenSource();
        var seen = new List<CancellationToken>();
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MustAsync((_, token) =>
        {
            seen.Add(token);
            return Task.FromResult(true);
        });

        await Wrap(Valid).ValidateAsync(validator, source.Token);
        await Wrap(Valid).ValidateAsync(validator, ToError, source.Token);

        Assert.That(seen, Is.EqualTo(new[] { source.Token, source.Token }));
    }

    [Test]
    public async Task AsyncErrorAndEmptyPassThrough()
    {
        var validator = new CustomerValidator();
        var error = Failed("Boom");

        var passedError = await error.ValidateAsync(validator);
        var passedMappedError = await error.ValidateAsync(validator, ToError);
        var passedEmpty = await default(CheckoutResult<Customer>).ValidateAsync(validator);
        var passedMappedEmpty = await default(CheckoutResult<Customer>).ValidateAsync(validator, ToError);

        Assert.That(passedError.Value, Is.EqualTo(error.Value));
        Assert.That(passedMappedError.Value, Is.EqualTo(error.Value));
        Assert.That(passedEmpty.Value, Is.Null);
        Assert.That(passedMappedEmpty.Value, Is.Null);
        Assert.That(validator.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task ResultOfTErrorWorksThroughTheSameOverloads()
    {
        Result<Customer, CheckoutError> invalid = Invalid;
        Result<Customer, CheckoutError> valid = Valid;

        var mapped = invalid.Validate(new CustomerValidator(), ToError);
        var thrown = await invalid.ValidateAsync(new CustomerValidator());
        var kept = valid.Validate(new CustomerValidator());

        Assert.That(((ValidationFailed)mapped.GetError().Value!).Messages, Has.Count.EqualTo(2));
        Assert.That(((Unexpected)thrown.GetError().Value!).Exception, Is.TypeOf<ValidationErrors>());
        Assert.That(kept.GetValue(), Is.SameAs(Valid));
    }

    [Test]
    public void NullArgumentsThrow()
    {
        var result = Wrap(Valid);

        Assert.Throws<ArgumentNullException>(() => result.Validate(null!));
        Assert.Throws<ArgumentNullException>(() => result.Validate(null!, ToError));
        Assert.Throws<ArgumentNullException>(() => result.Validate(new CustomerValidator(), null!));
        Assert.ThrowsAsync<ArgumentNullException>(() => result.ValidateAsync(null!));
        Assert.ThrowsAsync<ArgumentNullException>(() => result.ValidateAsync(new CustomerValidator(), null!));
    }
}
