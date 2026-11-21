using System.Diagnostics;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Scenarios.Tests.BankTransfer;

public record Account(string Id, decimal Balance, decimal DailyLimit);

public record Receipt(string From, string To, decimal Amount, decimal BalanceAfter);

public record AccountNotFound(string Id);

public record InsufficientFunds(decimal Missing);

public record DailyLimitExceeded(decimal Limit);

public record SameAccount;

public record Unexpected(Exception Exception);

public union TransferError(AccountNotFound, InsufficientFunds, DailyLimitExceeded, SameAccount, Unexpected)
    : IFromException<TransferError>
{
    public static TransferError FromException(Exception exception) => new Unexpected(exception);
}

public union ClientError(string, Unexpected) : IFromException<ClientError>
{
    public static ClientError FromException(Exception exception) => new Unexpected(exception);
}

[FluentResult<TransferError>]
public readonly partial struct TransferResult<T>;

[FluentResult<ClientError>]
public readonly partial struct ClientResult<T>;

/// <summary>
/// Moves money between accounts. Shows errors as data: every business failure is a record in a closed union, and
/// the compiler rejects a <c>switch</c> that forgets one when a new failure is added.
/// </summary>
public sealed class Bank(IEnumerable<Account> accounts, Action<Receipt> notify)
{
    private readonly Dictionary<string, Account> accounts = accounts.ToDictionary(account => account.Id);

    public int Processed { get; private set; }

    public decimal BalanceOf(string id) => accounts[id].Balance;

    public TransferResult<Receipt> Transfer(string fromId, string toId, decimal amount) =>
        Find(fromId)
            .Chain(source => Find(toId).Select(target => (Source: source, Target: target)))
            .Ensure(pair => pair.Source.Id != pair.Target.Id, _ => new SameAccount())
            .Ensure(pair => pair.Source.Balance >= amount, pair => new InsufficientFunds(amount - pair.Source.Balance))
            .Ensure(pair => amount <= pair.Source.DailyLimit, pair => new DailyLimitExceeded(pair.Source.DailyLimit))
            .Select(pair => new Receipt(pair.Source.Id, pair.Target.Id, amount, pair.Source.Balance - amount))
            .Do(notify)
            .Do(Book)
            .Finally(() => Processed++);

    public TransferResult<Receipt> TransferUpToLimit(string fromId, string toId, decimal amount) =>
        Transfer(fromId, toId, amount)
            .Catch(error => error is DailyLimitExceeded exceeded ? Transfer(fromId, toId, exceeded.Limit) : error);

    public TransferResult<Receipt> Execute(string fromId, string toId, decimal amount)
    {
        var result = Transfer(fromId, toId, amount);
        Activity.Current.RecordResult(result);
        return result;
    }

    private TransferResult<Account> Find(string id) =>
        accounts.GetValueOrNone(id).Match(
            account => (TransferResult<Account>)account,
            () => (TransferError)new AccountNotFound(id));

    private void Book(Receipt receipt)
    {
        accounts[receipt.From] = accounts[receipt.From] with { Balance = receipt.BalanceAfter };
        accounts[receipt.To] = accounts[receipt.To] with { Balance = accounts[receipt.To].Balance + receipt.Amount };
    }

    public static int ToStatusCode(TransferResult<Receipt> result) => result.Match(
        _ => 200,
        error => error switch
        {
            AccountNotFound => 404,
            InsufficientFunds => 422,
            DailyLimitExceeded => 422,
            SameAccount => 400,
            Unexpected => 500
        });

    public static ClientResult<Receipt> ForClient(TransferResult<Receipt> result) =>
        result.Match(receipt => (ClientResult<Receipt>)receipt, error => (ClientError)(error switch
        {
            AccountNotFound missing => $"Account {missing.Id} does not exist.",
            InsufficientFunds funds => FormattableString.Invariant($"{funds.Missing:0.00} is missing on the account."),
            DailyLimitExceeded limit => FormattableString.Invariant($"The daily limit is {limit.Limit:0.00}."),
            SameAccount => "Choose two different accounts.",
            Unexpected => "Try again later."
        }));
}

public class BankTransferScenarioTests
{
    private readonly List<Receipt> notified = [];
    private Bank bank = null!;

    [SetUp]
    public void SetUp()
    {
        notified.Clear();
        bank = new Bank([new Account("A", 500m, 200m), new Account("B", 50m, 200m)], notified.Add);
    }

    [Test]
    public void ATransferMovesTheMoneyAndNotifies()
    {
        var result = bank.Transfer("A", "B", 150m);

        Assert.That(result.GetValue(), Is.EqualTo(new Receipt("A", "B", 150m, 350m)));
        Assert.That(bank.BalanceOf("B"), Is.EqualTo(200m));
        Assert.That(notified, Has.Count.EqualTo(1));
    }

    [Test]
    public void EachFailureIsItsOwnCaseAndNothingIsBooked()
    {
        Assert.That(bank.Transfer("A", "Z", 10m).GetError().Value, Is.EqualTo(new AccountNotFound("Z")));
        Assert.That(bank.Transfer("A", "A", 10m).GetError().Value, Is.TypeOf<SameAccount>());
        Assert.That(bank.Transfer("B", "A", 80m).GetError().Value, Is.EqualTo(new InsufficientFunds(30m)));
        Assert.That(bank.Transfer("A", "B", 250m).GetError().Value, Is.EqualTo(new DailyLimitExceeded(200m)));

        Assert.That(bank.BalanceOf("A"), Is.EqualTo(500m));
        Assert.That(notified, Is.Empty);
        Assert.That(bank.Processed, Is.EqualTo(4));
    }

    [Test]
    public void StatusCodesComeFromAnExhaustiveSwitch()
    {
        var codes = new[]
        {
            bank.Transfer("A", "B", 1m),
            bank.Transfer("A", "Z", 1m),
            bank.Transfer("A", "A", 1m),
            bank.Transfer("B", "A", 90m)
        }.Select(Bank.ToStatusCode);

        Assert.That(codes, Is.EqualTo(new[] { 200, 404, 400, 422 }));
    }

    [Test]
    public void ACatchOnOneCaseRetriesWithinTheLimit()
    {
        var result = bank.TransferUpToLimit("A", "B", 250m);

        Assert.That(result.GetValue().Amount, Is.EqualTo(200m));
        Assert.That(bank.BalanceOf("A"), Is.EqualTo(300m));
        Assert.That(bank.TransferUpToLimit("B", "A", 500m).GetError().Value, Is.TypeOf<InsufficientFunds>());
    }

    [Test]
    public void AThrowingNotifierBecomesTheUnexpectedCaseAndNothingIsBooked()
    {
        var failing = new Bank(
            [new Account("A", 500m, 200m), new Account("B", 0m, 200m)],
            _ => throw new IOException("queue down"));

        var error = failing.Transfer("A", "B", 10m).GetError();

        Assert.That(error.Value, Is.TypeOf<Unexpected>());
        Assert.That(((Unexpected)error.Value!).Exception, Is.TypeOf<IOException>());
        Assert.That(failing.BalanceOf("A"), Is.EqualTo(500m));
    }

    [Test]
    public void ErrorsAreTranslatedAtTheLayerBoundary()
    {
        var message = Bank.ForClient(bank.Transfer("A", "B", 250m)).GetError();
        var passed = Bank.ForClient(bank.Transfer("A", "B", 5m));

        Assert.That(message.Value, Is.EqualTo("The daily limit is 200.00."));
        Assert.That(passed.IsSuccess, Is.True);
    }

    [Test]
    public void TheOutcomeIsRecordedOnTheTraceSpan()
    {
        using var success = new Activity("transfer").Start();
        bank.Execute("A", "B", 10m);
        var succeeded = success.Status;
        success.Stop();

        using var failure = new Activity("transfer").Start();
        bank.Execute("A", "Z", 10m);

        Assert.That(succeeded, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(failure.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(failure.Events.Count(), Is.EqualTo(1));
    }
}
