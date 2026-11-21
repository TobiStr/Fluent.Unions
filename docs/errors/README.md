# Errors

There are three ways to model a failure with Fluent.Unions. They differ in what an error is: any `Exception`, an exception of your own type, or one of a closed set of cases.

## In this section

- [Your own Result type](custom-error-types.md): `[FluentResult<DomainException>]` gives you a `Result<T>` whose error is your type, with every operator.
- [Errors as data](errors-as-data.md): an error union as the error type, checked by an exhaustive `switch`.

## Quick example

```csharp
using Fluent.Unions;
using Fluent.Unions.Abstractions;

// 1. Any exception: the library's Result<T>.
static Fluent.Unions.Result<Customer> GetCustomer(Guid id) =>
    id == Guid.Empty ? new KeyNotFoundException("No customer.") : new Customer(id, "Ada");

// 2. Your own error type.
[FluentResult<DomainException>]
public readonly partial struct Result<T>;

static Result<Customer> FindCustomer(Guid id) =>
    id == Guid.Empty ? new NotFoundException("No customer.") : new Customer(id, "Ada");

// 3. A closed set of errors: the error type is a union.
[FluentResult<CheckoutError>]
public readonly partial struct Outcome<T>;

static Outcome<Customer> LoadCustomer(Guid id) =>
    id == Guid.Empty ? new CustomerNotFound(id) : new Customer(id, "Ada");
```

The first form needs no setup. Forms 2 and 3 cost one declaration and give the compiler more to check. All three have the same operators, `Task` overloads and analyzers, because the generated operators forward to the library.

## When to use

| You want | Use | An error is | The compiler checks |
| --- | --- | --- | --- |
| The shortest path, errors you only log or map by exception type | `Result<T>` from `Fluent.Unions` | any `Exception` | that you handle both cases |
| Errors restricted to one exception family, mapped later to status codes | `[FluentResult<DomainException>]` | an exception of your type | that no other exception type can be an error |
| Every failure reason known and handled one by one | `[FluentResult<CheckoutError>]` with a union | one case of the union | that a `switch` covers every case |

Pick by who consumes the error:

- If a caller can only report it or turn it into a status code, an exception type is enough. Use `Result<T>`.
- If the type must say "this is a domain failure and nothing else", declare your own result type with your base exception.
- If a caller branches on the reason and each reason carries its own data, make the error a union of those reasons.

## Rules

- **Chains do not throw.** An exception thrown inside an operator becomes an error. The default type keeps it as it is. Your own type passes it through `FromException`, unless it already is your error type.
- **Cancellation propagates.** An `OperationCanceledException` is never turned into an error.
- **`Check()` throws in every form.** It rethrows an exception error with its stack trace, the exception case of an error union too. Any other error is thrown inside a `ResultErrorException` that holds it in `Error`.
- **Declare your type in your root namespace.** Code in that namespace and below uses your `Result<T>`, even with `using Fluent.Unions;`. A file outside it that imports both namespaces writes the namespace in front of `Result<T>`.
- **Start from your own type at the boundary.** Use `Result.Try` and `Result.TryAsync` of your own `Result` around code that throws, instead of converting a library `Result<T>`.

---

Back to index: [Docs index](../README.md) · Next: [Your own Result type](custom-error-types.md)
