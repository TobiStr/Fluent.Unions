# Errors as data

This page covers results whose error is a union of cases, so the compiler checks that every case is handled.

## Declaring the error union

The cases can be exceptions, records, or both. The union only has to say how an unexpected exception is represented:

```csharp
using Fluent.Unions;
using Fluent.Unions.Abstractions;

namespace Shop;

public sealed record CustomerNotFound(int Id);
public sealed record CartEmpty;
public sealed record PaymentDeclined(string Reason);
public sealed record Unexpected(Exception Exception);

public union CheckoutError(CustomerNotFound, CartEmpty, PaymentDeclined, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) => new Unexpected(exception);
}

[FluentResult<CheckoutError>]
public readonly partial struct Outcome<T>;
```

`FromException` must not return `null` and must not throw. `Outcome<T>` gets every operator, `Task` overload and the value-less `Outcome`, like [your own Result type](custom-error-types.md).

## Returning an error

Each case converts straight to the result, so no cast is needed:

```csharp
static Outcome<Customer> GetCustomer(int id) =>
    id > 0 ? new Customer(id, "Ada") : new CustomerNotFound(id);

static Outcome<Cart> GetCart(Customer customer) =>
    customer.Id == 2 ? new CartEmpty() : new Cart(customer.Id, 3);

static Outcome<Order> Pay(Cart cart) =>
    cart.Items > 5 ? new PaymentDeclined("Limit exceeded") : new Order(cart.CustomerId, cart.Items);
```

Set `CaseConversions = false` on the attribute if you prefer to write `(CheckoutError)new CustomerNotFound(id)`. A case that C# cannot convert, such as an interface, an enum or a case that is less accessible than the result, gets no conversion and is reported as [`FLU0103`](../analyzer/README.md#flu0103).

## Chaining

```csharp
static Outcome<Order> Checkout(int customerId) =>
    GetCustomer(customerId).Chain(GetCart).Chain(Pay);
```

The first error stops the chain. An exception thrown inside a step becomes the error through `FromException`, here an `Unexpected` case.

## Matching every case

A `switch` inside `Match` must cover every case, or the compiler reports CS8509:

```csharp
int status = Checkout(id).Match(
    order => 200,
    error => error switch
    {
        CustomerNotFound => 404,
        CartEmpty => 400,
        PaymentDeclined => 402,
        Unexpected => 500
    });
```

Add a case to `CheckoutError` and every such `switch` stops compiling until it handles the new case. CS8509 is only a warning by default; add `<WarningsAsErrors>CS8509</WarningsAsErrors>` to the project to make it fail the build.

## Catching one case

Typed `Catch` matches a case inside the error union when the case is an exception. Other errors pass through:

```csharp
public union LookupError(NotFoundException, Unexpected) : IFromException<LookupError>
{
    public static LookupError FromException(Exception exception) => new Unexpected(exception);
}

[FluentResult<LookupError>]
public readonly partial struct Lookup<T>;

Lookup<Customer> recovered = Find(id).Catch((NotFoundException _) => Lookup<Customer>.Ok(Customer.Guest));
```

For record cases, match inside `Catch(error => error switch { ... })`.

## Check

`Check()` throws on an error. An exception case is rethrown with its stack trace. A record case is thrown inside a `ResultErrorException`; read `Error` instead of the message:

```csharp
try
{
    Checkout(2).Check();
}
catch (ResultErrorException ex)
{
    var error = (CheckoutError)ex.Error; // CartEmpty { }
}
```

## The value type must differ from every case

`Outcome<CartEmpty>` would make a `CartEmpty` value indistinguishable from the error case, and the case conversion would turn values into errors. The analyzer reports it as `FLU0004`.

## Translating errors between layers

`MapError` moves a result to the result type of another layer. It keeps the value and maps the error. Name the target type and its error type:

```csharp
public sealed record BadRequest(string Message);
public sealed record ServerError;

public union ClientError(BadRequest, ServerError) : IFromException<ClientError>
{
    public static ClientError FromException(Exception exception) => new ServerError();
}

[FluentResult<ClientError>]
public readonly partial struct ClientResult<T>;

ClientResult<Order> ForClient(Outcome<Order> outcome) => outcome.MapError<ClientResult<Order>, ClientError>(error => error switch
{
    CustomerNotFound missing => new BadRequest($"No customer {missing.Id}."),
    CartEmpty => new BadRequest("The cart is empty."),
    PaymentDeclined declined => new BadRequest(declined.Reason),
    Unexpected => new ServerError()
});
```

The `switch` has to cover every case, so a new case in `CheckoutError` fails the build here. The lambda returns `ClientError`, so the arms need no cast.

| Situation | Behavior |
| --- | --- |
| The result holds a value | The value moves to the target type, and the map does not run |
| The map throws | The exception becomes an error of the target type, through its `FromException`. `OperationCanceledException` propagates. |
| The map returns `null` | An empty result of the target type, which counts as an error |
| A `default` result | The map receives the error for an uninitialized result |

Both types must have the same value type, otherwise the call does not compile. The result types without a value have `MapError` too: `outcome.MapError<ClientResult, ClientError>(...)`. The library's `Result<T>` and `Result` work as source and as target. To map a `Task` of a result, await it first: `(await PlaceAsync(cart)).MapError<ClientResult<Order>, ClientError>(...)`. To replace the error without changing the type, use `Catch`.

To recover from one case and keep the others, use the general `Catch`: `.Catch(error => error is PaymentDeclined declined ? Retry(declined.Reason) : error)`. The typed `Catch<TCase>` is for exception cases only.

## When to choose this over exceptions

Choose errors as data when:

- the caller branches on the reason, and each reason carries its own fields;
- the list of reasons is closed and you want the compiler to flag a forgotten one;
- a failure is a normal outcome of the business rule, such as "payment declined", not a fault.

Stay with an exception error type when a stack trace matters, when errors come from code that throws anyway, or when the caller only logs or maps by exception type.

## Pitfalls

- **No JSON.** Every serializer refuses results. Convert first, for example with `Match`.
- **`Check()` is for the edge.** It turns data back into an exception. Prefer `Match`.

## See also

- [Your own Result type](custom-error-types.md): an exception base class as the error type.
- [Error mapping](../aspnetcore/error-mapping.md): the HTTP mapping for errors as data.

---

Back to: [Errors](README.md) · Next: [Unions](../unions/README.md)
