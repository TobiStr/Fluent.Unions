![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Refit

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Refit.svg)](https://www.nuget.org/packages/Fluent.Unions.Refit) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Refit.svg)](https://www.nuget.org/packages/Fluent.Unions.Refit) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/refit/)

Converts Refit API responses to `Result`, `Result<T>` and `Result<Option<T>>`, so an HTTP error or a network failure becomes an error instead of an exception. It builds on Fluent.Unions and Refit (MIT), and works with the source-generated Refit clients, so it is trim and Native AOT safe.

## Installation

```
dotnet add package Fluent.Unions.Refit
```

## Usage

Declare the Refit method with `IApiResponse<T>`, which never throws for an HTTP error, and convert the call.

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
// the user, or an ApiException (HTTP error) or an ApiRequestException (network failure) as the error

Result<Option<User>> maybe = await api.GetUser(7).ToOptionalResult();
// none for a 404, the user for a 2xx, an error for anything else

Result deleted = await api.DeleteUser(7).ToResult();
```

Cancellation and timeouts are thrown, never turned into an error. `Task<ApiResponse<T>>` works the same way, and `ToResult()` and `ToOptionalResult()` convert a response you already hold.

### With your own result type

`ToResult()` and `ToOptionalResult()` produce the default `Fluent.Unions.Result<T>`. To get your own type, wrap the call in your own `Result.TryAsync(...)`, or convert with `Match`. See [your own result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

```csharp
Shop.Result<User> mine = user.Match(value => (Shop.Result<User>)value, error => DomainException.FromException(error));
```

## Documentation

- [Refit documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/refit/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
