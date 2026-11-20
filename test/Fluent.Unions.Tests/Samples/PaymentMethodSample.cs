using System.Text.Json;
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;
using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Tests.Samples;

public record Card(string Number, int ExpiryMonth);

public record BankTransfer(string Iban);

public record Voucher(string Code, decimal Value);

[JsonConverter(typeof(UnionJsonConverter))]
public union PaymentMethod(Card, BankTransfer, Voucher);

public record PaymentRequest(decimal Subtotal, PaymentMethod Method);

public static class PaymentApi
{
    public static Result<PaymentRequest> Read(string json) =>
        Result.Try(() => JsonSerializer.Deserialize<PaymentRequest>(json)!);

    public static Result<PaymentMethod> Check(PaymentMethod method) => method switch
    {
        Card card => Result.From(card)
            .Validate(
                c => c.Number.EnsureMatches(@"^\d{16}$"),
                c => c.ExpiryMonth.EnsureInRange(1, 12))
            .Select(_ => method),
        BankTransfer transfer => Result.From(transfer)
            .Validate(t => t.Iban.EnsureMatches(@"^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$"))
            .Select(_ => method),
        Voucher voucher => Result.From(voucher)
            .Validate(
                v => v.Code.EnsureNotWhiteSpace(),
                v => v.Value.EnsurePositive())
            .Select(_ => method),
        null => new ValidationError("A payment method is required.", "Method")
    };

    public static decimal AmountDue(PaymentMethod method, decimal subtotal) => method switch
    {
        Card => decimal.Round(subtotal * 1.02m, 2),
        BankTransfer => subtotal,
        Voucher voucher => decimal.Max(0, subtotal - voucher.Value)
    };

    public static Result<decimal> Price(string json) =>
        Read(json).Chain(request => Check(request.Method).Select(method => AmountDue(method, request.Subtotal)));
}

public class PaymentMethodSampleTests
{
    private const string ValidCard = """{ "$type": "Card", "$value": { "Number": "4242424242424242", "ExpiryMonth": 12 } }""";
    private const string ValidTransfer = """{ "$type": "BankTransfer", "$value": { "Iban": "DE89370400440532013000" } }""";
    private const string ValidVoucher = """{ "$type": "Voucher", "$value": { "Code": "SAVE30", "Value": 30 } }""";
    private const string BigVoucher = """{ "$type": "Voucher", "$value": { "Code": "SAVE500", "Value": 500 } }""";

    private static string Request(string method) => $$"""{ "Subtotal": 100, "Method": {{method}} }""";

    [TestCase(ValidCard, 102.00)]
    [TestCase(ValidTransfer, 100.00)]
    [TestCase(ValidVoucher, 70.00)]
    [TestCase(BigVoucher, 0.00)]
    public void EachCaseIsReadAndPriced(string method, double expected)
    {
        var price = PaymentApi.Price(Request(method));

        Assert.That(price.GetValue(), Is.EqualTo((decimal)expected));
    }

    [Test]
    public void TheCaseNameSelectsTheType()
    {
        var request = PaymentApi.Read(Request(ValidVoucher)).GetValue();

        Assert.That(request.Method.Is<Voucher>(), Is.True);
        Assert.That(request.Method.As<Voucher>().GetValue(), Is.EqualTo(new Voucher("SAVE30", 30m)));
    }

    [Test]
    public void EveryInvalidFieldOfTheCaseIsReported()
    {
        var price = PaymentApi.Price(Request("""{ "$type": "Card", "$value": { "Number": "4242", "ExpiryMonth": 13 } }"""));

        var errors = ((ValidationErrors)price.GetError()).Errors;
        Assert.That(errors.Select(e => e.MemberName), Is.EqualTo(new[] { "Number", "ExpiryMonth" }));
    }

    [Test]
    public void EachCaseHasItsOwnRules()
    {
        var transfer = PaymentApi.Price(Request("""{ "$type": "BankTransfer", "$value": { "Iban": "not an iban" } }"""));
        var voucher = PaymentApi.Price(Request("""{ "$type": "Voucher", "$value": { "Code": " ", "Value": 0 } }"""));

        Assert.That(((ValidationErrors)transfer.GetError()).Errors.Select(e => e.MemberName), Is.EqualTo(new[] { "Iban" }));
        Assert.That(((ValidationErrors)voucher.GetError()).Errors.Select(e => e.MemberName), Is.EqualTo(new[] { "Code", "Value" }));
    }

    [Test]
    public void UnknownCaseIsAJsonError()
    {
        var price = PaymentApi.Price(Request("""{ "$type": "Cash", "$value": { } }"""));

        Assert.That(price.GetError(), Is.TypeOf<JsonException>());
        Assert.That(price.GetError().Message, Does.Contain("'Cash' is not a case of PaymentMethod."));
    }

    [Test]
    public void MissingMethodIsAValidationError()
    {
        var price = PaymentApi.Price("""{ "Subtotal": 100 }""");

        var error = (ValidationError)price.GetError();
        Assert.That(error.MemberName, Is.EqualTo("Method"));
        Assert.That(error.Message, Is.EqualTo("A payment method is required."));
    }

    [Test]
    public void MalformedJsonStaysAnException()
    {
        Assert.That(PaymentApi.Price("{ nope").GetError(), Is.InstanceOf<JsonException>());
    }
}
