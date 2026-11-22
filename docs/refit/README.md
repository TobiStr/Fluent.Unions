# Refit

The package `Fluent.Unions.Refit` (namespace `Fluent.Unions.Refit`) converts [Refit](https://github.com/reactiveui/refit) API responses to `Result`, `Result<T>` and `Result<Option<T>>`. It depends on `Refit` 17 (MIT) and works with the source-generated clients.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): declare the method and convert the call.
- [What you get back](#what-you-get-back): the result for each outcome.
- [Why not a Result return type](#why-not-a-result-return-type): why the methods return `IApiResponse<T>`.
- [Methods that return the content](#methods-that-return-the-content): `Result.TryAsync` for `Task<T>`.
- [With your own result type](#with-your-own-result-type): get your own type from the call.
- [Rules](#rules): what is an error and what is thrown.

## Quick example

```csharp
using Refit;
using Fluent.Unions.Refit;

public interface IUserApi
{
    [Get("/users/{id}")]
    Task<IApiResponse<User>> GetUser(int id, CancellationToken cancellationToken = default);

    [Delete("/users/{id}")]
    Task<IApiResponse> DeleteUser(int id, CancellationToken cancellationToken = default);
}

Result<User> user = await api.GetUser(7).ToResult();
Result<Option<User>> maybe = await api.GetUser(7).ToOptionalResult();
Result deleted = await api.DeleteUser(7).ToResult();
```

Create the client with Refit as usual. Refit 17 needs a source-generated `JsonSerializerContext` for the content, for example `RestService.ForGenerated<IUserApi>(client, RefitSettings.ForJsonContext(AppJsonContext.Default))`.

## What you get back

| Response | `ToResult<T>()` | `ToOptionalResult<T>()` |
| --- | --- | --- |
| 2xx with content | Ok with the content | Ok with `Some(content)` |
| 2xx without content | error `InvalidOperationException` | Ok with `None` |
| 404 | error `ApiException` | Ok with `None` |
| other HTTP error | error `ApiException` | error `ApiException` |
| content that cannot be read | error `ApiException` | error `ApiException` |
| network failure | error `ApiRequestException` holding the `HttpRequestException` | the same error |
| cancellation or timeout | the `OperationCanceledException` is thrown | the same exception is thrown |

`ToResult()` on a non-generic `IApiResponse` is Ok for a 2xx response and the same errors otherwise. The error is the exception Refit reports, so `ApiException` still gives the status code, the headers and the content.

| Member | On |
| --- | --- |
| `ToResult()` | `IApiResponse` |
| `ToResult<T>()` | `IApiResponse<T>` |
| `ToOptionalResult<T>()` | `IApiResponse<T>` |
| `ToResult()` | `Task<IApiResponse>` |
| `ToResult<T>()` | `Task<IApiResponse<T>>` and `Task<ApiResponse<T>>` |
| `ToOptionalResult<T>()` | `Task<IApiResponse<T>>` and `Task<ApiResponse<T>>` |

## Why not a Result return type

`Task<Result<User>> GetUser(int id)` cannot be a Refit method. Refit has an extension point, `IReturnTypeAdapter<TReturn, TResult>` with `RefitSettings.ReturnTypeAdapters`, but in Refit 17.0.0 it has three limits, which were checked against that version.

- It is used only by the reflection request builder in the `Refit.Reflection` package. The source generator ignores adapters from another assembly, and it would then read `Result<User>` as the JSON body and fail with an `ApiException`.
- It adapts only return types that are not awaitable. A `Task<Result<User>>` or `ValueTask<Result<User>>` method is never adapted.
- A non-awaitable `Result<User>` method would have to block the calling thread on the HTTP call.

The conversion methods avoid all three: no adapter, no reflection, no blocking, and the generated clients stay Native AOT safe.

## Methods that return the content

For a method declared as `Task<User>`, Refit throws `ApiException` and `ApiRequestException`. The core already captures them, and rethrows cancellation:

```csharp
Result<User> user = await Result.TryAsync(() => api.GetUser(7));
```

Use the response shapes above when you need the 404 as `None` or want the status code without an exception.

## With your own result type

`ToResult()` produces the default `Fluent.Unions.Result<T>`, not a type declared with `[FluentResult<TError>]` (see [your own result type](../errors/custom-error-types.md)). Declare the method as `Task<User>` and wrap the call in your own `Result.TryAsync(...)`, which turns the thrown `ApiException` or `ApiRequestException` into your error. Or convert the default result with `Match`:

```csharp
Shop.Result<User> mine = (await api.GetUser(7).ToResult())
    .Match(value => (Shop.Result<User>)value, error => DomainException.FromException(error));
```

## Rules

- **Declare `IApiResponse<T>`, `ApiResponse<T>` or `IApiResponse`.** Refit then reports failures in the response instead of throwing. The conversion methods do not run the call for you.
- **Cancellation is not an error.** A cancelled token throws `OperationCanceledException`. A timeout of the `HttpClient` also throws, because Refit reports it as a cancelled request.
- **Only 404 becomes `None`.** Every other failure stays an error in `ToOptionalResult`.
- **The response is not disposed.** The conversion reads the content and the error and leaves the response to you.
- **An error is Refit's exception.** A network failure is an `ApiRequestException`; the `HttpRequestException` is its `InnerException`.

---

Back to index: [Docs index](../README.md)
