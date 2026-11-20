using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Tests.Samples;

public record Registered(Guid UserId, string Email);

public record AlreadyRegistered(Guid UserId);

public record PendingApproval(string Email, string Reason);

public union RegistrationOutcome(Registered, AlreadyRegistered, PendingApproval);

public sealed class UserDirectory(bool offline = false)
{
    public Dictionary<string, Guid> Users { get; } = [];

    public Guid? Find(string email) => Users.TryGetValue(email, out var id) ? id : null;

    public Guid Add(string email)
    {
        if (offline)
            throw new IOException("The user store is offline.");

        return Users[email] = Guid.NewGuid();
    }
}

public static class Registration
{
    public static Result<RegistrationOutcome> Register(UserDirectory users, string email) =>
        email.EnsureMatchesEmail().Chain(address => Result.Try(() => Decide(users, address)));

    private static RegistrationOutcome Decide(UserDirectory users, string email)
    {
        if (users.Find(email) is { } existing)
            return new AlreadyRegistered(existing);

        if (email.EndsWith("@competitor.example"))
            return new PendingApproval(email, "Addresses of this domain are reviewed by hand.");

        return new Registered(users.Add(email), email);
    }

    public static int ToStatusCode(Result<RegistrationOutcome> result) => result.Match(
        outcome => outcome switch
        {
            Registered => 201,
            AlreadyRegistered => 200,
            PendingApproval => 202
        },
        error => error is ValidationErrors or ValidationError ? 400 : 500);
}

public class RegistrationOutcomeSampleTests
{
    [Test]
    public void NewAddressIsRegistered()
    {
        var users = new UserDirectory();

        var result = Registration.Register(users, "ada@example.com");

        var registered = result.GetValue().As<Registered>().GetValue();
        Assert.That(registered.Email, Is.EqualTo("ada@example.com"));
        Assert.That(users.Find("ada@example.com"), Is.EqualTo(registered.UserId));
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(201));
    }

    [Test]
    public void KnownAddressIsNotAnError()
    {
        var users = new UserDirectory();
        var first = Registration.Register(users, "ada@example.com").GetValue().As<Registered>().GetValue();

        var again = Registration.Register(users, "ada@example.com");

        Assert.That(again.IsSuccess, Is.True);
        Assert.That(again.GetValue().As<AlreadyRegistered>().GetValue().UserId, Is.EqualTo(first.UserId));
        Assert.That(Registration.ToStatusCode(again), Is.EqualTo(200));
    }

    [Test]
    public void ReviewedAddressWaitsForApproval()
    {
        var users = new UserDirectory();

        var result = Registration.Register(users, "bob@competitor.example");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue().Is<PendingApproval>(), Is.True);
        Assert.That(users.Users, Is.Empty);
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(202));
    }

    [Test]
    public void InvalidAddressIsAnError()
    {
        var result = Registration.Register(new UserDirectory(), "not an email");

        Assert.That(result.IsError, Is.True);
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(400));
    }

    [Test]
    public void UnexpectedFailureStaysAnException()
    {
        var result = Registration.Register(new UserDirectory(offline: true), "ada@example.com");

        Assert.That(result.GetError(), Is.TypeOf<IOException>());
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(500));
    }
}
