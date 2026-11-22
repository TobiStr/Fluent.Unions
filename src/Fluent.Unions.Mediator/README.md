![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Mediator

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Mediator.svg)](https://www.nuget.org/packages/Fluent.Unions.Mediator) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Mediator.svg)](https://www.nuget.org/packages/Fluent.Unions.Mediator) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/mediator/)

Pipeline behaviors for the source-generated [Mediator](https://github.com/martinothamar/Mediator) library (MIT) that return a failed `Result<T>` instead of throwing. They build on Fluent.Unions and work with requests whose response is a C# 15 union.

## Installation

```
dotnet add package Fluent.Unions.Mediator
```

The package depends on `Mediator.Abstractions`. Your application also references `Mediator.SourceGenerator`, as Mediator requires.

## Usage

Register the behaviors as open generics. The first one in the list is the outermost.

```csharp
using Fluent.Unions.Mediator;

services.AddMediator(options =>
{
    options.PipelineBehaviors = [typeof(ExceptionToResultBehavior<,>), typeof(ValidationToResultBehavior<,>)];
});
```

A request whose response is a `Result<T>` now fails with an error instead of throwing.

```csharp
public sealed record CreateUser([property: Required] string Name, [property: Range(1, 120)] int Age)
    : IRequest<Result<UserId>>;

Result<UserId> result = await mediator.Send(new CreateUser("", 0));
// error: ValidationErrors for "Name" and "Age"; the handler did not run
```

- `ExceptionToResultBehavior<TMessage, TResponse>` returns an exception thrown by the handler or a later behavior as the error. `OperationCanceledException` still throws.
- `ValidationToResultBehavior<TMessage, TResponse>` validates the request with its `System.ComponentModel.DataAnnotations` attributes and `IValidatableObject`, and returns `ValidationErrors`. Put `[property: ...]` on record parameters.

### With your own result type

`TResponse` can be any result type: `Result`, `Result<T>` or your own declared result. The error is what the response type's `FromException` makes of the exception or of the `ValidationErrors`. An exception that the response type's capture policy excludes is not caught and propagates. A request with another response type is left alone.

A request that returns the value-less result needs it declared explicitly next to `Result<T>`, because the Mediator source generator cannot see types that another generator emits.

```csharp
[FluentResult<DomainException>]
public readonly partial struct Result<T>;

[FluentResult<DomainException>]
public readonly partial struct Result;
```

The behaviors use reflection, so they are not trimming or AOT friendly. See [your own result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

## Documentation

- [Mediator and MediatR documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/mediator/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
