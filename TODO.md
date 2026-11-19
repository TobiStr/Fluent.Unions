# TODO: integration packages

Packages that `Fluent.Unions` should ship extensions for, ranked by value. Download counts and licenses were checked on nuget.org on 2026-11-18. Recheck the license before starting a package, because several popular libraries switched to commercial terms.

## Priority 1

- [ ] **AwesomeAssertions** (Apache-2.0, ~30M downloads). Optionally also **FluentAssertions** (~821M downloads, but **commercial** since v8: Xceed license, free only for non-commercial use).
  - Sketch: `result.Should().BeOk().Which.Should().Be(5)`, `option.Should().BeNone()`, `result.Should().BeError<NotFoundException>()`.
  - Extension point: a `ReferenceTypeAssertions` subclass plus a `Should()` extension.
  - Effort: M. FluentAssertions v8 changed the assertion API, so it would be a second package.
  - Demand: the FluentAssertions add-on of the rival FluentResults has about 2.6M downloads.
- [ ] **FluentValidation** (Apache-2.0, ~1.15B downloads).
  - Sketch: `validator.ValidateToResult(command)` returns a `Result<T>` with a `ValidationException` holding the failures; `RuleFor(x => x.Nickname).WhenSome(...)` for options.
  - Extension points: `ValidationResult`, `IValidator<T>`, `IRuleBuilder`.
  - Effort: S. Align with `Validate`, which collects every error.
  - Demand: the equivalent from Ardalis.Result has about 2.0M downloads.
- [ ] **Polly** (Polly.Core, BSD-3-Clause, ~787M downloads).
  - Sketch: `new PredicateBuilder<Result<T>>().HandleError<HttpRequestException>()`; `pipeline.ExecuteOutcomeAsync(...)` mapped to `Result<T>`.
  - Extension points: `ShouldHandle` and `PredicateBuilder<T>`.
  - Effort: S. Also covers Microsoft.Extensions.Http.Resilience (~200M), which builds on Polly.
- [ ] **Mediator** (source-generated, MIT, ~15M downloads) and **MediatR** (~524M downloads, **dual license**: RPL-1.5 or commercial since v15).
  - Sketch: pipeline behaviors `ExceptionToResultBehavior` and `ValidationToResultBehavior` that return a failed response instead of throwing.
  - Extension point: `IPipelineBehavior<TRequest, TResponse>`.
  - Effort: M. A generic failed `TResponse` needs a static-abstract factory on `Result` and `Result<T>`. Ship the MIT Mediator package first.
- [ ] **OpenAPI**: Microsoft.AspNetCore.OpenApi (MIT, ~295M), Swashbuckle (MIT, ~1.31B), NSwag (MIT, ~230M).
  - Sketch: describe `Option<T>` as nullable `T`, matching its JSON.
  - Extension points: `IOpenApiSchemaTransformer`; Swashbuckle `ISchemaFilter` / `MapType`; NJsonSchema `ITypeMapper`.
  - Effort: M. Without it, APIs that expose options publish a misleading schema. Could live in `Fluent.Unions.AspNetCore` instead of a separate package.

## Priority 2

- [ ] **Testing set** (one package or several small ones):
  - **Shouldly** (BSD-3-Clause, ~142M): `result.ShouldBeOk()`, `option.ShouldBeNone()`.
  - **TUnit** (MIT, ~8.7M, growing): `await Assert.That(result).IsOk()` through `Assertion<T>` and `IAssertionSource<T>`.
  - **NUnit** (MIT, ~746M): `Assert.That(result, Is.Ok)` through a custom `Constraint`.
  - **xUnit v3** (Apache-2.0, ~71M): `Assert.Ok(result)` through the partial `Assert` class (assert.source).
- [ ] **Verify** (~57M downloads; source MIT, but binaries carry an **Open Source Maintenance Fee** EULA for organisations above US$10k revenue).
  - Sketch: snapshots as `{ Ok: ... }`, `{ Error: { Type, Message } }`, `None`.
  - Extension point: `VerifierSettings.AddExtraSettings` / a write-only JSON converter.
- [ ] **Refit** (MIT, ~209M).
  - Sketch: `Task<Result<User>> GetUser(int id);` turns `ApiException` and network errors into errors.
  - Extension point: `IReturnTypeAdapter<TReturn, TResult>` plus `RefitSettings.ReturnTypeAdapters`.
  - Caveat: Refit's source generator ignores adapters from referenced assemblies, so the package must use the reflection route, which is not AOT-safe.
- [ ] **Dapper** (Apache-2.0, ~836M).
  - Sketch: `OptionHandlers.Register<int>()` maps `Option<int>` to and from `NULL`.
  - Extension point: `SqlMapper.TypeHandler<T>`, registered per closed type. Dapper.AOT works differently.
- [ ] **Serilog** (Apache-2.0, ~3.6B) and **OpenTelemetry** (Apache-2.0, ~812M).
  - Sketch: `.Destructure.WithFluentUnions()`; `activity.RecordResult(result)` sets the status and records the exception.
  - Extension points: `IDestructuringPolicy`; `System.Diagnostics.Activity`. The OpenTelemetry part needs no dependency and could live in the core package.

## Priority 3

- [ ] **HotChocolate** (MIT, ~71M): resolvers returning `Result<T>`, with middleware rethrowing the error so the mutation conventions build GraphQL error unions. Unverified: support for generic wrapper types; v16 is still in preview.
- [ ] **Newtonsoft.Json** (MIT, ~9.9B): an `OptionJsonConverter` for projects not on System.Text.Json.
- [ ] **MongoDB.Bson** (Apache-2.0, ~526M): `BsonSerializer.RegisterGenericSerializerDefinition(typeof(Option<>), typeof(OptionSerializer<>))`.
- [ ] **MessagePack** (MIT, ~370M), **Orleans** (MIT, ~16M), **gRPC / Protobuf**: a formatter and resolver, an Orleans surrogate, and `RpcException` mapped to `Result` with a server interceptor. Unknown: whether their code generators handle C# 15 unions.
- [ ] **Mapster** (MIT, ~91M) / **AutoMapper** (~1.2B, **dual license** since v17): map `Option<T>` to and from `T?`. Low value.

## Not planned

- **WolverineFx** (MIT, ~12M): large effort because of its code-generation conventions.
- **MassTransit** (**commercial** since v9, subscription only).
- **DataAnnotations**: part of .NET, so it fits better into the core package than into an extension.
- **Bogus**, **System.CommandLine**: little gain.
- **Npgsql**: already covered by EF Core and Dapper.
- **Entity Framework Core**: a value-converter package cannot work, because EF makes `Option<T>` columns `NOT NULL` and forbids marking them optional. See [docs/entity-framework-core](docs/entity-framework-core/README.md).
