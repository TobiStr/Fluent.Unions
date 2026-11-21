using Fluent.Unions.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Tests.Samples;

public record CheckoutRequest(Guid CustomerId, string Sku, int Quantity);

public record Customer(Guid Id, string Name);

public record Product(string Sku, decimal Price, int Stock);

public record Receipt(string Customer, string Sku, decimal Total, string PaymentId);

public class PaymentDeclinedException(string message) : Exception(message);

public class OutOfStockException(string message) : Exception(message);

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
        error = null;
        if (string.IsNullOrWhiteSpace(request.Sku)) { error = "Sku must not be empty or white space."; return null; }
        if (request.Quantity is < 1 or > 10) { error = "Quantity must be between 1 and 10."; return null; }

        var customer = shop.FindCustomer(request.CustomerId);
        if (customer is null) { error = $"No customer {request.CustomerId}."; return null; }

        var product = shop.FindProduct(request.Sku);
        if (product is null) { error = $"No product {request.Sku}."; return null; }
        if (product.Stock < request.Quantity) { error = $"Only {product.Stock} left of {product.Sku}."; return null; }

        var total = product.Price * request.Quantity;
        string paymentId;
        try { paymentId = shop.Charge(customer, total); }
        catch (PaymentDeclinedException exception) { error = exception.Message; return null; }

        var receipt = new Receipt(customer.Name, product.Sku, total, paymentId);
        shop.Save(receipt);
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
    public static Result<Receipt> PlaceOrder(Shop shop, ILogger logger, CheckoutRequest request) =>
        Result.From(request)
            .Validate(
                r => r.Sku.EnsureNotWhiteSpace(),
                r => r.Quantity.EnsureInRange(1, 10))
            .Chain(r => FindCustomer(shop, r.CustomerId)
                .Zip(FindProduct(shop, r.Sku), (customer, product) => new Order(customer, product, r.Quantity)))
            .Ensure(o => o.Product.Stock >= o.Quantity, o => new OutOfStockException($"Only {o.Product.Stock} left of {o.Product.Sku}.")).Out(out var order)
            .Chain(o => Result.Try(() => shop.Charge(o.Customer, o.Total))
                .Select(paymentId => new Receipt(o.Customer.Name, o.Product.Sku, o.Total, paymentId)))
            .Do(shop.Save)
            .Do(receipt => logger.LogInformation(
                "Placed {Quantity} x {Sku} for customer {CustomerId}, payment {PaymentId}",
                order.Value.Quantity, receipt.Sku, order.Value.Customer.Id, receipt.PaymentId));

    private sealed record Order(Customer Customer, Product Product, int Quantity)
    {
        public decimal Total => Product.Price * Quantity;
    }

    private static Result<Customer> FindCustomer(Shop shop, Guid id) =>
        shop.FindCustomer(id).ToResult(() => new KeyNotFoundException($"No customer {id}."));

    private static Result<Product> FindProduct(Shop shop, string sku) =>
        shop.FindProduct(sku).ToResult(() => new KeyNotFoundException($"No product {sku}."));
}

public sealed class ListLogger : ILogger
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Messages.Add(formatter(state, exception));
}

public record HttpError(int StatusCode, string Message);

public record HttpResponse(int StatusCode, object? Body);

public static class HttpResponseExtensions
{
    public static HttpResponse ToHttpResponse<T>(this Result<T> result, int successStatusCode = 200) => result.Match(
        value => new HttpResponse(successStatusCode, value),
        error => ToHttpResponse(error.ToHttpError()));

    public static HttpError ToHttpError(this Exception error) => error switch
    {
        ValidationErrors validation => new(400, validation.Message),
        KeyNotFoundException => new(404, error.Message),
        OutOfStockException => new(409, error.Message),
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
        Assert.That(ResultCheckout.PlaceOrder(NewShop(), NullLogger.Instance, request).ToHttpResponse(201).StatusCode, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(Requests))]
    public void MatchingDirectlyGivesTheSameStatus(CheckoutRequest request, int expected)
    {
        var shop = NewShop();

        int status = ResultCheckout.PlaceOrder(shop, NullLogger.Instance, request) switch
        {
            Receipt => 201,
            ValidationErrors => 400,
            KeyNotFoundException => 404,
            OutOfStockException => 409,
            PaymentDeclinedException => 402,
            Exception => 500
        };

        Assert.That(status, Is.EqualTo(expected));
    }

    [Test]
    public void ErrorsBecomeHttpErrorsInOnePlace()
    {
        var invalid = ResultCheckout.PlaceOrder(NewShop(), NullLogger.Instance, new CheckoutRequest(Ada.Id, " ", 0)).ToHttpResponse(201);
        var unexpected = new IOException("Disk C:\\secrets is full.").ToHttpError();

        Assert.That(invalid.Body, Is.EqualTo(new HttpError(400, "Sku must not be empty or white space. Quantity must be between 1 and 10.")));
        Assert.That(unexpected, Is.EqualTo(new HttpError(500, "An unexpected error occurred.")));
    }

    [Test]
    public void PlacedOrderIsSaved()
    {
        var shop = NewShop();

        var receipt = ResultCheckout.PlaceOrder(shop, NullLogger.Instance, new CheckoutRequest(Ada.Id, "TEA", 2));

        Assert.That(receipt.GetValue(), Is.EqualTo(new Receipt("Ada", "TEA", 9m, "PAY-1")));
        Assert.That(shop.Orders, Is.EqualTo(new[] { receipt.GetValue() }));
    }

    [Test]
    public void PlacedOrderIsLoggedWithTheCapturedOrder()
    {
        var logger = new ListLogger();

        ResultCheckout.PlaceOrder(NewShop(), logger, new CheckoutRequest(Ada.Id, "TEA", 2));

        Assert.That(logger.Messages, Is.EqualTo(new[] { $"Placed 2 x TEA for customer {Ada.Id}, payment PAY-1" }));
    }

    [Test]
    public void FailedOrderIsNotLogged()
    {
        var logger = new ListLogger();

        ResultCheckout.PlaceOrder(NewShop(), logger, new CheckoutRequest(Ada.Id, "TEA", 3));

        Assert.That(logger.Messages, Is.Empty);
    }

    [Test]
    public void ValidationReportsEveryProblemAtOnce()
    {
        var result = ResultCheckout.PlaceOrder(NewShop(), NullLogger.Instance, new CheckoutRequest(Ada.Id, " ", 0));

        var errors = ((ValidationErrors)result.GetError()).Errors;
        Assert.That(errors.Select(e => e.Message), Is.EqualTo(new[] { "Sku must not be empty or white space.", "Quantity must be between 1 and 10." }));
        Assert.That(errors.Select(e => e.MemberName), Is.EqualTo(new[] { "Sku", "Quantity" }));
    }

    [Test]
    public void DeclinedPaymentKeepsItsExceptionType()
    {
        var shop = NewShop();

        var result = ResultCheckout.PlaceOrder(shop, NullLogger.Instance, new CheckoutRequest(Ada.Id, "TEA", 3));

        Assert.That(result.GetError(), Is.TypeOf<PaymentDeclinedException>());
        Assert.That(result.GetError().Message, Is.EqualTo("The card of Ada was declined."));
        Assert.That(shop.Orders, Is.Empty);
    }
}
