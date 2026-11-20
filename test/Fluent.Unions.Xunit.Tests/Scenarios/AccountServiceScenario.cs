using Fluent.Unions.Validation;
using Fluent.Unions.Xunit;
using Xunit;

namespace Fluent.Unions.Xunit.Tests.Scenarios;

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
    private readonly AccountService service = new();

    [Fact]
    public void Registering_a_customer_keeps_the_nickname_as_an_option()
    {
        var withNickname = ResultAssert.Ok(service.Register(new Registration("Ada", "ada@example.com", "countess")));
        var withoutNickname = ResultAssert.Ok(service.Register(new Registration("Bob", "bob@example.com")));

        Assert.Equal("countess", OptionAssert.Some(withNickname.Nickname));
        Assert.Equal(0m, withoutNickname.Balance);
        OptionAssert.None(withoutNickname.Nickname);
        Assert.Equal("Bob", OptionAssert.Some(service.Find("bob@example.com")).Owner);
    }

    [Fact]
    public void Registering_with_invalid_input_reports_every_problem_at_once()
    {
        var error = ResultAssert.Error<ValidationErrors>(service.Register(new Registration("", "not-an-email")));

        Assert.Equal(2, error.Errors.Count);
        Assert.Contains("Owner", error.Message);
        Assert.Contains("Email", error.Message);
        OptionAssert.None(service.Find("not-an-email"));
    }

    [Fact]
    public void Registering_twice_is_a_conflict()
    {
        ResultAssert.Ok(service.Register(new Registration("Ada", "ada@example.com")));

        var error = ResultAssert.Error<InvalidOperationException>(service.Register(new Registration("Ada", "ada@example.com")));

        Assert.EndsWith("already registered", error.Message);
    }

    [Fact]
    public void Withdrawing_more_than_the_balance_fails_with_the_missing_amount()
    {
        service.Register(new Registration("Ada", "ada@example.com"));
        Assert.Equal(50m, ResultAssert.Ok(service.Deposit("ada@example.com", 50m)).Balance);

        var error = ResultAssert.Error<InsufficientFundsException>(service.Withdraw("ada@example.com", 75m));

        Assert.Equal(25m, error.Missing);
        Assert.Equal(50m, OptionAssert.Some(service.Find("ada@example.com")).Balance);
    }

    [Fact]
    public void Operations_on_an_unknown_account_are_not_found_errors()
    {
        var error = ResultAssert.Error<AccountNotFoundException>(service.Withdraw("ghost@example.com", 1m));

        Assert.Contains("ghost@example.com", error.Message);
        OptionAssert.None(service.Find("ghost@example.com"));
    }

    [Fact]
    public void Depositing_a_negative_amount_is_a_validation_error()
    {
        service.Register(new Registration("Ada", "ada@example.com"));

        var error = ResultAssert.Error<ValidationError>(service.Deposit("ada@example.com", -5m));

        Assert.Equal("amount", error.MemberName);
    }
}
