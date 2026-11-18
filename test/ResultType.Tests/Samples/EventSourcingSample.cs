using System.Collections.Immutable;

namespace ResultType.Tests.Samples;

public record CartOpened(Guid CartId);

public record ItemAdded(string Item, decimal Price);

public record ItemRemoved(string Item);

public record CartCheckedOut(decimal Total);

public union CartEvent(CartOpened, ItemAdded, ItemRemoved, CartCheckedOut);

public record Cart(Guid Id, ImmutableDictionary<string, decimal> Items, bool CheckedOut)
{
    public static readonly Cart Empty = new(Guid.Empty, ImmutableDictionary<string, decimal>.Empty, false);

    public decimal Total => Items.Values.Sum();
}

public static class CartEvents
{
    public static Cart Evolve(Cart cart, CartEvent cartEvent) => cartEvent switch
    {
        CartOpened opened => cart with { Id = opened.CartId },
        ItemAdded added => cart with { Items = cart.Items.SetItem(added.Item, added.Price) },
        ItemRemoved removed => cart with { Items = cart.Items.Remove(removed.Item) },
        CartCheckedOut => cart with { CheckedOut = true }
    };

    public static Cart Replay(IEnumerable<CartEvent> history) => history.Aggregate(Cart.Empty, Evolve);

    public static Result<CartEvent> CheckOut(Cart cart) => cart switch
    {
        { CheckedOut: true } => new InvalidOperationException("The cart is already checked out."),
        { Items.Count: 0 } => new InvalidOperationException("An empty cart cannot be checked out."),
        _ => new CartEvent(new CartCheckedOut(cart.Total))
    };
}

public class EventSourcingSampleTests
{
    private static readonly Guid CartId = Guid.NewGuid();

    [Test]
    public void ReplayingEventsRebuildsTheState()
    {
        CartEvent[] history =
        [
            new CartOpened(CartId),
            new ItemAdded("Tea", 4.5m),
            new ItemAdded("Cake", 3m),
            new ItemRemoved("Cake")
        ];

        var cart = CartEvents.Replay(history);

        Assert.That(cart.Id, Is.EqualTo(CartId));
        Assert.That(cart.Total, Is.EqualTo(4.5m));
    }

    [Test]
    public void CommandsDecideTheNextEvent()
    {
        var cart = CartEvents.Replay([new CartOpened(CartId), new ItemAdded("Tea", 4.5m)]);
        var empty = CartEvents.Replay([new CartOpened(CartId)]);

        var checkedOut = CartEvents.CheckOut(cart);
        var again = checkedOut.Select(e => CartEvents.Evolve(cart, e)).Chain(CartEvents.CheckOut);

        Assert.That(checkedOut.GetOk().As<CartCheckedOut>().GetValue().Total, Is.EqualTo(4.5m));
        Assert.That(again.GetError().Message, Is.EqualTo("The cart is already checked out."));
        Assert.That(CartEvents.CheckOut(empty).GetError().Message, Is.EqualTo("An empty cart cannot be checked out."));
    }
}
