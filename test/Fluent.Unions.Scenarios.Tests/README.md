# Fluent.Unions.Scenarios.Tests

Realistic, end-to-end business flows written with the core package `Fluent.Unions` only. Each file has one scenario class, whose summary names the union benefit it shows, and NUnit tests for the happy path and the important failure paths. The tests are deterministic: no network, no real clock, delays of a few milliseconds at most.

Run them with `dotnet test test/Fluent.Unions.Scenarios.Tests -c Release`.

| Scenario | Flow | Shows |
| --- | --- | --- |
| `OrderImportScenario` | CSV order lines with per-line validation and a report | Collecting every validation error, `Partition`, `CombineAll`, `Result.Try`, `Catch`, `Do`, async streams |
| `BankTransferScenario` | Money transfer between accounts | Errors as data with `Result<T, TError>`, exhaustive `switch`, `Ensure`, typed `Catch`, `MapError`, `Finally`, `RecordResult` |
| `UserRegistrationScenario` | Sign-up with several outcomes | `ValidateAnnotations`, `Validate` and the rule catalogue, `EnsureAwait`, `TryAsync`, custom errors, one `switch` over every outcome |
| `SupplierPricesScenario` | Best price from several suppliers | Explicit async failure with `RetryAsync`, `Timeout`, `Catch` fallbacks, `WhenAll`, `Partition`, `FirstOrNone`, `ToResult` |
| `ConfigurationScenario` | Settings from a file and the environment | `Option` instead of `null`: `GetValueOrNone`, `Or`, `Where`, `Zip`, `Transpose`, `CombineAll` |
| `DocumentApprovalScenario` | Review workflow with an audit trail | State machine over unions, `Do` with two callbacks, `Chain`, `Is` and `As`, `TimeProvider` |
| `SearchPageScenario` | Search page view model | `Loadable<T>`: `Match`, `ToLoadable`, `ToOption`, cancellation that stays an exception |

`Support/ManualTimeProvider.cs` is the fake clock used by the scenarios that depend on time.
