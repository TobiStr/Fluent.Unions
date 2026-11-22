# Validation

Validation reports every problem at once, where `Combine` and `Chain` stop at the first. This page covers `Validate` for one value, the ready-made rules in `Fluent.Unions.Validation`, `CombineAll` for many results, `Partition` for reporting, and how a validation failure becomes an HTTP 400 response.

## In this section

This folder has one page. The topics are below.

- [Validate](#validate): run every rule on one value.
- [Rule catalogue](#rule-catalogue): the `Is` predicates and the `Ensure*` rules.
- [String rules](#string-rules): every string rule with its default message.
- [Number rules](#number-rules): comparisons, sign, parity and divisibility.
- [General rules](#general-rules): equality, membership, null and option rules for any value.
- [Collection rules](#collection-rules): count, uniqueness, membership and item predicates.
- [Date, time and other value rules](#date-time-and-other-value-rules): dates with a `TimeProvider`, `TimeSpan`, `char`, `Guid`, `bool` and enums.
- [DataAnnotations](#dataannotations): check the attributes of a value.
- [CombineAll](#combineall-for-many-inputs): collect every error from many results.
- [Partition](#partition-for-reporting): split values and errors without failing.
- [A realistic command](#a-realistic-command): validation in a handler.
- [HTTP 400](#http-400-with-an-errors-list): the mapping in the ASP.NET Core package.
- [Comparison with Combine](#comparison-with-combine)

## Quick example

```csharp
using Fluent.Unions;

Result<PlaceOrder> command = ParseCommand(dto).Validate(
    c => c.Quantity > 0 ? Result.Ok : new ValidationError("Quantity must be positive."),
    c => c.Email.Contains('@') ? Result.Ok : new ValidationError("Email is invalid."));
// error: ValidationErrors with both messages, when both rules fail
```

## When to use

- User input, where the caller should see every problem in one response.
- A value that must satisfy several independent rules.
- A list of items where each is parsed or checked on its own, and the report should list all failures.

Use `Chain` instead when a step depends on the previous one, such as loading a customer before checking its credit.

## How Validate behaves

- Every rule runs. There is no short circuit.
- When every failure is a `ValidationError`, the error is a `ValidationErrors`, even when only one rule failed. Match that one type.
- A rule that throws counts as a failure. When any failure is not a `ValidationError`, the error is an `AggregateException` with every failure instead, because it is not the client's fault.
- An error passes through without running the rules.

## Validate

`Validate(params Func<T, Result>[] rules)` runs every rule on the value of a `Result<T>`. A rule returns `Result.Ok` to pass, or an exception to fail. The exception converts to a failed `Result` implicitly.

```csharp
Result<PlaceOrder> validated = Result<PlaceOrder>.Ok(command).Validate(
    c => c.Lines.Count > 0 ? Result.Ok : new ValidationError("The order needs a line."),
    c => c.Email.Contains('@') ? Result.Ok : new ValidationError("Email is invalid."));
```

What you get back:

| Input | Output |
| --- | --- |
| ok, every rule passes | the same result |
| ok, one or more rules fail | error: `ValidationErrors` with every failure |
| ok, a rule fails with another exception | error: `AggregateException` with every failure |
| error | the same error, rules not run |

Because one failed rule also gives a `ValidationErrors`, one handler works for one failure or many:

```csharp
validated.OnErrorDo(e => { if (e is ValidationErrors v) Log(v.ToDictionary()); });
```

**Flattening.** Nested `ValidationErrors` and `AggregateException`s are flattened into one list. A rule can itself return a failed validation of a sub-object, and its errors join the list.

```csharp
Result<PlaceOrder> validated = Result<PlaceOrder>.Ok(command).Validate(
    c => Result<Address>.Ok(c.Address).Validate(CityIsSet, ZipIsSet), // Result<Address> converts to Result
    c => c.Lines.Count > 0 ? Result.Ok : new ValidationError("The order needs a line."));
// one ValidationErrors, holding the address errors and the line error side by side
```

To reuse rules, pass named methods:

```csharp
static Result HasLines(PlaceOrder c) =>
    c.Lines.Count > 0 ? Result.Ok : new ValidationError("The order needs a line.");

static Result HasValidEmail(PlaceOrder c) =>
    c.Email.Contains('@') ? Result.Ok : new ValidationError("Email is invalid.");

Result<PlaceOrder> validated = Result<PlaceOrder>.Ok(command).Validate(HasLines, HasValidEmail);
```

**Throwing rules.** A rule that throws is a failure, and its exception is added to the list. A rule that returns `default` is a failure too, because a `default` result is an error.

**Null rules.** Passing `null` as the rules array, or a `null` rule, throws `ArgumentNullException`.

**Tasks.** `Validate` works on `Task<Result<T>>`, so it can follow an asynchronous step. The rules are synchronous.

```csharp
Result<PlaceOrder> order = await ParseCommandAsync(request).Validate(HasLines, HasValidEmail);
```

## Rule catalogue

`Fluent.Unions.Validation` holds ready-made rules, so a rule is not a hand-written lambda. Add `using Fluent.Unions.Validation;`. The rules come in two layers.

- `Is` is a catalogue of plain predicates. `Is.NotEmpty` is a method group. A rule with arguments is a factory, such as `Is.ShorterThan(50)`, that returns a `Func<T, bool>`.
- `Ensure<Name>` methods wrap those predicates and return a `Result<T>` with a message and a member name.

```csharp
using Fluent.Unions.Validation;

Result<Customer> validated = Result<Customer>.Ok(customer).Validate(
    c => c.Name.EnsureNotEmpty(),
    c => c.Email.EnsureMatchesEmail(),
    c => c.Nickname.EnsureShorterThan(30));
// error: ValidationErrors, Name: "Name must not be empty.", Email: "Email must be a valid email address."
```

A rule returns `Result<string>`, and `Validate` takes `Func<T, Result>`. The implicit conversion from `Result<T>` to `Result` makes the lambda fit.

**Predicates.** Use them where a `Func<T, bool>` is expected:

```csharp
Option<string> nickname = Option.Some("").Where(Is.NotEmpty);
// None

Result<string> name = Result<string>.Ok(input).Ensure(Is.ShorterThan(50), "The name is too long.");
```

**Two forms of every rule.**

| Form | Example | Name in the message |
| --- | --- | --- |
| On the value | `c.Name.EnsureNotEmpty()` | captured from the argument expression |
| On a `Result<string>` | `Result<string>.Ok(input).EnsureNotEmpty()` | `Value`, or the `name` argument |

The value form returns a `Result<string>` for the value and treats `null` as an error. The `Result<T>` form is for chains: an error passes through unchanged and the rule does not run. On a `Result<T>` receiver the `Result<T>` form always wins.

Both forms take `string? message = null` and `string? name = null`. Rules with arguments take them first, for example `EnsureShorterThan(30, message: "Too long.")`.

**Messages.** Every rule has a default template such as `"{Name} must not be empty."`. Rule arguments are formatted with the invariant culture, and the value itself is never echoed. Passing `message` replaces the template word for word, so `{Name}` is not substituted. It also replaces the `null` message.

**Names.** The value form captures the argument expression and keeps the last member: `c.Age` becomes `Age`, `customer.Address.Zip` becomes `Zip`, `c.Name!` becomes `Name`, a local `age` stays `age`. Anything that is not an identifier, such as `GetAge()` or a literal, becomes `Value`. The same trimming applies to an explicit `name`. A captured name also fills `ValidationError.MemberName`. The fallback `Value` leaves `MemberName` as `null`.

<a id="errors"></a>**Errors.** A failed rule is a `Fluent.Unions.ValidationError` with `Message` and `MemberName`. Several of them are collected in a `ValidationErrors`, whose `Errors` lists them and whose `ToDictionary()` groups the messages by member, the shape ASP.NET Core uses for validation problems. A predicate that throws, such as a regular expression that times out, becomes the error as is. A `null` value is the error `"{Name} must not be null."`.

**Any value.** For a rule that is not in the catalogue, `Ensure` also works on a plain value:

```csharp
Result<int> quantity = request.Quantity.Ensure(q => q % 6 == 0, "Quantity must be a multiple of six.");
// error: ValidationError, MemberName = "Quantity"
```

`Ensure(predicate, v => exception)` returns your own exception. There is no default message: you supply the message or the exception. The captured name only fills `MemberName` of the message form.

Named rules are synchronous. In a `Task<Result<T>>` pipeline, use `.Ensure(Is.X, message)` from [Result operations](../result/operations.md).

## String rules

Every row has both forms, `Ensure<Name>` on a `string?` and on a `Result<string>`, and the predicate `Is.<Name>`. The `Name` placeholder is the resolved name.

| Rule | Passes when | Default message |
| --- | --- | --- |
| `NotEmpty` | not null and not empty | `{Name} must not be empty.` |
| `Empty` | null or empty | `{Name} must be empty.` |
| `NotWhiteSpace` | has a character that is not white space | `{Name} must not be empty or white space.` |
| `WhiteSpace` | null, empty or only white space | `{Name} must be empty or white space.` |
| `MatchesEmail` | valid email address (`EmailAddressAttribute`) | `{Name} must be a valid email address.` |
| `MatchesUrl` | absolute `http` or `https` URL | `{Name} must be a valid http or https URL.` |
| `MatchesGuid` | parses with `Guid.TryParse` | `{Name} must be a valid GUID.` |
| `MatchesIpAddress` | parses with `IPAddress.TryParse` | `{Name} must be a valid IP address.` |
| `MatchesPhoneNumber` | valid phone number (`PhoneAttribute`) | `{Name} must be a valid phone number.` |
| `Alpha` | not empty, only letters | `{Name} must contain only letters.` |
| `Alphanumeric` | not empty, only letters and digits | `{Name} must contain only letters and digits.` |
| `Numeric` | not empty, only digits | `{Name} must contain only digits.` |
| `Base64` | not empty, valid Base64 | `{Name} must be a valid Base64 string.` |
| `IncludesDigit`, `IncludesLetter`, `IncludesLetterOrDigit` | has such a character | `{Name} must include a digit.`, `a letter.`, `a letter or a digit.` |
| `IncludesLower`, `IncludesUpper` | has a lowercase or an uppercase letter | `{Name} must include a lowercase letter.`, `an uppercase letter.` |
| `IncludesPunctuation`, `IncludesSymbol`, `IncludesWhiteSpace` | has such a character | `{Name} must include a punctuation character.`, `a symbol.`, `a white space character.` |
| `IncludesSpecial` | has a character that is not a letter, digit or white space | `{Name} must include a special character.` |
| `HasLength(n)` | length equals `n` | `{Name} must be exactly 3 characters long.` |
| `LongerThan(n)`, `LongerThanOrEqual(n)` | length above `n`, or at least `n` | `{Name} must be longer than 3 characters.`, `at least 3 characters long.` |
| `ShorterThan(n)`, `ShorterThanOrEqual(n)` | length below `n`, or at most `n` | `{Name} must be shorter than 3 characters.`, `at most 3 characters long.` |
| `Contains(text)`, `NotContains(text)` | contains, or does not contain, the text | `{Name} must contain 'text'.`, `must not contain 'text'.` |
| `StartsWith(text)`, `NotStartsWith(text)` | starts, or does not start, with the text | `{Name} must start with 'text'.`, `must not start with 'text'.` |
| `EndsWith(text)`, `NotEndsWith(text)` | ends, or does not end, with the text | `{Name} must end with 'text'.`, `must not end with 'text'.` |
| `Matches(regex)`, `Matches(pattern, options)` | matches the expression | `{Name} must match the pattern 'pattern'.` |
| `NotMatches(regex)`, `NotMatches(pattern, options)` | does not match | `{Name} must not match the pattern 'pattern'.` |

- The text rules take `(string text, StringComparison comparison = StringComparison.Ordinal)`. There are no separate ignore-case rules.
- A `null` string fails every `Is` predicate except `Empty` and `WhiteSpace`, so `Option.Where(Is.NotEmpty)` and your own `Ensure` can use them directly.
- The format rules use the BCL parsers instead of hand-written patterns. An empty string fails them. `Base64` rejects the empty string even though it decodes to nothing.
- A string pattern is compiled once and cached by `Regex`'s static cache, and a match times out after one second. A timeout becomes the error. A pattern that does not parse throws when the rule is created, and `Ensure` forms on a `Result<string>` create it only when the result is Ok.

```csharp
Result<string> password = Result<string>.Ok(input)
    .EnsureLongerThanOrEqual(12, name: "Password")
    .EnsureIncludesDigit(name: "Password")
    .EnsureIncludesUpper(name: "Password");
// error: the first failed rule, "Password must be at least 12 characters long."
```

The `Ensure<Name>` chain is fail-fast. To report every problem at once, use `Validate` as shown above.

## Number rules

Every row has both forms, `Ensure<Name>` on a value and on a `Result<T>`, and the predicate `Is.<Name>`. Rules with arguments take them first, for example `age.EnsureInRange(18, 120)`. Bounds are formatted with the invariant culture.

| Rule | Receiver | Passes when | Default message |
| --- | --- | --- | --- |
| `GreaterThan(bound)`, `GreaterThanOrEqual(bound)` | `T : IComparable<T>` | above `bound`, or at least `bound` | `{Name} must be greater than 5.`, `greater than or equal to 5.` |
| `LessThan(bound)`, `LessThanOrEqual(bound)` | `T : IComparable<T>` | below `bound`, or at most `bound` | `{Name} must be less than 5.`, `less than or equal to 5.` |
| `InRange(min, max)` | `T : IComparable<T>` | between `min` and `max`, both inclusive | `{Name} must be between 18 and 120.` |
| `Positive`, `Negative` | `T : INumberBase<T>` | above zero, or below zero | `{Name} must be positive.`, `negative.` |
| `NonPositive`, `NonNegative` | `T : INumberBase<T>` | zero or below, or zero or above | `{Name} must not be positive.`, `not be negative.` |
| `Zero`, `NonZero` | `T : INumberBase<T>` | is zero, or is not zero | `{Name} must be zero.`, `must not be zero.` |
| `Even`, `Odd` | `T : IBinaryInteger<T>` | is even, or is odd | `{Name} must be even.`, `must be odd.` |
| `DivisibleBy(divisor)` | `T : IBinaryInteger<T>` | has no remainder | `{Name} must be divisible by 6.` |

- The sign rules, `Zero` and `NonZero` all fail on `NaN`. Negative zero counts as zero.
- `InRange` with `min` above `max` and `DivisibleBy(0)` throw `ArgumentOutOfRangeException` when the rule is created, not when it runs.
- The comparison rules fail for a `null` value. A `Result<T>` never holds `null`.

```csharp
Result<int> age = customer.Age.EnsureInRange(18, 120);
// error: ValidationError "Age must be between 18 and 120.", MemberName = "Age"
```

## General rules

These rules work on any `T`. On a `Result<T>` receiver the `Result<T>` form wins, so `Result<int>.Ok(1).EnsureEqualTo(1)` is a `Result<int>`.

| Rule | Passes when | Default message |
| --- | --- | --- |
| `EqualTo(expected, comparer = null)` | equal to `expected` | `{Name} must be equal to 5.` |
| `NotEqualTo(other, comparer = null)` | not equal to `other` | `{Name} must not be equal to 5.` |
| `In(params T[] allowed)` | is one of the values | `{Name} must be one of the allowed values.` |
| `NotIn(params T[] disallowed)` | is none of the values | `{Name} must not be one of the disallowed values.` |
| `Null`, `NotNull` | is null, or is not null | `{Name} must not be null.` |
| `Some`, `None` | the `Option<T>` has a value, or has none | `{Name} must have a value.`, `must not have a value.` |

- `EnsureIn` and `EnsureNotIn` take the values as an array, so `message` and `name` stay available: `status.EnsureIn(["PAID", "SHIPPED"])`. `Is.In` and `Is.NotIn` take `params`. `Is.In<int>()` never passes and `Is.NotIn<int>()` always passes.
- `EnsureNotNull` exists on the value only, because a `Result<T>` never holds `null`. There is no `EnsureNull`, because a passing `null` cannot be returned as a `Result<T>`. `Is.Null` and `Is.NotNull` are plain predicates.
- `EnsureSome` and `EnsureNone` work on an `Option<T>` and on a `Result<Option<T>>`. They return a `Result<Option<T>>`, so the option is kept.

```csharp
Result<string> status = order.Status.EnsureIn(["PAID", "SHIPPED"]);
// error: ValidationError "Status must be one of the allowed values."

Result<Option<int>> stock = Result<Option<int>>.Ok(Option.None).EnsureSome(name: "Stock");
// error: ValidationError "Stock must have a value."
```

## Collection rules

Every row has both forms and the predicate `Is.<Name>`. The receiver keeps its concrete type: `List<int>`, `int[]` or `Result<HashSet<string>>` come back as the same type. A `string` still uses the string rules of the same name.

| Rule | Passes when | Default message |
| --- | --- | --- |
| `NotEmpty`, `Empty` | has an item, or has none (`null` counts as empty for `Is.Empty`) | `{Name} must not be empty.`, `must be empty.` |
| `Unique`, `Unique(comparer)` | no two items are equal | `{Name} must contain unique items.` |
| `HasCount(n)`, `HasMinCount(n)`, `HasMaxCount(n)` | exactly, at least, or at most `n` items | `{Name} must contain exactly 2 items.`, `at least 2 items.`, `at most 2 items.` |
| `ContainsItem(item)`, `NotContainsItem(item)` | has, or does not have, the item | `{Name} must contain 2.`, `must not contain 2.` |
| `AllMatch(predicate)` | every item matches, an empty collection passes | `{Name} must have only items that match the condition.` |
| `AnyMatch(predicate)` | at least one item matches | `{Name} must have at least one item that matches the condition.` |
| `NoneMatch(predicate)` | no item matches, an empty collection passes | `{Name} must have no item that matches the condition.` |

- A rule enumerates the sequence once, and uses `ICollection.Count` when it can. The collection is returned as is, so a lazy sequence is enumerated again when you use it.
- The item rules `AllMatch`, `AnyMatch` and `NoneMatch` need a typed lambda parameter, because the item type cannot be inferred from the collection type: `lines.EnsureAnyMatch((OrderLine line) => line.Quantity > 0)`. The `Is` factories take the item type as an argument: `Is.AllMatch<int>(x => x > 0)`.
- `Ensure` without a comparer compares items with `Equals`. `Is.Unique` uses the default equality comparer of the item type.
- `Is.ContainsItem` has this name so that `Is.Contains("x")` keeps meaning the string rule.

```csharp
Result<List<OrderLine>> lines = order.Lines.EnsureNotEmpty().EnsureHasMaxCount(50);
// error: ValidationError "Lines must not be empty."
```

## Date, time and other value rules

Every row has both forms, `Ensure<Name>` on a value and on a `Result<T>`, and the predicate `Is.<Name>`.

**Dates.** The date rules never read the system clock on their own. Pass a `TimeProvider` or an explicit instant, so tests can use a fixed clock. The provider is read each time the rule runs, not when it is created.

| Rule | Receiver | Passes when | Default message |
| --- | --- | --- | --- |
| `InPast`, `InPastOrPresent` | `DateTimeOffset` | before `now`, or up to and including `now` | `{Name} must be in the past.`, `in the past or present.` |
| `InFuture`, `InFutureOrPresent` | `DateTimeOffset` | after `now`, or from `now` on | `{Name} must be in the future.`, `in the future or present.` |
| `InPast(now)`, `InPastOrPresent(now)`, `InFuture(now)`, `InFutureOrPresent(now)` | `DateTime` | the same, compared with the given `DateTime` | the same messages |
| `Positive`, `Negative` | `TimeSpan` | above zero, or below zero | `{Name} must be positive.`, `negative.` |
| `NonPositive`, `NonNegative` | `TimeSpan` | zero or below, or zero or above | `{Name} must not be positive.`, `not be negative.` |
| `Zero`, `NonZero` | `TimeSpan` | is zero, or is not zero | `{Name} must be zero.`, `must not be zero.` |

- A `DateTimeOffset` takes either a `TimeProvider` (`placedAt.EnsureInPast(timeProvider)`) or a `DateTimeOffset` (`placedAt.EnsureInPast(now)`). A `DateTime` takes a `DateTime`. There is no overload without a clock.
- `Is.InPast(timeProvider)` returns a `Func<DateTimeOffset, bool>` that reads `GetUtcNow()` when it is called. A `null` provider throws `ArgumentNullException` when the rule is created.
- `DateTime` values are compared as they are. Convert them to the same `DateTimeKind` first.
- The `TimeSpan` rules are overloads of the number rules. They win for a `TimeSpan` because it is not an `INumberBase<T>`.

**Characters.** Receiver `char`.

| Rule | Passes when | Default message |
| --- | --- | --- |
| `Digit` | decimal digit | `{Name} must be a digit.` |
| `Letter` | letter | `{Name} must be a letter.` |
| `LetterOrDigit` | letter or decimal digit | `{Name} must be a letter or a digit.` |
| `Lower`, `Upper` | lowercase, or uppercase letter | `{Name} must be lowercase.`, `uppercase.` |
| `Punctuation` | punctuation | `{Name} must be punctuation.` |
| `Special` | not a letter, digit or white space | `{Name} must be a special character.` |
| `Symbol` | symbol | `{Name} must be a symbol.` |
| `WhiteSpace` | white space | `{Name} must be white space.` |

`Is.WhiteSpace` is also the string rule. The target type of the method group picks the overload.

**Guid, bool and enum.**

| Rule | Receiver | Passes when | Default message |
| --- | --- | --- | --- |
| `NotEmpty`, `Empty` | `Guid` | is not, or is, `Guid.Empty` | `{Name} must not be empty.`, `must be empty.` |
| `True`, `False` | `bool` | is true, or is false | `{Name} must be true.`, `must be false.` |
| `Defined` | `TEnum : struct, Enum` | is a named member (`Enum.IsDefined`) | `{Name} must be a defined value.` |

`Defined` rejects combined `[Flags]` values such as `Channels.Web | Channels.Store`, unless the enum names that combination.

```csharp
Result<DateTimeOffset> placedAt = order.PlacedAt.EnsureInPastOrPresent(timeProvider);
// error: ValidationError "PlacedAt must be in the past or present."

Result<Guid> id = order.Id.EnsureNotEmpty();
// error: ValidationError "Id must not be empty."
```

## DataAnnotations

`ValidateAnnotations()` checks the value of a `Result<T>` with its `System.ComponentModel.DataAnnotations` attributes and `IValidatableObject`, as `Validator.TryValidateObject` does with every property. It needs no package:

```csharp
using System.ComponentModel.DataAnnotations;

public class SignUp
{
    [Required, EmailAddress]
    public string? Email { get; init; }

    [Range(18, 120)]
    public int Age { get; init; }
}

Result<SignUp> validated = Result.From(form).ValidateAnnotations();
// error: ValidationErrors with one ValidationError for Email and one for Age
```

- **The same error as `Validate`.** Each failure becomes a `ValidationError` named after its member, all of them in one `ValidationErrors`, so the [HTTP 400 mapping](#http-400-with-an-errors-list) applies unchanged.
- **One error per member.** A failure that names several members, such as a date range from `IValidatableObject`, gives one `ValidationError` for each. A failure without a member has no `MemberName`.
- **Nested objects are not checked.** DataAnnotations validates the properties of the value itself, not the objects they hold.
- **An attribute that throws gives the exception as the error.** Cancellation propagates.
- **Not trimming-safe.** The attributes are read through reflection, so the method is marked `[RequiresUnreferencedCode]`.
- `Task<Result<T>>` has the same method.

## CombineAll for many inputs

`Validate` checks one value against many rules. `CombineAll` checks many results. It gives all values when every result is ok, or every error: a `ValidationErrors` when all of them are validation failures, otherwise an `AggregateException`.

```csharp
Result<IReadOnlyList<OrderLine>> lines = dto.Lines
    .Select(ParseLine)          // Result<OrderLine> for each
    .CombineAll();
// ok with all lines, or a ValidationErrors with an error per bad line
```

Nested `ValidationErrors` and `AggregateException`s are flattened here as well.

## Partition for reporting

`Partition()` splits the results into `(Values, Errors)` in one pass and never fails. Use it when you want to keep the good items and report the bad ones.

```csharp
var (lines, errors) = dto.Lines.Select(ParseLine).Partition();

if (errors.Count > 0)
    logger.LogWarning("{Count} lines were skipped: {Messages}", errors.Count, errors.Select(e => e.Message));

await orders.SaveAsync(lines);
```

## A realistic command

A handler that parses a request, validates it as a whole, and only then runs the business logic:

```csharp
public record PlaceOrder(Guid CustomerId, string Email, IReadOnlyList<OrderLine> Lines);
public record OrderLine(string Sku, int Quantity);

static Result<PlaceOrder> ValidateCommand(PlaceOrder command) =>
    Result<PlaceOrder>.Ok(command).Validate(
        c => c.CustomerId != Guid.Empty ? Result.Ok : new ValidationError("Customer is required."),
        c => c.Email.Contains('@') ? Result.Ok : new ValidationError("Email is invalid."),
        c => c.Lines.Count > 0 ? Result.Ok : new ValidationError("The order needs at least one line."),
        c => c.Lines.All(l => l.Quantity > 0) ? Result.Ok : new ValidationError("Every quantity must be positive."));

static Task<Result<Receipt>> HandleAsync(PlaceOrder command) =>
    ValidateCommand(command)
        .ChainAwait(valid => PlaceOrderAsync(valid));
```

If the command has an invalid email and no lines, `HandleAsync` returns a `ValidationErrors` holding two `ValidationError`s. `PlaceOrderAsync` does not run.

## HTTP 400 with an errors list

With the `Fluent.Unions.AspNetCore` package, `ValidationError` and `ValidationErrors` map to status 400 by default. The response is a `ValidationProblemDetails` with the messages grouped by member. Messages without a member use the key `""`.

```csharp
app.MapPost("/orders", (PlaceOrder command) => HandleAsync(command).ToHttpResult());
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": [ "Email must be a valid email address." ],
    "Lines": [ "Lines must not be empty." ]
  },
  "traceId": "0HNQ5NHDL69CD"
}
```

Map `ValidationErrors` to another status, such as 422, with `ResultHttpOptions`; the body stays a validation problem. See [ASP.NET Core](../aspnetcore/error-mapping.md) for the mapping options.

## Comparison with Combine

| Operator | Input | On failure |
| --- | --- | --- |
| `Validate(rules)` | one `Result<T>` | `ValidationErrors` (or `AggregateException`) with every failed rule |
| `CombineAll()` | many results | `ValidationErrors` (or `AggregateException`) with every error |
| `Combine()` | many results | the first error, as is |
| `Chain(next)` | one result | the first error, as is, later steps skipped |

`Combine` is the right choice when the first error is enough, for example when loading dependencies that cannot work without each other. It returns the original exception, not a collection. `Validate` and `CombineAll` are for reporting.

## Pitfalls

- A validation failure from `Validate` is a `ValidationErrors` even for one rule. Read `Errors` or `ToDictionary()`. A single `Ensure(predicate, message)` gives one `ValidationError`.
- Rules run on the same value, in order, without short circuit. Do not write a rule that depends on an earlier rule having passed. Guard against `null` inside the rule, or validate in two steps.
- A rule that throws is reported, not rethrown.
- `Is` clashes with `NUnit.Framework.Is` in a test file. Add `using Is = Fluent.Unions.Validation.Is;` and write NUnit constraints as `NUnit.Framework.Is.EqualTo(...)`.
- Rules are synchronous. For asynchronous checks, use `EnsureAwait`, `ChainAwait`, or `Task.WhenAll` with `CombineAll`.

## See also

- [Option](../option/README.md)
- [Async: concurrency](../async/concurrency-and-resilience.md#collecting-all-errors)
- [ASP.NET Core](../aspnetcore/README.md)

---

Back to index: [Docs index](../README.md) · Next: [Serialization](../serialization/README.md)
