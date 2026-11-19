# Modeling with unions

This page shows two designs where unions pay off: a state machine for an order, and event sourcing for a cart. Both are runnable samples in `test/Fluent.Unions.Tests/Samples`.

## A state machine

An order is in exactly one state, and each state carries only the data that exists in it. Commands are the things a user can ask for. Both are unions of records:

```csharp
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
```

A `Draft` has no amount and no tracking number, so no code can read one before it exists.

### A transition is a Result

`Apply` switches on the pair of state and command. A valid pair returns the next state. Everything else returns an error, so an invalid transition is a value and not an exception:

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

public static Result<OrderState> Apply(OrderState state, OrderCommand command) => (state, command) switch
{
    (Draft draft, AddItem add) => new OrderState(new Draft([.. draft.Items, add.Item])),
    (Draft { Items.Count: 0 }, Submit) => new InvalidOperationException("An empty order cannot be submitted."),
    (Draft draft, Submit) => new OrderState(new Submitted(draft.Items)),
    (Submitted submitted, Pay pay) => new OrderState(new Paid(submitted.Items, pay.Amount)),
    (Paid paid, Ship ship) => new OrderState(new Shipped(paid.Items, ship.TrackingNumber)),
    _ => new InvalidOperationException($"{command.Value?.GetType().Name} is not allowed when the order is {state.Value?.GetType().Name}.")
};
```

Read the arms top to bottom. More specific patterns, such as the empty draft, come before the general ones. The final `_` arm is deliberate: it is the "not allowed" rule for every pair that is not listed. This `switch` is on a pair of unions, so the compiler cannot check the combinations for you. Test the transitions you care about.

### Running a list of commands

`Chain` runs the next transition only if the previous one succeeded, so a list of commands folds into one result:

```csharp
public static Result<OrderState> Run(OrderState state, params OrderCommand[] commands) =>
    commands.Aggregate((Result<OrderState>)state, (current, command) => current.Chain(s => Apply(s, command)));
```

```csharp
OrderState draft = new Draft([]);

var shipped = Run(draft, new AddItem("Tea"), new Submit(), new Pay(4.5m), new Ship("TRACK-1"));
// ok: Shipped { TrackingNumber = TRACK-1 }

var tooEarly = Run(draft, new AddItem("Tea"), new Pay(4.5m));
// error: Pay is not allowed when the order is Draft.

var empty = Run(draft, new Submit());
// error: An empty order cannot be submitted.
```

The first failing command stops the run. The commands after it are not applied.

### Showing a state

A view over the state is an exhaustive `switch`. Add a state and this method stops compiling until it handles it:

```csharp
public static string Describe(OrderState state) => state switch
{
    Draft draft => $"Draft with {draft.Items.Count} items",
    Submitted => "Waiting for payment",
    Paid paid => $"Paid {paid.Amount:0.00}",
    Shipped shipped => $"Shipped as {shipped.TrackingNumber}"
};

shipped.Select(Describe); // ok: "Shipped as TRACK-1"
```

## Event sourcing

Instead of storing the current state, store the facts that happened and rebuild the state from them. The facts are a union:

```csharp
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
```

### Evolve and replay

`Evolve` applies one event to a cart. It is an exhaustive `switch`, so every new kind of event forces you to say what it does to the state. `Replay` folds the whole history with `Aggregate`, starting from `Cart.Empty`:

```csharp
public static Cart Evolve(Cart cart, CartEvent cartEvent) => cartEvent switch
{
    CartOpened opened => cart with { Id = opened.CartId },
    ItemAdded added => cart with { Items = cart.Items.SetItem(added.Item, added.Price) },
    ItemRemoved removed => cart with { Items = cart.Items.Remove(removed.Item) },
    CartCheckedOut => cart with { CheckedOut = true }
};

public static Cart Replay(IEnumerable<CartEvent> history) => history.Aggregate(Cart.Empty, Evolve);
```

```csharp
CartEvent[] history =
[
    new CartOpened(cartId),
    new ItemAdded("Tea", 4.5m),
    new ItemAdded("Cake", 3m),
    new ItemRemoved("Cake")
];

var cart = Replay(history); // one item, total 4.5
```

`Evolve` never fails. An event is a fact that already happened, so replaying it must always work.

### Deciding commands

A command is a request, so it can be refused. A decision function looks at the current state and returns the event, or an error, as a `Result<CartEvent>`:

```csharp
public static Result<CartEvent> CheckOut(Cart cart) => cart switch
{
    { CheckedOut: true } => new InvalidOperationException("The cart is already checked out."),
    { Items.Count: 0 } => new InvalidOperationException("An empty cart cannot be checked out."),
    _ => new CartEvent(new CartCheckedOut(cart.Total))
};
```

Decide, then evolve, then decide again. The second checkout is refused:

```csharp
var checkedOut = CheckOut(cart);
var again = checkedOut.Select(e => Evolve(cart, e)).Chain(CheckOut);
// error: The cart is already checked out.

CheckOut(Replay([new CartOpened(cartId)]));
// error: An empty cart cannot be checked out.
```

Reading the event out of the result works with `As`: `checkedOut.GetOk().As<CartCheckedOut>()` gives an `Option<CartCheckedOut>`.

## Commands and results in a handler

A handler loads the history, replays it, lets the decision function decide, stores the new event and returns the new state. Go-style deconstruction gives an early return on a refusal:

```csharp
public record CheckOutCart(Guid CartId);

public class CheckOutHandler(ICartStore store)
{
    public async Task<Result<Cart>> HandleAsync(CheckOutCart command)
    {
        var history = await store.LoadEventsAsync(command.CartId);
        var cart = CartEvents.Replay(history);

        var (cartEvent, error) = CartEvents.CheckOut(cart);
        if (error is not null)
            return error;

        await store.AppendAsync(command.CartId, cartEvent);
        return CartEvents.Evolve(cart, cartEvent);
    }
}
```

The handler returns `Result<Cart>`. A refused command is an error value, and the caller maps it, for example with [`ToHttpResult`](../aspnetcore/http-results.md). Always check `error` before using the value: on an error, `cartEvent` is `default`.

`ICartStore` stands for your event store:

```csharp
public interface ICartStore
{
    Task<IReadOnlyList<CartEvent>> LoadEventsAsync(Guid id);
    Task AppendAsync(Guid id, CartEvent cartEvent);
}
```

## Pitfalls

- **A discard arm hides new cases.** The `_` arm in `Apply` is right for "everything else is invalid". In `Evolve` and `Describe`, leave it out so the compiler tells you about a new case.
- **Mutate nothing.** `Evolve` returns a new `Cart`. Use records with `with` and immutable collections.
- **Only decide from the replayed state.** Do not check a cached copy in the decision function, or two requests can decide from different histories. Concurrency control on `AppendAsync` is up to your store and is not shown here.
- **Errors here are exceptions.** `InvalidOperationException` is the simplest error type. If callers branch on the reason, use [errors as data](../errors/errors-as-data.md).

## See also

- [Your own unions](your-own-unions.md): declaring unions and the JSON converter, useful for storing events.
- [Errors as data](../errors/errors-as-data.md): typed error cases for refused commands.
- `test/Fluent.Unions.Tests/Samples` in the repo: both samples with tests.

---

Back to: [Unions](README.md) · Next: [ASP.NET Core](../aspnetcore/README.md)
