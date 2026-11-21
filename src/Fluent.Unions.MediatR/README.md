![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.MediatR

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.MediatR.svg)](https://www.nuget.org/packages/Fluent.Unions.MediatR) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.MediatR.svg)](https://www.nuget.org/packages/Fluent.Unions.MediatR) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/mediator/)

Pipeline behaviors for [MediatR](https://github.com/LuckyPennySoftware/MediatR) that return a failed `Result<T>` instead of throwing. They build on Fluent.Unions.

## License of MediatR

This package depends on `MediatR` 15, which is dual licensed: the Reciprocal Public License 1.5 (RPL-1.5) or a commercial license from Lucky Penny Software. Check that the license fits your project before you use it. Fluent.Unions.MediatR itself has the license of this repository. For a permissive alternative, see `Fluent.Unions.Mediator`.

## Installation

```
dotnet add package Fluent.Unions.MediatR
```

## Usage

Register the behaviors as open generics. The first one in the list is the outermost.

```csharp
using Fluent.Unions.MediatR;

services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<Program>();
    configuration.AddOpenBehavior(typeof(ExceptionToResultBehavior<,>));
    configuration.AddOpenBehavior(typeof(ValidationToResultBehavior<,>));
});
```

A request whose response is a `Result<T>` now fails with an error instead of throwing.

```csharp
public sealed record CreateUser([property: Required] string Name, [property: Range(1, 120)] int Age)
    : IRequest<Result<UserId>>;

Result<UserId> result = await sender.Send(new CreateUser("", 0));
// error: ValidationErrors for "Name" and "Age"; the handler did not run
```

- `ExceptionToResultBehavior<TRequest, TResponse>` returns an exception thrown by the handler or a later behavior as the error. `OperationCanceledException` still throws.
- `ValidationToResultBehavior<TRequest, TResponse>` validates the request with its `System.ComponentModel.DataAnnotations` attributes and `IValidatableObject`, and returns `ValidationErrors`. Put `[property: ...]` on record parameters.

### With your own result type

`TResponse` can be any result type: `Result`, `Result<T>`, `Result<T, TError>` or your own declared result. The error is what the response type's `FromException` makes of the exception or of the `ValidationErrors`. A request with another response type is left alone.

The behaviors use reflection, so they are not trimming or AOT friendly. See [your own result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

## Documentation

- [Mediator and MediatR documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/mediator/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
