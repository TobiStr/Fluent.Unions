# Errors

There are three ways to model a failure with Fluent.Unions. They differ in what an error is: any `Exception`, an exception from your own base class, or plain data.

## In this section

- [Custom error types](custom-error-types.md): your own result type whose errors are restricted to a base exception, such as `DomainException`.
- [Errors as data](errors-as-data.md): `Result<T, TError>` with an error union of records, checked by an exhaustive `switch`.

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.Abstractions;

// 1. Any exception: the library's Result<T>.
static Result<Customer> GetCustomer(Guid id) =>
    id == Guid.Empty ? new KeyNotFoundException("No customer.") : new Customer(id, "Ada");

// 2. Your own result type, restricted to DomainException (see custom-error-types.md).
//    static Result<Customer> GetCustomer(Guid id) => ...  (your own Result<T>)

// 3. Errors as data: the error is a union of records.
static Result<Customer, CheckoutError> FindCustomer(Guid id) =>
    id != Guid.Empty ? new Customer(id, "Ada") : (CheckoutError)new CustomerNotFound(id);
```

The first form is the default and needs no setup. Forms 2 and 3 cost you a type declaration and give the compiler more to check.

## When to use

| You want | Use | An error is | The compiler checks |
| --- | --- | --- | --- |
| The shortest path, errors you only log or map by exception type | `Result<T>` | any `Exception` | that you handle both cases |
| Errors restricted to one exception family, mapped later to HTTP codes | your own union implementing `IResult<TSelf, T, TError>` | an exception of your base class | that no other exception type can be an error |
| Every failure reason known and handled one by one | `Result<T, TError>` | a record in an error union | that a `switch` covers every reason |

Pick by who consumes the error:

- If a caller can only report it or turn it into a status code, an exception type is enough. Use `Result<T>`.
- If the type must say "this is a domain failure and nothing else", restrict it with your own result type.
- If a caller branches on the reason and each reason carries its own data, make the reasons records.

All three forms share the same operators: `Chain`, `Catch`, `Do`, `OnErrorDo`, `Finally`, `Ensure`, `Match` and `Check`. Some are not available on custom types, see the pages for the list.

## Rules

- **Chains do not throw.** In all three forms an exception thrown inside an operator becomes an error. Your result type decides how in `FromException`.
- **Only `Result<T>` has the full operator set.** `Zip`, `Combine`, `Validate`, `Unwrap`, `Transpose`, the `Task` overloads and the other `...Await` operators exist on the library's `Result<T>` only. See [Custom error types](custom-error-types.md#limitations).
- **`Check()` throws in every form.** On an exception it rethrows it. On an error that is data it throws a `ResultErrorException` holding the error in its `Error` property.
- **You can cross between forms.** `MapError` changes the error type of a result, and `ToDomain` (the wrapper you write) lifts a library `Result<T>` into your own type.
- **Do not mix the two `Result<T>` types in one file.** Importing `Fluent.Unions` and your own namespace makes `Result<T>` ambiguous (CS0104).

---

Back to index: [Docs index](../README.md) · Next: [Custom error types](custom-error-types.md)
