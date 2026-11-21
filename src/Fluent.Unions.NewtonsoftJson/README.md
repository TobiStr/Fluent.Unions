![Fluent.Unions](https://raw.githubusercontent.com/TobiStr/Fluent.Unions/main/assets/icon.png)

# Fluent.Unions.NewtonsoftJson

[![NuGet](https://img.shields.io/nuget/v/Fluent.Unions.NewtonsoftJson.svg)](https://www.nuget.org/packages/Fluent.Unions.NewtonsoftJson) [![Downloads](https://img.shields.io/nuget/dt/Fluent.Unions.NewtonsoftJson.svg)](https://www.nuget.org/packages/Fluent.Unions.NewtonsoftJson) [![Publish](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml/badge.svg)](https://github.com/TobiStr/Fluent.Unions/actions/workflows/publish.yml) [![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-7e57c2)](https://tobistr.github.io/Fluent.Unions/newtonsoft-json/)

Newtonsoft.Json support for Fluent.Unions, for projects that do not use `System.Text.Json`. It does what the core does for `System.Text.Json`: `Option<T>` is written as its value or `null`, your own unions keep their case name, and results refuse to serialize. It depends on Newtonsoft.Json (MIT license).

> **Warning:** without `AddFluentUnions()`, Newtonsoft.Json writes a `Result` as a plain object with the full exception, including its class name, message and `StackTraceString`. This also happens in ASP.NET Core with `AddNewtonsoftJson()`. Register `AddFluentUnions()` on every `JsonSerializerSettings` and convert results before returning them.

## Installation

```
dotnet add package Fluent.Unions.NewtonsoftJson
```

## Usage

```csharp
using Fluent.Unions;
using Fluent.Unions.NewtonsoftJson;
using Newtonsoft.Json;

var settings = new JsonSerializerSettings().AddFluentUnions();

public record CustomerDto(string Name, Option<string> Nickname);

JsonConvert.SerializeObject(new CustomerDto("Ada", Option.None), settings); // {"Name":"Ada","Nickname":null}
JsonConvert.SerializeObject(new CustomerDto("Ada", "Countess"), settings);  // {"Name":"Ada","Nickname":"Countess"}
```

Your own unions are written with `$type` and `$value`. Apply the converter with an attribute:

```csharp
[JsonConverter(typeof(UnionJsonConverter))]
public union Shape(Circle, Square);
```

Every result type throws `NotSupportedException` when written or read: `Result`, `Result<T>` and your own types declared with [`[FluentResult<TError>]`](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/errors/custom-error-types.md). Convert them first. See the [documentation](https://github.com/TobiStr/Fluent.Unions/blob/main/docs/newtonsoft-json/README.md).
