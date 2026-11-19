# Your own unions

This page covers declaring a union, the `Is` and `As` helpers that work on any union, and `UnionJsonConverter`, which writes the case name so the union can be read back from JSON.

## Declaring a union

A union lists its case types. Each case converts to the union implicitly, and `switch` checks that every case is handled:

```csharp
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
```

The held case is available as `method.Value`, typed `object?`. It is `null` only for a `default` union. A union is a struct, and `default` holds nothing: a `switch` over it throws `SwitchExpressionException` unless an arm matches `null`.

Cases may be any type, not only records. Use records when each case carries data, because their `ToString` and equality are useful in logs and tests.

## Is and As

Both helpers live in `Fluent.Unions.Extensions` and work on every union, including `Result<T>` and `Option<T>`, because every union implements .NET's `IUnion`.

| Helper | Returns |
| --- | --- |
| `Is<TCase>()` | `true` when the union holds a `TCase`. |
| `As<TCase>()` | An `Option<TCase>` with the case, or `None` when the union holds anything else, or nothing. |

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;

bool isCard = method.Is<Card>();                          // true
Option<BankTransfer> transfer = method.As<BankTransfer>(); // None
```

`As` returns an option, so it chains with the option operators:

```csharp
string last4 = method.As<Card>().Select(card => card.Last4).GetValueOrDefault("none"); // "4242"
```

Prefer `switch` when you handle several cases. Use `Is` and `As` when you care about one.

A union value type is boxed when it is passed to either helper, because the parameter is an `IUnion`. This is one small allocation per call. It does not matter in a request handler, but think twice in a tight loop.

## JSON with UnionJsonConverter

By default, `System.Text.Json` writes only the value of a union. That cannot be read back when cases look alike, and it does not say which case it was. Apply `UnionJsonConverter` to write the case name as well:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;

[JsonConverter(typeof(UnionJsonConverter))]
public union PaymentMethod(Card, BankTransfer, Voucher);

public record Checkout(Guid Id, PaymentMethod Method);

var json = JsonSerializer.Serialize(new Checkout(Guid.Empty, new Card("4242")));
```

```json
{"Id":"00000000-0000-0000-0000-000000000000","Method":{"$type":"Card","$value":{"Last4":"4242"}}}
```

Reading is the reverse:

```csharp
var method = JsonSerializer.Deserialize<PaymentMethod>("""{ "$value": { "Code": "SAVE5" }, "$type": "Voucher" }""");
// Voucher { Code = SAVE5 }
```

### Reading and writing rules

- **Order does not matter when reading.** `$type` and `$value` may come in either order, as in the example above. The converter always writes `$type` first.
- **An empty union is `null`.** A `default` union writes `null`, and `null` reads as a `default` union.
- **Case names come from the type name.** `$type` is the name of the case type without its namespace, so an `int` case is `"Int32"`.
- **Unknown or missing `$type` throws `JsonException`.** `{"$type":"Cash","$value":{}}` fails with `'Cash' is not a case of PaymentMethod.`. A missing `$type`, or `{"$type":null}`, fails with `A PaymentMethod needs a '$type' property.`.
- **Case names must be unique.** Two cases with the same type name from different namespaces cannot be told apart. The first serialization throws a `TargetInvocationException` that wraps an `InvalidOperationException` ("has several cases named 'Card'"). Rename one case type.

The converter uses reflection, like the option converter. Trimming and AOT are not investigated.

## Pattern matching tips

- **Match on the case type directly.** Write `Card card => ...`, not a test on `method.Value`.
- **Do not add a discard arm.** `_ => ...` silences CS8509, so adding a case later no longer warns you. Name every case.
- **Combine cases that behave alike** with `Card or Voucher => ...`.
- **Match on a pair for transitions.** `(state, command) switch { (Draft draft, Submit) => ... }` handles each valid combination. See [Modeling with unions](modeling.md).
- **Use `when` for rules inside a case.** `Card { Last4: "0000" } => ...` or `Card c when c.Last4 == "0000" => ...` does not hide the other cases from the exhaustiveness check.
- **Make CS8509 an error** with `<WarningsAsErrors>CS8509</WarningsAsErrors>`, so a missing case breaks the build.

## Pitfalls

- A `default` union reaches a `switch` without a matching arm and throws. Initialize fields and properties of union type.
- Without `[JsonConverter(typeof(UnionJsonConverter))]`, the case name is not written.
- `Is` and `As` check the held value's type. A subclass of `TCase` counts as a `TCase`.

## See also

- [Loadable](loadable.md): a library union for asynchronous UI state.
- [Modeling with unions](modeling.md): state machines and event sourcing.
- `test/Fluent.Unions.Tests/UnionHelperTests.cs` in the repo: the behavior above as tests.

---

Back to: [Unions](README.md) · Next: [Loadable](loadable.md)
