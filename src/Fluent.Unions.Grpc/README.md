![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Grpc

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Grpc.svg)](https://www.nuget.org/packages/Fluent.Unions.Grpc) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Grpc.svg)](https://www.nuget.org/packages/Fluent.Unions.Grpc) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/grpc/)

Converts [gRPC](https://github.com/grpc/grpc-dotnet) client calls to `Result<T>`, so an `RpcException` becomes an error. On the server, a Grpc.Core interceptor turns the error of a result into an `RpcException` with a status code. It builds on Fluent.Unions and `Grpc.Core.Api` (Apache-2.0), so it works with Grpc.Net.Client and Grpc.AspNetCore.

## Installation

```
dotnet add package Fluent.Unions.Grpc
```

## Usage

Client: convert the call.

```csharp
using Fluent.Unions.Grpc;

Result<UserReply> user = await client.GetUserAsync(new UserRequest { Id = 7 }).ToResult();
// the reply, or an RpcException with the status code only as the error

if (user.TryGetError(out var error) && error is RpcException { StatusCode: StatusCode.NotFound })
    // handle it
```

Server: a response is a protobuf message, not a `Result`, so the service method unwraps the result with `Check()` and the interceptor maps the error.

```csharp
builder.Services.AddGrpc(grpc => grpc.Interceptors.Add<ResultServerInterceptor>());

public override async Task<UserReply> GetUser(UserRequest request, ServerCallContext context) =>
    (await users.Find(request.Id)).Check().GetValue();
```

`KeyNotFoundException` becomes `NotFound`, a `ValidationError` or `ValidationErrors` becomes `InvalidArgument` with its messages and member names. An `RpcException` is passed on as it is. Map your own errors with `ResultGrpcOptions`, configured with the options pattern:

```csharp
builder.Services.Configure<ResultGrpcOptions>(options => options.Map<OutOfStockException>(StatusCode.FailedPrecondition, exposeMessage: true));
```

`error.ToRpcException()` does the same mapping by hand.

### With your own result type

`ToResult()` produces the default `Fluent.Unions.Result<T>`. To get your own type, wrap the call in your own `Result.TryAsync(...)`, or convert with `Match`. See [your own result type](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md).

```csharp
Shop.Result<UserReply> mine = user.Match(value => (Shop.Result<UserReply>)value, error => DomainException.FromException(error));
```

## Documentation

- [gRPC documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/grpc/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)

## Rules

- **Unary and client-streaming calls.** `ToResult()` covers calls with a single response. For a streaming response, the `RpcException` comes out of `ResponseStream.MoveNext`.
- **Only `RpcException` is an error.** Other exceptions are thrown. The error keeps the status code but not the detail or trailers of the downstream call, so it can be returned without leaking them. The original is `Status.DebugException`.
- **Cancellation is not an error.** A call with the status `Cancelled` throws `OperationCanceledException`, with the `RpcException` as its `InnerException`.
- **Messages stay hidden.** A mapped exception's message reaches the client only with `exposeMessage`; validation errors always list theirs. An unmapped exception is rethrown by the interceptor, so gRPC reports it as usual.
