![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Polly

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Polly.svg)](https://www.nuget.org/packages/Fluent.Unions.Polly) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Polly.svg)](https://www.nuget.org/packages/Fluent.Unions.Polly) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/polly/)

Connects `Result<T>` to Polly resilience strategies, so a retry can react to the error inside a result. It builds on Fluent.Unions and adds no retry logic of its own.

## Installation

```
dotnet add package Fluent.Unions.Polly
```

## Usage

Retry on the error in a result. `HandleError` reads only the result state, and an empty result is never retried.

```csharp
using Polly;
using Polly.Retry;
using Fluent.Unions.Polly;

var pipeline = new ResiliencePipelineBuilder<Result<Payment>>()
    .AddRetry(new RetryStrategyOptions<Result<Payment>>
    {
        ShouldHandle = new PredicateBuilder<Result<Payment>>()
            .HandleError(error => error is HttpRequestException),
        MaxRetryAttempts = 3,
    })
    .Build();
```

Run an operation through the pipeline. A thrown exception or a cancelled token becomes an error instead of being thrown.

```csharp
Result<Payment> payment = await pipeline.ExecuteResultAsync(ct => ChargeAsync(order, ct), cancellationToken);
// an Ok payment after the last attempt, or the last HttpRequestException as the error
```

Use a custom result type that implements `IResult<TSelf, T, TError>` with explicit type arguments, because Polly's builders carry only the result type.

```csharp
ShouldHandle = new PredicateBuilder<CallResult<Payment>>()
    .HandleError<CallResult<Payment>, CallError>(error => error.Value is Transient),

CallResult<Payment> payment = await pipeline
    .ExecuteResultAsync<CallResult<Payment>, CallError>(ct => ChargeAsync(order, ct), cancellationToken);
```

Convert the outcome that Polly passes to callbacks such as `OnRetry`.

```csharp
OnRetry = args =>
{
    Result<Payment> attempt = args.Outcome.ToResult();
    return ValueTask.CompletedTask;
},
```

## Documentation

- [Polly documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/polly/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
