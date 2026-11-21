using Fluent.Unions.Validation;
using NUnit.Framework;
using VerifyNUnit;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Verify.Tests.Scenarios;

public sealed record CheckoutRequest(string? Email, string? Sku, int Quantity, Option<string> GiftNote);

public sealed record Receipt(int OrderId, decimal Total, Option<string> GiftNote, AppResult<string> Payment);

public sealed class CheckoutService
{
    private static readonly Dictionary<string, decimal> Prices = new() { ["BOOK"] = 12.5m, ["MUG"] = 8m };

    private int _nextOrderId = 1;

    public Result<Receipt> Place(CheckoutRequest request) =>
        Result<CheckoutRequest>.Ok(request)
            .Validate(
                r => r.Email.EnsureMatchesEmail(name: "Email"),
                r => r.Sku.EnsureNotEmpty(name: "Sku"),
                r => r.Quantity.EnsureInRange(1, 10, name: "Quantity"),
                r => r.GiftNote.Match<string, Result>(note => note.EnsureShorterThanOrEqual(20, name: "GiftNote"), () => Result.Ok))
            .Chain<CheckoutRequest, Receipt>(Charge);

    private Result<Receipt> Charge(CheckoutRequest request)
    {
        if (!Prices.TryGetValue(request.Sku!, out var price))
            return new KeyNotFoundException($"Unknown product '{request.Sku}'.");

        var orderId = _nextOrderId++;
        var total = price * request.Quantity;
        AppResult<string> payment = total > 100
            ? new AppError(new Declined("insufficient funds"))
            : $"PAY-{orderId}";

        return new Receipt(orderId, total, request.GiftNote, payment);
    }
}

/// <summary>
/// Every outcome of a checkout is a value that a snapshot shows as Ok, Error, Some or None, with no custom serialization code.
/// </summary>
[TestFixture]
public class CheckoutScenario
{
    private static readonly CheckoutRequest Valid = new("ada@example.com", "BOOK", 2, Option.None);

    private static string Describe(Result<Receipt> outcome) => outcome.Match(
        receipt => receipt.Payment.Match(
            _ => $"order {receipt.OrderId} paid",
            error => error switch
            {
                NotFound notFound => $"order {receipt.OrderId} has no payment {notFound.Id}",
                Declined declined => $"order {receipt.OrderId} declined: {declined.Reason}",
                Unexpected unexpected => $"order {receipt.OrderId} failed: {unexpected.Exception.Message}"
            }),
        error => error switch
        {
            ValidationErrors validation => $"{validation.Errors.Count} invalid fields",
            KeyNotFoundException => "unknown product",
            _ => "unexpected"
        });

    [Test]
    public Task PlacedWithGiftNote() =>
        Verifier.Verify(new CheckoutService().Place(Valid with { GiftNote = "Happy birthday" }));

    [Test]
    public Task PlacedWithoutGiftNote() =>
        Verifier.Verify(new CheckoutService().Place(Valid));

    [Test]
    public Task InvalidRequest_ReportsEveryField() =>
        Verifier.Verify(new CheckoutService().Place(new CheckoutRequest("not-an-email", "BOOK", 0, "This note is far too long to print")));

    [Test]
    public Task UnknownProduct() =>
        Verifier.Verify(new CheckoutService().Place(Valid with { Sku = "TV" }));

    [Test]
    public Task DeclinedPayment_KeepsTheOrderAndTheDeclineAsData() =>
        Verifier.Verify(new CheckoutService().Place(Valid with { Quantity = 10 }));

    [Test]
    public void Describe_HandlesEveryOutcome()
    {
        var service = new CheckoutService();

        Assert.That(Describe(service.Place(Valid)), Is.EqualTo("order 1 paid"));
        Assert.That(Describe(service.Place(Valid with { Quantity = 10 })), Is.EqualTo("order 2 declined: insufficient funds"));
        Assert.That(Describe(service.Place(Valid with { Sku = "TV" })), Is.EqualTo("unknown product"));
        Assert.That(Describe(service.Place(Valid with { Email = null, Quantity = 11 })), Is.EqualTo("2 invalid fields"));
    }
}
