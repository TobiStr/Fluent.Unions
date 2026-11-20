using Fluent.Unions.Validation;
using Assert = TUnit.Assertions.Assert;
using SetUpAttribute = NUnit.Framework.SetUpAttribute;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace Fluent.Unions.TUnit.Tests.Scenarios;

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
    public async Task Registering_a_customer_keeps_the_nickname_as_an_option()
    {
        var withNickname = await Assert.That(service.Register(new Registration("Ada", "ada@example.com", "countess"))).IsOk();
        var withoutNickname = await Assert.That(service.Register(new Registration("Bob", "bob@example.com"))).IsOk();

        await Assert.That(await Assert.That(withNickname.Nickname).IsSome()).IsEqualTo("countess");
        await Assert.That(withoutNickname.Balance).IsEqualTo(0m);
        await Assert.That(withoutNickname.Nickname).IsNone();
        await Assert.That((await Assert.That(service.Find("bob@example.com")).IsSome()).Owner).IsEqualTo("Bob");
    }

    [Test]
    public async Task Registering_with_invalid_input_reports_every_problem_at_once()
    {
        Result result = service.Register(new Registration("", "not-an-email"));

        var error = await Assert.That(result).IsError<ValidationErrors>();

        await Assert.That(error.Errors.Count).IsEqualTo(2);
        await Assert.That(error.Message).Contains("Owner").And.Contains("Email");
        await Assert.That(service.Find("not-an-email")).IsNone();
    }

    [Test]
    public async Task Registering_twice_is_a_conflict()
    {
        await Assert.That(service.Register(new Registration("Ada", "ada@example.com"))).IsOk();

        Result second = service.Register(new Registration("Ada", "ada@example.com"));

        var error = await Assert.That(second).IsError<InvalidOperationException>();
        await Assert.That(error.Message).EndsWith("already registered");
    }

    [Test]
    public async Task Withdrawing_more_than_the_balance_fails_with_the_missing_amount()
    {
        service.Register(new Registration("Ada", "ada@example.com"));
        var funded = await Assert.That(service.Deposit("ada@example.com", 50m)).IsOk();
        await Assert.That(funded.Balance).IsEqualTo(50m);

        Result withdrawal = service.Withdraw("ada@example.com", 75m);

        var error = await Assert.That(withdrawal).IsError<InsufficientFundsException>();
        await Assert.That(error.Missing).IsEqualTo(25m);
        await Assert.That((await Assert.That(service.Find("ada@example.com")).IsSome()).Balance).IsEqualTo(50m);
    }

    [Test]
    public async Task Operations_on_an_unknown_account_are_not_found_errors()
    {
        Result withdrawal = service.Withdraw("ghost@example.com", 1m);

        var error = await Assert.That(withdrawal).IsError<AccountNotFoundException>();

        await Assert.That(error.Message).Contains("ghost@example.com");
        await Assert.That(service.Find("ghost@example.com")).IsNone();
    }

    [Test]
    public async Task Depositing_a_negative_amount_is_a_validation_error()
    {
        service.Register(new Registration("Ada", "ada@example.com"));
        Result deposit = service.Deposit("ada@example.com", -5m);

        var error = await Assert.That(deposit).IsError<ValidationError>();

        await Assert.That(error.MemberName).IsEqualTo("amount");
    }
}
