using FluentValidation;
using Fluent.Unions.Abstractions;
using Fluent.Unions.FluentValidation;
using NUnit.Framework;

namespace Fluent.Unions.FluentValidation.Tests.Scenarios;

public record UserProfile(string UserId, Option<string> Nickname, Option<int> Age);

public record UpdateUserProfile(Option<string> Nickname, Option<int> Age);

public record UpdateRequest(string? Nickname, int? Age);

public record InvalidInput(IReadOnlyList<string> Messages);

public record StorageFailure(Exception Exception);

public union UpdateError(InvalidInput, StorageFailure) : IFromException<UpdateError>
{
    public static UpdateError FromException(Exception exception) => new StorageFailure(exception);
}

[FluentResult<UpdateError>]
public readonly partial struct UpdateResult<T>;

public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfile>
{
    public int Lookups { get; private set; }

    public UpdateUserProfileValidator(HashSet<string> takenNicknames)
    {
        this.RuleForSome(p => p.Nickname)
            .NotEmpty().WithMessage("Nickname is required.")
            .MaximumLength(12).WithMessage("Nickname is too long.")
            .MustAsync(async (nickname, _) =>
            {
                Lookups++;
                await Task.Yield();
                return !takenNicknames.Contains(nickname);
            })
            .WithMessage("{PropertyName} is taken.");
        this.RuleForSome(p => p.Age).InclusiveBetween(13, 120).WithMessage("Age must be between 13 and 120.");
    }
}

public class ProfileService(UpdateUserProfileValidator validator)
{
    public async Task<Result<UserProfile>> UpdateAsync(UserProfile current, UpdateRequest request, CancellationToken cancellationToken = default)
    {
        Result<UpdateUserProfile> command = new UpdateUserProfile(request.Nickname.ToOption(), request.Age.ToOption());

        var validated = await command.ValidateAsync(validator, cancellationToken);

        return validated.Select(c => current with
        {
            Nickname = c.Nickname.Or(() => current.Nickname),
            Age = c.Age.Or(() => current.Age)
        });
    }

    public async Task<string> DescribeAsync(UpdateUserProfile command)
    {
        UpdateResult<UpdateUserProfile> typed = command;

        var validated = await typed.ValidateAsync(validator, failures => new InvalidInput(failures.Select(f => f.ErrorMessage).ToList()));

        return validated.Match(
            _ => "accepted",
            error => error switch
            {
                InvalidInput invalid => $"rejected: {string.Join(" ", invalid.Messages)}",
                StorageFailure => "unavailable"
            });
    }
}

/// <summary>A partial profile update where Option fields mean "not provided" and every failure comes back as one Result, not one exception.</summary>
public class ProfileUpdateScenario
{
    private static readonly UserProfile Current = new("u1", "ada", 36);

    private UpdateUserProfileValidator validator = null!;
    private ProfileService service = null!;

    [SetUp]
    public void SetUp()
    {
        validator = new UpdateUserProfileValidator(["taken"]);
        service = new ProfileService(validator);
    }

    [Test]
    public async Task Only_the_provided_fields_change()
    {
        var result = await service.UpdateAsync(Current, new UpdateRequest("countess", null));

        var profile = result.GetValue();
        Assert.That(profile.Nickname.GetValue(), Is.EqualTo("countess"));
        Assert.That(profile.Age.GetValue(), Is.EqualTo(36));
    }

    [Test]
    public async Task An_empty_request_is_valid_and_runs_no_rule()
    {
        var result = await service.UpdateAsync(Current, new UpdateRequest(null, null));

        Assert.That(result.GetValue(), Is.EqualTo(Current));
        Assert.That(validator.Lookups, Is.EqualTo(0));
    }

    [Test]
    public async Task Every_failure_is_reported_under_its_property_name()
    {
        var result = await service.UpdateAsync(Current, new UpdateRequest("", 7));

        var errors = (ValidationErrors)result.GetError();
        var byProperty = errors.ToDictionary();
        Assert.That(byProperty.Keys, Is.EquivalentTo(new[] { "Nickname", "Age" }));
        Assert.That(byProperty["Age"], Has.Length.EqualTo(1));
    }

    [Test]
    public async Task An_async_rule_rejects_a_taken_nickname()
    {
        var result = await service.UpdateAsync(Current, new UpdateRequest("taken", 40));

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.ToDictionary()["Nickname"], Is.EqualTo(new[] { "Nickname is taken." }));
        Assert.That(validator.Lookups, Is.EqualTo(1));
    }

    [Test]
    public async Task Errors_as_data_are_handled_with_an_exhaustive_switch()
    {
        var rejected = await service.DescribeAsync(new UpdateUserProfile("taken", 200));
        var accepted = await service.DescribeAsync(new UpdateUserProfile("grace", Option.None));
        var skipped = await service.DescribeAsync(new UpdateUserProfile(Option.None, Option.None));

        Assert.That(rejected, Is.EqualTo("rejected: Nickname is taken. Age must be between 13 and 120."));
        Assert.That(accepted, Is.EqualTo("accepted"));
        Assert.That(skipped, Is.EqualTo("accepted"));
    }
}
