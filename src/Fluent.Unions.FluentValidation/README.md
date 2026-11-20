![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.FluentValidation

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.FluentValidation.svg)](https://www.nuget.org/packages/Fluent.Unions.FluentValidation) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.FluentValidation.svg)](https://www.nuget.org/packages/Fluent.Unions.FluentValidation) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/fluent-validation/)

Runs a FluentValidation validator and returns the outcome as a `Result<T>`, so an existing validator plugs into a result pipeline. It builds on Fluent.Unions.

## Installation

```
dotnet add package Fluent.Unions.FluentValidation
```

## Usage

Validate an instance. Every failure ends up in one `ValidationErrors` with one `ValidationError` per failure, named after its property. A validator that throws gives the exception as the error.

```csharp
using FluentValidation;
using Fluent.Unions.FluentValidation;

var validator = new PlaceOrderValidator();

Result<PlaceOrder> command = validator.ValidateToResult(new PlaceOrder(Guid.Empty, 0));
// error: ValidationErrors with two ValidationError (CustomerId, Quantity)
```

Validate a result inside a pipeline. An error is passed on and the validator is not called.

```csharp
Result<PlaceOrder> parsed = ParseCommand(request);
Result<PlaceOrder> checkedCommand = await parsed.ValidateAsync(validator, cancellationToken);
```

Validate a custom result type that implements `IResult<TSelf, T, TError>`. Without `map`, a failure goes through `FromException`. With `map`, an error type that is data gets every failure.

```csharp
CheckoutResult<PlaceOrder> validated = command.Validate(
    validator,
    failures => new InvalidOrder(failures.Select(f => f.ErrorMessage).ToList()));
```

Validate an `Option<T>` property only when it has a value. `None` is always valid.

```csharp
this.RuleForSome(p => p.Nickname).NotEmpty().MaximumLength(20);
```

An endpoint that returns the error through Fluent.Unions.AspNetCore answers 400 with every message in `errors`.

Failures with `Severity.Warning` or `Severity.Info` also make the result an error.

## Documentation

- [FluentValidation documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/fluent-validation/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
