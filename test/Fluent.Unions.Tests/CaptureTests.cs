namespace Fluent.Unions.Tests;

public class CaptureTests
{
    private static readonly KeyNotFoundException NotFound = new("No customer");

    private record Customer(int Id, string Name);

    private record Cart(int Items);

    [Test]
    public void LaterStepsUseEarlierValues()
    {
        var order = GetCustomer(7).Out(out var customer)
            .Chain(GetCart).Out(out var cart)
            .Chain(_ => Submit(customer, cart))
            .Select(text => $"{text} for {customer.Value.Name}");

        Assert.That(order.GetValue(), Is.EqualTo("order#7-3 for Ada"));
        Assert.True(customer.HasValue);
        Assert.That(cart.Value.Items, Is.EqualTo(3));
    }

    [Test]
    public void FailedStepLeavesLaterHoldersEmptyAndSkipsLaterSteps()
    {
        var calls = 0;

        var order = GetCustomer(0).Out(out var customer)
            .Chain(c =>
            {
                calls++;
                return GetCart(c);
            }).Out(out var cart)
            .Chain(_ => Submit(customer, cart));

        Assert.That(order.GetError(), Is.SameAs(NotFound));
        Assert.That(calls, Is.EqualTo(0));
        Assert.False(customer.HasValue);
        Assert.False(cart.HasValue);
    }

    [Test]
    public void ReadingAnEmptyHolderThrowsInsteadOfReturningNull()
    {
        GetCustomer(0).Out(out var customer);
        default(Result<int>).Out(out var number);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = customer.Value);
        Assert.That(ex!.Message, Does.Contain("No Customer was captured"));
        Assert.Throws<InvalidOperationException>(() => _ = (int)number);
        Assert.False(number.HasValue);
    }

    [Test]
    public void ImplicitConversionAndToString()
    {
        ((Result<int>)42).Out(out var answer);

        int value = answer;

        Assert.That(value, Is.EqualTo(42));
        Assert.That(answer.ToString(), Is.EqualTo("42"));
        Assert.Throws<ArgumentNullException>(() => _ = (int)(Captured<int>)null!);
    }

    [Test]
    public void OutReturnsTheSameResult()
    {
        Result<int> error = new InvalidOperationException();

        Assert.That(((Result<int>)5).Out(out _).GetValue(), Is.EqualTo(5));
        Assert.That(error.Out(out _).GetError(), Is.SameAs(error.GetError()));
        Assert.That(default(Result<int>).Out(out _).Value, Is.Null);
    }

    [Test]
    public void OptionsCaptureToo()
    {
        Option<string> name = "Ada";

        var greeting = name.Out(out var captured).Select(n => n.Length).Select(length => $"{captured.Value} has {length} letters");
        var none = default(Option<string>).Out(out var missing);

        Assert.That(greeting.GetValue(), Is.EqualTo("Ada has 3 letters"));
        Assert.False(missing.HasValue);
        Assert.That(none.Value, Is.TypeOf<None>());
    }

    [Test]
    public async Task AsyncChainsCaptureWithoutAwaitingEachStep()
    {
        var order = await GetCustomerAsync(7).Out(out var customer)
            .ChainAwait(GetCartAsync).Out(out var cart)
            .ChainAwait(_ => SubmitAsync(customer, cart))
            .Select(text => $"{text} for {customer.Value.Name}");
        var failed = await GetCustomerAsync(0).Out(out var missing)
            .ChainAwait(GetCartAsync);

        Assert.That(order.GetValue(), Is.EqualTo("order#7-3 for Ada"));
        Assert.That(failed.GetError(), Is.SameAs(NotFound));
        Assert.False(missing.HasValue);
    }

    [Test]
    public async Task FaultedTaskLeavesTheHolderEmpty()
    {
        var result = await Task.FromException<Result<int>>(NotFound).Out(out var captured);

        Assert.That(result.GetError(), Is.SameAs(NotFound));
        Assert.False(captured.HasValue);
    }

    [Test]
    public async Task AsyncOptionsCaptureToo()
    {
        var length = await Task.FromResult<Option<string>>("Ada").Out(out var name).Select(n => n.Length);
        var none = await Task.FromResult(default(Option<string>)).Out(out var missing);

        Assert.That(length.GetValue(), Is.EqualTo(3));
        Assert.That(name.Value, Is.EqualTo("Ada"));
        Assert.That(none.Value, Is.TypeOf<None>());
        Assert.False(missing.HasValue);
    }

    private static Result<Customer> GetCustomer(int id) => id > 0 ? new Customer(id, "Ada") : NotFound;

    private static Result<Cart> GetCart(Customer customer) => new Cart(3);

    private static Result<string> Submit(Customer customer, Cart cart) => $"order#{customer.Id}-{cart.Items}";

    private static async Task<Result<Customer>> GetCustomerAsync(int id)
    {
        await Task.Yield();
        return GetCustomer(id);
    }

    private static async Task<Result<Cart>> GetCartAsync(Customer customer)
    {
        await Task.Yield();
        return GetCart(customer);
    }

    private static async Task<Result<string>> SubmitAsync(Customer customer, Cart cart)
    {
        await Task.Yield();
        return Submit(customer, cart);
    }
}
