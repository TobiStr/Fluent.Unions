using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Tests.Samples;

public record CheckoutRequest(Guid CustomerId, string Sku, int Quantity);

public record Customer(Guid Id, string Name);

public record Product(string Sku, decimal Price, int Stock);

public record Receipt(string Customer, string Sku, decimal Total, string PaymentId);

public class PaymentDeclinedException(string message) : Exception(message);

public sealed class Shop(IReadOnlyDictionary<Guid, Customer> customers, IReadOnlyDictionary<string, Product> products, decimal cardLimit)
{
    public List<Receipt> Orders { get; } = [];

    public Customer? FindCustomer(Guid id) => customers.GetValueOrDefault(id);

    public Product? FindProduct(string sku) => products.GetValueOrDefault(sku);

    public string Charge(Customer customer, decimal amount) =>
        amount <= cardLimit ? $"PAY-{Orders.Count + 1}" : throw new PaymentDeclinedException($"The card of {customer.Name} was declined.");

    public void Save(Receipt receipt) => Orders.Add(receipt);
}

public static class TraditionalCheckout
{
    public static Receipt? PlaceOrder(Shop shop, CheckoutRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            error = "Sku must not be empty or white space.";
            return null;
        }

        if (request.Quantity is < 1 or > 10)
        {
            error = "Quantity must be between 1 and 10.";
            return null;
        }

        var customer = shop.FindCustomer(request.CustomerId);
        if (customer is null)
        {
            error = $"No customer {request.CustomerId}.";
            return null;
        }

        var product = shop.FindProduct(request.Sku);
        if (product is null)
        {
            error = $"No product {request.Sku}.";
            return null;
        }

        if (product.Stock < request.Quantity)
        {
            error = $"Only {product.Stock} left of {product.Sku}.";
            return null;
        }

        var total = product.Price * request.Quantity;
        string paymentId;
        try
        {
            paymentId = shop.Charge(customer, total);
        }
        catch (PaymentDeclinedException exception)
        {
            error = exception.Message;
            return null;
        }

        var receipt = new Receipt(customer.Name, product.Sku, total, paymentId);
        shop.Save(receipt);
        error = null;
        return receipt;
    }

    public static int ToStatusCode(Shop shop, CheckoutRequest request)
    {
        var receipt = PlaceOrder(shop, request, out var error);
        if (receipt is not null) return 201;
        if (error!.StartsWith("No ")) return 404;
        if (error.StartsWith("Only ")) return 409;
        if (error.Contains("declined")) return 402;
        return 400;
    }
}

public static class ResultCheckout
{
    public static Result<Receipt> PlaceOrder(Shop shop, CheckoutRequest request) =>
        Result<CheckoutRequest>.Ok(request)
            .Validate(
                r => r.Sku.EnsureNotWhiteSpace(),
                r => r.Quantity.EnsureInRange(1, 10))
            .Chain(r => FindCustomer(shop, r.CustomerId)).Out(out var customer)
            .Chain(_ => FindProduct(shop, request.Sku))
            .Ensure(p => p.Stock >= request.Quantity, p => new InvalidOperationException($"Only {p.Stock} left of {p.Sku}."))
            .Select(p => (Product: p, Total: p.Price * request.Quantity))
            .Select(x => new Receipt(customer.Value.Name, x.Product.Sku, x.Total, shop.Charge(customer, x.Total)))
            .Do(shop.Save);

    private static Result<Customer> FindCustomer(Shop shop, Guid id) =>
        shop.FindCustomer(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));

    private static Result<Product> FindProduct(Shop shop, string sku) =>
        shop.FindProduct(sku).ToResult(() => new KeyNotFoundException($"No product {sku}."));
}

public record HttpError(int StatusCode, string Message);

public record HttpResponse(int StatusCode, object? Body);

public static class HttpResponseExtensions
{
    public static HttpResponse ToHttpResponse<T>(this Result<T> result, int successStatusCode = 200) => result.Match(
        value => new HttpResponse(successStatusCode, value),
        error => ToHttpResponse(error.ToHttpError()),
        () => new HttpResponse(204, null));

    public static HttpError ToHttpError(this Exception error) => error switch
    {
        AggregateException validation => new(400, string.Join(" ", validation.InnerExceptions.Select(e => e.Message))),
        KeyNotFoundException => new(404, error.Message),
        InvalidOperationException => new(409, error.Message),
        PaymentDeclinedException => new(402, error.Message),
        _ => new(500, "An unexpected error occurred.")
    };

    private static HttpResponse ToHttpResponse(HttpError error) => new(error.StatusCode, error);
}

public class CheckoutSampleTests
{
    private static readonly Customer Ada = new(Guid.NewGuid(), "Ada");

    private static Shop NewShop() => new(
        new Dictionary<Guid, Customer> { [Ada.Id] = Ada },
        new Dictionary<string, Product> { ["TEA"] = new("TEA", 4.5m, 3) },
        cardLimit: 10m);

    private static IEnumerable<TestCaseData> Requests()
    {
        yield return new TestCaseData(new CheckoutRequest(Ada.Id, "TEA", 2), 201).SetName("Order is placed");
        yield return new TestCaseData(new CheckoutRequest(Ada.Id, " ", 0), 400).SetName("Invalid request");
        yield return new TestCaseData(new CheckoutRequest(Guid.NewGuid(), "TEA", 1), 404).SetName("Unknown customer");
        yield return new TestCaseData(new CheckoutRequest(Ada.Id, "COFFEE", 1), 404).SetName("Unknown product");
        yield return new TestCaseData(new CheckoutRequest(Ada.Id, "TEA", 5), 409).SetName("Out of stock");
        yield return new TestCaseData(new CheckoutRequest(Ada.Id, "TEA", 3), 402).SetName("Payment declined");
    }

    [TestCaseSource(nameof(Requests))]
    public void BothVersionsAnswerTheSame(CheckoutRequest request, int expected)
    {
        Assert.That(TraditionalCheckout.ToStatusCode(NewShop(), request), Is.EqualTo(expected));
        Assert.That(ResultCheckout.PlaceOrder(NewShop(), request).ToHttpResponse(201).StatusCode, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(Requests))]
    public void MatchingDirectlyGivesTheSameStatus(CheckoutRequest request, int expected)
    {
        var shop = NewShop();

        int status = ResultCheckout.PlaceOrder(shop, request) switch
        {
            Receipt => 201,
            AggregateException => 400,
            KeyNotFoundException => 404,
            InvalidOperationException => 409,
            PaymentDeclinedException => 402,
            Exception => 500
        };

        Assert.That(status, Is.EqualTo(expected));
    }

    [Test]
    public void ErrorsBecomeHttpErrorsInOnePlace()
    {
        var invalid = ResultCheckout.PlaceOrder(NewShop(), new CheckoutRequest(Ada.Id, " ", 0)).ToHttpResponse(201);
        var unexpected = new IOException("Disk C:\\secrets is full.").ToHttpError();
        var empty = default(Result<Receipt>).ToHttpResponse(201);

        Assert.That(invalid.Body, Is.EqualTo(new HttpError(400, "Sku must not be empty or white space. Quantity must be between 1 and 10.")));
        Assert.That(unexpected, Is.EqualTo(new HttpError(500, "An unexpected error occurred.")));
        Assert.That(empty, Is.EqualTo(new HttpResponse(204, null)));
    }

    [Test]
    public void PlacedOrderIsSaved()
    {
        var shop = NewShop();

        var receipt = ResultCheckout.PlaceOrder(shop, new CheckoutRequest(Ada.Id, "TEA", 2));

        Assert.That(receipt.GetOk(), Is.EqualTo(new Receipt("Ada", "TEA", 9m, "PAY-1")));
        Assert.That(shop.Orders, Is.EqualTo(new[] { receipt.GetOk() }));
    }

    [Test]
    public void ValidationReportsEveryProblemAtOnce()
    {
        var result = ResultCheckout.PlaceOrder(NewShop(), new CheckoutRequest(Ada.Id, " ", 0));

        var messages = ((AggregateException)result.GetError()).InnerExceptions.Select(e => e.Message);
        Assert.That(messages, Is.EqualTo(new[] { "Sku must not be empty or white space.", "Quantity must be between 1 and 10." }));
    }

    [Test]
    public void DeclinedPaymentKeepsItsExceptionType()
    {
        var shop = NewShop();

        var result = ResultCheckout.PlaceOrder(shop, new CheckoutRequest(Ada.Id, "TEA", 3));

        Assert.That(result.GetError(), Is.TypeOf<PaymentDeclinedException>());
        Assert.That(result.GetError().Message, Is.EqualTo("The card of Ada was declined."));
        Assert.That(shop.Orders, Is.Empty);
    }
}
