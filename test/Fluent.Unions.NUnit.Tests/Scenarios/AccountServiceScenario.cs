using Fluent.Unions.NUnit;
using Fluent.Unions.Validation;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.NUnit.Tests.Scenarios;

internal sealed record Registration(string Owner, string Email, string? Nickname = null);

internal sealed record Account(string Owner, string Email, decimal Balance, Option<string> Nickname);

internal sealed class AccountNotFoundException(string email) : Exception($"Account {email} was not found");

internal sealed class InsufficientFundsException(decimal missing) : Exception($"Short by {missing:0.00}")
{
    public decimal Missing { get; } = missing;
}

internal sealed class AccountService
{
    private readonly Dictionary<string, Account> accounts = [];

    public Result<Account> Register(Registration registration) =>
        Result<Registration>.Ok(registration)
            .Validate(r => r.Owner.EnsureNotEmpty(), r => r.Email.EnsureMatchesEmail())
            .Ensure(r => !accounts.ContainsKey(r.Email), r => new InvalidOperationException($"{r.Email} is already registered"))
            .Select(r => Save(new Account(r.Owner, r.Email, 0m, r.Nickname.ToOption())));

    public Option<Account> Find(string email)
    {
        if (accounts.TryGetValue(email, out var account))
        {
            return account;
        }

        return Option.None;
    }

    public Result<Account> Deposit(string email, decimal amount) =>
        Load(email)
            .Ensure(_ => amount > 0, _ => new ValidationError("Amount must be positive.", nameof(amount)))
            .Select(a => Save(a with { Balance = a.Balance + amount }));

    public Result<Account> Withdraw(string email, decimal amount) =>
        Load(email)
            .Ensure(a => a.Balance >= amount, a => new InsufficientFundsException(amount - a.Balance))
            .Select(a => Save(a with { Balance = a.Balance - amount }));

    private Result<Account> Load(string email) => Find(email).ToResult(() => new AccountNotFoundException(email));

    private Account Save(Account account) => accounts[account.Email] = account;
}

/// <summary>A test suite for an account service whose outcomes are Result and Option values, asserted by state instead of caught exceptions.</summary>
public class AccountServiceScenario
{
    private AccountService service = null!;

    [SetUp]
    public void SetUp() => service = new AccountService();

    [Test]
    public void Registering_a_customer_keeps_the_nickname_as_an_option()
    {
        var withNickname = service.Register(new Registration("Ada", "ada@example.com", "countess"));
        var withoutNickname = service.Register(new Registration("Bob", "bob@example.com"));

        Assert.That(withNickname, IsResult.OkWith(Has.Property("Nickname").With.Matches(IsOption.SomeWith(Is.EqualTo("countess")))));
        Assert.That(withoutNickname, IsResult.OkWith(Has.Property("Balance").EqualTo(0m)));
        Assert.That(service.Find("bob@example.com"), IsOption.SomeWith(Has.Property("Owner").EqualTo("Bob")));
    }

    [Test]
    public void Registering_with_invalid_input_reports_every_problem_at_once()
    {
        var result = service.Register(new Registration("", "not-an-email"));

        Assert.That(result, IsResult.ErrorOfType<ValidationErrors>());
        Assert.That(result, IsResult.ErrorWith(Has.Property("Errors").Count.EqualTo(2).And.Message.Contains("Owner")));
        Assert.That(service.Find("not-an-email"), IsOption.None);
    }

    [Test]
    public void Registering_twice_is_a_conflict()
    {
        Assert.That(service.Register(new Registration("Ada", "ada@example.com")), IsResult.Ok);

        Assert.That(
            service.Register(new Registration("Ada", "ada@example.com")),
            IsResult.ErrorOfType<InvalidOperationException>());
    }

    [Test]
    public void Withdrawing_more_than_the_balance_fails_with_the_missing_amount()
    {
        service.Register(new Registration("Ada", "ada@example.com"));
        Assert.That(service.Deposit("ada@example.com", 50m), IsResult.OkWith(Has.Property("Balance").EqualTo(50m)));

        var result = service.Withdraw("ada@example.com", 75m);

        Assert.That(result, IsResult.ErrorOfType<InsufficientFundsException>());
        Assert.That(result, IsResult.ErrorWith(Has.Property("Missing").EqualTo(25m)));
        Assert.That(service.Find("ada@example.com"), IsOption.SomeWith(Has.Property("Balance").EqualTo(50m)));
    }

    [Test]
    public void Operations_on_an_unknown_account_are_not_found_errors()
    {
        Assert.That(
            service.Withdraw("ghost@example.com", 1m),
            IsResult.ErrorWith(Is.TypeOf<AccountNotFoundException>().And.Message.Contains("ghost@example.com")));
        Assert.That(service.Find("ghost@example.com"), IsOption.None);
    }

    [Test]
    public void Depositing_a_negative_amount_is_a_validation_error()
    {
        service.Register(new Registration("Ada", "ada@example.com"));

        Assert.That(
            service.Deposit("ada@example.com", -5m),
            IsResult.ErrorWith(Has.Property("MemberName").EqualTo("amount")));
    }
}
