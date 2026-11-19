![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.FluentValidation

Runs a FluentValidation validator and returns the outcome as a `Result<T>`, so an existing validator plugs into a result pipeline. It builds on Fluent.Unions.

## Installation

```
dotnet add package Fluent.Unions.FluentValidation
```

## Usage

Validate an instance. Every failure ends up in one `AggregateException` with one `ValidationException` per failure.

```csharp
using FluentValidation;
using Fluent.Unions.FluentValidation;

var validator = new PlaceOrderValidator();

Result<PlaceOrder> command = validator.ValidateToResult(new PlaceOrder(Guid.Empty, 0));
// error: AggregateException with two ValidationException
```

Validate a result inside a pipeline. An error or empty result is passed on and the validator is not called.

```csharp
Result<PlaceOrder> parsed = ParseCommand(request);
Result<PlaceOrder> checkedCommand = await parsed.ValidateAsync(validator, cancellationToken);
```

An endpoint that returns the error through Fluent.Unions.AspNetCore answers 400 with every message in `errors`.

Failures with `Severity.Warning` or `Severity.Info` also make the result an error.

## Documentation

- [FluentValidation documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/fluent-validation/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
