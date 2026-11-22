# Unions

A C# 15 `union` holds exactly one of a fixed list of case types, and `switch` makes the compiler check that you handled each. Fluent.Unions builds `Result`, `Option<T>` and `Loadable<T>` on unions, and adds helpers that work on your own.

## In this section

- [Your own unions](your-own-unions.md): declaring unions, `Is<T>` and `As<T>`, and `UnionJsonConverter` for JSON that can be read back.
- [Loadable](loadable.md): `Loadable<T>`, the state of data that loads asynchronously, for views and view models.
- [Modeling with unions](modeling.md): a state machine, event sourcing, polymorphic API input, operations with several outcomes, a UI reducer and configuration variants.

## Quick example

```csharp
using Fluent.Unions;

public record Card(string Last4);
public record BankTransfer(string Iban);
public record Voucher(string Code);

public union PaymentMethod(Card, BankTransfer, Voucher);

PaymentMethod method = new Card("4242");

string description = method switch
{
    Card card => $"Card ending {card.Last4}",
    BankTransfer bank => $"Transfer from {bank.Iban}",
    Voucher voucher => $"Voucher {voucher.Code}"
};
// "Card ending 4242"

bool isCard = method.Is<Card>();                // true
Option<BankTransfer> transfer = method.As<BankTransfer>(); // None
```

## When to use

- A value is one of several distinct shapes, and each shape carries its own data: payment methods, order states, commands, events, storage settings.
- An operation has several normal outcomes that are not errors, such as registered, already registered and pending approval.
- A UI sends actions and one function computes the next state.
- You want a forgotten case to be a compiler warning, not a runtime surprise.
- You need a state for asynchronous UI data: use `Loadable<T>`.

For "a value or an error" use [`Result<T>`](../errors/README.md). For "a value or nothing" use `Option<T>`.

## Rules

- **`switch` exhaustiveness is a warning.** Omitting a case gives CS8509. Add `<WarningsAsErrors>CS8509</WarningsAsErrors>` to make it fail the build.
- **A `default` union holds nothing.** `switch` over it throws at runtime unless an arm matches `null`. The compiler does not warn about a missing `null` arm. `Option<T>` and `Loadable<T>` treat `default` as `None` and `NotLoaded`, so their arms need `or null`, for example `NotLoaded or null`. A `default` result counts as an error.
- **Unions are structs.** The helpers take an `IUnion`, so a union value is boxed for each call.
- **Plain JSON loses the case.** Without `UnionJsonConverter`, `System.Text.Json` writes only the value, which cannot be read back when cases look alike.

---

Back to index: [Docs index](../README.md) · Next: [Your own unions](your-own-unions.md)
