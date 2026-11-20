![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.Serilog

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.Serilog.svg)](https://www.nuget.org/packages/Fluent.Unions.Serilog) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.Serilog.svg)](https://www.nuget.org/packages/Fluent.Unions.Serilog) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/serilog/)

Logs `Result`, `Result<T>`, `Result<T, TError>`, `Option<T>` and `Loadable<T>` as structured values instead of opaque objects. It builds on Fluent.Unions and Serilog.

## Installation

```
dotnet add package Fluent.Unions.Serilog
```

## Usage

Register the destructuring policy, then destructure with `@`.

```csharp
using Serilog;
using Fluent.Unions.Serilog;

Log.Logger = new LoggerConfiguration()
    .Destructure.WithFluentUnions()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Nickname {@Nickname}", Option.Some("Countess")); // Nickname "Countess"
Log.Information("Nickname {@Nickname}", (Option<string>)Option.None); // Nickname null
Log.Information("Order {@Order}", LoadOrder(id));
// Order { Id: 42 } for a value, Order { Error: { Type: "System.TimeoutException", Message: "Slow" } } for an error
```

A value is logged as itself and an error as `{ Error: { Type, Message } }`. The stack trace is never logged. Pass the exception to the log call to put it in the log event's exception slot: `Log.Error(result.GetError(), "Load failed")`.

## Documentation

- [Serilog documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/serilog/README.md)
- [Fluent.Unions on GitHub](https://github.com/TobiStr/Fluent.Unions)
