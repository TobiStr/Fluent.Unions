using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Tests;

public record CustomerNotFound(int Id);

public record CartEmpty;

public record PaymentDeclined(string Reason);

public record Unexpected(Exception Exception);

public union CheckoutError(CustomerNotFound, CartEmpty, PaymentDeclined, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) => new Unexpected(exception);
}

public union AuditError(string, Unexpected) : IFromException<AuditError>
{
    public static AuditError FromException(Exception exception) => new Unexpected(exception);
}

public class ErrorsAsDataTests
{
    private record Customer(int Id, string Name);

    private record Order(int CustomerId, int Items);

    [Test]
    public void ChainsReturnTheErrorUnion()
    {
        Assert.That(Checkout(7, 2).GetOk(), Is.EqualTo(new Order(7, 2)));
        Assert.That(Checkout(0, 2).GetError().Value, Is.EqualTo(new CustomerNotFound(0)));
        Assert.That(Checkout(7, 0).GetError().Value, Is.TypeOf<CartEmpty>());
    }

    [Test]
    public void ErrorsAreMatchedExhaustively()
    {
        static int StatusCode(Result<Order, CheckoutError> result) => result.Match(
            _ => 200,
            error => error switch
            {
                CustomerNotFound => 404,
                CartEmpty => 400,
                PaymentDeclined => 402,
                Unexpected => 500
            },
            () => 204);

        Assert.That(StatusCode(Checkout(7, 2)), Is.EqualTo(200));
        Assert.That(StatusCode(Checkout(0, 2)), Is.EqualTo(404));
        Assert.That(StatusCode(Checkout(7, 0)), Is.EqualTo(400));
        Assert.That(StatusCode(default), Is.EqualTo(204));
    }

    [Test]
    public void ThrownExceptionsBecomeTheUnexpectedCase()
    {
        var failure = new InvalidOperationException("Boom");

        var result = GetCustomer(7).Chain(Result<Order, CheckoutError> (_) => throw failure);

        Assert.That(result.GetError().Value, Is.EqualTo(new Unexpected(failure)));
    }

    [Test]
    public void TypedCatchMatchesACaseOfTheErrorUnion()
    {
        var recovered = Checkout(7, 0).Catch((CartEmpty _) => new Order(7, 1));
        var untouched = Checkout(0, 2).Catch((CartEmpty _) => new Order(7, 1));

        Assert.That(recovered.GetOk(), Is.EqualTo(new Order(7, 1)));
        Assert.That(untouched.GetError().Value, Is.TypeOf<CustomerNotFound>());
    }

    [Test]
    public void SelectAndQuerySyntaxWork()
    {
        var name = GetCustomer(7).Select(customer => customer.Name);
        var order =
            from customer in GetCustomer(7)
            from items in CountItems(customer, 3)
            where items > 1
            select new Order(customer.Id, items);
        var missing =
            from customer in GetCustomer(0)
            from items in CountItems(customer, 3)
            select new Order(customer.Id, items);

        Assert.That(name.GetOk(), Is.EqualTo("Ada"));
        Assert.That(order.GetOk(), Is.EqualTo(new Order(7, 3)));
        Assert.That(missing.GetError().Value, Is.TypeOf<CustomerNotFound>());
    }

    [Test]
    public void MapErrorChangesTheErrorType()
    {
        var audited = Checkout(0, 2).MapError(error => (AuditError)$"Checkout failed: {error.Value}");

        Assert.That(audited.GetError().Value, Is.EqualTo("Checkout failed: CustomerNotFound { Id = 0 }"));
    }

    [Test]
    public void CheckThrowsAResultErrorException()
    {
        var ex = Assert.Throws<ResultErrorException>(() => Checkout(7, 0).Check());

        Assert.That(((CheckoutError)ex!.Error).Value, Is.TypeOf<CartEmpty>());
        Assert.That(Checkout(7, 2).Check().GetOk(), Is.EqualTo(new Order(7, 2)));
    }

    [Test]
    public void SuccessAndGetters()
    {
        Assert.True(Checkout(7, 2).Success);
        Assert.False(Checkout(0, 2).Success);
        Assert.False(default(Result<Order, CheckoutError>).Success);
        Assert.Throws<InvalidOperationException>(() => Checkout(0, 2).GetOk());
        Assert.Throws<InvalidOperationException>(() => Checkout(7, 2).GetError());
    }

    private static Result<Order, CheckoutError> Checkout(int customerId, int items) =>
        GetCustomer(customerId).Chain(customer => CountItems(customer, items).Select(count => new Order(customer.Id, count)));

    private static Result<Customer, CheckoutError> GetCustomer(int id) =>
        id > 0 ? new Customer(id, "Ada") : (CheckoutError)new CustomerNotFound(id);

    private static Result<int, CheckoutError> CountItems(Customer customer, int items) =>
        items > 0 ? items : (CheckoutError)new CartEmpty();
}
