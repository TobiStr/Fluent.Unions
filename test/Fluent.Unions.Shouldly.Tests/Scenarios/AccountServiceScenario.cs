using Fluent.Unions.Shouldly;
using Fluent.Unions.Validation;
using NUnit.Framework;
using Shouldly;

namespace Fluent.Unions.Shouldly.Tests.Scenarios;

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
        var withNickname = service.Register(new Registration("Ada", "ada@example.com", "countess")).ShouldBeOk();
        var withoutNickname = service.Register(new Registration("Bob", "bob@example.com")).ShouldBeOk();

        withNickname.Nickname.ShouldBeSome().ShouldBe("countess");
        withoutNickname.Balance.ShouldBe(0m);
        withoutNickname.Nickname.ShouldBeNone();
        service.Find("bob@example.com").ShouldBeSome().Owner.ShouldBe("Bob");
    }

    [Test]
    public void Registering_with_invalid_input_reports_every_problem_at_once()
    {
        var error = service.Register(new Registration("", "not-an-email")).ShouldBeError().ShouldBeOfType<ValidationErrors>();

        error.Errors.Count.ShouldBe(2);
        error.Message.ShouldContain("Owner");
        error.Message.ShouldContain("Email");
        service.Find("not-an-email").ShouldBeNone();
    }

    [Test]
    public void Registering_twice_is_a_conflict()
    {
        service.Register(new Registration("Ada", "ada@example.com")).ShouldBeOk();

        var error = service.Register(new Registration("Ada", "ada@example.com")).ShouldBeError();

        error.ShouldBeOfType<InvalidOperationException>().Message.ShouldEndWith("already registered");
    }

    [Test]
    public void Withdrawing_more_than_the_balance_fails_with_the_missing_amount()
    {
        service.Register(new Registration("Ada", "ada@example.com"));
        service.Deposit("ada@example.com", 50m).ShouldBeOk().Balance.ShouldBe(50m);

        var error = service.Withdraw("ada@example.com", 75m).ShouldBeError().ShouldBeOfType<InsufficientFundsException>();

        error.Missing.ShouldBe(25m);
        service.Find("ada@example.com").ShouldBeSome().Balance.ShouldBe(50m);
    }

    [Test]
    public void Operations_on_an_unknown_account_are_not_found_errors()
    {
        var error = service.Withdraw("ghost@example.com", 1m).ShouldBeError();

        error.ShouldBeOfType<AccountNotFoundException>().Message.ShouldContain("ghost@example.com");
        service.Find("ghost@example.com").ShouldBeNone();
    }

    [Test]
    public void Depositing_a_negative_amount_is_a_validation_error()
    {
        service.Register(new Registration("Ada", "ada@example.com"));

        var error = service.Deposit("ada@example.com", -5m).ShouldBeError().ShouldBeOfType<ValidationError>();

        error.MemberName.ShouldBe("amount");
    }
}
