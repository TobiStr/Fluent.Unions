namespace ResultType.Tests.Samples;

public record Draft(IReadOnlyList<string> Items);

public record Submitted(IReadOnlyList<string> Items);

public record Paid(IReadOnlyList<string> Items, decimal Amount);

public record Shipped(IReadOnlyList<string> Items, string TrackingNumber);

public union OrderState(Draft, Submitted, Paid, Shipped);

public record AddItem(string Item);

public record Submit;

public record Pay(decimal Amount);

public record Ship(string TrackingNumber);

public union OrderCommand(AddItem, Submit, Pay, Ship);

public static class OrderStateMachine
{
    public static Result<OrderState> Apply(OrderState state, OrderCommand command) => (state, command) switch
    {
        (Draft draft, AddItem add) => new OrderState(new Draft([.. draft.Items, add.Item])),
        (Draft { Items.Count: 0 }, Submit) => new InvalidOperationException("An empty order cannot be submitted."),
        (Draft draft, Submit) => new OrderState(new Submitted(draft.Items)),
        (Submitted submitted, Pay pay) => new OrderState(new Paid(submitted.Items, pay.Amount)),
        (Paid paid, Ship ship) => new OrderState(new Shipped(paid.Items, ship.TrackingNumber)),
        _ => new InvalidOperationException($"{command.Value?.GetType().Name} is not allowed when the order is {state.Value?.GetType().Name}.")
    };

    public static Result<OrderState> Run(OrderState state, params OrderCommand[] commands) =>
        commands.Aggregate((Result<OrderState>)state, (current, command) => current.Chain(s => Apply(s, command)));

    public static string Describe(OrderState state) => state switch
    {
        Draft draft => $"Draft with {draft.Items.Count} items",
        Submitted => "Waiting for payment",
        Paid paid => $"Paid {paid.Amount:0.00}",
        Shipped shipped => $"Shipped as {shipped.TrackingNumber}"
    };
}

public class OrderStateMachineSampleTests
{
    private static readonly OrderState NewOrder = new Draft([]);

    [Test]
    public void ValidTransitionsReachShipped()
    {
        var result = OrderStateMachine.Run(NewOrder, new AddItem("Tea"), new Submit(), new Pay(4.5m), new Ship("TRACK-1"));

        Assert.That(result.Select(OrderStateMachine.Describe).GetOk(), Is.EqualTo("Shipped as TRACK-1"));
    }

    [Test]
    public void InvalidTransitionsBecomeErrors()
    {
        var paidTooEarly = OrderStateMachine.Run(NewOrder, new AddItem("Tea"), new Pay(4.5m));
        var emptySubmit = OrderStateMachine.Run(NewOrder, new Submit());

        Assert.That(paidTooEarly.GetError().Message, Is.EqualTo("Pay is not allowed when the order is Draft."));
        Assert.That(emptySubmit.GetError().Message, Is.EqualTo("An empty order cannot be submitted."));
    }
}
