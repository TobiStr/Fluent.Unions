using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;
using Fluent.Unions.AspNetCore;
using Fluent.Unions.FluentValidation;

namespace Fluent.Unions.FluentValidation.Tests;

public record Customer(string Name, string Email);

public class CustomerValidator : AbstractValidator<Customer>
{
    public int Calls { get; private set; }

    public CustomerValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(c => c.Email).EmailAddress().WithMessage("Email is invalid.");
        RuleFor(c => c).Must(_ =>
        {
            Calls++;
            return true;
        });
    }
}

public class ValidatorExtensionsTests
{
    private static readonly Customer Valid = new("Ada", "ada@example.com");

    [Test]
    public void ValidInstanceGivesTheInstance()
    {
        var result = new CustomerValidator().ValidateToResult(Valid);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue(), Is.SameAs(Valid));
    }

    [Test]
    public void EveryFailureBecomesAValidationErrorWithItsMemberName()
    {
        var result = new CustomerValidator().ValidateToResult(new Customer("", "nope"));

        var error = (ValidationErrors)result.GetError();
        Assert.That(error.Errors.Select(f => f.Message), Is.EqualTo(new[] { "Name is required.", "Email is invalid." }));
        Assert.That(error.Errors.Select(f => f.MemberName), Is.EqualTo(new[] { "Name", "Email" }));
    }

    [Test]
    public void OneFailureIsStillValidationErrors()
    {
        var result = new CustomerValidator().ValidateToResult(new Customer("Ada", "nope"));

        var error = (ValidationErrors)result.GetError();
        Assert.That(error.Errors, Has.Count.EqualTo(1));
        Assert.That(error.Errors[0].MemberName, Is.EqualTo("Email"));
    }

    [Test]
    public void NullInstanceThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomerValidator().ValidateToResult(null!));
    }

    [Test]
    public void ResultFormValidatesTheValue()
    {
        Result<Customer> ok = Valid;
        Result<Customer> invalid = new Customer("", "ada@example.com");

        Assert.That(ok.Validate(new CustomerValidator()).IsSuccess, Is.True);
        Assert.That(invalid.Validate(new CustomerValidator()).GetError(), Is.TypeOf<ValidationErrors>());
    }

    [Test]
    public void ErrorPassesThroughWithoutCallingTheValidator()
    {
        var validator = new CustomerValidator();
        var failure = new InvalidOperationException("Boom");
        Result<Customer> result = failure;

        var validated = result.Validate(validator);

        Assert.That(validated.GetError(), Is.SameAs(failure));
        Assert.That(validator.Calls, Is.EqualTo(0));
    }

    [Test]
    public void DefaultStaysAnErrorWithoutCallingTheValidator()
    {
        var validator = new CustomerValidator();

        var validated = default(Result<Customer>).Validate(validator);

        Assert.That(validated.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(validator.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task AsyncFormsValidateAndPassTheTokenOn()
    {
        using var source = new CancellationTokenSource();
        var seen = default(CancellationToken);
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MustAsync((_, token) =>
        {
            seen = token;
            return Task.FromResult(true);
        });

        var direct = await validator.ValidateToResultAsync(Valid, source.Token);
        Result<Customer> wrapped = Valid;
        var fromResult = await wrapped.ValidateAsync(new CustomerValidator());
        var invalid = await ((Result<Customer>)new Customer("", "ada@example.com")).ValidateAsync(new CustomerValidator());

        Assert.That(direct.IsSuccess, Is.True);
        Assert.That(seen, Is.EqualTo(source.Token));
        Assert.That(fromResult.IsSuccess, Is.True);
        Assert.That(invalid.GetError(), Is.TypeOf<ValidationErrors>());
    }

    [Test]
    public async Task AsyncErrorAndEmptyPassThrough()
    {
        var validator = new CustomerValidator();
        Result<Customer> error = new InvalidOperationException("Boom");

        var passedError = await error.ValidateAsync(validator);
        var passedEmpty = await default(Result<Customer>).ValidateAsync(validator);

        Assert.That(passedError.GetError().Message, Is.EqualTo("Boom"));
        Assert.That(passedEmpty.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(validator.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task FailuresBecomeAnHttp400WithTheirMessages()
    {
        var result = new CustomerValidator().ValidateToResult(new Customer("", "nope"));

        var services = new ServiceCollection().AddLogging().AddProblemDetails();
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        await result.ToHttpResult().ExecuteAsync(context);

        context.Response.Body.Position = 0;
        var body = JsonDocument.Parse(await new StreamReader(context.Response.Body).ReadToEndAsync()).RootElement;
        Assert.That(context.Response.StatusCode, Is.EqualTo(400));
        var errors = body.GetProperty("errors");
        Assert.That(errors.GetProperty("Name").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "Name is required." }));
        Assert.That(errors.GetProperty("Email").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "Email is invalid." }));
    }

    [Test]
    public void ThrowingValidatorGivesTheExceptionAsTheError()
    {
        var failure = new InvalidOperationException("Rule blew up");
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).Must(_ => throw failure);

        var result = validator.ValidateToResult(Valid);

        Assert.That(result.GetError(), Is.SameAs(failure));
    }

    [Test]
    public void ThrowingValidatorGivesTheExceptionAsTheErrorOfAResultForm()
    {
        var failure = new InvalidOperationException("Rule blew up");
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).Must(_ => throw failure);
        Result<Customer> result = Valid;

        Assert.That(result.Validate(validator).GetError(), Is.SameAs(failure));
    }

    [Test]
    public void MemberNameIsThePropertyNameAndMissingForEntityRules()
    {
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();
        validator.RuleFor(c => c).Must(_ => false).WithMessage("Customer rejected.");

        var error = (ValidationErrors)validator.ValidateToResult(new Customer("", "ada@example.com")).GetError();

        Assert.That(error.Errors.Select(e => e.MemberName), Is.EqualTo(new string?[] { "Name", null }));
        Assert.That(error.ToDictionary().Keys, Is.EquivalentTo(new[] { "Name", "" }));
    }
}
