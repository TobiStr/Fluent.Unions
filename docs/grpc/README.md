# gRPC

The package `Fluent.Unions.Grpc` (namespace `Fluent.Unions.Grpc`) connects `Result<T>` to [gRPC for .NET](https://github.com/grpc/grpc-dotnet). It depends on `Grpc.Core.Api` 2 (Apache-2.0). The client side turns an `RpcException` into an error. The server side maps the error of a result to a status code.

## In this section

This folder has one page. The topics are below.

- [Quick example](#quick-example): client and server.
- [Client](#client): `ToResult()` and what it returns.
- [Server](#server): why a service method does not return a result, and the interceptor.
- [Status code mapping](#status-code-mapping): defaults and `ResultGrpcOptions`.
- [Code generation](#code-generation): protoc with C# 15.
- [With your own result type](#with-your-own-result-type): get your own type from the call.
- [Rules](#rules): limits and pitfalls.

## Quick example

```csharp
using Grpc.Core;
using Fluent.Unions.Grpc;

// client
Result<UserReply> user = await client.GetUserAsync(new UserRequest { Id = 7 }).ToResult();

// server
builder.Services.AddGrpc(grpc => grpc.Interceptors.Add<ResultServerInterceptor>());

public override async Task<UserReply> GetUser(UserRequest request, ServerCallContext context) =>
    (await users.Find(request.Id)).Check().GetValue();
```

`users.Find` returns a `Result<UserReply>`. When it is an error, `Check()` throws it, and the interceptor turns it into an `RpcException`.

## Client

`ToResult()` is defined for `AsyncUnaryCall<TResponse>` and `AsyncClientStreamingCall<TRequest, TResponse>`. It awaits the response and disposes the call.

| Outcome of the call | Result |
| --- | --- |
| a response | `Result<TResponse>` holding it |
| the call fails with an `RpcException` | error holding a new `RpcException` with the same status code, an empty detail and no trailers; the original is its `Status.DebugException` |
| the status is `Cancelled` | `OperationCanceledException` is thrown, the `RpcException` is its inner exception |
| any other exception | thrown |

```csharp
var result = await client.GetUserAsync(request, deadline: DateTime.UtcNow.AddSeconds(2)).ToResult();

var message = result.Match(
    reply => $"Found {reply.Name}",
    error => error is RpcException { StatusCode: StatusCode.DeadlineExceeded } ? "Too slow." : "Failed.");
```

The error carries only the status code, so returning it to your own caller does not leak the message or trailers of the downstream service. Log or inspect the original through `((RpcException)error.Status.DebugException!)`. A deadline gives `DeadlineExceeded`, which is an error. Only `Cancelled` is thrown, because it means the caller or the server cancelled the call.

For a call with a streaming response, read `call.ResponseStream` as usual: an `RpcException` comes out of `MoveNext`. `ToResult()` does not cover it.

## Server

A gRPC service method returns a protobuf message. The generated base class has no place for a `Result<T>`, so the server cannot send a result as it is, and an interceptor cannot see an error that was returned. The way to a status code is an exception:

1. The service method turns a result into its value with `result.Check().GetValue()`. `Check()` throws the error with its stack trace.
2. `ResultServerInterceptor` catches the exception and throws an `RpcException` with the mapped status code.

The interceptor wraps unary, client-streaming, server-streaming and duplex-streaming methods. Once the client has cancelled the call, an `OperationCanceledException` becomes status `Cancelled`, so the client's `ToResult()` throws `OperationCanceledException` instead of returning an `Unknown` error. Any other exception after a cancellation is mapped like it would be before. An `RpcException` is left as it is, so a service can throw or return one with its own detail and trailers. The interceptor rethrows an exception without a mapping, so gRPC logs it and reports it as usual.

Register it once, and configure the options with the options pattern if you changed the mapping:

```csharp
builder.Services.AddGrpc(grpc => grpc.Interceptors.Add<ResultServerInterceptor>());
builder.Services.Configure<ResultGrpcOptions>(options => options.Map<OutOfStockException>(StatusCode.FailedPrecondition, exposeMessage: true));
```

To map one error by hand, call `error.ToRpcException(options)`. It returns an `RpcException` you can throw. An `RpcException` is returned as it is.

## Status code mapping

| Exception | Status code | Message sent |
| --- | --- | --- |
| `ValidationError`, `ValidationErrors` | `InvalidArgument` | yes, with the member names |
| `RpcException` | its own status code | its own detail and trailers; a downstream error captured by `ToResult()` has neither |
| `KeyNotFoundException` | `NotFound` | no |
| `UnauthorizedAccessException` | `PermissionDenied` | no |
| `TimeoutException` | `DeadlineExceeded` | no |
| anything else | `Internal` | no |

`Map<TException>(statusCode, exposeMessage)` maps `TException` and its subclasses. The most specific mapping wins, and a mapping replaces a default. The `Internal` row applies to `ToRpcException`. The interceptor does not convert an unmapped exception, so gRPC reports it as `Unknown`, and sends the message only if you enabled `EnableDetailedErrors`.

Only the status code and a message are sent. A validation error is written as `Name: is required; Age: must be positive`, with the member name before each message that has one. Structured details such as `google.rpc.BadRequest` are not part of this package.

## Code generation

`Grpc.Tools` runs protoc and generates plain classes from the `.proto` files. It does not know about C# 15 unions and is not affected by them: the tests build a service and client from a `.proto` file with `net11.0` and run them in process.

## With your own result type

`ToResult()` produces the default `Fluent.Unions.Result<T>`, not a type declared with `[FluentResult<TError>]` (see [your own result type](../errors/custom-error-types.md)). Wrap the call in your own `Result.TryAsync(...)`, or convert with `Match`:

```csharp
Shop.Result<UserReply> mine = (await client.GetUserAsync(request).ToResult())
    .Match(value => (Shop.Result<UserReply>)value, error => DomainException.FromException(error));
```

## Rules

- A service method does not return a `Result<T>`. Unwrap it with `Check().GetValue()` and register the interceptor.
- `ToResult()` covers unary and client-streaming calls. Other exceptions than `RpcException` are thrown.
- A `Cancelled` call throws `OperationCanceledException`.
- The interceptor only maps exceptions that have a mapping. Add yours with `Map<TException>`.
- `ResultGrpcOptions` comes from `IOptions<ResultGrpcOptions>`, so use `services.Configure<ResultGrpcOptions>(...)`. A `ResultGrpcOptions` registered as a singleton is not used.
- There is no client interceptor, and no helper for streaming responses.

## See also

- [Result](../result/README.md)
- [ASP.NET Core](../aspnetcore/README.md): the same mapping idea for HTTP status codes.

---

Back to index: [Docs index](../README.md)
