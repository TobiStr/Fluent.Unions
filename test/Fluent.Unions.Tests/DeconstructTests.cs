namespace Fluent.Unions.Tests;

public class DeconstructTests
{
    private static readonly KeyNotFoundException NotFound = new("No customer");

    private record Customer(int Id, string Name);

    [Test]
    public void DeconstructsValueErrorAndEmpty()
    {
        var (value, error) = (Result<int>)5;
        var (failedValue, failure) = (Result<int>)NotFound;
        var (emptyValue, emptyError) = default(Result<string>);

        Assert.That(value, Is.EqualTo(5));
        Assert.That(error, Is.Null);
        Assert.That(failedValue, Is.EqualTo(0));
        Assert.That(failure, Is.SameAs(NotFound));
        Assert.That(emptyValue, Is.Null);
        Assert.That(emptyError, Is.Null);
    }

    [Test]
    public void SupportsGoStyleEarlyReturns()
    {
        Assert.That(Greet(7).GetOk(), Is.EqualTo("Hello Ada"));
        Assert.That(Greet(0).GetError(), Is.SameAs(NotFound));
        Assert.That(Greet(-1).Value, Is.Null);
    }

    [Test]
    public void TypePatternsStillMatch()
    {
        Result<int> ok = 5;

        var text = ok switch
        {
            int value => $"value {value}",
            Exception error => error.Message
        };

        Assert.That(text, Is.EqualTo("value 5"));
    }

    private static Result<string> Greet(int id)
    {
        var (customer, error) = GetCustomer(id);

        if (error is not null)
            return error;

        if (customer is null)
            return default;

        return $"Hello {customer.Name}";
    }

    private static Result<Customer> GetCustomer(int id) => id switch
    {
        > 0 => new Customer(id, "Ada"),
        0 => NotFound,
        _ => default
    };
}
