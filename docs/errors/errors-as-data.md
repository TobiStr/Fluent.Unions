# Errors as data

This page covers `Result<T, TError>`, where an error is a union of plain records and the compiler checks that every case is handled.

## Declaring the error union

The error type only has to say how an unexpected exception is represented. Implement `IFromException<TSelf>` on a union of records:

```csharp
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Extensions;

public record CustomerNotFound(int Id);
public record CartEmpty;
public record PaymentDeclined(string Reason);
public record Unexpected(Exception Exception);

public union CheckoutError(CustomerNotFound, CartEmpty, PaymentDeclined, Unexpected) : IFromException<CheckoutError>
{
    public static CheckoutError FromException(Exception exception) => new Unexpected(exception);
}
```

`FromException` has the same contract as on [custom error types](custom-error-types.md#the-three-static-members): it must not return `null` and must not throw.

## Returning an error

A value converts to `Result<T, TError>` implicitly. An error case needs a cast to the error union first, because C# applies only one implicit conversion and `CustomerNotFound` to `Result<Customer, CheckoutError>` would take two:

```csharp
static Result<Customer, CheckoutError> GetCustomer(int id) =>
    id > 0 ? new Customer(id, "Ada") : (CheckoutError)new CustomerNotFound(id);

static Result<Cart, CheckoutError> GetCart(Customer customer) =>
    customer.Id == 2 ? (CheckoutError)new CartEmpty() : new Cart(customer.Id, 3);

static Result<Order, CheckoutError> Pay(Cart cart) =>
    cart.Items > 5 ? (CheckoutError)new PaymentDeclined("Limit exceeded") : new Order(cart.CustomerId, cart.Items);
```

The cast is the only ceremony. Without it, the compiler reports that it cannot implicitly convert the record to the result.

## Chaining

The operators of [custom error types](custom-error-types.md#operators) apply as they are: `Chain`, `Catch`, `Do`, `OnErrorDo`, `Finally`, `Where`, `Match` and the rest. `Select`, `SelectMany` and `MapError` need no wrapper, because the library ships them for `Result<T, TError>`.

```csharp
static Result<Order, CheckoutError> Checkout(int customerId) =>
    GetCustomer(customerId).Chain(GetCart).Chain(Pay);
```

The first error stops the chain. An exception thrown inside a step becomes the error through `FromException`:

```csharp
var thrown = GetCustomer(1).Chain(Result<Order, CheckoutError> (_) => throw new InvalidOperationException("Boom"));
// error: an Unexpected case that holds the InvalidOperationException
```

## Matching every case

`Match` takes one callback for the value, one for the error and one for the empty state. A `switch` inside the error callback must cover every record, or the compiler reports CS8509:

```csharp
int status = Checkout(id).Match(
    order => 200,
    error => error switch
    {
        CustomerNotFound => 404,
        CartEmpty => 400,
        PaymentDeclined => 402,
        Unexpected => 500
    },
    () => 204);
```

Add a case to `CheckoutError` and every such `switch` stops compiling until it handles the new case. This is the main reason to choose errors as data. CS8509 is only a warning by default. Add `<WarningsAsErrors>CS8509</WarningsAsErrors>` to the project to make it fail the build.

## Catching one case

Typed `Catch` matches a case inside the error union. Other errors pass through:

```csharp
Result<Order, CheckoutError> recovered = Checkout(2).Catch((CartEmpty _) => new Order(2, 1));
// ok: Order { CustomerId = 2, Items = 1 }

Checkout(0).Catch((CartEmpty _) => new Order(2, 1));
// error: CustomerNotFound { Id = 0 }
```

## Select, query syntax and MapError

`Select` projects the value. `SelectMany` enables query syntax with several `from` clauses:

```csharp
Result<string, CheckoutError> name = GetCustomer(1).Select(customer => customer.Name); // "Ada"

Result<Order, CheckoutError> order =
    from customer in GetCustomer(1)
    from cart in GetCart(customer)
    where cart.Items > 1
    select new Order(customer.Id, cart.Items);
```

`MapError` converts the error into another error type, for example at a layer boundary. The target type needs `IFromException` too:

```csharp
public union AuditError(string, Unexpected) : IFromException<AuditError>
{
    public static AuditError FromException(Exception exception) => new Unexpected(exception);
}

Result<Order, AuditError> audited = Checkout(0).MapError(error => (AuditError)$"Checkout failed: {error.Value}");
// error: "Checkout failed: CustomerNotFound { Id = 0 }"
```

A `null` from the mapping becomes an error, not an empty result.

## Check

`Check()` throws on an error. The error is not an exception, so it is wrapped in a `ResultErrorException`:

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

Read `Error` instead of the message. The message only names the error union type: `The result failed with CheckoutError.` On a result that holds a value, `Check()` returns it unchanged.

## Mapping to HTTP

`ToHttpResult` for `Result<T, TError>` needs the mapping passed in, so the compiler checks every case. See [Error mapping](../aspnetcore/error-mapping.md#errors-as-data) for the full example:

```csharp
app.MapGet("/checkout/{id}", (int id) => Checkout(id).ToHttpResult(error => error switch
{
    CustomerNotFound => TypedResults.NotFound(),
    CartEmpty => TypedResults.BadRequest(),
    PaymentDeclined declined => TypedResults.Problem(declined.Reason, statusCode: StatusCodes.Status402PaymentRequired),
    Unexpected => TypedResults.StatusCode(500)
}));
```

The endpoint filter does not map `Result<T, TError>`, see [Endpoint filter](../aspnetcore/endpoint-filter.md).

## When to choose this over exceptions

Choose errors as data when:

- the caller branches on the reason, and each reason carries its own fields;
- the list of reasons is closed and you want the compiler to flag a forgotten one;
- a failure is a normal outcome of the business rule, such as "payment declined", not a fault.

Stay with `Result<T>` or a [custom error type](custom-error-types.md) when a stack trace matters, when errors come from code that throws anyway, or when the caller only logs or maps by exception type.

## Pitfalls

- **Forgetting the cast.** `return new CustomerNotFound(id);` does not compile. Write `(CheckoutError)new CustomerNotFound(id)`.
- **A `Task` of `Result<T, TError>` has no operators.** There are no `Task` overloads for it, except `ToHttpResult`. Await first.
- **No JSON.** `JsonSerializer` throws `NotSupportedException` for `Result<T, TError>`, as for every result. Convert it first, for example with `Match`.
- **`Check()` is for the edge.** It turns data back into an exception. Prefer `Match`.

## See also

- [Custom error types](custom-error-types.md): exceptions restricted to your own base class.
- [Error mapping](../aspnetcore/error-mapping.md): the HTTP mapping for errors as data.
- `test/Fluent.Unions.Tests/ErrorsAsDataTests.cs` in the repo: every operator on `Result<T, TError>`.

---

Back to: [Errors](README.md) · Next: [Unions](../unions/README.md)
