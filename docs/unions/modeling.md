# Modeling with unions

This page shows six designs where unions pay off: a state machine for an order, event sourcing for a cart, polymorphic API input, operations with several outcomes, UI actions with a reducer, and configuration variants. Each is a runnable sample with tests in `test/Fluent.Unions.Tests/Samples`.

## In this section

- [A state machine](#a-state-machine): an order moves through states with commands.
- [Event sourcing](#event-sourcing): rebuild a cart from the facts that happened.
- [Commands and results in a handler](#commands-and-results-in-a-handler): decide, store, return a `Result`.
- [Polymorphic API input](#polymorphic-api-input): a payment method read from JSON and validated per case.
- [Operations with several outcomes](#operations-with-several-outcomes): normal answers that are not errors.
- [UI actions and a reducer](#ui-actions-and-a-reducer): actions in, next state out.
- [Configuration variants](#configuration-variants): storage settings chosen by case.

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

Reading the event out of the result works with `As`: `checkedOut.GetValue().As<CartCheckedOut>()` gives an `Option<CartCheckedOut>`.

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

## Polymorphic API input

A request can carry one of several payment methods, each with its own fields. The alternative is one class with a `Type` string and a pile of nullable fields, where nothing stops a card request from carrying an IBAN. A union holds exactly one method, and `UnionJsonConverter` writes and reads the case name:

```csharp
public record Card(string Number, int ExpiryMonth);
public record BankTransfer(string Iban);
public record Voucher(string Code, decimal Value);

[JsonConverter(typeof(UnionJsonConverter))]
public union PaymentMethod(Card, BankTransfer, Voucher);

public record PaymentRequest(decimal Subtotal, PaymentMethod Method);
```

```json
{ "Subtotal": 100, "Method": { "$type": "Card", "$value": { "Number": "4242424242424242", "ExpiryMonth": 12 } } }
```

Reading is a `Result`, so malformed JSON and an unknown `$type` are error values. Validation is a `switch` with the rules of each case, and pricing is a second `switch`:

```csharp
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
```

A card for 100 costs `102.00`, a transfer `100`, and a voucher worth 30 `70`. An invalid card reports every failed field at once, with the member names `Number` and `ExpiryMonth`.

Pitfalls:

- **A missing property is an empty union.** `{ "Subtotal": 100 }` leaves `Method` as `default`, and a `switch` without a `null` arm would throw. The `null` arm turns it into a validation error.
- **Deserializing is not validating.** `Read` only checks the shape. Run `Check` before you trust a value, and keep both steps in one `Chain`.
- **The case name is the type name.** Renaming `Voucher` breaks stored and sent JSON. Treat case names as part of the contract, and pin them with [`[UnionCaseName]`](../serialization/README.md#stable-case-names) before a rename.

## Operations with several outcomes

"Registered", "already registered" and "waiting for approval" are all normal answers. Returning a `bool`, or throwing for the duplicate, forces callers to guess which "failure" is really fine. A union names each outcome and carries its own data. The `Result` around it is kept for what went wrong:

```csharp
public record Registered(Guid UserId, string Email);
public record AlreadyRegistered(Guid UserId);
public record PendingApproval(string Email, string Reason);

public union RegistrationOutcome(Registered, AlreadyRegistered, PendingApproval);
```

```csharp
public static Result<RegistrationOutcome> Register(UserDirectory users, string email) =>
    email.EnsureMatchesEmail().Chain(address => Result.Try(() => Decide(users, address)));

private static RegistrationOutcome Decide(UserDirectory users, string email)
{
    if (users.Find(email) is { } existing)
        return new AlreadyRegistered(existing);

    if (email.EndsWith("@competitor.example"))
        return new PendingApproval(email, "Addresses of this domain are reviewed by hand.");

    return new Registered(users.Add(email), email);
}
```

An invalid address is a `ValidationError`, and a store that is offline throws, which `Result.Try` catches as an error. Neither is an outcome. The caller reads both layers with `Match` and an exhaustive `switch`:

```csharp
public static int ToStatusCode(Result<RegistrationOutcome> result) => result.Match(
    outcome => outcome switch
    {
        Registered => 201,
        AlreadyRegistered => 200,
        PendingApproval => 202
    },
    error => error is ValidationErrors or ValidationError ? 400 : 500);
```

Add a fourth case, `Blocked`, and the `switch` stops compiling cleanly:

```text
error CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Blocked' is not covered.
```

CS8509 is a warning by default. The test project turns it into an error with `<WarningsAsErrors>CS8509</WarningsAsErrors>`.

Pitfalls:

- **Keep errors out of the union.** If a case is something you would log or alert on, it belongs in the `Result` as an exception. Outcomes are what a caller may reasonably handle.
- **Do not add a `_` arm.** It silences the warning above, and a new case silently gets the wrong status code.
- **The result can still be an error.** Use `Match`, `IsSuccess` or `TryGetValue` before you read the outcome.

## UI actions and a reducer

In Blazor or any model-view-update design, the UI sends actions and one function computes the next state. With an enum plus an untyped payload, each action's data is a guess. With a union each action carries exactly its own data, and `Reduce` must handle every one:

```csharp
public record AddProduct(string Sku, decimal Price);
public record RemoveProduct(string Sku);
public record ApplyCoupon(string Code);
public record ClearBasket;

public union BasketAction(AddProduct, RemoveProduct, ApplyCoupon, ClearBasket);

public record Basket(ImmutableList<BasketLine> Lines, string? Coupon, Loadable<decimal> Discount)
{
    public static readonly Basket Empty = new([], null, Loadable.NotLoaded);

    public decimal Total => decimal.Max(0, Lines.Sum(line => line.Price * line.Quantity) - Discount.GetValueOrDefault(0m));
}
```

The reducer is pure, so it has no I/O and is easy to test:

```csharp
public static Basket Reduce(Basket basket, BasketAction action) => action switch
{
    AddProduct add => basket with { Lines = AddLine(basket.Lines, add) },
    RemoveProduct remove => basket with { Lines = basket.Lines.RemoveAll(line => line.Sku == remove.Sku) },
    ApplyCoupon coupon => basket with { Coupon = coupon.Code, Discount = Loadable.Loading },
    ClearBasket => Basket.Empty
};
```

`ApplyCoupon` only marks the discount as [loading](loadable.md). The store runs the check, which is the asynchronous part, and keeps its outcome as a `Loadable<decimal>`:

```csharp
public sealed class BasketStore(ICouponService coupons)
{
    public Basket State { get; private set; } = Basket.Empty;

    public async Task DispatchAsync(BasketAction action)
    {
        State = BasketReducer.Reduce(State, action);
        if (action is not ApplyCoupon coupon)
            return;

        var discount = await coupons.CheckAsync(coupon.Code).ToLoadable();
        if (State.Coupon == coupon.Code)
            State = State with { Discount = discount };
    }
}
```

`ICouponService.CheckAsync` returns a `Task<Result<decimal>>`, and `ToLoadable` turns it into loaded or failed. A view reads the state with `Match`:

```csharp
public static string DescribeDiscount(Loadable<decimal> discount) => discount.Match(
    () => "No coupon",
    () => "Checking coupon...",
    amount => $"Discount {amount}",
    error => $"Coupon rejected: {error.Message}");
```

Pitfalls:

- **Check that the answer is still wanted.** The `State.Coupon == coupon.Code` test drops a late reply for a coupon that was replaced or cleared. Without it, an old answer overwrites a newer state.
- **Keep I/O out of `Reduce`.** A reducer that awaits cannot be replayed or tested without the service.
- **Mutate nothing.** `Reduce` returns a new `Basket`. Use records with `with` and immutable collections.
- **The store is not thread-safe.** It assumes one dispatcher, as in a Blazor component. Serialize calls if you share it.

## Configuration variants

An app can store files on disk, in Azure Blob Storage or in S3. A single options class with a `Provider` enum and every setting as a nullable property allows `Provider: "S3"` with only a path set. A union holds the settings of the chosen provider and nothing else:

```csharp
public record LocalDisk(string Path);
public record AzureBlob(string ConnectionString, string Container);
public record S3(string Bucket, string Region);

[JsonConverter(typeof(UnionJsonConverter))]
public union StorageOptions(LocalDisk, AzureBlob, S3);

public record AppSettings(StorageOptions Storage);
```

```json
{ "Storage": { "$type": "S3", "$value": { "Bucket": "uploads", "Region": "eu-west-1" } } }
```

Validation checks only the chosen case. Choosing the implementation is an exhaustive `switch`, and `Load` ties parsing, validation and creation into one `Result`:

```csharp
public static Result<StorageOptions> Check(StorageOptions options) => options switch
{
    LocalDisk disk => Result.From(disk)
        .Validate(d => d.Path.EnsureNotWhiteSpace())
        .Select(_ => options),
    AzureBlob blob => Result.From(blob)
        .Validate(
            b => b.ConnectionString.EnsureNotWhiteSpace(),
            b => b.Container.EnsureMatches(@"^[a-z0-9-]{3,63}$"))
        .Select(_ => options),
    S3 s3 => Result.From(s3)
        .Validate(
            s => s.Bucket.EnsureNotWhiteSpace(),
            s => s.Region.EnsureMatches(@"^[a-z]{2}-[a-z]+-\d$"))
        .Select(_ => options),
    null => new ValidationError("Storage must be configured.", "Storage")
};

public static IBlobStorage Create(StorageOptions options) => options switch
{
    LocalDisk disk => new LocalDiskStorage(disk.Path),
    AzureBlob blob => new AzureBlobStorage(blob.ConnectionString, blob.Container),
    S3 s3 => new S3Storage(s3.Bucket, s3.Region)
};

public static Result<IBlobStorage> Load(string json) =>
    Result.Try(() => JsonSerializer.Deserialize<AppSettings>(json)!)
        .Chain(settings => Check(settings.Storage))
        .Select(Create);
```

An empty `Bucket` and a bad `Region` are reported together, with the member names `Bucket` and `Region`. Add a provider and both `switch` expressions warn until they handle it. Call `Load` at startup, so a wrong configuration stops the app before it serves a request.

Pitfalls:

- **The sample reads JSON text.** It does not bind through `IConfiguration`. Binding a union through `Microsoft.Extensions.Configuration` was not investigated.
- **A missing section is an empty union.** The `null` arm reports it. Without it, `Create` would throw.
- **Secrets do not belong in the file.** The connection string is shown for the shape only. Read real ones from a secret store.

## Pitfalls

- **A discard arm hides new cases.** The `_` arm in `Apply` is right for "everything else is invalid". In `Evolve` and `Describe`, leave it out so the compiler tells you about a new case.
- **Mutate nothing.** `Evolve` returns a new `Cart`. Use records with `with` and immutable collections.
- **Only decide from the replayed state.** Do not check a cached copy in the decision function, or two requests can decide from different histories. Concurrency control on `AppendAsync` is up to your store and is not shown here.
- **Errors here are exceptions.** `InvalidOperationException` is the simplest error type. If callers branch on the reason, use [errors as data](../errors/errors-as-data.md).

## See also

- [Your own unions](your-own-unions.md): declaring unions and the JSON converter, useful for storing events.
- [Errors as data](../errors/errors-as-data.md): typed error cases for refused commands.
- [Loadable](loadable.md): the state of asynchronous data, used in the reducer sample.
- `test/Fluent.Unions.Tests/Samples` in the repo: every sample with tests.

---

Back to: [Unions](README.md) · Next: [ASP.NET Core](../aspnetcore/README.md)
