# Mediator and MediatR

The package `Fluent.Unions.Mediator` (namespace `Fluent.Unions.Mediator`) adds pipeline behaviors to the source-generated [Mediator](https://github.com/martinothamar/Mediator) library, and `Fluent.Unions.MediatR` (namespace `Fluent.Unions.MediatR`) adds the same behaviors to [MediatR](https://github.com/LuckyPennySoftware/MediatR). A request whose response is a `Result<T>` then fails with an error result instead of throwing.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): register the behaviors and send a request.
- [Which package](#which-package): the libraries and their licenses.
- [The behaviors](#the-behaviors): what each one does.
- [Custom result types](#custom-result-types): which response types are covered.
- [Using FluentValidation](#using-fluentvalidation): a behavior built on `Fluent.Unions.FluentValidation`.
- [Rules](#rules): order, cancellation and limits.

## Quick example

```csharp
using Fluent.Unions.Mediator;

services.AddMediator(options =>
{
    options.PipelineBehaviors = [typeof(ExceptionToResultBehavior<,>), typeof(ValidationToResultBehavior<,>)];
});

public sealed record CreateUser([property: Required] string Name, [property: Range(1, 120)] int Age)
    : IRequest<Result<UserId>>;

Result<UserId> result = await mediator.Send(new CreateUser("", 0));
// error: ValidationErrors for "Name" and "Age"; the handler did not run
```

The same with MediatR:

```csharp
using Fluent.Unions.MediatR;

services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<Program>();
    configuration.AddOpenBehavior(typeof(ExceptionToResultBehavior<,>));
    configuration.AddOpenBehavior(typeof(ValidationToResultBehavior<,>));
});
```

Mediator needs the `Mediator.SourceGenerator` package in the application project. The source generator also works with requests and handlers whose response is a C# 15 union such as `Result<T>`, your own result type, or a plain union.

## Which package

| Package | Library | License of the library |
| --- | --- | --- |
| `Fluent.Unions.Mediator` | `Mediator` 3.0 (`Mediator.Abstractions`) | MIT |
| `Fluent.Unions.MediatR` | `MediatR` 15 | RPL-1.5 or a commercial license from Lucky Penny Software |

Check the MediatR license against your project before you use that package. Mediator has no such restriction. The behaviors differ only in the signature that the host library defines.

## The behaviors

| Behavior | Does |
| --- | --- |
| `ExceptionToResultBehavior<TMessage, TResponse>` | returns an exception from the handler or a later behavior as the error |
| `ValidationToResultBehavior<TMessage, TResponse>` | validates the request, and returns `ValidationErrors` as the error when it is invalid |

MediatR names the first type parameter `TRequest`. Both classes are sealed and have no options.

`ValidationToResultBehavior` uses `System.ComponentModel.DataAnnotations`: the attributes on the request's properties and `IValidatableObject`. Each failed member becomes one `ValidationError` with its `MemberName`. A failure without a member has a `null` member name. The handler does not run for an invalid request. A request without rules passes. On a record, write `[property: Required]`, or the attribute lands on the constructor parameter and is ignored.

## Custom result types

`TResponse` must be a struct that implements `IErrorResult<TResponse, Exception>`. That is `Result`, `Result<T>` and any type that implements `IResult<TSelf, T, Exception>`. The behaviors turn an exception into `TResponse.FromError(exception)`.

A request with any other response is passed through untouched: both libraries skip a behavior whose type constraints the response does not meet. That includes `Result<T, TError>` with a data error, whose error type is not `Exception`: open generic behaviors cannot carry the extra `TError` argument. Return `Result<T>` from the pipeline and convert at the edge.

## Using FluentValidation

`ValidationToResultBehavior` needs no extra package. To run FluentValidation validators instead, use `ValidateToResultAsync` from [Fluent.Unions.FluentValidation](../fluent-validation/README.md) in a behavior of your own:

```csharp
public sealed class FluentValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : struct, IErrorResult<TResponse, Exception>
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var validator in validators)
        {
            var validated = await validator.ValidateToResultAsync(message, cancellationToken);

            if (validated.IsError)
                return TResponse.FromError(validated.GetError());
        }

        return await next(message, cancellationToken);
    }
}
```

Register it instead of `ValidationToResultBehavior<,>`. For MediatR the signature is `Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)`.

## Rules

- **Order matters.** The first behavior in the list is the outermost. Put `ExceptionToResultBehavior` first, so it also catches an exception from validation or from another behavior.
- **Cancellation is not an error.** `OperationCanceledException` is thrown as usual.
- **Only requests with a response.** A notification has no response to fail with, so the behaviors do not apply to it.
- **The error stays an `Exception`.** Map it to a status code at the edge, for example with the [ASP.NET Core](../aspnetcore/README.md) package.

---

Back to index: [Docs index](../README.md)
