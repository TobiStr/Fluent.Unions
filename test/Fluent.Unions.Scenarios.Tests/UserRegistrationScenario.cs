using System.ComponentModel.DataAnnotations;
using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Scenarios.Tests.UserRegistration;

public class SignUpForm
{
    [Required, EmailAddress]
    public string? Email { get; init; }

    [Required, StringLength(40, MinimumLength = 2)]
    public string? DisplayName { get; init; }

    public string? Password { get; init; }

    public DateTimeOffset BirthDate { get; init; }
}

public record User(string Email, string DisplayName);

public class EmailAlreadyRegisteredException(string email) : Exception($"{email} is already registered.");

public class DuplicateKeyException : Exception;

public interface IUserStore
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<User> CreateAsync(User user, CancellationToken cancellationToken);
}

/// <summary>
/// Registers a user. Shows an async pipeline with several outcomes: annotations and rules report every problem
/// at once, a failing store is an error value instead of an exception, and the caller handles each outcome in
/// one <c>switch</c>.
/// </summary>
public sealed class Registration(IUserStore store, TimeProvider clock, Action<string> log)
{
    public Task<Result<User>> RegisterAsync(SignUpForm form, CancellationToken cancellationToken = default) =>
        Result.From(form)
            .Validate(
                f => Result.From(f).ValidateAnnotations(),
                f => f.Password.EnsureLongerThanOrEqual(12),
                f => f.Password.EnsureIncludesDigit(),
                f => f.BirthDate.EnsureInPast(clock))
            .EnsureAwait(
                async f => !await store.EmailExistsAsync(f.Email!, cancellationToken),
                f => new EmailAlreadyRegisteredException(f.Email!))
            .ChainAwait(f => Result.TryAsync(() => store.CreateAsync(new User(f.Email!, f.DisplayName!), cancellationToken)))
            .Catch((DuplicateKeyException _) => new EmailAlreadyRegisteredException(form.Email!))
            .Do(
                user => log($"Registered {user.Email}"),
                error => log($"Registration failed: {error.GetType().Name}"));

    public static int ToStatusCode(Result<User> result) => result switch
    {
        User => 201,
        ValidationErrors => 400,
        EmailAlreadyRegisteredException => 409,
        Exception => 503
    };
}

public class UserRegistrationScenarioTests
{
    private static readonly ManualTimeProvider Clock = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

    private readonly List<string> log = [];

    [SetUp]
    public void SetUp() => log.Clear();

    private static SignUpForm ValidForm() => new()
    {
        Email = "ada@example.com",
        DisplayName = "Ada",
        Password = "correct horse 9",
        BirthDate = new DateTimeOffset(1990, 5, 1, 0, 0, 0, TimeSpan.Zero)
    };

    private Registration Create(FakeStore store) => new(store, Clock, log.Add);

    [Test]
    public async Task AValidFormCreatesTheUser()
    {
        var store = new FakeStore();

        var result = await Create(store).RegisterAsync(ValidForm());

        Assert.That(result.GetValue(), Is.EqualTo(new User("ada@example.com", "Ada")));
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(201));
        Assert.That(store.Users, Has.Count.EqualTo(1));
        Assert.That(log, Is.EqualTo(new[] { "Registered ada@example.com" }));
    }

    [Test]
    public async Task EveryInvalidFieldIsReportedAtOnce()
    {
        var form = new SignUpForm { Email = "nope", DisplayName = "A", Password = "short", BirthDate = Clock.GetUtcNow().AddDays(1) };
        var store = new FakeStore();

        var result = await Create(store).RegisterAsync(form);

        var errors = ((ValidationErrors)result.GetError()).ToDictionary();
        Assert.That(errors.Keys, Is.EquivalentTo(new[] { "Email", "DisplayName", "Password", "BirthDate" }));
        Assert.That(errors["Password"], Has.Length.EqualTo(2));
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(400));
        Assert.That(store.Users, Is.Empty);
        Assert.That(log, Is.EqualTo(new[] { "Registration failed: ValidationErrors" }));
    }

    [Test]
    public async Task ATakenEmailIsAConflict()
    {
        var store = new FakeStore { Users = { ["ada@example.com"] = new User("ada@example.com", "Other") } };

        var result = await Create(store).RegisterAsync(ValidForm());

        Assert.That(result.GetError(), Is.TypeOf<EmailAlreadyRegisteredException>());
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(409));
    }

    [Test]
    public async Task ARaceAgainstAnotherRegistrationIsStillAConflict()
    {
        var store = new FakeStore { ReportsEmailsAsFree = true, Users = { ["ada@example.com"] = new User("ada@example.com", "Other") } };

        var result = await Create(store).RegisterAsync(ValidForm());

        Assert.That(result.GetError().Message, Is.EqualTo("ada@example.com is already registered."));
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(409));
    }

    [Test]
    public async Task ADatabaseOutageIsAnErrorValueNotAnException()
    {
        var result = await Create(new FakeStore { Down = true }).RegisterAsync(ValidForm());

        Assert.That(result.GetError(), Is.TypeOf<IOException>());
        Assert.That(Registration.ToStatusCode(result), Is.EqualTo(503));
    }

    [Test]
    public void CancellationStillThrows()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() => Create(new FakeStore()).RegisterAsync(ValidForm(), cancellation.Token));
    }

    private sealed class FakeStore : IUserStore
    {
        public Dictionary<string, User> Users { get; } = [];

        public bool ReportsEmailsAsFree { get; init; }

        public bool Down { get; init; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(!ReportsEmailsAsFree && Users.ContainsKey(email));
        }

        public Task<User> CreateAsync(User user, CancellationToken cancellationToken)
        {
            if (Down) throw new IOException("The database is down.");
            if (!Users.TryAdd(user.Email, user)) throw new DuplicateKeyException();
            return Task.FromResult(user);
        }
    }
}
